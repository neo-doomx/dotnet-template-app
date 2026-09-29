using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Template.Application.Abstractions.Persistence;
using Template.Application.Abstractions.Security;
using Template.Infrastructure.Persistence;
using Template.Infrastructure.Repositories;
using Template.Infrastructure.Security;

namespace Template.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string databaseConnection = GetRequiredConnectionString(configuration, "Database");
        string redisConnection = GetRequiredConnectionString(configuration, "Redis");

        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(databaseConnection, sql => sql.EnableRetryOnFailure()));

        // HybridCache uses the registered Redis IDistributedCache as its second level.
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "template:";
        });

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            };
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }

    private static string GetRequiredConnectionString(IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name) ?? throw new InvalidOperationException(
            $"Connection string '{name}' is missing. Run ./scripts/setup-secrets.ps1 to store it in user secrets.");
}
