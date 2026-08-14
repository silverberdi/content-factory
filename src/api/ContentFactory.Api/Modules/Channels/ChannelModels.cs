namespace ContentFactory.Api.Modules.Channels;

public class Channel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
    public string Niche { get; set; } = string.Empty;
    public string Status { get; set; } = ChannelStatus.Pilot;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class ChannelStatus
{
    public const string Idea = "idea";
    public const string SetupPending = "setup-pending";
    public const string Pilot = "pilot";
    public const string Active = "active";
    public const string Scaling = "scaling";
    public const string Paused = "paused";
    public const string Archived = "archived";

    public static readonly HashSet<string> All =
    [
        Idea,
        SetupPending,
        Pilot,
        Active,
        Scaling,
        Paused,
        Archived
    ];
}

public record CreateChannelRequest(string Name, string Slug, string Language, string Niche, string? Status);
public record UpdateChannelRequest(string Name, string Language, string Niche, string Status);
public record ChannelDto(Guid Id, string Slug, string Name, string Language, string Niche, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
