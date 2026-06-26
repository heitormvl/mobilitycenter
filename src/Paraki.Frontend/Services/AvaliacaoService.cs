using System.Net;
using System.Net.Http.Json;

namespace Paraki.Frontend.Services;

public class AvaliacaoService(HttpClient http)
{
    public async Task<string?> CreateAsync(string bicicletarioId, int nota, string? comentario)
    {
        try
        {
            var response = await http.PostAsJsonAsync(
                $"api/bicicletarios/{bicicletarioId}/avaliacoes",
                new { nota, comentario });

            if (response.IsSuccessStatusCode) return null;
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return "Sua sessão expirou. Entre novamente para avaliar.";

            var err = await response.Content.ReadFromJsonAsync<ApiError>();
            return err?.Message ?? "Não foi possível enviar a avaliação.";
        }
        catch { return "Erro de conexão. Verifique sua internet e tente novamente."; }
    }

    public async Task<string?> UpdateAsync(string avaliacaoId, string bicicletarioId, int nota, string? comentario)
    {
        try
        {
            var response = await http.PutAsJsonAsync(
                $"api/bicicletarios/{bicicletarioId}/avaliacoes/{avaliacaoId}",
                new { nota, comentario });

            if (response.IsSuccessStatusCode) return null;
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return "Sua sessão expirou. Entre novamente.";

            try
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Não foi possível atualizar a avaliação.";
            }
            catch { return $"Erro {(int)response.StatusCode} ao atualizar a avaliação."; }
        }
        catch { return "Erro de conexão. Verifique sua internet e tente novamente."; }
    }

    private record ApiError(string Message);
}
