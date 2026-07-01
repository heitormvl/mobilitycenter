using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Paraki.RazorLib.Interfaces;

namespace Paraki.Maui.Services;

/// <summary>
/// Login com Google no MAUI usando <see cref="WebAuthenticator"/> (Chrome Custom
/// Tabs no Android / ASWebAuthenticationSession no iOS) + Authorization Code Flow
/// com PKCE. Retorna o <c>id_token</c> do Google, aceito pela política do Google
/// para apps nativos (ao contrário do OAuth em WebView).
///
/// Configuração necessária (Google Cloud Console):
///  1. Criar um OAuth Client "Android" (package <c>br.com.paraki</c> + SHA-1 da
///     keystore) e/ou "iOS", e preencher Google:AndroidClientId / Google:iOSClientId
///     no appsettings.json.
///  2. Registrar o redirect scheme <c>br.com.paraki:/oauthredirect</c>:
///     - Android: já coberto por <c>WebAuthenticationCallbackActivity</c>.
///     - iOS: CFBundleURLSchemes no Info.plist.
/// </summary>
public class MauiGoogleAuthService(IConfiguration config, IHttpClientFactory httpFactory)
    : IGoogleAuthService
{
    private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    public async Task<string?> SignInAsync()
    {
#if IOS
        var clientId = config["Google:iOSClientId"];
#else
        var clientId = config["Google:AndroidClientId"];
#endif
        if (string.IsNullOrEmpty(clientId))
            clientId = config["Google:ClientId"];
        if (string.IsNullOrEmpty(clientId))
            return null;

        var scheme = config["Google:RedirectScheme"] ?? "br.com.paraki";
        var redirectUri = $"{scheme}:/oauthredirect";

        // PKCE (RFC 7636)
        var codeVerifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var codeChallenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

        var authUrl =
            $"{AuthEndpoint}?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&response_type=code" +
            "&scope=openid%20email%20profile" +
            $"&code_challenge={codeChallenge}&code_challenge_method=S256";

        WebAuthenticatorResult authResult;
        try
        {
            authResult = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(authUrl), new Uri(redirectUri));
        }
        catch (TaskCanceledException)
        {
            return null; // usuário cancelou o fluxo
        }

        if (!authResult.Properties.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
            return null;

        // Troca o authorization code pelo id_token
        var http = httpFactory.CreateClient();
        var response = await http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["code"] = code,
                ["code_verifier"] = codeVerifier,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }));

        if (!response.IsSuccessStatusCode)
            return null;

        var token = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        return token?.IdToken;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; init; }
    }
}
