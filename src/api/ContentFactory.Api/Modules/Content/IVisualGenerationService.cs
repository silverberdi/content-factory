namespace ContentFactory.Api.Modules.Content;

public record DispatchVisualGenerationResult(
    bool Success,
    List<JobDto> Jobs,
    string? BlockerReason
);

public interface IVisualGenerationService
{
    Task<DispatchVisualGenerationResult> DispatchGenerationAsync(
        Guid contentItemId,
        Guid storyboardId,
        DispatchVisualGenerationRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<VisualProductionOverviewDto?> GetProductionOverviewAsync(
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
