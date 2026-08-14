namespace ContentFactory.Api.Modules.Discovery;

public static class SourceType
{
    public const string Feed = "Feed";
    public const string Web = "Web";
    public const string Podcast = "Podcast";
    public const string Curated = "Curated";
    public const string Manual = "Manual";
    public const string ProviderApi = "ProviderApi";

    public static readonly string[] All = [Feed, Web, Podcast, Curated, Manual, ProviderApi];
}

public static class DiscoverySourceStatus
{
    public const string Active = "Active";
    public const string Paused = "Paused";
    public const string Error = "Error";

    public static readonly string[] All = [Active, Paused, Error];
}

public static class DiscoveryCandidateStatus
{
    public const string PendingReview = "PendingReview";
    public const string Promoted = "Promoted";
    public const string Dismissed = "Dismissed";

    public static readonly string[] All = [PendingReview, Promoted, Dismissed];
}

public static class OriginType
{
    public const string Automated = "Automated";
    public const string Manual = "Manual";

    public static readonly string[] All = [Automated, Manual];
}

public class DiscoverySource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChannelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OriginUrl { get; set; } = string.Empty;
    public string SourceType { get; set; } = Modules.Discovery.SourceType.Feed;
    public string Language { get; set; } = "es";
    public int PollingIntervalMinutes { get; set; } = 60;
    public string Status { get; set; } = DiscoverySourceStatus.Active;
    public DateTime? LastSyncAtUtc { get; set; }
    public DateTime? NextSyncAtUtc { get; set; }
    public int FailureCount { get; set; }
    public string? LastErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DiscoveryCandidate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChannelId { get; set; }
    public Guid? DiscoverySourceId { get; set; }
    public string? ExternalUrl { get; set; }
    public string? NormalizedUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? RawContent { get; set; }
    public string Language { get; set; } = "es";
    public string? Author { get; set; }
    public DateTime DiscoveredAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = DiscoveryCandidateStatus.PendingReview;
    public string OriginType { get; set; } = Modules.Discovery.OriginType.Automated;
    public string? SubmitterEmail { get; set; }
    public string? DismissalReason { get; set; }
    public string? EditorialNotes { get; set; }
    public DateTime? PromotedAtUtc { get; set; }
    public string? PromotedByEmail { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public record DiscoverySourceDto(
    Guid Id,
    Guid ChannelId,
    string? ChannelName,
    string Name,
    string OriginUrl,
    string SourceType,
    string Language,
    int PollingIntervalMinutes,
    string Status,
    DateTime? LastSyncAtUtc,
    DateTime? NextSyncAtUtc,
    int FailureCount,
    string? LastErrorMessage,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record CreateDiscoverySourceRequest(
    Guid ChannelId,
    string Name,
    string OriginUrl,
    string? SourceType,
    string? Language,
    int? PollingIntervalMinutes
);

public record UpdateDiscoverySourceRequest(
    string Name,
    string OriginUrl,
    string? SourceType,
    string? Language,
    int? PollingIntervalMinutes,
    string? Status
);

public record DiscoveryCandidateDto(
    Guid Id,
    Guid ChannelId,
    string? ChannelName,
    Guid? DiscoverySourceId,
    string? SourceName,
    string? ExternalUrl,
    string? NormalizedUrl,
    string Title,
    string? Summary,
    string? RawContent,
    string Language,
    string? Author,
    DateTime DiscoveredAtUtc,
    string Status,
    string OriginType,
    string? SubmitterEmail,
    string? DismissalReason,
    string? EditorialNotes,
    DateTime? PromotedAtUtc,
    string? PromotedByEmail,
    DateTime CreatedAtUtc
);

public record QuickSubmitCandidateRequest(
    Guid ChannelId,
    string? ExternalUrl,
    string Title,
    string? Summary,
    string? Language
);

public record TriageCandidateRequest(
    string Status,
    string? DismissalReason,
    string? EditorialNotes
);

public record DiscoverySummaryDto(
    int PendingCandidatesCount,
    int PromotedCandidatesCount,
    int DismissedCandidatesCount,
    int ActiveSourcesCount,
    int PausedSourcesCount,
    int ErrorSourcesCount
);
