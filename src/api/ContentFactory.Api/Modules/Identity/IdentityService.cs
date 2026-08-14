using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Audit;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Identity;

public record UserDto(Guid Id, string Email, bool IsOwner, bool IsActive, List<string> Roles, DateTime CreatedAtUtc);
public record UserInvitationDto(Guid Id, string Email, List<string> Roles, string Status, DateTime ExpiresAtUtc, DateTime CreatedAtUtc);
public record InviteUserRequest(string Email, List<string> Roles);
public record UpdateUserRolesRequest(List<string> Roles);

public interface IIdentityService
{
    Task<User?> AuthenticateGoogleTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<UserDto> GetCurrentUserAsync(string email, CancellationToken cancellationToken = default);
    Task<List<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<List<UserInvitationDto>> GetPendingInvitationsAsync(CancellationToken cancellationToken = default);
    Task<UserInvitationDto> CreateInvitationAsync(InviteUserRequest request, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default);
    Task RevokeInvitationAsync(Guid invitationId, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default);
    Task UpdateUserRolesAsync(Guid targetUserId, List<string> roles, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default);
    Task SetUserActiveStatusAsync(Guid targetUserId, bool isActive, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid targetUserId, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default);
}

public class IdentityService(AppDbContext dbContext, IAuditService auditService, ILogger<IdentityService> logger) : IIdentityService
{
    public const string CanonicalOwnerEmail = "silverio.bernal@gmail.com";

    public async Task<User?> AuthenticateGoogleTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // For production, token decoding/validation happens here.
        // If token provides verified email:
        var email = token.Trim().ToLowerInvariant(); // In actual Google OIDC, extract claims.email

