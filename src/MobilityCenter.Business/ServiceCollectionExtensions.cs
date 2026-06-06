using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MobilityCenter.Business.Interfaces;
using MobilityCenter.Business.Services;

namespace MobilityCenter.Business;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBicicletarioService, BicicletarioService>();
        services.AddScoped<IAvaliacaoService, AvaliacaoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        return services;
    }
}
