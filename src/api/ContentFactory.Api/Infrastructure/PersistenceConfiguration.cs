using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace ContentFactory.Api.Infrastructure;

public enum PersistenceProvider
{
    PostgreSql,
    InMemory
}

public sealed record PersistenceSelection(PersistenceProvider Provider, string? ConnectionString, string? InMemoryDatabaseName);

public static class PersistenceConfiguration
{
    public static PersistenceSelection Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var declaredProvider = FirstNonEmpty(configuration["DATABASE_PROVIDER"], configuration["DB_PROVIDER"]);
        var explicitlyInMemory = IsTrue(configuration["USE_IN_MEMORY_DB"])
            || string.Equals(declaredProvider, "in-memory", StringComparison.OrdinalIgnoreCase)
            || string.Equals(declaredProvider, "inmemory", StringComparison.OrdinalIgnoreCase);

        if (explicitlyInMemory)
        {
            if (environment.IsProduction())
                throw new InvalidOperationException("Persistence configuration rejected: InMemory is not permitted in Production.");

            return new PersistenceSelection(
                PersistenceProvider.InMemory,
                null,
                configuration["IN_MEMORY_DB_NAME"] ?? "ContentFactoryDb");
        }

        if (!string.IsNullOrWhiteSpace(declaredProvider)
            && !string.Equals(declaredProvider, "postgresql", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(declaredProvider, "postgres", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(declaredProvider, "npgsql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Persistence configuration rejected: DATABASE_PROVIDER must be postgresql or an explicitly authorized in-memory mode.");
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_URL"]
            ?? BuildConnectionString(configuration);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("PostgreSQL persistence is required, but no connection configuration was provided.");

        ValidatePostgreSqlConnectionString(connectionString);
        return new PersistenceSelection(PersistenceProvider.PostgreSql, connectionString, null);
    }

    public static void ValidatePostgreSqlConnectionString(string connectionString)
    {
        try
        {
            var parsed = new NpgsqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(parsed.Host)
                || string.IsNullOrWhiteSpace(parsed.Database)
                || string.IsNullOrWhiteSpace(parsed.Username))
            {
                throw new InvalidOperationException("PostgreSQL connection configuration requires host, database, and username.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("PostgreSQL connection configuration is invalid.", ex);
        }
    }

    private static string? BuildConnectionString(IConfiguration configuration)
    {
        var host = FirstNonEmpty(configuration["POSTGRES_HOST"], configuration["DB_HOST"]);
        var port = FirstNonEmpty(configuration["POSTGRES_PORT"], configuration["DB_PORT"]) ?? "5432";
        var database = FirstNonEmpty(configuration["POSTGRES_DATABASE"], configuration["POSTGRES_DB"], configuration["DB_NAME"]);
        var username = FirstNonEmpty(configuration["POSTGRES_USER"], configuration["DB_USER"]);
        var password = FirstNonEmpty(configuration["POSTGRES_PASSWORD"], configuration["DB_PASSWORD"]);

        return string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(username)
            ? null
            : $"Host={host};Port={port};Database={database};Username={username};Password={password};";
    }

    private static bool IsTrue(string? value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
