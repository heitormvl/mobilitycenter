using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MobilityCenter.Frontend;
using MobilityCenter.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthStateProvider>());

builder.Services.AddTransient<AuthTokenHandler>();

builder.Services.AddHttpClient("api", client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BicicletarioService>();
builder.Services.AddScoped<ToastService>();

builder.Services.AddHttpClient("geocoder", client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.Add("User-Agent", "MobilityCenter/1.0");
    client.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9");
});

builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
