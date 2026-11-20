using ContentFactory.Api.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ContentFactory.Api.Tests;

public sealed class PersistenceConfigurationTests
{
    [Fact]
    public void MissingModeAndConnectionRequiresPostgreSql()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Resolve(new Dictionary<string, string?>(), "Development"));
        Assert.Contains("PostgreSQL", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExplicitInMemoryIsAllowedOnlyOutsideProduction()
    {
        var selection = Resolve(new Dictionary<string, string?> { ["USE_IN_MEMORY_DB"] = "true" }, "Development");
        Assert.Equal(PersistenceProvider.InMemory, selection.Provider);

        var ex = Assert.Throws<InvalidOperationException>(() => Resolve(new Dictionary<string, string?> { ["USE_IN_MEMORY_DB"] = "true" }, "Production"));
        Assert.Contains("InMemory", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PostgreSqlModeDoesNotFallbackForInvalidConfiguration()
    {
        var config = new Dictionary<string, string?>
        {
            ["DATABASE_PROVIDER"] = "postgresql",
            ["ConnectionStrings:DefaultConnection"] = "Host=;Database=;Username=;Password=super-secret"
        };

        var ex = Assert.Throws<InvalidOperationException>(() => Resolve(config, "Development"));
        Assert.DoesNotContain("super-secret", ex.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("InMemory", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidPostgreSqlConfigurationSelectsOnlyPostgreSql()
    {
        var selection = Resolve(new Dictionary<string, string?>
        {
            ["DATABASE_PROVIDER"] = "postgresql",
            ["POSTGRES_HOST"] = "localhost",
            ["POSTGRES_DATABASE"] = "content_factory_test",
            ["POSTGRES_USER"] = "test_user",
            ["POSTGRES_PASSWORD"] = "test-only-secret"
        }, "Development");

        Assert.Equal(PersistenceProvider.PostgreSql, selection.Provider);
        Assert.Contains("Host=localhost", selection.ConnectionString, StringComparison.Ordinal);
    }

    private static PersistenceSelection Resolve(Dictionary<string, string?> values, string environmentName)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return PersistenceConfiguration.Resolve(configuration, new TestHostEnvironment(environmentName));
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "ContentFactory.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
