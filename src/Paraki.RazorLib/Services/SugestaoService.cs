using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Paraki.RazorLib.Services;

public class SugestaoService(HttpClient http)
{
    public async Task<SugestaoDto[]?> GetPendentesAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<SugestaoDto[]>("api/sugestoes/pendentes");
        }
        catch
        {
            return null;
        }
    }

    public async Task<int> GetContagemAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<int>("api/sugestoes/pendentes/contagem");
        }
        catch
        {
            return 0;
        }
    }

    public async Task<string?> AprovarAsync(string id)
    {
        try
        {
            var response = await http.PostAsync($"api/sugestoes/{id}/aprovar", null);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<SugestaoApiError>();
                return err?.Message ?? "Erro ao aprovar.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public async Task<string?> RejeitarAsync(string id, string? motivo)
    {
        try
        {
            var response = await http.PostAsJsonAsync($"api/sugestoes/{id}/rejeitar", new { motivo });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<SugestaoApiError>();
                return err?.Message ?? "Erro ao rejeitar.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public async Task<string?> UploadFotoComprovanteAsync(string sugestaoId, byte[] bytes, string mimeType)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var bc = new ByteArrayContent(bytes);
            bc.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            content.Add(bc, "foto", "comprovante.jpg");
            var response = await http.PostAsync($"api/sugestoes/{sugestaoId}/foto", content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<SugestaoApiError>();
                return err?.Message ?? "Erro ao enviar foto.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public string GetComprovanteUrl(string sugestaoId, string fotoKey) =>
        http.BaseAddress is { } b
            ? $"{b.ToString().TrimEnd('/')}/api/fotos/comprovante/{sugestaoId}/{fotoKey}"
            : $"api/fotos/comprovante/{sugestaoId}/{fotoKey}";

    private record SugestaoApiError(string Message);
}

public class SugestaoDto
{
    public string Id { get; set; } = "";
    public string BicicletarioId { get; set; } = "";
    public string NomeBicicletario { get; set; } = "";
    public string NomeAutor { get; set; } = "";
    public string? Comprovante { get; set; }
    public string? ComprovanteFotoKey { get; set; }
    public bool AplicadaAutomaticamente { get; set; }
    public int TierAutor { get; set; }
    public DateTime CriadoEm { get; set; }
    public SugestaoDadosDto DadosEdicao { get; set; } = new();
}

public class SugestaoDadosDto
{
    public bool? TemTomada           { get; set; }
    public bool? TemCalibrador       { get; set; }
    public bool? TemVestiario        { get; set; }
    public bool? TemArmario          { get; set; }
    public bool? TemEspacoManutencao { get; set; }
    public bool? TemCadeado          { get; set; }
    public bool? TemBanheiro         { get; set; }
    public bool? AcessoLivre         { get; set; }
    public bool? AcessoPago          { get; set; }
    public bool? AcessoCadastro      { get; set; }
    public bool? AcessoMensal        { get; set; }
    public int?  VeiculosSuportados  { get; set; }
    public HorarioModel[]? Horarios  { get; set; }
}
