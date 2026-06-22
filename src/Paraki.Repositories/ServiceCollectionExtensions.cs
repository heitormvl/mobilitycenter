using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Paraki.Repositories.Context;
using Paraki.Shared.Models;

namespace Paraki.Repositories;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ParakiDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseNetTopologySuite()
            )
        );

        // AddIdentityCore não sobrescreve o scheme de autenticação padrão,
        // permitindo que o JWT Bearer do Program.cs seja o scheme ativo.
        services
            .AddIdentityCore<Usuario>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ParakiDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
