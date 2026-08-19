namespace ContentFactory.Api.Modules.Content;

public static class ContentItemStage
{
    public const string DraftingEvidence = "DraftingEvidence";
    public const string TruthSourceApproved = "TruthSourceApproved";
    public const string IdeaSelected = "IdeaSelected";
    public const string ScriptDrafted = "ScriptDrafted";
    public const string ScriptUnderReview = "ScriptUnderReview";
    public const string ScriptApproved = "ScriptApproved";
    public const string InProduction = "InProduction";
    public const string Published = "Published";
    public const string Archived = "Archived";

    public static readonly string[] All =
    [
        DraftingEvidence,
        TruthSourceApproved,
        IdeaSelected,
        ScriptDrafted,
        ScriptUnderReview,
        ScriptApproved,
        InProduction,
        Published,
        Archived
    ];
}

public static class ContentItemStatus
{
    public const string Active = "Active";
    public const string Paused = "Paused";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Active, Paused, Completed, Cancelled];
}

public static class EvidenceRole
{
    public const string PrimaryLead = "PrimaryLead";
    public const string SupportingEvidence = "SupportingEvidence";
    public const string Reference = "Reference";

    public static readonly string[] All = [PrimaryLead, SupportingEvidence, Reference];
}

public static class EvidenceStatus
{
    public const string Captured = "Captured";
    public const string CaptureFailed = "CaptureFailed";
    public const string Excluded = "Excluded";

    public static readonly string[] All = [Captured, CaptureFailed, Excluded];
}

public static class TruthSourceStatus
{
    public const string Draft = "Draft";
    public const string UnderReview = "UnderReview";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly string[] All = [Draft, UnderReview, Approved, Rejected];
}

public static class EditorialTaskType
{
    public const string ReviewTruthSource = "ReviewTruthSource";
    public const string ReviewScript = "ReviewScript";
    public const string ReviewVideo = "ReviewVideo";
    public const string ApprovePublication = "ApprovePublication";

    public static readonly string[] All = [ReviewTruthSource, ReviewScript, ReviewVideo, ApprovePublication];
}

public static class EditorialTaskPriority
{
    public const string Low = "Low";
    public const string Normal = "Normal";
    public const string High = "High";
    public const string Urgent = "Urgent";

    public static readonly string[] All = [Low, Normal, High, Urgent];
}

public static class EditorialTaskStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Pending, InProgress, Completed, Cancelled];
}

public class ContentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChannelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Stage { get; set; } = ContentItemStage.DraftingEvidence;
    public string Status { get; set; } = ContentItemStatus.Active;
    public long Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByEmail { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? UpdatedByEmail { get; set; }

    public List<ContentItemEvidence> Evidences { get; set; } = [];
}

