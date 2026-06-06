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

    private async Task PersistSession(AuthResponse result)
    {
        await localStorage.SetItemAsync("authToken", result.Token);
        await localStorage.SetItemAsync("userInfo", result.Usuario);
        authStateProvider.NotifyStateChanged(result.Token);
    }

    private record ApiError(string Message);
}
