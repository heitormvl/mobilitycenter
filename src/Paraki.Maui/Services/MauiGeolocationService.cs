using Paraki.RazorLib.Interfaces;

namespace Paraki.Maui.Services;

public class MauiGeolocationService : IGeolocationService
{
    public async Task<(double Lat, double Lng)?> GetLocationAsync()
    {
        try
        {
            // Tenta a última posição conhecida primeiro (instantâneo, sem custo de GPS)
            var location = await Geolocation.Default.GetLastKnownLocationAsync();

            if (location is null)
            {
                var req = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                location = await Geolocation.Default.GetLocationAsync(req);
            }

            return location is null ? null : (location.Latitude, location.Longitude);
        }
        catch { return null; }
    }
}
