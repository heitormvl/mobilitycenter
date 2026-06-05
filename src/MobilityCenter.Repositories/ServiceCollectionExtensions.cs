using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MobilityCenter.Repositories.Context;
using MobilityCenter.Shared.Models;

namespace MobilityCenter.Repositories;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MobilityCenterDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseNetTopologySuite()
            )
        );

        services
            .AddIdentity<Usuario, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MobilityCenterDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
