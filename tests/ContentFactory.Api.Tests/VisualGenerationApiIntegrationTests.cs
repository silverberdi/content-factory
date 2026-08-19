using System.Security.Claims;
using ContentFactory.Api.Controllers;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Infrastructure.Storage;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContentFactory.Api.Tests;

public class VisualGenerationApiIntegrationTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static (VisualGenerationService service, MinioStorageService storage, AppDbContext dbContext) CreateTestSetup()
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
        return (service, storage, dbContext);
    }

    private static void SetUserContext(ControllerBase controller, string email = "editor@silverman.pro", string role = "EDITORIAL")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        ], "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private static async Task<(ContentItem item, Storyboard storyboard, StoryboardVersion sbVer, AssetRequirement req)> SeedStoryboardPrerequisitesAsync(AppDbContext dbContext, bool isApproved = true, bool isStale = false)
    {
        var channel = new Channel { Id = Guid.NewGuid(), Name = "IA Simple ES", Slug = "ia-simple-es", Language = "es", Niche = "AI" };
        var contentItem = new ContentItem { Id = Guid.NewGuid(), ChannelId = channel.Id, Title = "Piece 1", Slug = "piece-1", Stage = ContentItemStage.StoryboardApproved };

        var scriptId = Guid.NewGuid();
        var scriptVerId = isStale ? Guid.NewGuid() : Guid.NewGuid();

        var script = new Script
        {
            Id = scriptId,
            ContentItemId = contentItem.Id,
            ChannelId = channel.Id,
            Status = ScriptStatus.Approved
        };

        var scriptVersion = new ScriptVersion
        {
            Id = isStale ? Guid.NewGuid() : scriptVerId,
            ScriptId = scriptId,
            VersionNumber = 1,
            Status = ScriptStatus.Approved
        };

        var storyboardId = Guid.NewGuid();
        var storyboard = new Storyboard
        {
            Id = storyboardId,
            ContentItemId = contentItem.Id,
            ChannelId = channel.Id,
            ScriptId = scriptId,
            ScriptVersionId = scriptVerId,
            IsCurrent = true,
            Status = isApproved ? StoryboardStatus.Approved : StoryboardStatus.Draft,
            Title = "Approved Storyboard",
            Version = 1,
            CreatedByEmail = "editor@silverman.pro"
        };

        var frameId = Guid.NewGuid();
        var frame = new StoryboardFrame
        {
            Id = frameId,
            StoryboardId = storyboardId,
            OrderIndex = 1,
            VisualPrompt = "Close-up of neural interface on vertical display, 9:16 framing",
            EstimatedDurationSeconds = 4.0
        };
        storyboard.Frames.Add(frame);

        var assetPlan = new AssetPlan
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboardId,
            ContentItemId = contentItem.Id,
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
            VisualPrompt = "Close-up of neural interface on vertical display, 9:16 framing",
            StyleIntent = "Futuristic tech minimalism"
        };
        assetPlan.Requirements.Add(req);
        storyboard.AssetPlan = assetPlan;

        var sbVersion = new StoryboardVersion
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboardId,
            ContentItemId = contentItem.Id,
            ScriptId = scriptId,
            ScriptVersionId = scriptVerId,
            VersionNumber = 1,
            Status = isApproved ? StoryboardStatus.Approved : StoryboardStatus.Draft,
            SnapshotJson = "{}",
            CreatedByEmail = "editor@silverman.pro"
        };

        dbContext.Channels.Add(channel);
        dbContext.ContentItems.Add(contentItem);
        dbContext.Scripts.Add(script);
        dbContext.ScriptVersions.Add(scriptVersion);
        dbContext.Storyboards.Add(storyboard);
        dbContext.StoryboardVersions.Add(sbVersion);
        await dbContext.SaveChangesAsync();

        return (contentItem, storyboard, sbVersion, req);
    }

    [Fact]
    public async Task DispatchEndpoint_Returns202Accepted_OnValidApprovedStoryboard()
    {
        var (service, _, dbContext) = CreateTestSetup();
        var (item, storyboard, _, req) = await SeedStoryboardPrerequisitesAsync(dbContext);

        var controller = new VisualGenerationDispatchController(service);
        SetUserContext(controller, "editor@silverman.pro", "EDITORIAL");

        var result = await controller.DispatchVisualGeneration(
            item.Id,
            storyboard.Id,
            new DispatchVisualGenerationRequest(req.Id, 2),
            CancellationToken.None
        );

        var acceptedResult = Assert.IsType<AcceptedResult>(result.Result);
        var jobs = Assert.IsAssignableFrom<List<JobDto>>(acceptedResult.Value);
        Assert.Single(jobs);
        Assert.Equal(JobStatus.Queued, jobs[0].Status);
        Assert.Equal(2, jobs[0].CandidateCount);
    }

    [Fact]
    public async Task OverviewEndpoint_Returns200Ok_WithRequirementsAndCandidates()
    {
        var (service, _, dbContext) = CreateTestSetup();
        var (item, storyboard, _, req) = await SeedStoryboardPrerequisitesAsync(dbContext);

        // Dispatch & process jobs
        await service.DispatchGenerationAsync(item.Id, storyboard.Id, new DispatchVisualGenerationRequest(req.Id, 2), "editor@silverman.pro");
        await service.ProcessQueuedJobsAsync();

        var controller = new VisualAssetsOverviewController(service);
        SetUserContext(controller);

        var result = await controller.GetVisualAssetsOverview(item.Id, storyboard.Id, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var overview = Assert.IsType<VisualProductionOverviewDto>(okResult.Value);

        Assert.Equal(1, overview.TotalRequirementsCount);
        Assert.Equal(1, overview.GeneratedCount);
        Assert.Equal(0, overview.ApprovedCount);
        Assert.Equal(1, overview.PendingReviewCount);
        Assert.True(overview.IsEligibleForGeneration);
        Assert.Single(overview.Requirements);
        Assert.Equal(2, overview.Requirements[0].Candidates.Count);
    }

    [Fact]
    public async Task JobsController_GetAndRetry_WorkCorrectly()
    {
        var (service, _, dbContext) = CreateTestSetup();
        var (item, storyboard, _, req) = await SeedStoryboardPrerequisitesAsync(dbContext);

        var dispatch = await service.DispatchGenerationAsync(item.Id, storyboard.Id, new DispatchVisualGenerationRequest(req.Id, 1), "editor@silverman.pro");
        var jobId = dispatch.Jobs[0].Id;

        // Simulate failed job
        var jobEntity = await dbContext.Jobs.FindAsync(jobId);
        Assert.NotNull(jobEntity);
        jobEntity.Status = JobStatus.FailedActionRequired;
        jobEntity.ErrorCode = "TEST_ERROR";
        jobEntity.SanitizedErrorMessage = "Test technical error.";
        await dbContext.SaveChangesAsync();

        var controller = new JobsController(service);
        SetUserContext(controller, "tech@silverman.pro", "TECHNICAL");

        var getResult = await controller.GetJob(jobId, CancellationToken.None);
        var okGet = Assert.IsType<OkObjectResult>(getResult.Result);
        var jobDto = Assert.IsType<JobDto>(okGet.Value);
        Assert.Equal(JobStatus.FailedActionRequired, jobDto.Status);

        var retryResult = await controller.RetryJob(jobId, CancellationToken.None);
        var okRetry = Assert.IsType<OkObjectResult>(retryResult.Result);
        var retriedDto = Assert.IsType<JobDto>(okRetry.Value);
        Assert.Equal(JobStatus.Queued, retriedDto.Status);
        Assert.Null(retriedDto.ErrorCode);
    }

    [Fact]
    public async Task GeneratedAssetsController_ReviewSelectAndStream_EndToEnd()
    {
        var (service, storage, dbContext) = CreateTestSetup();
        var (item, storyboard, _, req) = await SeedStoryboardPrerequisitesAsync(dbContext);

        await service.DispatchGenerationAsync(item.Id, storyboard.Id, new DispatchVisualGenerationRequest(req.Id, 2), "editor@silverman.pro");
        await service.ProcessQueuedJobsAsync();

        var candidates = await dbContext.GeneratedAssets.Where(ga => ga.AssetRequirementId == req.Id).ToListAsync();
        Assert.Equal(2, candidates.Count);

        var controller = new GeneratedAssetsController(service, storage, dbContext);
        SetUserContext(controller, "editor@silverman.pro", "EDITORIAL");

        // 1. Approve Candidate 1
        var reviewResult = await controller.ReviewCandidate(
            candidates[0].Id,
            new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Approved, null),
            CancellationToken.None
        );
        var okReview = Assert.IsType<OkObjectResult>(reviewResult.Result);
        var approvedDto = Assert.IsType<GeneratedAssetDto>(okReview.Value);
        Assert.Equal(GeneratedAssetStatus.Approved, approvedDto.Status);
        Assert.True(approvedDto.IsSelectedForAssembly);

        // 2. Select Candidate 2
        var selectResult = await controller.SelectCandidate(candidates[1].Id, CancellationToken.None);
        var okSelect = Assert.IsType<OkObjectResult>(selectResult.Result);
        var selectedDto = Assert.IsType<GeneratedAssetDto>(okSelect.Value);
        Assert.True(selectedDto.IsSelectedForAssembly);

        var reloadedCand1 = await dbContext.GeneratedAssets.FindAsync(candidates[0].Id);
        Assert.NotNull(reloadedCand1);
        Assert.False(reloadedCand1.IsSelectedForAssembly);

        // 3. Stream Candidate 2 media
        var streamResult = await controller.StreamGeneratedAsset(candidates[1].Id, CancellationToken.None);
        var fileResult = Assert.IsType<FileStreamResult>(streamResult);
        Assert.Equal("image/svg+xml", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
    }

    private class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name = "") => new();
    }
}
