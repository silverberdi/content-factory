using System.Security.Claims;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using ContentFactory.Api.Modules.Dashboard;
using ContentFactory.Api.Modules.Discovery;
using ContentFactory.Api.Modules.Discovery.Adapters;
using ContentFactory.Api.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 0. Load root .env or .env.development if present
LoadEnvFile();

// 1. Environment & Auth Mode Configuration
var authMode = builder.Configuration["AUTH_MODE"] 
    ?? (builder.Environment.IsDevelopment() ? "development-bypass" : "google");

// PRODUCTION FAIL-FAST GUARD
if (builder.Environment.IsProduction() && string.Equals(authMode, "development-bypass", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("FATAL SECURITY VIOLATION: AUTH_MODE 'development-bypass' (GOD mode) is strictly forbidden in Production.");
}

// 2. Database Persistence Configuration
var useInMemory = string.Equals(Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB"), "true", StringComparison.OrdinalIgnoreCase)
    || string.Equals(builder.Configuration["USE_IN_MEMORY_DB"], "true", StringComparison.OrdinalIgnoreCase)
    || string.Equals(builder.Configuration["DATABASE_PROVIDER"], "in-memory", StringComparison.OrdinalIgnoreCase)
    || builder.Configuration.GetConnectionString("DefaultConnection")?.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase) == true;

if (useInMemory)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseInMemoryDatabase("ContentFactoryDb");
    });
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? builder.Configuration["DATABASE_URL"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        var host = builder.Configuration["MYSQL_HOST"];
        var port = builder.Configuration["MYSQL_PORT"] ?? "3306";
        var db = builder.Configuration["MYSQL_DATABASE"];
        var user = builder.Configuration["MYSQL_USER"];
        var pass = builder.Configuration["MYSQL_PASSWORD"];
        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(db) && !string.IsNullOrWhiteSpace(user))
        {
            connectionString = $"Server={host};Port={port};Database={db};User={user};Password={pass};";
        }
    }

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            try
            {
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)), mySqlOptions =>
                {
                    mySqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                });
            }
            catch
            {
                options.UseInMemoryDatabase("ContentFactoryDb");
            }
        });
    }
    else
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase("ContentFactoryDb");
        });
    }
}

// 3. Authentication Configuration
var authBuilder = builder.Services.AddAuthentication(options =>
{
    if (string.Equals(authMode, "development-bypass", StringComparison.OrdinalIgnoreCase))
    {
        options.DefaultAuthenticateScheme = DevelopmentBypassAuthOptions.SchemeName;
        options.DefaultChallengeScheme = DevelopmentBypassAuthOptions.SchemeName;
    }
    else
    {
        options.DefaultAuthenticateScheme = GoogleAuthOptions.SchemeName;
        options.DefaultChallengeScheme = GoogleAuthOptions.SchemeName;
    }
});

if (string.Equals(authMode, "development-bypass", StringComparison.OrdinalIgnoreCase))
{
    authBuilder.AddScheme<DevelopmentBypassAuthOptions, DevelopmentBypassAuthenticationHandler>(
        DevelopmentBypassAuthOptions.SchemeName, null);
}
else
{
    authBuilder.AddScheme<GoogleAuthOptions, GoogleAuthenticationHandler>(
        GoogleAuthOptions.SchemeName, options =>
        {
            options.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"];
        });
}

// 4. Authorization Policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireTechnical", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(Roles.Technical) ||
            ctx.User.HasClaim("capability", Capabilities.ChannelManage) ||
            ctx.User.HasClaim("capability", Capabilities.UsersInvite)))
    .AddPolicy("RequireEditorial", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(Roles.Editorial) ||
            ctx.User.HasClaim("capability", Capabilities.EditorialVideoApprove)))
    .AddPolicy("RequireChannelManage", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(Roles.Technical) ||
            ctx.User.HasClaim("capability", Capabilities.ChannelManage)))
    .AddPolicy("RequireUsersInvite", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(Roles.Technical) ||
            ctx.User.HasClaim("capability", Capabilities.UsersInvite)))
    .AddPolicy("RequireUsersRolesManage", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(Roles.Technical) ||
            ctx.User.HasClaim("capability", Capabilities.UsersRolesManage)))
    .AddPolicy("RequireDiscoveryManage", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(Roles.Technical) ||
            ctx.User.IsInRole(Roles.Editorial) ||
            ctx.User.HasClaim("capability", Capabilities.ChannelManage)));

// 5. Domain & Infrastructure Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IChannelService, ChannelService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISourceSyncAdapter, FeedSyncAdapter>();
builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddHostedService<DiscoveryBackgroundSyncService>();
builder.Services.AddScoped<IAiProviderRouter, AiProviderRouter>();
builder.Services.AddScoped<IEvidenceCaptureService, EvidenceCaptureService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<ITruthSourceService, TruthSourceService>();
builder.Services.AddScoped<IEditorialTaskService, EditorialTaskService>();

// 6. Controllers & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 7. CORS for local Angular PWA
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                var uri = new Uri(origin);
                return uri.Host is "localhost" or "127.0.0.1" || uri.Host.EndsWith("silverman.pro", StringComparison.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Database initialization
try
{
    await DatabaseInitializer.InitializeAsync(app.Services, app.Environment.IsDevelopment());
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Initial database seeding deferred or already completed.");
}

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program 
{
    public static void LoadEnvFile()
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (currentDir != null)
        {
            var envFile = Path.Combine(currentDir.FullName, ".env");
            if (File.Exists(envFile))
            {
                ParseAndApplyEnv(envFile);
                break;
            }
            var envDevFile = Path.Combine(currentDir.FullName, ".env.development");
            if (File.Exists(envDevFile))
            {
                ParseAndApplyEnv(envDevFile);
                break;
            }
            currentDir = currentDir.Parent;
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

