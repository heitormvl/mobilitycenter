using System.Net.Http.Json;

namespace Paraki.Frontend.Services;

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

    private record ApiError(string Message);
}

public class PendenciasContagemModel
{
    public int Sugestoes { get; set; }
    public int Bicis { get; set; }
    public int Total => Sugestoes + Bicis;
}
