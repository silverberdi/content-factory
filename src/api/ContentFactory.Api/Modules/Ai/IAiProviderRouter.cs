using ContentFactory.Api.Modules.Content;

namespace ContentFactory.Api.Modules.Ai;

public static class AiCapabilities
{
    public const string BuildTruthSource = "build_truth_source";
    public const string SuggestTopics = "suggest_topics";
    public const string ScoreSource = "score_source";
    public const string GenerateIdeas = "generate_ideas";
    public const string GenerateScript = "generate_script";

    public static readonly string[] All =
    [
        BuildTruthSource,
        SuggestTopics,
        ScoreSource,
        GenerateIdeas,
        GenerateScript
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
}
