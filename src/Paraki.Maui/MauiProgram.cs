using System.Reflection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Paraki.Maui.Services;
using Paraki.RazorLib.Interfaces;
using Paraki.RazorLib.Services;

namespace Paraki.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        // Carrega appsettings.json embutido (API base URL, Google ClientId)
        var assembly = Assembly.GetExecutingAssembly();
        using var settingsStream = assembly.GetManifestResourceStream("Paraki.Maui.appsettings.json");
        if (settingsStream is not null)
            builder.Configuration.AddJsonStream(settingsStream);

        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://paraki-api.fly.dev";

        // Implementações específicas do MAUI das abstrações compartilhadas (RazorLib)
        builder.Services.AddSingleton<ILocalStorageService, MauiLocalStorageService>();
        builder.Services.AddSingleton<IGoogleAuthService, MauiGoogleAuthService>();

        builder.Services.AddScoped<JwtAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<JwtAuthStateProvider>());

        builder.Services.AddTransient<AuthTokenHandler>();

        builder.Services.AddHttpClient("api", client =>
            client.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthTokenHandler>();

        // Cliente sem handler de autenticação — usado para chamadas de refresh token
        builder.Services.AddHttpClient("auth-api", client =>
            client.BaseAddress = new Uri(apiBaseUrl));

        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));

        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<BicicletarioService>();
        builder.Services.AddScoped<ToastService>();
        builder.Services.AddScoped<ConfirmService>();
        builder.Services.AddScoped<AvaliacaoService>();
        builder.Services.AddScoped<AdicionarStateService>();
        builder.Services.AddScoped<SugestaoService>();
        builder.Services.AddScoped<AdminService>();

        builder.Services.AddHttpClient("geocoder", client =>
        {
            client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            client.DefaultRequestHeaders.Add("User-Agent", "Paraki/1.0");
            client.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9");
        });

        builder.Services.AddAuthorizationCore();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
