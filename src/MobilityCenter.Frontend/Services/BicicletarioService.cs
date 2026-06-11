using System.Globalization;
using System.Net.Http.Json;

namespace MobilityCenter.Frontend.Services;

public class BicicletarioService(HttpClient http)
{
    public async Task<BicicletarioDto[]?> GetAllAsync(double? lat = null, double? lng = null)
    {
        try
        {
            var url = "api/bicicletarios?take=100";
            if (lat.HasValue && lng.HasValue)
                url += $"&latitude={lat.Value.ToString(CultureInfo.InvariantCulture)}&longitude={lng.Value.ToString(CultureInfo.InvariantCulture)}&radiusKm=50";
            return await http.GetFromJsonAsync<BicicletarioDto[]>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<BicicletarioDetalheModel?> GetByIdAsync(string id)
    {
        try
        {
            return await http.GetFromJsonAsync<BicicletarioDetalheModel>($"api/bicicletarios/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<(string? Error, string? Id)> CreateAsync(CreateBicicletarioRequest req)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/bicicletarios", req);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return (err?.Message ?? "Erro ao publicar o bicicletário.", null);
            }
            var result = await response.Content.ReadFromJsonAsync<CreatedResponse>();
            return (null, result?.Id);
        }
        catch
        {
            return ("Erro de conexão. Verifique sua internet e tente novamente.", null);
        }
    }

    public async Task<string?> UpdateAsync(string id, AdminUpdateRequest req)
    {
        try
        {
            var response = await http.PutAsJsonAsync($"api/bicicletarios/{id}", req);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao atualizar.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public async Task<string?> SoftDeleteAsync(string id)
    {
        try
        {
            var response = await http.DeleteAsync($"api/bicicletarios/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao ocultar.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public async Task<string?> HardDeleteAsync(string id)
    {
        try
        {
            var response = await http.DeleteAsync($"api/bicicletarios/{id}/permanente");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao excluir.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public async Task<string?> UploadFotoAsync(string bicicletarioId, MultipartFormDataContent content)
    {
        try
        {
            var response = await http.PostAsync($"api/fotos?bicicletarioId={bicicletarioId}", content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao enviar foto.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public async Task<bool> FotoExisteAsync(string bicicletarioId)
    {
        try
        {
            var response = await http.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"api/fotos/bicicletario/{bicicletarioId}"));
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public string GetFotoBicicletarioUrl(string bicicletarioId, long? cacheBust = null) =>
        http.BaseAddress is { } b
            ? $"{b}api/fotos/bicicletario/{bicicletarioId}{(cacheBust.HasValue ? $"?t={cacheBust}" : "")}"
            : $"api/fotos/bicicletario/{bicicletarioId}";

    private record ApiError(string Message);
    private record CreatedResponse(string Id);
}

// Matches BicicletarioResumoDto (Portuguese names, camelCase from API)
public class BicicletarioDto
{
    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double NotaMedia { get; set; }
    public int TotalAvaliacoes { get; set; }
    public int VeiculosSuportados { get; set; }

    public bool TemTomada { get; set; }
    public bool TemCalibrador { get; set; }
    public bool TemVestiario { get; set; }
    public bool TemArmario { get; set; }
    public bool TemEspacoManutencao { get; set; }
    public bool TemCadeado { get; set; }

    public bool AcessoLivre { get; set; }
    public bool AcessoPago { get; set; }
    public bool AcessoCadastro { get; set; }
    public bool AcessoMensal { get; set; }
}

// Matches BicicletarioDetalheDto (full detail with reviews)
public class BicicletarioDetalheModel
{
    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool TemTomada { get; set; }
    public bool TemCalibrador { get; set; }
    public bool TemVestiario { get; set; }
    public bool TemArmario { get; set; }
    public bool TemEspacoManutencao { get; set; }
    public bool TemCadeado { get; set; }
    public bool AcessoLivre { get; set; }
    public bool AcessoPago { get; set; }
    public bool AcessoCadastro { get; set; }
    public bool AcessoMensal { get; set; }
    public int VeiculosSuportados { get; set; }
    public string? OperadorId { get; set; }
    public string? NomeOperador { get; set; }
    public double NotaMedia { get; set; }
    public AvaliacaoModel[] Avaliacoes { get; set; } = [];
    public DateTime CriadoEm { get; set; }
}

public class AvaliacaoModel
{
    public string Id { get; set; } = "";
    public string UsuarioId { get; set; } = "";
    public string NomeUsuario { get; set; } = "";
    public int Nota { get; set; }
    public string? Comentario { get; set; }
    public DateTime CriadoEm { get; set; }
}

// Partial update DTO for admin edits — all fields nullable, send only the section being changed
public class AdminUpdateRequest
{
    public bool? TemTomada           { get; set; }
    public bool? TemCalibrador       { get; set; }
    public bool? TemVestiario        { get; set; }
    public bool? TemArmario          { get; set; }
    public bool? TemEspacoManutencao { get; set; }
    public bool? TemCadeado          { get; set; }
    public bool? AcessoLivre         { get; set; }
    public bool? AcessoPago          { get; set; }
    public bool? AcessoCadastro      { get; set; }
    public bool? AcessoMensal        { get; set; }
    public int?  VeiculosSuportados  { get; set; }
}

// Matches CriarBicicletarioDto (Portuguese names)
// TipoVeiculo flags: Bicicleta=1, Scooter=2, Monociclo=4, Patinete=8
public class CreateBicicletarioRequest
{
    public string Nome { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool TemTomada { get; set; }
    public bool TemCalibrador { get; set; }
    public bool TemVestiario { get; set; }
    public bool TemArmario { get; set; }
    public bool TemEspacoManutencao { get; set; }
    public bool TemCadeado { get; set; }
    public bool AcessoLivre { get; set; }
    public bool AcessoPago { get; set; }
    public bool AcessoCadastro { get; set; }
    public bool AcessoMensal { get; set; }
    public int VeiculosSuportados { get; set; }
}
