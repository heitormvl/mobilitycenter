using System.Net.Http.Headers;
using System.Net.Http.Json;
using Paraki.RazorLib.Models;

using Paraki.RazorLib.Interfaces;

namespace Paraki.RazorLib.Services;

public class AuthService(
    HttpClient http,
    ILocalStorageService localStorage,
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
                var msg = err?.Message ?? "Credenciais inválidas.";
                // Prefixo especial para o frontend detectar email não confirmado
                if ((int)response.StatusCode == 403)
                    return $"EMAIL_NAO_CONFIRMADO:{email}";
                return msg;
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

            return null;
        }
        catch
        {
            return "Não foi possível conectar ao servidor.";
        }
    }

    public async Task<string?> ConfirmarEmailAsync(string userId, string token)
    {
        try
        {
            var response = await http.GetAsync($"api/auth/confirmar-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Link inválido ou expirado.";
            }
            return null;
        }
        catch
        {
            return "Não foi possível conectar ao servidor.";
        }
    }

    public async Task<string?> ReenviarConfirmacaoAsync(string email)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/reenviar-confirmacao", new { email });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao reenviar e-mail.";
            }
            return null;
        }
        catch
        {
            return "Não foi possível conectar ao servidor.";
        }
    }

    public async Task<string?> EsquecerSenhaAsync(string email)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/esqueci-senha", new { email });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao enviar e-mail.";
            }
            return null;
        }
        catch
        {
            return "Não foi possível conectar ao servidor.";
        }
    }

    public async Task<string?> RedefinirSenhaAsync(string email, string token, string novaSenha)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/redefinir-senha", new { email, token, novaSenha });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Link inválido ou expirado.";
            }
            return null;
        }
        catch
        {
            return "Não foi possível conectar ao servidor.";
        }
    }

    public async Task<string?> LoginWithGoogleAsync(string idToken)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/google", new { idToken });

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao entrar com Google.";
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
        await localStorage.RemoveItemAsync("refreshToken");
        await localStorage.RemoveItemAsync("userInfo");
        authStateProvider.NotifyStateChanged(null);
    }

    public async Task<string?> ExcluirContaAsync()
    {
        try
        {
            var response = await http.DeleteAsync("api/usuarios/me");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao excluir a conta.";
            }

            await LogoutAsync();
            return null;
        }
        catch
        {
            return "Não foi possível conectar ao servidor.";
        }
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

    public async Task<string?> AtualizarPerfilAsync(string displayName, string email)
    {
        try
        {
            var response = await http.PutAsJsonAsync("api/usuarios/me", new { displayName, email });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao salvar as alterações.";
            }

            var atualizado = await response.Content.ReadFromJsonAsync<UserInfo>();
            var userInfo = await GetUserInfoAsync();
            if (userInfo is not null)
            {
                userInfo.DisplayName = atualizado?.DisplayName ?? displayName;
                userInfo.Email = atualizado?.Email ?? email;
                await localStorage.SetItemAsync("userInfo", userInfo);
            }

            return null;
        }
        catch
        {
            return "Não foi possível conectar ao servidor.";
        }
    }

    public async Task<string?> AlterarSenhaAsync(string senhaAtual, string novaSenha)
    {
        try
        {
            var response = await http.PutAsJsonAsync("api/usuarios/me/senha", new { senhaAtual, novaSenha });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao alterar a senha.";
            }

            return null;
        }
        catch
        {
            return "Não foi possível conectar ao servidor.";
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
        if (!string.IsNullOrEmpty(result.RefreshToken))
            await localStorage.SetItemAsync("refreshToken", result.RefreshToken);
        authStateProvider.NotifyStateChanged(result.Token);
    }

    private record ApiError(string Message);
    private record FotoResponse(string Url);
}
