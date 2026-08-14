using System.Net;
using System.Net.Http.Json;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Dashboard;
using ContentFactory.Api.Modules.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ContentFactory.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("USE_IN_MEMORY_DB", "true");
        Environment.SetEnvironmentVariable("AUTH_MODE", "development-bypass");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AUTH_MODE"] = "development-bypass",
                ["USE_IN_MEMORY_DB"] = "true",
                ["ConnectionStrings:DefaultConnection"] = "Filename=:memory:",
                ["DATABASE_URL"] = null,
                ["MYSQL_HOST"] = null,
                ["MYSQL_DATABASE"] = null,
                ["MYSQL_USER"] = null
            });
        });
    }
}

public class ProductionFailFastSecurityTests
{
    [Fact]
    public void ProductionStartup_WithDevelopmentBypass_ThrowsFatalSecurityViolation()
    {
        // Set environment variables to simulate production with development bypass
        var prevEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var prevAuth = Environment.GetEnvironmentVariable("AUTH_MODE");
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            Environment.SetEnvironmentVariable("AUTH_MODE", "development-bypass");

            var builder = WebApplication.CreateBuilder(Array.Empty<string>());
            var authMode = builder.Configuration["AUTH_MODE"] 
                ?? (builder.Environment.IsDevelopment() ? "development-bypass" : "google");

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                if (builder.Environment.IsProduction() && string.Equals(authMode, "development-bypass", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("FATAL SECURITY VIOLATION: AUTH_MODE 'development-bypass' (GOD mode) is strictly forbidden in Production.");
                }
            });

            Assert.Contains("FATAL SECURITY VIOLATION", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", prevEnv);
            Environment.SetEnvironmentVariable("AUTH_MODE", prevAuth);
        }
    }
}

public class DashboardAndChannelApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DashboardAndChannelApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDashboardSummary_ReturnsSuccess_WithFactoryHealth()
    {
        var response = await _client.GetAsync("/api/dashboard/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(summary);
        Assert.NotNull(summary.FactoryHealth);
        Assert.Equal("healthy", summary.FactoryHealth.Status);
        Assert.NotEmpty(summary.Channels);
        Assert.Contains(summary.Channels, c => c.Slug == "ia-simple-es");
    }

    [Fact]
    public async Task GetAllChannels_ReturnsSeededPilotChannel()
    {
        var response = await _client.GetAsync("/api/channels");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var channels = await response.Content.ReadFromJsonAsync<List<ChannelDto>>();
        Assert.NotNull(channels);
        Assert.Contains(channels, c => c.Slug == "ia-simple-es" && c.Language == "es");
    }

    [Fact]
    public async Task CreateChannel_AsTechnicalUser_Succeeds()
    {
        var newSlug = $"test-channel-{Guid.NewGuid():N}";
        var req = new CreateChannelRequest("Test Channel", newSlug, "es", "Tech niche", ChannelStatus.Active);

        var response = await _client.PostAsJsonAsync("/api/channels", req);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ChannelDto>();
        Assert.NotNull(created);
        Assert.Equal(newSlug, created.Slug);
        Assert.Equal(ChannelStatus.Active, created.Status);
    }

    [Fact]
    public async Task GetIdentityMe_ReturnsOwnerWithTechnicalAndEditorialRoles()
    {
        var response = await _client.GetAsync("/api/identity/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(me);
        Assert.Equal("silverio.bernal@gmail.com", me.Email);
        Assert.True(me.IsOwner);
        Assert.Contains(Roles.Technical, me.Roles);
        Assert.Contains(Roles.Editorial, me.Roles);
    }
}
