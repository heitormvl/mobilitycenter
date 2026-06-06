using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace MobilityCenter.Frontend.Services;

public class JwtAuthStateProvider(LocalStorageService localStorage) : AuthenticationStateProvider
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
                await localStorage.RemoveItemAsync("authToken");
                return Anonymous;
            }
        }

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
    }

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
