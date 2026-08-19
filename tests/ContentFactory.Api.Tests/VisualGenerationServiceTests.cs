using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Infrastructure.Storage;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContentFactory.Api.Tests;

public class VisualGenerationServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static (VisualGenerationService Service, AppDbContext DbContext) CreateTestSetup()
    {
        var dbContext = CreateInMemoryDbContext();
        var config = new ConfigurationBuilder().Build();
        var storage = new MinioStorageService(config, new TestHttpClientFactory(), NullLogger<MinioStorageService>.Instance);
        var mockProvider = new MockVisualGenerationProvider(NullLogger<MockVisualGenerationProvider>.Instance);
        var service = new VisualGenerationService(
            dbContext,
            storage,
            [mockProvider],
            config,
            NullLogger<VisualGenerationService>.Instance
        );
        return (service, dbContext);
    }

    private static async Task<(ContentItem ContentItem, Script Script, ScriptVersion ScriptVersion, Storyboard Storyboard, StoryboardVersion StoryboardVersion, AssetRequirement Requirement)> SeedApprovedStoryboardAsync(AppDbContext dbContext, bool isApproved = true, bool isStale = false)
    {
        var channelId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();

        var contentItem = new ContentItem
        {
            Id = contentItemId,
            ChannelId = channelId,
            Title = "Test Video Production",
            Stage = ContentItemStage.StoryboardApproved
        };

        var scriptId = Guid.NewGuid();
        var scriptVersionId = isStale ? Guid.NewGuid() : Guid.NewGuid();
        var newerScriptVersionId = Guid.NewGuid();

        var script = new Script
        {
            Id = scriptId,
            ContentItemId = contentItemId,
            ChannelId = channelId,
            Status = ScriptStatus.Approved
        };

        var scriptVersion = new ScriptVersion
        {
            Id = isStale ? newerScriptVersionId : scriptVersionId,
            ScriptId = scriptId,
            VersionNumber = 1,
            Status = ScriptStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "author@silverman.pro"
        };

        var storyboardId = Guid.NewGuid();
        var storyboard = new Storyboard
        {
            Id = storyboardId,
            ContentItemId = contentItemId,
            ChannelId = channelId,
            ScriptId = scriptId,
            ScriptVersionId = scriptVersionId, // if isStale is true, this won't match the newest approved script version
            IsCurrent = true,
            Status = isApproved ? StoryboardStatus.Approved : StoryboardStatus.Draft,
            Title = "Test Storyboard",
            Version = 1,
            CreatedByEmail = "author@silverman.pro"
        };

        var frameId = Guid.NewGuid();
        var frame = new StoryboardFrame
        {
            Id = frameId,
            StoryboardId = storyboardId,
            OrderIndex = 1,
            VisualPrompt = "Close up of futuristic robotic hand in neon lab",
            EstimatedDurationSeconds = 4.0
        };
        storyboard.Frames.Add(frame);

        var assetPlan = new AssetPlan
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboardId,
            ContentItemId = contentItemId,
            Status = AssetPlanStatus.ReadyForGeneration
        };

        var req = new AssetRequirement
        {
            Id = Guid.NewGuid(),
            AssetPlanId = assetPlan.Id,
            FrameId = frameId,
            FrameOrderIndex = 1,
            AssetType = AssetType.AiImage,
            AspectRatio = "9:16",
            VisualPrompt = "Close up of futuristic robotic hand in neon lab",
            StyleIntent = "Moody dark cyberpunk"
        };
        assetPlan.Requirements.Add(req);
        storyboard.AssetPlan = assetPlan;

        var sbVersion = new StoryboardVersion
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboardId,
            ContentItemId = contentItemId,
            ScriptId = scriptId,
            ScriptVersionId = scriptVersionId,
            VersionNumber = 1,
            Status = isApproved ? StoryboardStatus.Approved : StoryboardStatus.Draft,
            SnapshotJson = "{}",
            CreatedByEmail = "author@silverman.pro"
        };

        dbContext.ContentItems.Add(contentItem);
        dbContext.Scripts.Add(script);
        dbContext.ScriptVersions.Add(scriptVersion);
        dbContext.Storyboards.Add(storyboard);
        dbContext.StoryboardVersions.Add(sbVersion);
        await dbContext.SaveChangesAsync();

        return (contentItem, script, scriptVersion, storyboard, sbVersion, req);
    }

    [Fact]
    public async Task DispatchGenerationAsync_Succeeds_AndIsIdempotent()
    {
        var (service, dbContext) = CreateTestSetup();
        var (_, _, _, storyboard, _, req) = await SeedApprovedStoryboardAsync(dbContext, isApproved: true, isStale: false);

        var dispatchRequest = new DispatchVisualGenerationRequest(
            AssetRequirementId: req.Id,
            CandidateCount: 2
        );

        // First dispatch
        var result1 = await service.DispatchGenerationAsync(storyboard.ContentItemId, storyboard.Id, dispatchRequest, "editor@silverman.pro");
        Assert.True(result1.Success);
        Assert.Single(result1.Jobs);
        var job1 = result1.Jobs[0];
        Assert.Equal(JobStatus.Queued, job1.Status);
        Assert.Equal("Mock", job1.Provider);
        Assert.Equal(req.Id, job1.SourceAssetRequirementId);

        // Second dispatch for same intent (idempotency check)
        var result2 = await service.DispatchGenerationAsync(storyboard.ContentItemId, storyboard.Id, dispatchRequest, "editor@silverman.pro");
        Assert.True(result2.Success);
        Assert.Single(result2.Jobs);
        var job2 = result2.Jobs[0];
        Assert.Equal(job1.Id, job2.Id); // Reused active job

        var totalJobs = await dbContext.Jobs.CountAsync();
        Assert.Equal(1, totalJobs);
    }

    [Fact]
    public async Task DispatchGenerationAsync_Fails_WhenStoryboardIsUnapproved()
    {
        var (service, dbContext) = CreateTestSetup();
        var (_, _, _, storyboard, _, req) = await SeedApprovedStoryboardAsync(dbContext, isApproved: false, isStale: false);

        var dispatchRequest = new DispatchVisualGenerationRequest(AssetRequirementId: req.Id, CandidateCount: 1);
        var result = await service.DispatchGenerationAsync(storyboard.ContentItemId, storyboard.Id, dispatchRequest, "editor@silverman.pro");

        Assert.False(result.Success);
        Assert.Contains("Approved", result.BlockerReason);
        Assert.Empty(result.Jobs);
    }

    [Fact]
    public async Task DispatchGenerationAsync_Fails_WhenStoryboardIsStale()
    {
        var (service, dbContext) = CreateTestSetup();
        var (_, _, _, storyboard, _, req) = await SeedApprovedStoryboardAsync(dbContext, isApproved: true, isStale: true);

        var dispatchRequest = new DispatchVisualGenerationRequest(AssetRequirementId: req.Id, CandidateCount: 1);
        var result = await service.DispatchGenerationAsync(storyboard.ContentItemId, storyboard.Id, dispatchRequest, "editor@silverman.pro");

        Assert.False(result.Success);
        Assert.Contains("stale", result.BlockerReason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Jobs);
    }

    [Fact]
    public async Task ProcessQueuedJobsAsync_ExecutesJob_AndCreatesGeneratedAssets()
    {
        var (service, dbContext) = CreateTestSetup();
        var (_, _, _, storyboard, _, req) = await SeedApprovedStoryboardAsync(dbContext, isApproved: true, isStale: false);

        var dispatchResult = await service.DispatchGenerationAsync(
            storyboard.ContentItemId,
            storyboard.Id,
            new DispatchVisualGenerationRequest(req.Id, 2),
            "editor@silverman.pro"
        );

        Assert.True(dispatchResult.Success);

        // Process queue
        await service.ProcessQueuedJobsAsync();

        var job = await dbContext.Jobs.Include(j => j.Attempts).FirstOrDefaultAsync(j => j.Id == dispatchResult.Jobs[0].Id);
        Assert.NotNull(job);
        Assert.Equal(JobStatus.Succeeded, job.Status);
        Assert.NotNull(job.CompletedAtUtc);
        Assert.Single(job.Attempts);
        Assert.Equal(JobStatus.Succeeded, job.Attempts[0].Status);

        var candidates = await dbContext.GeneratedAssets
            .Where(ga => ga.AssetRequirementId == req.Id)
            .OrderBy(ga => ga.VariantIndex)
            .ToListAsync();

        Assert.Equal(2, candidates.Count);
        Assert.Equal(GeneratedAssetStatus.PendingReview, candidates[0].Status);
        Assert.False(candidates[0].IsSelectedForAssembly);
        Assert.StartsWith("content-factory/development/", candidates[0].StorageKey);
    }

    [Fact]
    public async Task CandidateReviewAndSelection_EnforcesInvariants()
    {
        var (service, dbContext) = CreateTestSetup();
        var (_, _, _, storyboard, _, req) = await SeedApprovedStoryboardAsync(dbContext, isApproved: true, isStale: false);

        await service.DispatchGenerationAsync(
            storyboard.ContentItemId,
            storyboard.Id,
            new DispatchVisualGenerationRequest(req.Id, 2),
            "editor@silverman.pro"
        );
        await service.ProcessQueuedJobsAsync();

        var candidates = await dbContext.GeneratedAssets.Where(ga => ga.AssetRequirementId == req.Id).ToListAsync();
        Assert.Equal(2, candidates.Count);

        // 1. Reject Candidate 1
        var rejectResult = await service.ReviewCandidateAsync(
            candidates[0].Id,
            new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Rejected, "Unclear background lighting"),
            "editor@silverman.pro"
        );
        Assert.NotNull(rejectResult);
        Assert.Equal(GeneratedAssetStatus.Rejected, rejectResult.Status);
        Assert.Equal("Unclear background lighting", rejectResult.RejectionReason);
        Assert.False(rejectResult.IsSelectedForAssembly);

        // 2. Rejecting without reason throws ArgumentException
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ReviewCandidateAsync(
                candidates[1].Id,
                new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Rejected, ""),
                "editor@silverman.pro"
            )
        );

        // 3. Approve Candidate 2
        var approveResult = await service.ReviewCandidateAsync(
            candidates[1].Id,
            new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Approved, null),
            "editor@silverman.pro"
        );
        Assert.NotNull(approveResult);
        Assert.Equal(GeneratedAssetStatus.Approved, approveResult.Status);
        Assert.True(approveResult.IsSelectedForAssembly);

        // 4. Verify Overview DTO reflects state
        var overview = await service.GetProductionOverviewAsync(storyboard.ContentItemId, storyboard.Id);
        Assert.NotNull(overview);
        Assert.Equal(1, overview.ApprovedCount);
        Assert.Equal(0, overview.PendingReviewCount);
        Assert.Equal(1, overview.GeneratedCount);
        Assert.True(overview.IsEligibleForGeneration);
        Assert.NotNull(overview.Requirements[0].SelectedCandidate);
        Assert.Equal(candidates[1].Id, overview.Requirements[0].SelectedCandidate!.Id);
    }

    [Fact]
    public async Task TryClaimNextJobAsync_UnderConcurrentWorkers_ClaimsEachJobExactlyOnce()
    {
        var (service, dbContext) = CreateTestSetup();
        var (_, _, _, storyboard, _, req) = await SeedApprovedStoryboardAsync(dbContext, isApproved: true, isStale: false);

        // Queue 5 distinct jobs
        var jobIds = new List<Guid>();
        for (int i = 1; i <= 5; i++)
        {
            var job = new Job
            {
                Id = Guid.NewGuid(),
                ContentItemId = storyboard.ContentItemId,
                ChannelId = storyboard.ChannelId,
                JobType = JobTypes.GenerateVisualAsset,
                Capability = JobTypes.GenerateVisualAsset,
                SourceAssetRequirementId = req.Id,
                StoryboardId = storyboard.Id,
                StoryboardVersionId = Guid.NewGuid(),
                GenerationRevision = i,
                Status = JobStatus.Queued,
                Provider = "Mock",
                ModelOrWorkflowIdentifier = "flux-image-schnell",
                AttemptCount = 0,
                MaxAttempts = 3,
                AutomaticRetriesRemaining = 2,
                CandidateCount = 1,
                CorrelationId = $"corr-{i}",
                IdempotencyKey = $"idem-{i}",
                CreatedByEmail = "worker@silverman.pro",
                CreatedAtUtc = DateTime.UtcNow.AddSeconds(-10 + i),
                UpdatedAtUtc = DateTime.UtcNow.AddSeconds(-10 + i)
            };
            dbContext.Jobs.Add(job);
            jobIds.Add(job.Id);
        }
        await dbContext.SaveChangesAsync();

        // 10 concurrent claim attempts
        var claimTasks = Enumerable.Range(0, 10).Select(_ => service.TryClaimNextJobAsync()).ToList();
        var claimedJobs = await Task.WhenAll(claimTasks);

        var nonNullClaimed = claimedJobs.Where(j => j != null).ToList();
        Assert.Equal(5, nonNullClaimed.Count);

        // Assert all claimed IDs are unique
        var distinctClaimedIds = nonNullClaimed.Select(j => j!.Id).Distinct().ToList();
        Assert.Equal(5, distinctClaimedIds.Count);

        // Assert all in DB are now Running
        var runningCount = await dbContext.Jobs.CountAsync(j => j.Status == JobStatus.Running);
        Assert.Equal(5, runningCount);
    }

    [Fact]
    public async Task RetryJobAsync_PreservesPastAttemptHistory_AndResetsBudget()
    {
        var (service, dbContext) = CreateTestSetup();
        var (_, _, _, storyboard, _, req) = await SeedApprovedStoryboardAsync(dbContext, isApproved: true, isStale: false);

        req.VisualPrompt = "[mock:action-required-failure] invalid token";
        await dbContext.SaveChangesAsync();

        var dispatch = await service.DispatchGenerationAsync(
            storyboard.ContentItemId,
            storyboard.Id,
            new DispatchVisualGenerationRequest(req.Id, 1),
            "editor@silverman.pro"
        );
        var jobId = dispatch.Jobs[0].Id;

        // Process attempt 1 (fails with action-required)
        await service.ProcessQueuedJobsAsync();

        var failedJob = await dbContext.Jobs.Include(j => j.Attempts).FirstOrDefaultAsync(j => j.Id == jobId);
        Assert.NotNull(failedJob);
        Assert.Equal(JobStatus.FailedActionRequired, failedJob.Status);
        Assert.Single(failedJob.Attempts);
        Assert.Equal(1, failedJob.Attempts[0].AttemptNumber);

        // Manual retry
        var retriedDto = await service.RetryJobAsync(jobId, "tech@silverman.pro");
        Assert.NotNull(retriedDto);
        Assert.Equal(JobStatus.Queued, retriedDto.Status);
        Assert.Equal(2, retriedDto.AutomaticRetriesRemaining);

        // Fix the prompt on requirement
        req.VisualPrompt = "Fixed valid prompt";
        await dbContext.SaveChangesAsync();

        // Process attempt 2 (succeeds)
        await service.ProcessQueuedJobsAsync();

        var succeededJob = await dbContext.Jobs.Include(j => j.Attempts).FirstOrDefaultAsync(j => j.Id == jobId);
        Assert.NotNull(succeededJob);
        Assert.Equal(JobStatus.Succeeded, succeededJob.Status);
        // History preserves both Attempt 1 and Attempt 2!
        Assert.Equal(2, succeededJob.Attempts.Count);
        Assert.Equal(1, succeededJob.Attempts[0].AttemptNumber);
        Assert.Equal(JobStatus.FailedActionRequired, succeededJob.Attempts[0].Status);
        Assert.Equal(2, succeededJob.Attempts[1].AttemptNumber);
        Assert.Equal(JobStatus.Succeeded, succeededJob.Attempts[1].Status);
    }

    [Fact]
    public async Task ReviewCandidateAsync_EnforcesStatusConflictGuard()
    {
        var (service, dbContext) = CreateTestSetup();
        var (_, _, _, storyboard, _, req) = await SeedApprovedStoryboardAsync(dbContext, isApproved: true, isStale: false);

        await service.DispatchGenerationAsync(storyboard.ContentItemId, storyboard.Id, new DispatchVisualGenerationRequest(req.Id, 1), "editor@silverman.pro");
        await service.ProcessQueuedJobsAsync();

        var candidate = await dbContext.GeneratedAssets.FirstAsync(a => a.AssetRequirementId == req.Id);

        // Review 1: Approve
        await service.ReviewCandidateAsync(candidate.Id, new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Approved, null, ExpectedStatus: GeneratedAssetStatus.PendingReview), "editor1@silverman.pro");

        // Concurrent Review 2: Expects PendingReview but is already Approved -> throws InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReviewCandidateAsync(candidate.Id, new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Rejected, "Out of focus", ExpectedStatus: GeneratedAssetStatus.PendingReview), "editor2@silverman.pro")
        );
    }

    [Fact]
    public async Task DispatchGenerationAsync_RejectsUnsupportedAssetType()
    {
        var (service, dbContext) = CreateTestSetup();
        var (_, _, _, storyboard, _, req) = await SeedApprovedStoryboardAsync(dbContext, isApproved: true, isStale: false);

        req.AssetType = AssetType.TtsVoiceover; // Non-visual requirement
        await dbContext.SaveChangesAsync();

        var result = await service.DispatchGenerationAsync(
            storyboard.ContentItemId,
            storyboard.Id,
            new DispatchVisualGenerationRequest(req.Id, 1),
            "editor@silverman.pro"
        );

        Assert.False(result.Success);
        Assert.Contains("not supported", result.BlockerReason);
        Assert.Empty(result.Jobs);
    }

    private class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name = "") => new();
    }
}
