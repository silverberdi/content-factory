namespace ContentFactory.Api.Modules.Ai;

public record VisualGenerationRequest(
    Guid JobId,
    string CorrelationId,
    Guid ContentItemId,
    Guid ChannelId,
    Guid StoryboardId,
    Guid StoryboardVersionId,
    Guid AssetRequirementId,
    string AssetType,
    string AspectRatio,
    int TargetWidth,
    int TargetHeight,
    double? TargetDurationSeconds,
    string VisualPrompt,
    string NegativePrompt,
    string StyleIntent,
    string MotionIntent,
    int CandidateCount,
    string? ChannelSlug = null
);

public record VisualGeneratedMediaOutput(
    int VariantIndex,
    byte[] MediaBytes,
    string ContentType,
    string FileExtension,
    int Width,
    int Height,
    double? DurationSeconds,
    string ProviderModelOrWorkflow,
    string GenerationParametersSnapshot
);

public record VisualGenerationResult(
    bool Success,
    List<VisualGeneratedMediaOutput> Outputs,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsRetryable,
    long ExecutionDurationMs,
    decimal? EstimatedCostUsd,
    decimal? ActualCostUsd
);

public interface IVisualGenerationProvider
{
    string ProviderName { get; }
    IReadOnlyList<string> SupportedAssetTypes { get; }
    Task<VisualGenerationResult> GenerateVisualAssetAsync(
        VisualGenerationRequest request,
        CancellationToken cancellationToken = default);
}
