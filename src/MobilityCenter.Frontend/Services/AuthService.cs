using System.Net.Http.Headers;
using System.Net.Http.Json;
using MobilityCenter.Frontend.Models;

namespace MobilityCenter.Frontend.Services;

public class AuthService(
    HttpClient http,
    LocalStorageService localStorage,
    JwtAuthStateProvider authStateProvider)
{
    public async Task<string?> LoginAsync(string email, string password)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/login", new { email, password });

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Credenciais inválidas.";
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            await PersistSession(result!);
            return null;
        }
        catch
        {
            return "Não foi possível conectar ao servidor.";
        }
    }

    public async Task<string?> RegisterAsync(string displayName, string email, string password)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/register", new
            {
                displayName,
                email,
                password,
                type = 0
            });

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao criar conta.";
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            await PersistSession(result!);
            return null;
        }
        catch
        {
            return "Não foi possível conectar ao servidor.";
        }
    }

    public async Task LogoutAsync()
    {
        await localStorage.RemoveItemAsync("authToken");
        await localStorage.RemoveItemAsync("userInfo");
        authStateProvider.NotifyStateChanged(null);
    }

    public Task<UserInfo?> GetUserInfoAsync() =>
        localStorage.GetItemAsync<UserInfo>("userInfo");

    public async Task<UserProfile?> GetPerfilAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<UserProfile>("api/usuarios/me");
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> UploadFotoAsync(MultipartFormDataContent content)
    {
        try
        {
            var response = await http.PostAsync("api/usuarios/me/foto", content);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<FotoResponse>();
            if (result?.Url is null) return null;

            var fullUrl = ResolveUrl(result.Url);

            var userInfo = await GetUserInfoAsync();
            if (userInfo is not null)
            {
                userInfo.FotoPerfilUrl = fullUrl;
                await localStorage.SetItemAsync("userInfo", userInfo);
            }

            return fullUrl;
        }
        catch
        {
            return null;
        }
    }

    public string? ResolveUrl(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return null;
        if (relativePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return relativePath;
        return new Uri(http.BaseAddress!, relativePath.TrimStart('/')).ToString();
    }

    private async Task PersistSession(AuthResponse result)
    {
        await localStorage.SetItemAsync("authToken", result.Token);
        await localStorage.SetItemAsync("userInfo", result.Usuario);
        authStateProvider.NotifyStateChanged(result.Token);
    }

    private record ApiError(string Message);
    private record FotoResponse(string Url);
}
