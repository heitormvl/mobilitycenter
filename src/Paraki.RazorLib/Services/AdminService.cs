using System.Net.Http.Json;

namespace Paraki.RazorLib.Services;

public class AdminService(HttpClient http)
{
    public async Task<PendenciasContagemModel?> GetContagemAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<PendenciasContagemModel>("api/admin/pendencias/contagem");
        }
        catch
        {
            return null;
        }
    }

    public async Task<UsuariosPageModel?> GetUsuariosAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            return await http.GetFromJsonAsync<UsuariosPageModel>($"api/admin/usuarios?page={page}&pageSize={pageSize}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> SetRoleAsync(string userId, int role)
    {
        try
        {
            var response = await http.PutAsJsonAsync($"api/admin/usuarios/{userId}/role", new { role });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao alterar role.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public async Task<string?> SetTierAsync(string userId, int? tier)
    {
        try
        {
            var response = await http.PutAsJsonAsync($"api/admin/usuarios/{userId}/tier", new { tier });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao alterar tier.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    private record ApiError(string Message);
}

public class PendenciasContagemModel
{
    public int Sugestoes { get; set; }
    public int Bicis { get; set; }
    public int Total => Sugestoes + Bicis;
}

public class UsuariosPageModel
{
    public UsuarioAdminModel[] Usuarios { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class UsuarioAdminModel
{
    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public int Tipo { get; set; }
    public int TierEfetivo { get; set; }
    public int? TierOverride { get; set; }
    public int PontosAprovados { get; set; }
    public DateTime CriadoEm { get; set; }
}
