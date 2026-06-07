using System.Net.Http.Json;

namespace MobilityCenter.Frontend.Services;

public class BicicletarioService(HttpClient http)
{
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

    private record ApiError(string Message);
    private record CreatedResponse(string Id);
}

public class CreateBicicletarioRequest
{
    public string Name { get; set; } = "";
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool HasPowerOutlet { get; set; }
    public bool HasAirPump { get; set; }
    public bool HasLocker { get; set; }
    public bool HasStorage { get; set; }
    public bool HasMaintenanceSpace { get; set; }
    public bool HasBikeLock { get; set; }
    public bool IsFree { get; set; }
    public bool IsPaid { get; set; }
    public bool RequiresSignup { get; set; }
    public bool IsMonthlySubscription { get; set; }
    public int VehicleTypes { get; set; }
}