public class ContentItemEvidence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public Guid? DiscoveryCandidateId { get; set; }
    public string? OriginUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Role { get; set; } = EvidenceRole.PrimaryLead;
    public string Status { get; set; } = EvidenceStatus.Captured;
    public string? RawContent { get; set; }
    public string? ObjectStorageKey { get; set; }
    public string? ExtractedText { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? Notes { get; set; }
    public string? Author { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByEmail { get; set; } = string.Empty;
}

public class TruthSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public string Status { get; set; } = TruthSourceStatus.Draft;
    public string Summary { get; set; } = string.Empty;
    public string KeyIdeasJson { get; set; } = "[]";
    public string VerifiableClaimsJson { get; set; } = "[]";
    public string EvidenceReferencesJson { get; set; } = "[]";
    public string RiskNotes { get; set; } = string.Empty;
    public string DoNotSayConstraintsJson { get; set; } = "[]";
    public string PossibleAnglesJson { get; set; } = "[]";
    public string LocalizationNotes { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectedByEmail { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedByEmail { get; set; }
    public long Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByEmail { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? UpdatedByEmail { get; set; }
}

public class TruthSourceVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TruthSourceId { get; set; }
    public Guid ContentItemId { get; set; }
    public long VersionNumber { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string SupportingEvidenceIdsJson { get; set; } = "[]";
    public string? ChangeSummary { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByEmail { get; set; } = string.Empty;
}

public class EditorialTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChannelId { get; set; }
    public Guid ContentItemId { get; set; }
    public string TaskType { get; set; } = EditorialTaskType.ReviewTruthSource;
    public string Priority { get; set; } = EditorialTaskPriority.Normal;
    public string Status { get; set; } = EditorialTaskStatus.Pending;
    public string? AssignedUserEmail { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletedByEmail { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByEmail { get; set; } = string.Empty;
}

public class AiRecommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChannelId { get; set; }
    public Guid? ContentItemId { get; set; }
    public Guid? TruthSourceVersionId { get; set; }
    public string Capability { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PromptPolicyVersion { get; set; } = "1.0";
    public string StructuredOutputJson { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public string? Rationale { get; set; }
    public long LatencyMs { get; set; }
    public int TokensIn { get; set; }
    public int TokensOut { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public string AcceptedState { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

// DTOs & Records
public record VerifiableClaimDto(
    string Claim,
    string? SourceCitation,
    Guid? EvidenceId
);

public record ContentItemDto(
    Guid Id,
    Guid ChannelId,
    string? ChannelName,
    string Title,
    string Slug,
    string Stage,
    string Status,
    long Version,
    int EvidenceCount,
    string? TruthSourceStatus,
    long? TruthSourceVersion,
    DateTime CreatedAtUtc,
    string CreatedByEmail,
    DateTime UpdatedAtUtc,
    string? UpdatedByEmail
);

public record ContentItemDetailDto(
    Guid Id,
    Guid ChannelId,
    string? ChannelName,
    string Title,
    string Slug,
    string Stage,
    string Status,
    long Version,
    DateTime CreatedAtUtc,
    string CreatedByEmail,
    DateTime UpdatedAtUtc,
    string? UpdatedByEmail,
    List<ContentItemEvidenceDto> Evidences,
    TruthSourceDto? TruthSource
);

public record ContentItemEvidenceDto(
    Guid Id,
    Guid ContentItemId,
    Guid? DiscoveryCandidateId,
    string? OriginUrl,
    string Title,
    string Role,
    string Status,
    string? RawContent,
    string? ObjectStorageKey,
    string? ExtractedText,
    string ContentHash,
    string? ErrorMessage,
    string? Notes,
    string? Author,
    DateTime CapturedAtUtc,
    DateTime CreatedAtUtc,
    string CreatedByEmail
);

public record TruthSourceDto(
    Guid Id,
    Guid ContentItemId,
    string Status,
    string Summary,
    List<string> KeyIdeas,
    List<VerifiableClaimDto> VerifiableClaims,
    List<Guid> EvidenceReferences,
    string RiskNotes,
    List<string> DoNotSayConstraints,
    List<string> PossibleAngles,
    string LocalizationNotes,
    string? RejectionReason,
    DateTime? RejectedAtUtc,
    string? RejectedByEmail,
    DateTime? ApprovedAtUtc,
    string? ApprovedByEmail,
    long Version,
    DateTime CreatedAtUtc,
    string CreatedByEmail,
    DateTime UpdatedAtUtc,
    string? UpdatedByEmail
);

public record TruthSourceVersionDto(
    Guid Id,
    Guid TruthSourceId,
    Guid ContentItemId,
    long VersionNumber,
    string SnapshotJson,
    List<Guid> SupportingEvidenceIds,
    string? ChangeSummary,
    DateTime CreatedAtUtc,
    string CreatedByEmail
);

public record EditorialTaskDto(
    Guid Id,
    Guid ChannelId,
    string? ChannelName,
    Guid ContentItemId,
    string? ContentItemTitle,
    string TaskType,
    string Priority,
    string Status,
    string? AssignedUserEmail,
    DateTime? DueDateUtc,
    DateTime? CompletedAtUtc,
    string? CompletedByEmail,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string CreatedByEmail
);

public record CreateContentItemRequest(
    Guid ChannelId,
    string Title
);

public record UpdateContentItemRequest(
    string Title,
    string? Status,
    long ExpectedVersion
);

public record AttachEvidenceRequest(
    Guid? DiscoveryCandidateId,
    string? OriginUrl,
    string Title,
    string? ContentText,
    string? Role,
    string? Notes
);

public record SaveTruthSourceRequest(
    string Summary,
    List<string> KeyIdeas,
    List<VerifiableClaimDto> VerifiableClaims,
    List<Guid> EvidenceReferences,
    string RiskNotes,
    List<string> DoNotSayConstraints,
    List<string> PossibleAngles,
    string LocalizationNotes,
    string? ChangeSummary,
    long ExpectedVersion
);

public record RejectTruthSourceRequest(
    string Reason
);

public record AssignEditorialTaskRequest(
    string? AssignedUserEmail,
    string? Priority,
    DateTime? DueDateUtc
);

public record InitiateContentFromCandidateRequest(
    string? TitleOverride
);

public record AttachCandidateToContentRequest(
    Guid ContentItemId,
    string? Role,
    string? Notes
);

public static class ContentIdeaStatus
{
    public const string Proposed = "Proposed";
    public const string Selected = "Selected";
    public const string Dismissed = "Dismissed";

    public static readonly string[] All = [Proposed, Selected, Dismissed];
}

public static class IdeaFreshnessClass
{
    public const string Breaking = "Breaking";
    public const string Timely = "Timely";
    public const string Evergreen = "Evergreen";

    public static readonly string[] All = [Breaking, Timely, Evergreen];
}

public static class IdeaPriority
{
    public const string Low = "Low";
    public const string Normal = "Normal";
    public const string High = "High";
    public const string Urgent = "Urgent";

    public static readonly string[] All = [Low, Normal, High, Urgent];
}

public class ContentIdea
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public Guid TruthSourceId { get; set; }
    public Guid TruthSourceVersionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Angle { get; set; } = string.Empty;
    public string HookStrategy { get; set; } = string.Empty;
    public string AudienceValue { get; set; } = string.Empty;
    public string Format { get; set; } = "YouTube Short 30-60s";
    public string IntendedOutcome { get; set; } = "Educational";
    public string FreshnessClass { get; set; } = IdeaFreshnessClass.Timely;
    public string Priority { get; set; } = IdeaPriority.Normal;
    public string Rationale { get; set; } = string.Empty;
    public string Status { get; set; } = ContentIdeaStatus.Proposed;
    public string? DismissalNotes { get; set; }
    public DateTime? SelectedAtUtc { get; set; }
    public string? SelectedByEmail { get; set; }
    public long Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByEmail { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? UpdatedByEmail { get; set; }
}

public class ContentIdeaVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentIdeaId { get; set; }
    public Guid ContentItemId { get; set; }
    public Guid TruthSourceId { get; set; }
    public Guid TruthSourceVersionId { get; set; }
    public long VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Angle { get; set; } = string.Empty;
    public string HookStrategy { get; set; } = string.Empty;
    public string AudienceValue { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string IntendedOutcome { get; set; } = string.Empty;
    public string FreshnessClass { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? DismissalNotes { get; set; }
    public string EditedByEmail { get; set; } = string.Empty;
    public DateTime EditedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ChangeSummary { get; set; }
}

public record ContentIdeaDto(
    Guid Id,
    Guid ContentItemId,
    Guid TruthSourceId,
    Guid TruthSourceVersionId,
    string Title,
    string Angle,
    string HookStrategy,
    string AudienceValue,
    string Format,
    string IntendedOutcome,
    string FreshnessClass,
    string Priority,
    string Rationale,
    string Status,
    string? DismissalNotes,
    DateTime? SelectedAtUtc,
    string? SelectedByEmail,
    long Version,
    DateTime CreatedAtUtc,
    string CreatedByEmail,
    DateTime UpdatedAtUtc,
    string? UpdatedByEmail
);

public record ContentIdeaVersionDto(
    Guid Id,
    Guid ContentIdeaId,
    Guid ContentItemId,
    Guid TruthSourceId,
    Guid TruthSourceVersionId,
    long VersionNumber,
    string Title,
    string Angle,
    string HookStrategy,
    string AudienceValue,
    string Format,
    string IntendedOutcome,
    string FreshnessClass,
    string Priority,
    string Rationale,
    string Status,
    string? DismissalNotes,
    string EditedByEmail,
    DateTime EditedAtUtc,
    string? ChangeSummary
);

public record CreateIdeaRequest(
    string Title,
    string Angle,
    string HookStrategy,
    string? AudienceValue,
    string? Format,
    string? IntendedOutcome,
    string? FreshnessClass,
    string? Priority,
    string? Rationale
);

public record UpdateIdeaRequest(
    string Title,
    string Angle,
    string HookStrategy,
    string? AudienceValue,
    string? Format,
    string? IntendedOutcome,
    string? FreshnessClass,
    string? Priority,
    string? Rationale,
    string? ChangeSummary,
    long ExpectedVersion
);

public record SelectIdeaRequest(
    long ExpectedVersion
);

public record DismissIdeaRequest(
    string? Notes,
    long ExpectedVersion
);

public record ReopenIdeaRequest(
    long ExpectedVersion
);

public record GenerateIdeasOptions(
    int Count = 3,
    string? TargetAudience = null,
    string? FocusAngleStyle = null
);

public record GeneratedIdeaItem(
    string Title,
    string Angle,
    string HookStrategy,
    string AudienceValue,
    string Format,
    string IntendedOutcome,
    string FreshnessClass,
    string Priority,
    string Rationale
);

public static class ScriptStatus
{
    public const string Draft = "Draft";
    public const string UnderReview = "UnderReview";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly string[] All = [Draft, UnderReview, Approved, Rejected];
}

public static class SceneType
{
    public const string Hook = "Hook";
    public const string Problem = "Problem";
    public const string Insight = "Insight";
    public const string Climax = "Climax";
    public const string CallToAction = "CallToAction";

    public static readonly string[] All = [Hook, Problem, Insight, Climax, CallToAction];
}

public class Script
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid ContentIdeaId { get; set; }
    public Guid ContentIdeaVersionId { get; set; }
    public Guid TruthSourceId { get; set; }
    public Guid TruthSourceVersionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TargetDurationSeconds { get; set; } = 45;
    public int PacingWpm { get; set; } = 140;
    public double EstimatedDurationSeconds { get; set; }
    public int TotalWordCount { get; set; }
    public string Language { get; set; } = "es-ES";
    public string Status { get; set; } = ScriptStatus.Draft;
    public string? RejectionReason { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectedByEmail { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedByEmail { get; set; }
    public DateTime? SubmittedForReviewAtUtc { get; set; }
    public string? SubmittedForReviewByEmail { get; set; }
    public long Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByEmail { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? UpdatedByEmail { get; set; }

    public List<ScriptScene> Scenes { get; set; } = [];
}

public class ScriptScene
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScriptId { get; set; }
    public int OrderIndex { get; set; }
    public string SceneType { get; set; } = ContentFactory.Api.Modules.Content.SceneType.Hook;
    public string NarrationText { get; set; } = string.Empty;
    public string VisualPrompt { get; set; } = string.Empty;
    public double EstimatedDurationSeconds { get; set; }
    public int WordCount { get; set; }

    public List<ScriptSceneEvidenceReference> EvidenceReferences { get; set; } = [];
}

public class ScriptSceneEvidenceReference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScriptSceneId { get; set; }
    public Guid? TruthSourceClaimId { get; set; }
    public string ClaimStatement { get; set; } = string.Empty;
    public string? EditorialNote { get; set; }
}

public class ScriptVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScriptId { get; set; }
    public Guid ContentItemId { get; set; }
    public Guid ContentIdeaId { get; set; }
    public Guid ContentIdeaVersionId { get; set; }
    public Guid TruthSourceId { get; set; }
    public Guid TruthSourceVersionId { get; set; }
    public long VersionNumber { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string? ChangeSummary { get; set; }
    public string Status { get; set; } = ScriptStatus.Draft;
    public string? RejectionReason { get; set; }
    public int PacingWpm { get; set; } = 140;
    public double EstimatedDurationSeconds { get; set; }
    public int TotalWordCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByEmail { get; set; } = string.Empty;
}

public record ScriptSceneEvidenceReferenceDto(
    Guid Id,
    Guid ScriptSceneId,
    Guid? TruthSourceClaimId,
    string ClaimStatement,
    string? EditorialNote
);

public record ScriptSceneDto(
    Guid Id,
    Guid ScriptId,
    int OrderIndex,
    string SceneType,
    string NarrationText,
    string VisualPrompt,
    double EstimatedDurationSeconds,
    int WordCount,
    List<ScriptSceneEvidenceReferenceDto> EvidenceReferences
);

public record ScriptDto(
    Guid Id,
    Guid ContentItemId,
    Guid ChannelId,
    Guid ContentIdeaId,
    Guid ContentIdeaVersionId,
    Guid TruthSourceId,
    Guid TruthSourceVersionId,
    string Title,
    int TargetDurationSeconds,
    int PacingWpm,
    double EstimatedDurationSeconds,
    int TotalWordCount,
    string Language,
    string Status,
    string? RejectionReason,
    DateTime? RejectedAtUtc,
    string? RejectedByEmail,
    DateTime? ApprovedAtUtc,
    string? ApprovedByEmail,
    DateTime? SubmittedForReviewAtUtc,
    string? SubmittedForReviewByEmail,
    bool IsStale,
    string? StaleReason,
    long Version,
    DateTime CreatedAtUtc,
    string CreatedByEmail,
    DateTime UpdatedAtUtc,
    string? UpdatedByEmail,
    List<ScriptSceneDto> Scenes
);

public record ScriptVersionDto(
    Guid Id,
    Guid ScriptId,
    Guid ContentItemId,
    Guid ContentIdeaId,
    Guid ContentIdeaVersionId,
    Guid TruthSourceId,
    Guid TruthSourceVersionId,
    long VersionNumber,
    string SnapshotJson,
    string? ChangeSummary,
    string Status,
    string? RejectionReason,
    int PacingWpm,
    double EstimatedDurationSeconds,
    int TotalWordCount,
    DateTime CreatedAtUtc,
    string CreatedByEmail
);

public record SaveScriptSceneRequest(
    Guid? Id,
    int OrderIndex,
    string SceneType,
    string NarrationText,
    string VisualPrompt,
    List<ScriptSceneEvidenceReferenceDto>? EvidenceReferences
);

public record CreateScriptRequest(
    string Title,
    int? TargetDurationSeconds,
    int? PacingWpm,
    string? Language,
    List<SaveScriptSceneRequest>? Scenes
);

public record UpdateScriptRequest(
    string Title,
    int? TargetDurationSeconds,
    int? PacingWpm,
    string? Language,
    List<SaveScriptSceneRequest> Scenes,
    string? ChangeSummary,
    long ExpectedVersion
);

public record SubmitScriptForReviewRequest(
    long ExpectedVersion
);

public record ApproveScriptRequest(
    long ExpectedVersion
);

public record RejectScriptRequest(
    string Reason,
    long ExpectedVersion
);

public record ReopenScriptRequest(
    long ExpectedVersion
);

public record GenerateScriptOptions(
    string? CustomInstructions = null,
    string? ToneStyle = null,
    int? TargetDurationSeconds = null,
    int? PacingWpm = null
);

public record ScriptSceneCritiqueDto(
    int OrderIndex,
    string SceneType,
    string Status, // Pass, Warning, Critical
    string? ClaimFidelityNotes,
    string? RetentionNotes,
    string? PacingNotes,
    List<string> Suggestions
);

public record ScriptReviewDimensionDto(
    string Dimension,
    string Status, // Pass, Warning, Critical
    string Notes
);

public record ScriptReviewResultDto(
    string OverallStatus, // Pass, Warning, Critical
    double FactualAlignmentScore,
    string RetentionAnalysis,
    string PacingAssessment,
    List<string> DoNotSayComplianceNotes,
    List<ScriptReviewDimensionDto> Dimensions,
    List<ScriptSceneCritiqueDto> SceneCritiques,
    List<string> ActionableRecommendations
);

public record GeneratedScriptSceneItem(
    int OrderIndex,
    string SceneType,
    string NarrationText,
    string VisualPrompt,
    List<ScriptSceneEvidenceReferenceDto>? EvidenceReferences = null
);

public record GeneratedScriptResult(
    string Title,
    int TargetDurationSeconds,
    int PacingWpm,
    string Language,
    List<GeneratedScriptSceneItem> Scenes
);

