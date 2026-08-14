using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ContentFactory.Api.Modules.Identity;

public class DevelopmentBypassAuthOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "DevelopmentBypass";
    public string Email { get; set; } = "silverio.bernal@gmail.com";
}

public class DevelopmentBypassAuthenticationHandler(
    IOptionsMonitor<DevelopmentBypassAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<DevelopmentBypassAuthOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
            new(ClaimTypes.Email, Options.Email),
            new(ClaimTypes.Name, "Silverio Bernal (System Owner)"),
            new("is_owner", "true"),
            new(ClaimTypes.Role, Roles.Technical),
            new(ClaimTypes.Role, Roles.Editorial),
            new("capability", Capabilities.ChannelManage),
            new("capability", Capabilities.UsersInvite),
            new("capability", Capabilities.UsersRolesManage),
            new("capability", Capabilities.EditorialVideoApprove),
            new("capability", Capabilities.PublicationExecute),
            new("capability", Capabilities.CostsView),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class GoogleAuthOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "GoogleBearer";
    public string? ClientId { get; set; }
}

public class GoogleAuthenticationHandler(
    IOptionsMonitor<GoogleAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceProvider serviceProvider) : AuthenticationHandler<GoogleAuthOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check authorization header
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var authHeader = authHeaderValues.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Missing token.");
        }

        // Resolve identity service to validate user or pending invitation
        using var scope = serviceProvider.CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var user = await identityService.AuthenticateGoogleTokenAsync(token);
        if (user == null || !user.IsActive)
        {
            return AuthenticateResult.Fail("User is not authorized or active.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Email),
            new("is_owner", user.IsOwner ? "true" : "false")
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Role));
            if (role.Role == Roles.Technical)
            {
                claims.Add(new Claim("capability", Capabilities.ChannelManage));
                claims.Add(new Claim("capability", Capabilities.UsersInvite));
                claims.Add(new Claim("capability", Capabilities.UsersRolesManage));
            }
            if (role.Role == Roles.Editorial)
            {
                claims.Add(new Claim("capability", Capabilities.EditorialVideoApprove));
                claims.Add(new Claim("capability", Capabilities.PublicationExecute));
                claims.Add(new Claim("capability", Capabilities.CostsView));
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
