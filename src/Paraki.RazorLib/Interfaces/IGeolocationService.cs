namespace Paraki.RazorLib.Interfaces;

public interface IGeolocationService
{
    Task<(double Lat, double Lng)?> GetLocationAsync();
}
