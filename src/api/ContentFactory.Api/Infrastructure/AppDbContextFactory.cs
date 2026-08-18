using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContentFactory.Api.Infrastructure;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "192.168.0.194";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var db = Environment.GetEnvironmentVariable("POSTGRES_DATABASE") ?? "content_factory_dev";
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "content_factory_app";
        var pass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "";

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? $"Host={host};Port={port};Database={db};Username={user};Password={pass};";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        });

        return new AppDbContext(optionsBuilder.Options);
    }
}
