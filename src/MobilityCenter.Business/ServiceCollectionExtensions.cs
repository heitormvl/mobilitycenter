using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MobilityCenter.Business.Interfaces;
using MobilityCenter.Business.Services;

namespace MobilityCenter.Business;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        var storageType = configuration["AzureStorage:StorageType"] ?? "Azure";

        if (storageType.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            var localPath = configuration["AzureStorage:LocalPath"];
            if (string.IsNullOrEmpty(localPath))
                localPath = Path.Combine(Path.GetTempPath(), "mobilitycenter", "fotos-perfil");
            services.AddScoped<IFotoStorageService>(_ => new LocalFotoStorageService(localPath));
        }
        else
        {
            var connStr = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString não configurado.");
            var blobOptions = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2024_05_04);
            services.AddSingleton(new BlobServiceClient(connStr, blobOptions));
            services.AddScoped<IFotoStorageService, FotoStorageService>();
        }

        services.AddHttpClient();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBicicletarioService, BicicletarioService>();
        services.AddScoped<IAvaliacaoService, AvaliacaoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<ISugestaoEdicaoService, SugestaoEdicaoService>();
        return services;
    }
}
