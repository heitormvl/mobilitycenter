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

public class BicicletarioDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
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
    public RatingDto[] Ratings { get; set; } = [];
}

public class RatingDto
{
    public int Rating { get; set; }
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
