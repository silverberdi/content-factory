using System.Security.Claims;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Infrastructure.BackgroundWorkers;
using ContentFactory.Api.Infrastructure.Storage;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using ContentFactory.Api.Modules.Dashboard;
using ContentFactory.Api.Modules.Discovery;
using ContentFactory.Api.Modules.Discovery.Adapters;
using ContentFactory.Api.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

// 2. Database Persistence Configuration. PostgreSQL is mandatory unless a
// non-production fixture requests InMemory explicitly.
var persistence = PersistenceConfiguration.Resolve(builder.Configuration, builder.Environment);
if (persistence.Provider == PersistenceProvider.InMemory)
{
    // Keep explicitly requested test fixtures isolated even when multiple hosts
    // run in parallel and happen to use the same logical database name.
    var inMemoryRoot = new InMemoryDatabaseRoot();
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(persistence.InMemoryDatabaseName!, inMemoryRoot));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseNpgsql(persistence.ConnectionString!, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        });
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    });
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
    .AddPolicy("RequireEditorialOrTechnical", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(Roles.Editorial) ||
            ctx.User.IsInRole(Roles.Technical) ||
            ctx.User.HasClaim("capability", Capabilities.EditorialVideoApprove) ||
            ctx.User.HasClaim("capability", Capabilities.ChannelManage)))
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
builder.Services.AddHttpClient("evidence-capture")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IChannelService, ChannelService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISourceSyncAdapter, FeedSyncAdapter>();
builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddHostedService<DiscoveryBackgroundSyncService>();
builder.Services.AddScoped<IAiProviderRouter, AiProviderRouter>();
builder.Services.AddScoped<IEvidenceCaptureService, EvidenceCaptureService>();
builder.Services.AddScoped<IEditorialUrlAnalysisService, EditorialUrlAnalysisService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<ITruthSourceService, TruthSourceService>();
builder.Services.AddScoped<IContentIdeaService, ContentIdeaService>();
builder.Services.AddScoped<IScriptService, ScriptService>();
builder.Services.AddScoped<IStoryboardService, StoryboardService>();
builder.Services.AddScoped<IEditorialTaskService, EditorialTaskService>();

builder.Services.AddSingleton<IStorageService, MinioStorageService>();
builder.Services.AddSingleton<IVisualGenerationProvider, MockVisualGenerationProvider>();
builder.Services.AddSingleton<IVisualGenerationProvider, ComfyVisualGenerationProvider>();
builder.Services.AddScoped<IVisualGenerationService, VisualGenerationService>();
builder.Services.AddHostedService<VisualGenerationBackgroundWorker>();

builder.Services.AddSingleton<IVoiceGenerationProvider, MockVoiceGenerationProvider>();
builder.Services.AddSingleton<IVoiceGenerationProvider, GeminiTtsProvider>();
builder.Services.AddScoped<IVoiceGenerationService, VoiceGenerationService>();
builder.Services.AddHostedService<VoiceGenerationBackgroundWorker>();

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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Database initialization
await DatabaseInitializer.InitializeAsync(app.Services, app.Environment.IsDevelopment());

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
