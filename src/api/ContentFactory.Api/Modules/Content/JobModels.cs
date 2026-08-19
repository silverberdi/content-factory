namespace ContentFactory.Api.Modules.Content;

public static class JobStatus
{
    public const string Queued = "Queued";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string FailedRetryable = "FailedRetryable";
    public const string FailedActionRequired = "FailedActionRequired";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Queued, Running, Succeeded, FailedRetryable, FailedActionRequired, Cancelled];
}

public static class JobType
{
    public const string GenerateVisualAsset = "generate_visual_asset";

    public static readonly string[] All = [GenerateVisualAsset];
}

public static class JobTypes
{
    public const string GenerateVisualAsset = "generate_visual_asset";

    public static readonly string[] All = [GenerateVisualAsset];
}

public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public Guid ChannelId { get; set; }
    public string JobType { get; set; } = JobTypes.GenerateVisualAsset;
    public string Capability { get => JobType; set => JobType = value; }
    public Guid? SourceAssetRequirementId { get; set; }
    public Guid? StoryboardId { get; set; }
    public Guid? StoryboardVersionId { get; set; }
    public int GenerationRevision { get; set; } = 1;
    public string Status { get; set; } = JobStatus.Queued;
    public string Provider { get; set; } = string.Empty;
    public string ModelOrWorkflowIdentifier { get; set; } = string.Empty;
    public int AttemptCount { get; set; } = 1;
    public int MaxAttempts { get; set; } = 3;
    public int AutomaticRetriesRemaining { get; set; } = 2;
    public int CandidateCount { get; set; } = 1;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long DurationMs { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public decimal? ActualCostUsd { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public string? ErrorCode { get; set; }
    public string? SanitizedErrorMessage { get; set; }
    public bool IsRetryable { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<JobAttempt> Attempts { get; set; } = [];
}

public class JobAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public long DurationMs { get; set; }
    public string Status { get; set; } = JobStatus.Running;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProviderResponseSummary { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public decimal? ActualCostUsd { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public record JobDto(
    Guid Id,
    Guid ContentItemId,
    Guid ChannelId,
    string JobType,
    string Capability,
    Guid? SourceAssetRequirementId,
    Guid? StoryboardId,
    Guid? StoryboardVersionId,
    int GenerationRevision,
    string Status,
    string Provider,
    string ModelOrWorkflowIdentifier,
    int AttemptCount,
    int MaxAttempts,
    int AutomaticRetriesRemaining,
    int CandidateCount,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    long DurationMs,
    decimal? EstimatedCostUsd,
    decimal? ActualCostUsd,
    string CorrelationId,
    string? ErrorCode,
    string? SanitizedErrorMessage,
    bool IsRetryable,
    string CreatedByEmail,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<JobAttemptDto> Attempts
);

public record JobAttemptDto(
    Guid Id,
    Guid JobId,
    int AttemptNumber,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    long DurationMs,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    decimal? EstimatedCostUsd,
    decimal? ActualCostUsd
);
