using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Paraki.RazorLib.Models;

using Paraki.RazorLib.Interfaces;

namespace Paraki.RazorLib.Services;

public class JwtAuthStateProvider(
    ILocalStorageService localStorage,
    IHttpClientFactory httpFactory) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsync("authToken");
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = ParseClaimsFromJwt(token);

        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim is not null)
        {
            var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value));
            if (exp < DateTimeOffset.UtcNow)
            {
                var refreshed = await TryRefreshAsync();
                if (refreshed is null)
                {
                    await localStorage.RemoveItemAsync("authToken");
                    await localStorage.RemoveItemAsync("refreshToken");
                    return Anonymous;
                }
                token = refreshed;
                claims = ParseClaimsFromJwt(token);
            }
        }

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
    }

    private async Task<string?> TryRefreshAsync()
    {
        try
        {
            var refreshToken = await localStorage.GetItemAsync("refreshToken");
            if (string.IsNullOrEmpty(refreshToken)) return null;

            var http = httpFactory.CreateClient("auth-api");
            var response = await http.PostAsJsonAsync("api/auth/refresh", new { token = refreshToken });
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<AuthRefreshResult>();
            if (result is null) return null;

            await localStorage.SetItemAsync("authToken", result.Token);
            if (!string.IsNullOrEmpty(result.RefreshToken))
                await localStorage.SetItemAsync("refreshToken", result.RefreshToken);
            if (result.Usuario is not null)
                await localStorage.SetItemAsync("userInfo", result.Usuario);

            return result.Token;
        }
        catch
        {
            return null;
        }
    }

    private record AuthRefreshResult(string Token, string? RefreshToken, UserInfo? Usuario);

    public void NotifyStateChanged(string? token)
    {
        AuthenticationState state;

        if (string.IsNullOrWhiteSpace(token))
        {
            state = Anonymous;
        }
        else
        {
            var claims = ParseClaimsFromJwt(token);
            state = new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
        }

        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];

        // Pad base64url to base64
        var rem = payload.Length % 4;
        if (rem == 2) payload += "==";
        else if (rem == 3) payload += "=";
        payload = payload.Replace('-', '+').Replace('_', '/');

        var bytes = Convert.FromBase64String(payload);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(bytes)!;
        return dict.Select(kv => new Claim(kv.Key, kv.Value.ToString()));
    }
}
