using System.Security.Claims;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Dashboard;
using ContentFactory.Api.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Environment & Auth Mode Configuration
var authMode = builder.Configuration["AUTH_MODE"] 
    ?? (builder.Environment.IsDevelopment() ? "development-bypass" : "google");

// PRODUCTION FAIL-FAST GUARD
if (builder.Environment.IsProduction() && string.Equals(authMode, "development-bypass", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("FATAL SECURITY VIOLATION: AUTH_MODE 'development-bypass' (GOD mode) is strictly forbidden in Production.");
}

// 2. Database Persistence Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration["DATABASE_URL"];

if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    });
}
else
{
    // Fallback for isolated unit tests / in-memory local testing
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseInMemoryDatabase("ContentFactoryDb");
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
            ctx.User.HasClaim("capability", Capabilities.UsersRolesManage)));

// 5. Domain & Infrastructure Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IChannelService, ChannelService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

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
public partial class Program { }
