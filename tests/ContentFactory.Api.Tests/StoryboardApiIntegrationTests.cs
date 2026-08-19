using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContentFactory.Api.Tests;

public class StoryboardApiIntegrationTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private static (StoryboardService storyboardService, AppDbContext dbContext) CreateTestService()
    {
        var dbContext = CreateInMemoryDbContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI_DEFAULT_PROVIDER"] = "mock"
            })
            .Build();
        var aiRouter = new AiProviderRouter(
            dbContext,
            new TestHttpClientFactory(),
            config,
            NullLogger<AiProviderRouter>.Instance);
        var auditService = new AuditService(dbContext, NullLogger<AuditService>.Instance);
        var storyboardService = new StoryboardService(dbContext, aiRouter, auditService, NullLogger<StoryboardService>.Instance);
        return (storyboardService, dbContext);
    }

    private static async Task<(ContentItem item, Script script, ScriptVersion scriptVer, TruthSource ts, TruthSourceVersion tsVer)> SeedPrerequisitesAsync(AppDbContext dbContext)
    {
        var channel = new Channel { Id = Guid.NewGuid(), Name = "IA Simple ES", Slug = "ia-simple-es", Language = "es", Niche = "AI" };
        var contentItem = new ContentItem { Id = Guid.NewGuid(), ChannelId = channel.Id, Title = "Piece 1", Slug = "piece-1", Stage = ContentItemStage.ScriptApproved };
        var truthSource = new TruthSource
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItem.Id,
            Status = TruthSourceStatus.Approved,
            Summary = "Approved factual foundation",
            KeyIdeasJson = "[\"Key idea 1\"]",
            VerifiableClaimsJson = "[{\"claim\":\"Claim 1\",\"sourceCitation\":\"Citation 1\"}]",
            DoNotSayConstraintsJson = "[\"No sensacionalismo\"]"
        };
        var tsVersion = new TruthSourceVersion
        {
            Id = Guid.NewGuid(),
            TruthSourceId = truthSource.Id,
            ContentItemId = contentItem.Id,
            VersionNumber = 1,
            SnapshotJson = "{}"
        };

        var script = new Script
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItem.Id,
            ChannelId = channel.Id,
            TruthSourceId = truthSource.Id,
            TruthSourceVersionId = tsVersion.Id,
            Title = "Approved Script Title",
            TargetDurationSeconds = 45,
            PacingWpm = 140,
            EstimatedDurationSeconds = 45.0,
            TotalWordCount = 105,
            Language = "es",
            Status = ScriptStatus.Approved,
            Version = 1,
            Scenes =
            [
                new ScriptScene { Id = Guid.NewGuid(), OrderIndex = 1, SceneType = SceneType.Hook, NarrationText = "Gancho", VisualPrompt = "Visual hook", EstimatedDurationSeconds = 8.0, WordCount = 18 },
                new ScriptScene { Id = Guid.NewGuid(), OrderIndex = 2, SceneType = SceneType.Problem, NarrationText = "Problema", VisualPrompt = "Visual problem", EstimatedDurationSeconds = 10.0, WordCount = 24 },
                new ScriptScene { Id = Guid.NewGuid(), OrderIndex = 3, SceneType = SceneType.Insight, NarrationText = "Insight", VisualPrompt = "Visual insight", EstimatedDurationSeconds = 12.0, WordCount = 28 },
                new ScriptScene { Id = Guid.NewGuid(), OrderIndex = 4, SceneType = SceneType.Climax, NarrationText = "Climax", VisualPrompt = "Visual climax", EstimatedDurationSeconds = 8.0, WordCount = 18 },
                new ScriptScene { Id = Guid.NewGuid(), OrderIndex = 5, SceneType = SceneType.CallToAction, NarrationText = "CTA", VisualPrompt = "Visual CTA", EstimatedDurationSeconds = 7.0, WordCount = 17 }
            ]
        };

        foreach (var sc in script.Scenes) sc.ScriptId = script.Id;

        var scriptVersion = new ScriptVersion
        {
            Id = Guid.NewGuid(),
            ScriptId = script.Id,
            ContentItemId = contentItem.Id,
            TruthSourceId = truthSource.Id,
            TruthSourceVersionId = tsVersion.Id,
            VersionNumber = 1,
            SnapshotJson = "{}",
            Status = ScriptStatus.Approved,
            PacingWpm = 140,
            EstimatedDurationSeconds = 45.0,
            TotalWordCount = 105,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro"
        };

        dbContext.Channels.Add(channel);
        dbContext.ContentItems.Add(contentItem);
        dbContext.TruthSources.Add(truthSource);
        dbContext.TruthSourceVersions.Add(tsVersion);
        dbContext.Scripts.Add(script);
        dbContext.ScriptVersions.Add(scriptVersion);
        await dbContext.SaveChangesAsync();

        return (contentItem, script, scriptVersion, truthSource, tsVersion);
    }

    [Fact]
    public async Task CreateStoryboard_InitializesDraft_BuildsAssetPlan_AndAdvancesStageToStoryboardDrafted()
    {
        var (storyboardService, dbContext) = CreateTestService();
        var (contentItem, script, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        var request = new CreateStoryboardRequest(
            Title: "Storyboard 3 Habilidades Clave",
            TargetDurationSeconds: 45,
            Frames: null,
            AssetRequirements: null
        );

        var result = await storyboardService.CreateStoryboardAsync(contentItem.Id, request, "creator@silverman.pro");

        Assert.NotNull(result);
        Assert.Equal(contentItem.Id, result.ContentItemId);
        Assert.Equal(StoryboardStatus.Draft, result.Status);
        Assert.Equal(1, result.Version);
        Assert.Equal(5, result.Frames.Count);
        Assert.NotNull(result.AssetPlan);
        Assert.Equal(AssetPlanStatus.Planned, result.AssetPlan.Status);
        Assert.True(result.AssetPlan.Requirements.Count >= 5);

        var updatedContentItem = await dbContext.ContentItems.FindAsync(contentItem.Id);
        Assert.Equal(ContentItemStage.StoryboardDrafted, updatedContentItem!.Stage);

        var versionSnapshot = await dbContext.StoryboardVersions.FirstOrDefaultAsync(v => v.StoryboardId == result.Id);
        Assert.NotNull(versionSnapshot);
        Assert.Equal(1, versionSnapshot.VersionNumber);
    }

    [Fact]
    public async Task UpdateStoryboard_EnforcesOptimisticConcurrency_AndUpdatesFrames()
    {
        var (storyboardService, dbContext) = CreateTestService();
        var (contentItem, script, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        var createReq = new CreateStoryboardRequest("Storyboard Original", 45, null, null);
        var created = await storyboardService.CreateStoryboardAsync(contentItem.Id, createReq, "creator@silverman.pro");

        // Concurrent update with wrong expected version
        var updateReqWrongVersion = new UpdateStoryboardRequest(
            Title: "Concurrent Title",
            TargetDurationSeconds: 45,
            Frames:
            [
                new SaveStoryboardFrameRequest(null, 1, script.Scenes[0].Id, 1, FramingIntent.CloseUp, "Comp", CameraMotionIntent.SlowZoomIn, "Subj", "Env", "Style", "Prompt", null, "Audio", 5.0, "Text", TransitionIntent.Cut)
            ],
            AssetRequirements: null,
            ChangeSummary: "Conflict edit",
            ExpectedVersion: 999
        );

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            storyboardService.UpdateStoryboardAsync(contentItem.Id, created.Id, updateReqWrongVersion, "other@silverman.pro"));

        // Correct update
        var updateReqCorrect = new UpdateStoryboardRequest(
            Title: "Storyboard v2",
            TargetDurationSeconds: 45,
            Frames:
            [
                new SaveStoryboardFrameRequest(null, 1, script.Scenes[0].Id, 1, FramingIntent.CloseUp, "Comp", CameraMotionIntent.SlowZoomIn, "Subj", "Env", "Style", "Prompt 1 updated", null, "Audio 1", 6.0, "Text 1", TransitionIntent.Cut),
                new SaveStoryboardFrameRequest(null, 2, script.Scenes[1].Id, 2, FramingIntent.MediumShot, "Comp", CameraMotionIntent.TrackingShot, "Subj", "Env", "Style", "Prompt 2 updated", null, "Audio 2", 8.0, "Text 2", TransitionIntent.Cut)
            ],
            AssetRequirements: null,
            ChangeSummary: "Updated frames",
            ExpectedVersion: 1
        );

        var updated = await storyboardService.UpdateStoryboardAsync(contentItem.Id, created.Id, updateReqCorrect, "creator@silverman.pro");

        Assert.Equal(2, updated.Version);
        Assert.Equal("Storyboard v2", updated.Title);
        Assert.Equal(2, updated.Frames.Count);
        Assert.Equal(14.0, updated.TotalEstimatedDurationSeconds);

        var versions = await storyboardService.GetStoryboardVersionsAsync(contentItem.Id, created.Id);
        Assert.Equal(2, versions.Count);
    }

    [Fact]
    public async Task GenerateAiStoryboard_PlansVisualFramesAndProviderAgnosticRequirements()
    {
        var (storyboardService, dbContext) = CreateTestService();
        var (contentItem, _, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        var options = new GenerateStoryboardOptions(
            VisualStylePreset: "Cyber-Minimalist 9:16",
            CameraMotionIntensity: "Dynamic",
            TargetDurationSeconds: 45
        );

        var result = await storyboardService.GenerateAiStoryboardAsync(contentItem.Id, options, "creator@silverman.pro");

        Assert.NotNull(result);
        Assert.Equal(StoryboardStatus.Draft, result.Status);
        Assert.NotEmpty(result.Frames);
        Assert.NotNull(result.AssetPlan);
        Assert.Contains(result.AssetPlan.Requirements, r => r.AssetType == AssetType.AiImage);
        Assert.Contains(result.AssetPlan.Requirements, r => r.AssetType == AssetType.TtsVoiceover);
        Assert.Contains(result.AssetPlan.Requirements, r => r.AssetType == AssetType.SubtitleTrack);
    }

    [Fact]
    public async Task SubmitForReview_TransitionsToUnderReview_AndCreatesReviewStoryboardTask()
    {
        var (storyboardService, dbContext) = CreateTestService();
        var (contentItem, _, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        var created = await storyboardService.CreateStoryboardAsync(contentItem.Id, new CreateStoryboardRequest("Storyboard", 45, null, null), "creator@silverman.pro");

        var submitReq = new SubmitStoryboardForReviewRequest(ExpectedVersion: 1);
        var underReview = await storyboardService.SubmitForReviewAsync(contentItem.Id, created.Id, submitReq, "creator@silverman.pro");

        Assert.Equal(StoryboardStatus.UnderReview, underReview.Status);
        Assert.Equal(2, underReview.Version);

        var contentItemDb = await dbContext.ContentItems.FindAsync(contentItem.Id);
        Assert.Equal(ContentItemStage.StoryboardUnderReview, contentItemDb!.Stage);

        var task = await dbContext.EditorialTasks.FirstOrDefaultAsync(t => t.ContentItemId == contentItem.Id && t.TaskType == EditorialTaskType.ReviewStoryboard);
        Assert.NotNull(task);
        Assert.Equal(EditorialTaskStatus.Pending, task.Status);
    }

    [Fact]
    public async Task ApproveStoryboard_EnforcesSingleGate_ApprovesAssetPlan_AndClosesTask()
    {
        var (storyboardService, dbContext) = CreateTestService();
        var (contentItem, _, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        var created = await storyboardService.CreateStoryboardAsync(contentItem.Id, new CreateStoryboardRequest("Storyboard", 45, null, null), "creator@silverman.pro");
        var underReview = await storyboardService.SubmitForReviewAsync(contentItem.Id, created.Id, new SubmitStoryboardForReviewRequest(1), "creator@silverman.pro");

        var approveReq = new ApproveStoryboardRequest(ExpectedVersion: 2);
        var approved = await storyboardService.ApproveStoryboardAsync(contentItem.Id, created.Id, approveReq, "editor@silverman.pro");

        Assert.Equal(StoryboardStatus.Approved, approved.Status);
        Assert.NotNull(approved.ApprovedAtUtc);
        Assert.Equal("editor@silverman.pro", approved.ApprovedByEmail);
        Assert.Equal(AssetPlanStatus.ReadyForGeneration, approved.AssetPlan!.Status);

        var contentItemDb = await dbContext.ContentItems.FindAsync(contentItem.Id);
        Assert.Equal(ContentItemStage.StoryboardApproved, contentItemDb!.Stage);

        var task = await dbContext.EditorialTasks.FirstOrDefaultAsync(t => t.ContentItemId == contentItem.Id && t.TaskType == EditorialTaskType.ReviewStoryboard);
        Assert.NotNull(task);
        Assert.Equal(EditorialTaskStatus.Completed, task.Status);
    }

    [Fact]
    public async Task RejectStoryboard_TransitionsToRejected_RecordsReason_AndClosesTask()
    {
        var (storyboardService, dbContext) = CreateTestService();
        var (contentItem, _, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        var created = await storyboardService.CreateStoryboardAsync(contentItem.Id, new CreateStoryboardRequest("Storyboard", 45, null, null), "creator@silverman.pro");
        var underReview = await storyboardService.SubmitForReviewAsync(contentItem.Id, created.Id, new SubmitStoryboardForReviewRequest(1), "creator@silverman.pro");

        var rejectReq = new RejectStoryboardRequest("Falta variedad visual en el gancho", ExpectedVersion: 2);
        var rejected = await storyboardService.RejectStoryboardAsync(contentItem.Id, created.Id, rejectReq, "editor@silverman.pro");

        Assert.Equal(StoryboardStatus.Rejected, rejected.Status);
        Assert.Equal("Falta variedad visual en el gancho", rejected.RejectionReason);

        var task = await dbContext.EditorialTasks.FirstOrDefaultAsync(t => t.ContentItemId == contentItem.Id && t.TaskType == EditorialTaskType.ReviewStoryboard);
        Assert.NotNull(task);
        Assert.Equal(EditorialTaskStatus.Completed, task.Status);
    }

    [Fact]
    public async Task ReopenStoryboard_TransitionsBackToDraft()
    {
        var (storyboardService, dbContext) = CreateTestService();
        var (contentItem, _, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        var created = await storyboardService.CreateStoryboardAsync(contentItem.Id, new CreateStoryboardRequest("Storyboard", 45, null, null), "creator@silverman.pro");
        var underReview = await storyboardService.SubmitForReviewAsync(contentItem.Id, created.Id, new SubmitStoryboardForReviewRequest(1), "creator@silverman.pro");
        var approved = await storyboardService.ApproveStoryboardAsync(contentItem.Id, created.Id, new ApproveStoryboardRequest(2), "editor@silverman.pro");

        var reopenReq = new ReopenStoryboardRequest(ExpectedVersion: 3);
        var reopened = await storyboardService.ReopenStoryboardAsync(contentItem.Id, created.Id, reopenReq, "creator@silverman.pro");

        Assert.Equal(StoryboardStatus.Draft, reopened.Status);
        Assert.Equal(AssetPlanStatus.Planned, reopened.AssetPlan!.Status);
        Assert.Equal(4, reopened.Version);
    }

    [Fact]
    public async Task ReconcileStoryboard_PreservesLineageImmutability_CreatesSuccessorDraft()
    {
        var (storyboardService, dbContext) = CreateTestService();
        var (contentItem, script, scriptVer1, ts, tsVer1) = await SeedPrerequisitesAsync(dbContext);

        var created = await storyboardService.CreateStoryboardAsync(contentItem.Id, new CreateStoryboardRequest("Storyboard v1", 45, null, null), "creator@silverman.pro");

        // Simulate upstream script update to Version 2
        var scriptVer2 = new ScriptVersion
        {
            Id = Guid.NewGuid(),
            ScriptId = script.Id,
            ContentItemId = contentItem.Id,
            TruthSourceId = ts.Id,
            TruthSourceVersionId = tsVer1.Id,
            VersionNumber = 2,
            SnapshotJson = "{}",
            Status = ScriptStatus.Approved,
            PacingWpm = 140,
            EstimatedDurationSeconds = 48.0,
            TotalWordCount = 112,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro"
        };
        dbContext.ScriptVersions.Add(scriptVer2);
        await dbContext.SaveChangesAsync();

        // Storyboard should now be stale
        var fetched = await storyboardService.GetStoryboardByContentItemIdAsync(contentItem.Id);
        Assert.NotNull(fetched);
        Assert.True(fetched.IsStale);

        // Reconcile
        var reconcileReq = new ReconcileStoryboardRequest(ExpectedVersion: 1);
        var successor = await storyboardService.ReconcileStoryboardAsync(contentItem.Id, created.Id, reconcileReq, "editor@silverman.pro");

        Assert.NotNull(successor);
        Assert.True(successor.IsCurrent);
        Assert.Equal(created.Id, successor.ReconciledFromStoryboardId);
        Assert.Equal(StoryboardStatus.Draft, successor.Status);
        Assert.False(successor.IsStale);

        // Predecessor is archived
        var predecessor = await dbContext.Storyboards.FindAsync(created.Id);
        Assert.NotNull(predecessor);
        Assert.False(predecessor.IsCurrent);
        Assert.NotNull(predecessor.SupersededAtUtc);
    }

    [Fact]
    public async Task CheckProductionEligibility_ValidatesApprovedStatus_AndAssetPlanCompleteness()
    {
        var (storyboardService, dbContext) = CreateTestService();
        var (contentItem, _, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        // 1. Not created yet
        var eligibility1 = await storyboardService.CheckProductionEligibilityAsync(contentItem.Id);
        Assert.False(eligibility1.IsEligibleForGeneration);
        Assert.Contains("No current Storyboard exists", eligibility1.BlockerReason);

        // 2. Created in Draft
        var created = await storyboardService.CreateStoryboardAsync(contentItem.Id, new CreateStoryboardRequest("Storyboard", 45, null, null), "creator@silverman.pro");
        var eligibility2 = await storyboardService.CheckProductionEligibilityAsync(contentItem.Id);
        Assert.False(eligibility2.IsEligibleForGeneration);
        Assert.Contains("must be Approved", eligibility2.BlockerReason);

        // 3. Approved
        await storyboardService.SubmitForReviewAsync(contentItem.Id, created.Id, new SubmitStoryboardForReviewRequest(1), "creator@silverman.pro");
        await storyboardService.ApproveStoryboardAsync(contentItem.Id, created.Id, new ApproveStoryboardRequest(2), "editor@silverman.pro");

        var eligibility3 = await storyboardService.CheckProductionEligibilityAsync(contentItem.Id);
        Assert.True(eligibility3.IsEligibleForGeneration);
        Assert.Null(eligibility3.BlockerReason);
        Assert.True(eligibility3.VisualAssetCount >= 5);
        Assert.True(eligibility3.AudioAssetCount >= 1);
        Assert.True(eligibility3.SubtitleAssetCount >= 1);
    }

    [Fact]
    public async Task SubmitForReview_Throws_WhenStoryboardIsStale()
    {
        var (storyboardService, dbContext) = CreateTestService();
        var (contentItem, script, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        var created = await storyboardService.CreateStoryboardAsync(contentItem.Id, new CreateStoryboardRequest("Storyboard", 45, null, null), "creator@silverman.pro");

        // Bump script version to simulate stale lineage
        var newScriptVersion = new ScriptVersion
        {
            Id = Guid.NewGuid(),
            ScriptId = script.Id,
            ContentItemId = contentItem.Id,
            ContentIdeaId = script.ContentIdeaId,
            ContentIdeaVersionId = script.ContentIdeaVersionId,
            TruthSourceId = script.TruthSourceId,
            TruthSourceVersionId = script.TruthSourceVersionId,
            VersionNumber = 2,
            SnapshotJson = "{}",
            ChangeSummary = "Bumped script",
            Status = ScriptStatus.Approved,
            TotalWordCount = 100,
            EstimatedDurationSeconds = 45,
            PacingWpm = 140,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "editor@silverman.pro"
        };
        dbContext.ScriptVersions.Add(newScriptVersion);
        await dbContext.SaveChangesAsync();

        // Attempting to submit stale storyboard should fail
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storyboardService.SubmitForReviewAsync(contentItem.Id, created.Id, new SubmitStoryboardForReviewRequest(1), "creator@silverman.pro"));
        Assert.Contains("reconciliation", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StoryboardsController_RequiresEditorialPolicy_OnMutationEndpoints()
    {
        var controllerType = typeof(ContentFactory.Api.Controllers.StoryboardsController);
        
        var methods = new[]
        {
            nameof(ContentFactory.Api.Controllers.StoryboardsController.CreateStoryboard),
            nameof(ContentFactory.Api.Controllers.StoryboardsController.UpdateStoryboard),
            nameof(ContentFactory.Api.Controllers.StoryboardsController.GenerateAiStoryboard),
            nameof(ContentFactory.Api.Controllers.StoryboardsController.ReviewStoryboard),
            nameof(ContentFactory.Api.Controllers.StoryboardsController.SubmitForReview),
            nameof(ContentFactory.Api.Controllers.StoryboardsController.ApproveStoryboard),
            nameof(ContentFactory.Api.Controllers.StoryboardsController.RejectStoryboard),
            nameof(ContentFactory.Api.Controllers.StoryboardsController.ReopenStoryboard),
            nameof(ContentFactory.Api.Controllers.StoryboardsController.ReconcileStoryboard)
        };

        foreach (var methodName in methods)
        {
            var method = controllerType.GetMethod(methodName);
            Assert.NotNull(method);
            var authAttr = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .FirstOrDefault();
            Assert.NotNull(authAttr);
            Assert.Equal("RequireEditorial", authAttr.Policy);
        }
    }
}
