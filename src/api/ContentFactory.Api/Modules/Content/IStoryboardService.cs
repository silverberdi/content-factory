namespace ContentFactory.Api.Modules.Content;

public interface IStoryboardService
{
    Task<StoryboardDto?> GetStoryboardByContentItemIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default);

    Task<StoryboardDto?> GetStoryboardByIdAsync(
        Guid contentItemId,
        Guid storyboardId,
        CancellationToken cancellationToken = default);

    Task<List<StoryboardVersionDto>> GetStoryboardVersionsAsync(
        Guid contentItemId,
        Guid storyboardId,
        CancellationToken cancellationToken = default);

    Task<StoryboardVersionDto?> GetStoryboardVersionAsync(
        Guid contentItemId,
        Guid storyboardId,
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<StoryboardDto> CreateStoryboardAsync(
        Guid contentItemId,
        CreateStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<StoryboardDto> UpdateStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        UpdateStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<StoryboardDto> GenerateAiStoryboardAsync(
        Guid contentItemId,
        GenerateStoryboardOptions? options,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<StoryboardReviewResultDto> ReviewStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<StoryboardDto> SubmitForReviewAsync(
        Guid contentItemId,
        Guid storyboardId,
        SubmitStoryboardForReviewRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<StoryboardDto> ApproveStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        ApproveStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<StoryboardDto> RejectStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        RejectStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<StoryboardDto> ReopenStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        ReopenStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<StoryboardDto> ReconcileStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        ReconcileStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ProductionEligibilityDto> CheckProductionEligibilityAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default);
}
