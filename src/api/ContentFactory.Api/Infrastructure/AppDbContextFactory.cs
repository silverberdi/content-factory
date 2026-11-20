using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContentFactory.Api.Infrastructure;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        LoadEnvFile();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
            var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
            var db = Environment.GetEnvironmentVariable("POSTGRES_DATABASE") ?? Environment.GetEnvironmentVariable("POSTGRES_DB");
            var user = Environment.GetEnvironmentVariable("POSTGRES_USER");
            var pass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(user))
                throw new InvalidOperationException("Design-time PostgreSQL configuration requires DATABASE_URL or POSTGRES_HOST, POSTGRES_DATABASE, and POSTGRES_USER.");

            connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass};";
        }

        PersistenceConfiguration.ValidatePostgreSqlConnectionString(connectionString);

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        });

        return new AppDbContext(optionsBuilder.Options);
    }

    private static void LoadEnvFile()
    {
        var current = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(current))
        {
            var envDev = Path.Combine(current, ".env.development");
            if (File.Exists(envDev))
            {
                ParseAndApplyEnv(envDev);
                break;
            }

            var env = Path.Combine(current, ".env");
            if (File.Exists(env))
            {
                ParseAndApplyEnv(env);
                break;
            }

            var parent = Directory.GetParent(current);
            current = parent?.FullName;
        }
    }

    private static void ParseAndApplyEnv(string path)
    {
        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            if (Environment.GetEnvironmentVariable(key) == null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
