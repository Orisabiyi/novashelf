using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Novashelf.Api.Data;

public static class DatabaseSetup
{
    public static IServiceCollection AddNovashelfDatabase(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IArticleStore, NullArticleStore>();
            return services;
        }

        services.AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IArticleStore, PostgresArticleStore>();
        return services;
    }

    // Migrations use the direct connection when there is one: schema changes and Neon's pooler
    // don't mix well.
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        var config = app.Configuration;
        if (string.IsNullOrWhiteSpace(config.GetConnectionString("Default")))
        {
            // Easy to hit by accident: user-secrets only load in the Development environment.
            app.Logger.LogWarning(
                "No database configured (ConnectionStrings:Default is empty in the {Environment} environment). " +
                "Extracted articles are kept in memory only.", app.Environment.EnvironmentName);
            return;
        }

        if (!config.GetValue("Database:MigrateOnStartup", defaultValue: true)) return;

        var connectionString = config.GetConnectionString("Migrations") ?? config.GetConnectionString("Default");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Database ready: {Host}", new NpgsqlConnectionStringBuilder(connectionString).Host);
    }
}

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("Migrations")
            ?? config.GetConnectionString("Default")
            // Generating a migration doesn't connect, so a placeholder is enough.
            ?? "Host=localhost;Database=novashelf";

        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
    }
}
