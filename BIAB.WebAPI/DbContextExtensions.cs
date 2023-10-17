using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BIAB.WebAPI;

public static class DbContextExtensions
{
    public static IServiceCollection AddDbContext<TContext>(
        this IServiceCollection services,
        ApiSettings settings)
        where TContext : DbContext
    {
        string contextName = typeof(TContext).Name;
        // Get The Settings for the Context
        DatabaseConnectionSettings? dbSettings = settings.GetDatabaseConnectionSettings(contextName);
        if (dbSettings == null)
        {
            throw new Exception($"No Database Connection Settings Found for {contextName}");
        }
        services.AddDbContext<TContext>(dbSettings.ConnectionString, dbSettings.Provider, dbSettings.MigrationAssembly);
        return services;
    }

    // Function to AddDbContext for a Context With a provider from settings
    public static IServiceCollection AddDbContext<TContext>(
        this IServiceCollection services,
        string connectionString, string provider, string? migrationsAssembly = null)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(options =>
        {
            options.AddProvider(connectionString, provider, migrationsAssembly);
        });
        return services;
    }
    
    // Function to Pass to AddDbContext for a Context With a provider from settings
    public static void AddProvider(
        this DbContextOptionsBuilder options,
        string connectionString, string provider, string? migrationsAssembly = null)
    {
        switch (provider)
        {
            case DbProviders.InMemory:
                options.UseInMemoryDatabase(connectionString);
                break;
            case DbProviders.Sqlite:
                options.UseSqlite(connectionString, x => x.MigrationsAssembly(migrationsAssembly));
                break;
            case DbProviders.SqlServer:
                options.UseSqlServer(connectionString, x => x.MigrationsAssembly(migrationsAssembly));
                break;
            case DbProviders.Postgres:
                options.UseNpgsql(connectionString, x => x.MigrationsAssembly(migrationsAssembly));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}