        var user = await dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user != null)
        {
            return user;
        }

        // Check if there is an active pending invitation for this exact email
        var invitation = await dbContext.UserInvitations
            .FirstOrDefaultAsync(i => i.Email == email && i.Status == "Pending" && i.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);

        if (invitation == null)
        {
            logger.LogWarning("Access denied: no active invitation found for {Email}", email);
            return null;
        }

        // Activate user
        var isOwner = string.Equals(email, CanonicalOwnerEmail, StringComparison.OrdinalIgnoreCase);
        user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            IsOwner = isOwner,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var roleNames = invitation.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var r in roleNames)
        {
            user.Roles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Role = r.ToUpperInvariant(),
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        invitation.Status = "Accepted";
        invitation.AcceptedAtUtc = DateTime.UtcNow;

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "user.activated_via_invitation",
            targetType: "user",
            targetId: user.Id.ToString(),
            detailsJson: $"{{\"email\":\"{email}\",\"roles\":\"{invitation.Roles}\"}}",
            actorUserId: user.Id,
            actorEmail: email,
            cancellationToken: cancellationToken
        );

        return user;
    }

    public async Task<UserDto> GetCurrentUserAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
        {
            // If in Development GOD mode and user not in DB yet, create owner representation
            var isOwner = string.Equals(normalizedEmail, CanonicalOwnerEmail, StringComparison.OrdinalIgnoreCase);
            return new UserDto(
                Id: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Email: normalizedEmail,
                IsOwner: isOwner,
                IsActive: true,
                Roles: [Roles.Technical, Roles.Editorial],
                CreatedAtUtc: DateTime.UtcNow
            );
        }

        return new UserDto(
            user.Id,
            user.Email,
            user.IsOwner,
            user.IsActive,
            user.Roles.Select(r => r.Role).ToList(),
            user.CreatedAtUtc
        );
    }

    public async Task<List<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .Include(u => u.Roles)
            .OrderBy(u => u.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return users.Select(u => new UserDto(
            u.Id,
            u.Email,
            u.IsOwner,
            u.IsActive,
            u.Roles.Select(r => r.Role).ToList(),
            u.CreatedAtUtc
        )).ToList();
    }

    public async Task<List<UserInvitationDto>> GetPendingInvitationsAsync(CancellationToken cancellationToken = default)
    {
        var invitations = await dbContext.UserInvitations
            .Where(i => i.Status == "Pending")
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return invitations.Select(i => new UserInvitationDto(
            i.Id,
            i.Email,
            i.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            i.Status,
            i.ExpiresAtUtc,
            i.CreatedAtUtc
        )).ToList();
    }

    public async Task<UserInvitationDto> CreateInvitationAsync(InviteUserRequest request, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) || !normalizedEmail.Contains('@'))
        {
            throw new ArgumentException("A valid Google email is required.");
        }

        var rolesJoined = string.Join(",", request.Roles.Select(r => r.Trim().ToUpperInvariant()));
        var invitation = new UserInvitation
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            Roles = rolesJoined,
            InvitedByUserId = actingUserId,
            Status = "Pending",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.UserInvitations.Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "user.invited",
            targetType: "user_invitation",
            targetId: invitation.Id.ToString(),
            detailsJson: $"{{\"email\":\"{normalizedEmail}\",\"roles\":\"{rolesJoined}\"}}",
            actorUserId: actingUserId,
            actorEmail: actingEmail,
            cancellationToken: cancellationToken
        );

        return new UserInvitationDto(
            invitation.Id,
            invitation.Email,
            request.Roles,
            invitation.Status,
            invitation.ExpiresAtUtc,
            invitation.CreatedAtUtc
        );
    }

    public async Task RevokeInvitationAsync(Guid invitationId, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.UserInvitations.FindAsync([invitationId], cancellationToken);
        if (invitation == null || invitation.Status != "Pending")
        {
            return;
        }

        invitation.Status = "Revoked";
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "user_invitation.revoked",
            targetType: "user_invitation",
            targetId: invitation.Id.ToString(),
            detailsJson: $"{{\"email\":\"{invitation.Email}\"}}",
            actorUserId: actingUserId,
            actorEmail: actingEmail,
            cancellationToken: cancellationToken
        );
    }

    public async Task UpdateUserRolesAsync(Guid targetUserId, List<string> roles, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default)
    {
        var targetUser = await dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        // SYSTEM_OWNER PROTECTION INVARIANT
        if (targetUser.IsOwner || string.Equals(targetUser.Email, CanonicalOwnerEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot modify protected SYSTEM_OWNER roles or security state.");
        }

        var existingRoles = await dbContext.UserRoles
            .Where(r => r.UserId == targetUserId)
            .ToListAsync(cancellationToken);
        dbContext.UserRoles.RemoveRange(existingRoles);

        foreach (var r in roles)
        {
            dbContext.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = targetUser.Id,
                Role = r.Trim().ToUpperInvariant(),
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "user.roles_updated",
            targetType: "user",
            targetId: targetUser.Id.ToString(),
            detailsJson: $"{{\"newRoles\":\"{string.Join(",", roles)}\"}}",
            actorUserId: actingUserId,
            actorEmail: actingEmail,
            cancellationToken: cancellationToken
        );
    }

    public async Task SetUserActiveStatusAsync(Guid targetUserId, bool isActive, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default)
    {
        var targetUser = await dbContext.Users.FindAsync([targetUserId], cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        // SYSTEM_OWNER PROTECTION INVARIANT
        if (targetUser.IsOwner || string.Equals(targetUser.Email, CanonicalOwnerEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot disable or change status of protected SYSTEM_OWNER.");
        }

        targetUser.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: isActive ? "user.enabled" : "user.disabled",
            targetType: "user",
            targetId: targetUser.Id.ToString(),
            detailsJson: $"{{\"isActive\":{isActive.ToString().ToLowerInvariant()}}}",
            actorUserId: actingUserId,
            actorEmail: actingEmail,
            cancellationToken: cancellationToken
        );
    }

    public async Task DeleteUserAsync(Guid targetUserId, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default)
    {
        var targetUser = await dbContext.Users.FindAsync([targetUserId], cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        // SYSTEM_OWNER PROTECTION INVARIANT
        if (targetUser.IsOwner || string.Equals(targetUser.Email, CanonicalOwnerEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot delete protected SYSTEM_OWNER.");
        }

        dbContext.Users.Remove(targetUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "user.deleted",
            targetType: "user",
            targetId: targetUserId.ToString(),
            detailsJson: $"{{\"email\":\"{targetUser.Email}\"}}",
            actorUserId: actingUserId,
            actorEmail: actingEmail,
            cancellationToken: cancellationToken
        );
    }
}
