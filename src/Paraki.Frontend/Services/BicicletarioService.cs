using System.Globalization;
using System.Net.Http.Json;

namespace Paraki.Frontend.Services;

public class BicicletarioService(HttpClient http)
{
    public async Task<BicicletarioDto[]?> GetAllAsync(double? lat = null, double? lng = null, bool incluirOcultas = false)
    {
        try
        {
            var baseRoute = incluirOcultas ? "api/bicicletarios/admin" : "api/bicicletarios";
            var url = $"{baseRoute}?take=100";
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

    public async Task<(string? Error, string? Id, int StatusAprovacao)> CreateAsync(CreateBicicletarioRequest req)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/bicicletarios", req);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return (err?.Message ?? "Erro ao publicar o bicicletário.", null, 0);
            }
            var result = await response.Content.ReadFromJsonAsync<CreatedResponse>();
            return (null, result?.Id, result?.StatusAprovacao ?? 1);
        }
        catch
        {
            return ("Erro de conexão. Verifique sua internet e tente novamente.", null, 0);
        }
    }

    public async Task<BicicletarioPendenteModel[]?> GetPendentesAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<BicicletarioPendenteModel[]>("api/bicicletarios/pendentes");
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> AprovarCriacaoAsync(string id)
    {
        try
        {
            var response = await http.PostAsync($"api/bicicletarios/{id}/aprovar", null);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao aprovar.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public async Task<string?> RejeitarCriacaoAsync(string id, string? motivo)
    {
        try
        {
            var response = await http.PostAsJsonAsync($"api/bicicletarios/{id}/rejeitar", new { Motivo = motivo });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao rejeitar.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
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

    // Para não-admins: retorna o ID da sugestão criada (necessário para upload de foto de comprovante)
    public async Task<(string? Error, string? SugestaoId)> SugerirEdicaoAsync(string bicicletarioId, AdminUpdateRequest req)
    {
        try
        {
            var response = await http.PutAsJsonAsync($"api/bicicletarios/{bicicletarioId}", req);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return (err?.Message ?? "Erro ao enviar sugestão.", null);
            }
            var body = await response.Content.ReadFromJsonAsync<ResultadoAtualizacaoDto>();
            return (null, body?.Sugestao?.Id);
        }
        catch
        {
            return ("Erro de conexão.", null);
        }
    }

    public async Task<string?> RestaurarAsync(string id)
    {
        try
        {
            var response = await http.PatchAsync($"api/bicicletarios/{id}/restaurar", null);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao restaurar.";
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

    public async Task<FotoModel[]?> GetFotosAsync(string bicicletarioId)
    {
        try
        {
            return await http.GetFromJsonAsync<FotoModel[]>($"api/fotos?bicicletarioId={bicicletarioId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<(string? Error, FotoModel? Foto)> UploadFotoAsync(string bicicletarioId, MultipartFormDataContent content)
    {
        try
        {
            var response = await http.PostAsync($"api/fotos?bicicletarioId={bicicletarioId}", content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return (err?.Message ?? "Erro ao enviar foto.", null);
            }
            var foto = await response.Content.ReadFromJsonAsync<FotoModel>();
            return (null, foto);
        }
        catch
        {
            return ("Erro de conexão.", null);
        }
    }

    public async Task<string?> SetCapaAsync(string fotoId)
    {
        try
        {
            var response = await http.PatchAsync($"api/fotos/{fotoId}/capa", null);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao definir capa.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public async Task<string?> DeleteFotoAsync(string fotoId)
    {
        try
        {
            var response = await http.DeleteAsync($"api/fotos/{fotoId}");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Erro ao excluir foto.";
            }
            return null;
        }
        catch
        {
            return "Erro de conexão.";
        }
    }

    public string GetFotoBicicletarioUrl(string bicicletarioId, long? cacheBust = null) =>
        http.BaseAddress is { } b
            ? $"{b}api/fotos/bicicletario/{bicicletarioId}{(cacheBust.HasValue ? $"?t={cacheBust}" : "")}"
            : $"api/fotos/bicicletario/{bicicletarioId}";

    public string GetFotoUrl(string fotoUrl) =>
        http.BaseAddress is { } b && fotoUrl.StartsWith("/")
            ? $"{b.ToString().TrimEnd('/')}{fotoUrl}"
            : fotoUrl;

    private record ApiError(string Message);
    private record CreatedResponse(string Id, int StatusAprovacao);
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
    public bool TemBanheiro { get; set; }

    public bool AcessoLivre { get; set; }
    public bool AcessoPago { get; set; }
    public bool AcessoCadastro { get; set; }
    public bool AcessoMensal { get; set; }
    public bool IsDeleted { get; set; }
    public HorarioModel[] Horarios { get; set; } = [];
    public string? CapaUrl { get; set; }
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
    public bool TemBanheiro { get; set; }
    public bool AcessoLivre { get; set; }
    public bool AcessoPago { get; set; }
    public bool AcessoCadastro { get; set; }
    public bool AcessoMensal { get; set; }
    public int VeiculosSuportados { get; set; }
    public string? OperadorId { get; set; }
    public string? NomeOperador { get; set; }
    public double NotaMedia { get; set; }
    public AvaliacaoModel[] Avaliacoes { get; set; } = [];
    public HorarioModel[] Horarios { get; set; } = [];
    public DateTime CriadoEm { get; set; }
    public bool IsDeleted { get; set; }
}

public class AvaliacaoModel
{
    public string Id { get; set; } = "";
    public string UsuarioId { get; set; } = "";
    public string NomeUsuario { get; set; } = "";
    public int Nota { get; set; }
    public string? Comentario { get; set; }
    public DateTime CriadoEm { get; set; }
    public string? FotoPerfilUrl { get; set; }
}

public class HorarioModel
{
    public int DiaSemana { get; set; }
    public string HoraAbertura { get; set; } = "";
    public string HoraFechamento { get; set; } = "";
}

public enum StatusHorario { NaoInformado, Aberto, FechaEmBreve, Fechado }

public static class HorarioHelper
{
    public static StatusHorario GetStatus(HorarioModel[] horarios)
    {
        if (horarios.Length == 0) return StatusHorario.NaoInformado;
        var now = DateTime.Now;
        var diaSemana = (int)now.DayOfWeek;
        var hora = TimeOnly.FromDateTime(now);
        var h = horarios.FirstOrDefault(x => x.DiaSemana == diaSemana);
        if (h == null) return StatusHorario.Fechado;
        if (!TimeOnly.TryParse(h.HoraAbertura, out var abertura)) return StatusHorario.NaoInformado;
        if (!TimeOnly.TryParse(h.HoraFechamento, out var fechamento)) return StatusHorario.NaoInformado;

        // fechamento == 00:00 significa meia-noite (fim do dia), trata como 24:00
        bool aberto;
        if (fechamento == TimeOnly.MinValue || fechamento <= abertura)
        {
            // turno cruza a meia-noite (ex: 06:00–00:00 ou 22:00–02:00)
            aberto = hora >= abertura || hora < fechamento;
        }
        else
        {
            aberto = hora >= abertura && hora < fechamento;
        }

        if (!aberto) return StatusHorario.Fechado;

        // minutos restantes (com suporte a turno que cruza meia-noite)
        double minutosRestantes = fechamento == TimeOnly.MinValue
            ? (TimeOnly.MaxValue - hora).TotalMinutes + 1
            : fechamento > hora
                ? (fechamento - hora).TotalMinutes
                : (TimeOnly.MaxValue - hora).TotalMinutes + fechamento.ToTimeSpan().TotalMinutes + 1;

        if (minutosRestantes <= 30) return StatusHorario.FechaEmBreve;
        return StatusHorario.Aberto;
    }

    public static (string Text, string Bg, string Color) GetBadgeStyle(StatusHorario status) => status switch
    {
        StatusHorario.Aberto       => ("Aberto",         "#16a34a", "#fff"),
        StatusHorario.FechaEmBreve => ("Fecha em breve", "#d97706", "#fff"),
        StatusHorario.Fechado      => ("Fechado",        "#dc2626", "#fff"),
        _                          => ("",               "",        "")
    };
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
    public bool? TemBanheiro         { get; set; }
    public bool? AcessoLivre         { get; set; }
    public bool? AcessoPago          { get; set; }
    public bool? AcessoCadastro      { get; set; }
    public bool? AcessoMensal        { get; set; }
    public int?  VeiculosSuportados  { get; set; }
    public HorarioModel[]? Horarios  { get; set; }
    public string? Comprovante       { get; set; }
}

public class FotoModel
{
    public string Id { get; set; } = "";
    public string BicicletarioId { get; set; } = "";
    public string FotoUrl { get; set; } = "";
    public bool IsCapa { get; set; }
    public int Ordem { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class BicicletarioPendenteModel
{
    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int StatusAprovacao { get; set; }
    public string? NomeCriador { get; set; }
    public int TierCriador { get; set; }
    public bool TemTomada { get; set; }
    public bool TemCalibrador { get; set; }
    public bool TemVestiario { get; set; }
    public bool TemArmario { get; set; }
    public bool TemEspacoManutencao { get; set; }
    public bool TemCadeado { get; set; }
    public bool TemBanheiro { get; set; }
    public bool AcessoLivre { get; set; }
    public bool AcessoPago { get; set; }
    public bool AcessoCadastro { get; set; }
    public bool AcessoMensal { get; set; }
    public int VeiculosSuportados { get; set; }
    public HorarioModel[] Horarios { get; set; } = [];
    public string? CapaUrl { get; set; }
    public DateTime CriadoEm { get; set; }
}

// For SugerirEdicaoAsync: parses the 202 response body to extract sugestaoId
public class ResultadoAtualizacaoDto
{
    public bool EditadoDireto { get; set; }
    public SugestaoResumoDto? Sugestao { get; set; }
}

public class SugestaoResumoDto
{
    public string Id { get; set; } = "";
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
    public bool TemBanheiro { get; set; }
    public bool AcessoLivre { get; set; }
    public bool AcessoPago { get; set; }
    public bool AcessoCadastro { get; set; }
    public bool AcessoMensal { get; set; }
    public int VeiculosSuportados { get; set; }
    public HorarioModel[]? Horarios { get; set; }
}
