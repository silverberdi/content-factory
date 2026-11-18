namespace ContentFactory.Api.Modules.Content;

public record DispatchVoiceGenerationResult(
    bool Success,
    List<JobDto> Jobs,
    string? BlockerReason
);

public interface IVoiceGenerationService
{
    Task<DispatchVoiceGenerationResult> DispatchGenerationAsync(
        Guid contentItemId,
        Guid storyboardId,
        DispatchVoiceGenerationRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<VoiceProductionOverviewDto?> GetProductionOverviewAsync(
        Guid contentItemId,
        Guid storyboardId,
        CancellationToken cancellationToken = default);

    Task<GeneratedAssetDto?> ReviewCandidateAsync(
        Guid generatedAssetId,
        ReviewGeneratedAssetRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<GeneratedAssetDto?> SelectCandidateForAssemblyAsync(
        Guid generatedAssetId,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<JobDto?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<JobDto?> RetryJobAsync(
        Guid jobId,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<Job?> TryClaimNextJobAsync(
        CancellationToken cancellationToken = default);

    Task ProcessQueuedJobsAsync(
        CancellationToken cancellationToken = default);
}
