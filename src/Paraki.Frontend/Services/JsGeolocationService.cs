using Microsoft.JSInterop;
using Paraki.RazorLib.Interfaces;

namespace Paraki.Frontend.Services;

public class JsGeolocationService(IJSRuntime js) : IGeolocationService
{
    public async Task<(double Lat, double Lng)?> GetLocationAsync()
    {
        try
        {
            var arr = await js.InvokeAsync<double[]>("mapInterop.getUserLocation");
            return arr is { Length: 2 } ? (arr[0], arr[1]) : null;
        }
        catch { return null; }
    }
}
