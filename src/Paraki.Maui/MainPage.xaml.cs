namespace Paraki.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

#if ANDROID
        blazorWebView.BlazorWebViewInitialized += (_, e) =>
        {
            // Habilita a API de geolocalização no WebView e instala o cliente
            // customizado que responde ao prompt de permissão do JavaScript.
            e.WebView.Settings.SetGeolocationEnabled(true);
            var existing = e.WebView.WebChromeClient;
            if (existing is not null)
                e.WebView.SetWebChromeClient(new GeoPermissionWebChromeClient(existing));
        };
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RequestLocationPermissionAsync();
    }

    private static async Task RequestLocationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
    }
}
