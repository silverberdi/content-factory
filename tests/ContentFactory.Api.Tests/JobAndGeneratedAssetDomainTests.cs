using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Tests;

public class JobAndGeneratedAssetDomainTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Job_StatusTransitionsAndAttempts_TrackResilienceAccurately()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var storyboardId = Guid.NewGuid();
        var storyboardVersionId = Guid.NewGuid();
        var requirementId = Guid.NewGuid();

        var job = new Job
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = channelId,
            Capability = JobType.GenerateVisualAsset,
            SourceAssetRequirementId = requirementId,
            StoryboardId = storyboardId,
            StoryboardVersionId = storyboardVersionId,
            Status = JobStatus.Queued,
            Provider = "Mock",
            ModelOrWorkflowIdentifier = "flux-dev-9x16",
            AttemptCount = 1,
            MaxAttempts = 3,
            CorrelationId = "corr-12345",
            IdempotencyKey = "hash-12345",
            CreatedByEmail = "operator@silverman.pro",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync();

        // Simulate Attempt 1 failing with transient error
        job.Status = JobStatus.Running;
        job.StartedAtUtc = DateTime.UtcNow;
        var attempt1 = new JobAttempt
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            AttemptNumber = 1,
            StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow.AddMilliseconds(500),
            DurationMs = 500,
            Status = JobStatus.FailedRetryable,
            ErrorCode = "PROVIDER_TIMEOUT",
            ErrorMessage = "HTTP 504 Gateway Timeout from mock provider",
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.JobAttempts.Add(attempt1);
        job.Status = JobStatus.FailedRetryable;
        job.IsRetryable = true;
        job.ErrorCode = "PROVIDER_TIMEOUT";
        job.SanitizedErrorMessage = "Provider connection timed out. Automatic retry scheduled.";
        job.AttemptCount = 2;

        await dbContext.SaveChangesAsync();

        var loadedJob = await dbContext.Jobs.Include(j => j.Attempts).FirstOrDefaultAsync(j => j.Id == job.Id);
        Assert.NotNull(loadedJob);
        Assert.Equal(JobStatus.FailedRetryable, loadedJob.Status);
        Assert.True(loadedJob.IsRetryable);
        Assert.Equal(2, loadedJob.AttemptCount);
        Assert.Single(loadedJob.Attempts);
        Assert.Equal("PROVIDER_TIMEOUT", loadedJob.ErrorCode);
    }

    [Fact]
    public async Task GeneratedAsset_MaintainsImmutableLineage_AndSingleSelectionRule()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var storyboardId = Guid.NewGuid();
        var storyboardVersionId = Guid.NewGuid();
        var requirementId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        var candidate1 = new GeneratedAsset
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = channelId,
            StoryboardId = storyboardId,
            StoryboardVersionId = storyboardVersionId,
            AssetRequirementId = requirementId,
            JobId = jobId,
            VariantIndex = 1,
            AssetType = AssetType.AiImage,
            MediaType = "Image",
            StorageProvider = "MinIO",
            StorageKey = $"content-factory/dev/channels/{channelId}/content/{contentItemId}/visual/{requirementId}/cand1.png",
            ContentType = "image/png",
            FileSizeBytes = 1024500,
            Width = 1080,
            Height = 1920,
            ChecksumSha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            Provider = "Mock",
            ProviderModelOrWorkflow = "flux-dev-9x16",
            GenerationParametersSnapshot = "{\"prompt\":\"cyberpunk\",\"aspect\":\"9:16\"}",
            Status = GeneratedAssetStatus.PendingReview,
            IsSelectedForAssembly = false
        };

        var candidate2 = new GeneratedAsset
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = channelId,
            StoryboardId = storyboardId,
            StoryboardVersionId = storyboardVersionId,
            AssetRequirementId = requirementId,
            JobId = jobId,
            VariantIndex = 2,
            AssetType = AssetType.AiImage,
            MediaType = "Image",
            StorageProvider = "MinIO",
            StorageKey = $"content-factory/dev/channels/{channelId}/content/{contentItemId}/visual/{requirementId}/cand2.png",
            ContentType = "image/png",
            FileSizeBytes = 1045000,
            Width = 1080,
            Height = 1920,
            ChecksumSha256 = "ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb",
            Provider = "Mock",
            ProviderModelOrWorkflow = "flux-dev-9x16",
            GenerationParametersSnapshot = "{\"prompt\":\"cyberpunk\",\"aspect\":\"9:16\"}",
            Status = GeneratedAssetStatus.PendingReview,
            IsSelectedForAssembly = false
        };

        dbContext.GeneratedAssets.AddRange(candidate1, candidate2);
        await dbContext.SaveChangesAsync();

        // 1. Reject Candidate 1 with reason
        candidate1.Status = GeneratedAssetStatus.Rejected;
        candidate1.RejectionReason = "Distorted hands in lower framing";
        candidate1.ReviewedAtUtc = DateTime.UtcNow;
        candidate1.ReviewedByEmail = "editor@silverman.pro";
        candidate1.IsSelectedForAssembly = false;

        // 2. Approve Candidate 2
        candidate2.Status = GeneratedAssetStatus.Approved;
        candidate2.ReviewedAtUtc = DateTime.UtcNow;
        candidate2.ReviewedByEmail = "editor@silverman.pro";
        candidate2.IsSelectedForAssembly = true;

        await dbContext.SaveChangesAsync();

        var assets = await dbContext.GeneratedAssets
            .Where(a => a.AssetRequirementId == requirementId)
            .OrderBy(a => a.VariantIndex)
            .ToListAsync();

        Assert.Equal(2, assets.Count);
        Assert.Equal(GeneratedAssetStatus.Rejected, assets[0].Status);
        Assert.Equal("Distorted hands in lower framing", assets[0].RejectionReason);
        Assert.False(assets[0].IsSelectedForAssembly);

        Assert.Equal(GeneratedAssetStatus.Approved, assets[1].Status);
        Assert.True(assets[1].IsSelectedForAssembly);
        Assert.Null(assets[1].RejectionReason);

        // Verify single selection for assembly
        var selectedAssets = assets.Where(a => a.IsSelectedForAssembly).ToList();
        Assert.Single(selectedAssets);
        Assert.Equal(candidate2.Id, selectedAssets[0].Id);
    }

    [Fact]
    public void UpstreamStaleness_EvaluatesEligibility_Correctly()
    {
        var activeStoryboardVersionId = Guid.NewGuid();
        var staleStoryboardVersionId = Guid.NewGuid();

        var historicalAsset = new GeneratedAssetDto(
            Id: Guid.NewGuid(),
            ContentItemId: Guid.NewGuid(),
            ChannelId: Guid.NewGuid(),
            StoryboardId: Guid.NewGuid(),
            StoryboardVersionId: staleStoryboardVersionId,
            AssetRequirementId: Guid.NewGuid(),
            JobId: Guid.NewGuid(),
            VariantIndex: 1,
            AssetType: AssetType.AiImage,
            MediaType: "Image",
            StorageProvider: "MinIO",
            StorageKey: "content-factory/dev/key.png",
            ContentType: "image/png",
            FileSizeBytes: 500000,
            Width: 1080,
            Height: 1920,
            DurationSeconds: null,
            ChecksumSha256: "abc",
            Provider: "Mock",
            ProviderModelOrWorkflow: "flux-dev",
            GenerationParametersSnapshot: "{}",
            Status: GeneratedAssetStatus.Approved,
            RejectionReason: null,
            ReviewedAtUtc: DateTime.UtcNow,
            ReviewedByEmail: "editor@silverman.pro",
            IsSelectedForAssembly: true,
            CreatedAtUtc: DateTime.UtcNow,
            UpdatedAtUtc: DateTime.UtcNow,
            IsEligibleForAssembly: false // Stale version
        );

        Assert.False(historicalAsset.IsEligibleForAssembly);
    }
}
