using ContentFactory.Api.Modules.Content;

namespace ContentFactory.Api.Modules.Ai;

public static class AiCapabilities
{
    public const string BuildTruthSource = "build_truth_source";
    public const string SuggestTopics = "suggest_topics";
    public const string ScoreSource = "score_source";
    public const string GenerateIdeas = "generate_ideas";
    public const string GenerateScript = "generate_script";
    public const string ReviewScript = "review_script";

    public static readonly string[] All =
    [
        BuildTruthSource,
        SuggestTopics,
        ScoreSource,
        GenerateIdeas,
        GenerateScript,
        ReviewScript
    ];
}

public static class AiProviders
{
    public const string DeepSeek = "DeepSeek";
    public const string Gemini = "Gemini";
    public const string Mock = "Mock";

    public static readonly string[] All = [DeepSeek, Gemini, Mock];
}

public record AiRoutingContext(
    Guid ChannelId,
    Guid? ContentItemId,
    string? PreferredProvider = null,
    string? PreferredModel = null
);

public record AiCapabilityResult<TResponse>(
    bool Success,
    TResponse? Data,
    AiRecommendation? Recommendation,
    string? ErrorMessage
);

public record BuildTruthSourceRequest(
    string ChannelName,
    string ChannelLanguage,
    string ChannelNiche,
    List<EvidenceSnippetDto> Evidences
);

public record EvidenceSnippetDto(
    Guid EvidenceId,
    string Title,
    string? OriginUrl,
    string Role,
    string ExtractedText
);

public record BuildTruthSourceResponse(
    string Summary,
    List<string> KeyIdeas,
    List<VerifiableClaimDto> VerifiableClaims,
    List<Guid> EvidenceReferences,
    string RiskNotes,
    List<string> DoNotSayConstraints,
    List<string> PossibleAngles,
    string LocalizationNotes,
    string ConciseRationale
);

public record GenerateIdeasRequest(
    Guid ChannelId,
    string ChannelName,
    string ChannelLanguage,
    string ChannelNiche,
    Guid TruthSourceId,
    Guid TruthSourceVersionId,
    string Summary,
    List<string> KeyIdeas,
    List<VerifiableClaimDto> VerifiableClaims,
    List<string> DoNotSayConstraints,
    List<string> PossibleAngles,
    int Count = 3,
    string? TargetAudience = null,
    string? FocusAngleStyle = null
);

public record GenerateIdeasResponse(
    List<GeneratedIdeaItem> Ideas,
    string ConciseRationale
);

public record GenerateScriptRequest(
    Guid ChannelId,
    string ChannelName,
    string ChannelLanguage,
    string ChannelNiche,
    Guid TruthSourceId,
    Guid TruthSourceVersionId,
    Guid ContentIdeaId,
    Guid ContentIdeaVersionId,
    string IdeaTitle,
    string IdeaAngle,
    string IdeaHookStrategy,
    string IdeaAudienceValue,
    string IdeaFormat,
    string IdeaIntendedOutcome,
    string Summary,
    List<string> KeyIdeas,
    List<VerifiableClaimDto> VerifiableClaims,
    List<string> DoNotSayConstraints,
    int TargetDurationSeconds = 45,
    int PacingWpm = 140,
    string? CustomInstructions = null,
    string? ToneStyle = null
);

public record GenerateScriptResponse(
    GeneratedScriptResult Script,
    string ConciseRationale
);

public record ReviewScriptRequest(
    Guid ChannelId,
    string ChannelName,
    string ChannelLanguage,
    Guid TruthSourceId,
    Guid TruthSourceVersionId,
    string TruthSourceSummary,
    List<string> KeyIdeas,
    List<VerifiableClaimDto> VerifiableClaims,
    List<string> DoNotSayConstraints,
    string ScriptTitle,
    int TargetDurationSeconds,
    int PacingWpm,
    List<ScriptSceneDto> Scenes
);

public record ReviewScriptResponse(
    ScriptReviewResultDto ReviewResult,
    string ConciseRationale
);

public interface IAiProviderRouter
{
    Task<AiCapabilityResult<BuildTruthSourceResponse>> BuildTruthSourceAsync(
        BuildTruthSourceRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default);

    Task<AiCapabilityResult<GenerateIdeasResponse>> GenerateIdeasAsync(
        GenerateIdeasRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default);

    Task<AiCapabilityResult<GenerateScriptResponse>> GenerateScriptAsync(
        GenerateScriptRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default);

    Task<AiCapabilityResult<ReviewScriptResponse>> ReviewScriptAsync(
        ReviewScriptRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default);
}
