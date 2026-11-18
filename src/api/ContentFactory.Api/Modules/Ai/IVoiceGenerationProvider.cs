namespace ContentFactory.Api.Modules.Ai;

public record VoiceProfile(
    string ProfileKey,
    string ProfileName,
    string Language,
    string? ConfiguredLocale,
    string SpeakingStyle,
    string Tone,
    string Warmth,
    string Energy,
    string Pace,
    Dictionary<string, string>? PronunciationHints = null
);

public record VoiceGenerationRequest(
    Guid JobId,
    string CorrelationId,
    Guid ContentItemId,
    Guid ChannelId,
    Guid ScriptId,
    Guid ScriptVersionId,
    Guid StoryboardId,
    Guid StoryboardVersionId,
    Guid AssetRequirementId,
    string Language,
    string? ConfiguredLocale,
    string NarrationText,
    VoiceProfile VoiceProfile,
    string? PronunciationGuidance,
    int CandidateCount,
    string TargetAudioFormat = "audio/wav",
    string? ChannelSlug = null
);

public record VoiceGeneratedMediaOutput(
    int VariantIndex,
    byte[] AudioBytes,
    string ContentType,
    string FileExtension,
    double DurationSeconds,
    int SampleRateHz,
    int Channels,
    int BitDepth,
    string ProviderModel,
    string GenerationParametersSnapshot
);

public record VoiceGenerationResult(
    bool Success,
    List<VoiceGeneratedMediaOutput> Outputs,
    string? ErrorCode,
    string? SanitizedErrorMessage,
    bool IsRetryable,
    long ExecutionDurationMs,
    decimal? EstimatedCostUsd,
    decimal? ActualCostUsd
);

public interface IVoiceGenerationProvider
{
    string ProviderName { get; }
    string ModelOrWorkflowIdentifier { get; }
    IReadOnlyList<string> SupportedAudioFormats { get; }
    Task<VoiceGenerationResult> GenerateVoiceAssetAsync(
        VoiceGenerationRequest request,
        CancellationToken cancellationToken = default);
}
