using System.Net;
using System.Net.Http.Json;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContentFactory.Api.Tests;

public class SystemOwnerProtectionTests
{
    private readonly AppDbContext _dbContext;
    private readonly IdentityService _identityService;

    public SystemOwnerProtectionTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        var auditService = new AuditService(_dbContext, new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditService>());
        _identityService = new IdentityService(_dbContext, auditService, new Microsoft.Extensions.Logging.Abstractions.NullLogger<IdentityService>());

        // Seed SYSTEM_OWNER
        var owner = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Email = "silverio.bernal@gmail.com",
            IsOwner = true,
            IsActive = true
        };
        owner.Roles.Add(new UserRole { UserId = owner.Id, Role = Roles.Technical });
        owner.Roles.Add(new UserRole { UserId = owner.Id, Role = Roles.Editorial });
        _dbContext.Users.Add(owner);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task Owner_CannotBeDeleted_ByAnyUser()
    {
        var ownerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var attackerId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _identityService.DeleteUserAsync(ownerId, attackerId, "attacker@example.com"));

        Assert.Contains("Cannot delete protected SYSTEM_OWNER", ex.Message);
    }

    [Fact]
    public async Task Owner_CannotBeDisabled_ByAnyUser()
    {
        var ownerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var attackerId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _identityService.SetUserActiveStatusAsync(ownerId, false, attackerId, "attacker@example.com"));

        Assert.Contains("Cannot disable or change status of protected SYSTEM_OWNER", ex.Message);
    }

    [Fact]
    public async Task Owner_RolesCannotBeModified_ByAnyUser()
    {
        var ownerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var attackerId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _identityService.UpdateUserRolesAsync(ownerId, [Roles.Editorial], attackerId, "attacker@example.com"));

        Assert.Contains("Cannot modify protected SYSTEM_OWNER roles", ex.Message);
    }

    [Fact]
    public async Task NonOwner_CanBeModified_ByTechnicalUser()
    {
        var regularUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "member@example.com",
            IsOwner = false,
            IsActive = true
        };
        _dbContext.Users.Add(regularUser);
        await _dbContext.SaveChangesAsync();

        var technicalUserId = Guid.NewGuid();

        // Update roles
        await _identityService.UpdateUserRolesAsync(regularUser.Id, [Roles.Editorial], technicalUserId, "tech@example.com");

        var updated = await _dbContext.Users.Include(u => u.Roles).FirstAsync(u => u.Id == regularUser.Id);
        Assert.Single(updated.Roles);
        Assert.Equal(Roles.Editorial, updated.Roles[0].Role);
    }
}

public class InvitationAndActivationTests
{
    private readonly AppDbContext _dbContext;
    private readonly IdentityService _identityService;

    public InvitationAndActivationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        var auditService = new AuditService(_dbContext, new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditService>());
        _identityService = new IdentityService(_dbContext, auditService, new Microsoft.Extensions.Logging.Abstractions.NullLogger<IdentityService>());
    }

    [Fact]
    public async Task Invitation_RequiresExactGoogleEmail_ToActivate()
    {
        var invitedEmail = "editor@example.com";
        var inviteReq = new InviteUserRequest(invitedEmail, [Roles.Editorial]);

        var inv = await _identityService.CreateInvitationAsync(inviteReq, Guid.NewGuid(), "admin@example.com");
        Assert.Equal("Pending", inv.Status);

        // Attempt activation with mismatched email -> fails
        var userMismatch = await _identityService.AuthenticateGoogleTokenAsync("uninvited@example.com");
        Assert.Null(userMismatch);

        // Attempt activation with matching exact email -> succeeds
        var userMatch = await _identityService.AuthenticateGoogleTokenAsync(invitedEmail);
        Assert.NotNull(userMatch);
        Assert.Equal(invitedEmail, userMatch.Email);
        Assert.Single(userMatch.Roles);
        Assert.Equal(Roles.Editorial, userMatch.Roles[0].Role);

        // Invitation is now Accepted
        var invInDb = await _dbContext.UserInvitations.FindAsync(inv.Id);
        Assert.Equal("Accepted", invInDb!.Status);
    }
}

public class ChannelLifecycleTests
{
    private readonly AppDbContext _dbContext;
    private readonly ChannelService _channelService;

    public ChannelLifecycleTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        var auditService = new AuditService(_dbContext, new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditService>());
        _channelService = new ChannelService(_dbContext, auditService);
    }

    [Fact]
    public async Task CreateChannel_GeneratesAuditEvent_AndPersists()
    {
        var req = new CreateChannelRequest("IA Simple ES", "ia-simple-es", "es", "AI and future of work", ChannelStatus.Pilot);
        var actingId = Guid.NewGuid();
        var actingEmail = "silverio.bernal@gmail.com";

        var channel = await _channelService.CreateChannelAsync(req, actingId, actingEmail);

        Assert.Equal("ia-simple-es", channel.Slug);
        Assert.Equal(ChannelStatus.Pilot, channel.Status);

        var audit = await _dbContext.AuditEvents.FirstOrDefaultAsync(a => a.TargetId == channel.Id.ToString());
        Assert.NotNull(audit);
        Assert.Equal("channel.created", audit.Action);
    }

    [Fact]
    public async Task CreateChannel_WithDuplicateSlug_ThrowsConflict()
    {
        var req = new CreateChannelRequest("IA Simple ES", "ia-simple-es", "es", "AI", ChannelStatus.Pilot);
        await _channelService.CreateChannelAsync(req, Guid.NewGuid(), "admin@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _channelService.CreateChannelAsync(req, Guid.NewGuid(), "admin@example.com"));
    }
}
