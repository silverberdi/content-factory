namespace ContentFactory.Api.Modules.Identity;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<UserRole> Roles { get; set; } = [];
}

public class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty; // TECHNICAL, EDITORIAL
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UserInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty; // e.g. "TECHNICAL,EDITORIAL"
    public Guid InvitedByUserId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Revoked, Expired
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAtUtc { get; set; }
}

public static class Roles
{
    public const string Technical = "TECHNICAL";
    public const string Editorial = "EDITORIAL";
}

public static class Capabilities
{
    public const string ChannelManage = "channel.manage";
    public const string UsersInvite = "users.invite";
    public const string UsersRolesManage = "users.roles.manage";
    public const string EditorialVideoApprove = "editorial.video.approve";
    public const string PublicationExecute = "publication.execute";
    public const string CostsView = "costs.view";
}
