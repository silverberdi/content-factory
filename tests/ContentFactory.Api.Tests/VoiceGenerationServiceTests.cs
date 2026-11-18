using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Infrastructure.Storage;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContentFactory.Api.Tests;

public class VoiceGenerationServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static (VoiceGenerationService Service, AppDbContext DbContext) CreateTestSetup()
    {
        var dbContext = CreateInMemoryDbContext();
        var config = new ConfigurationBuilder().Build();
        var storage = new MinioStorageService(config, new TestHttpClientFactory(), NullLogger<MinioStorageService>.Instance);
        var mockProvider = new MockVoiceGenerationProvider(NullLogger<MockVoiceGenerationProvider>.Instance);
        var service = new VoiceGenerationService(
            dbContext,
            storage,
            [mockProvider],
            config,
            NullLogger<VoiceGenerationService>.Instance
        );
        return (service, dbContext);
    }

    private static async Task<(ContentItem ContentItem, Script Script, ScriptVersion ScriptVersion, Storyboard Storyboard, StoryboardVersion StoryboardVersion, AssetRequirement Requirement)>
        SeedApprovedVoiceStoryboardAsync(AppDbContext dbContext, bool isApproved = true, bool isStale = false)
    {
        var channelId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();

        var channel = new Channel
        {
            Id = channelId,
            Slug = "ia-simple-es",
            Name = "IA Simple ES",
            Language = "es",
            Niche = "AI and future of work",
            Status = ChannelStatus.Pilot
        };
        dbContext.Channels.Add(channel);

        var contentItem = new ContentItem
        {
            Id = contentItemId,
            ChannelId = channelId,
            Title = "Test Voice Production",
            Stage = ContentItemStage.StoryboardApproved
        };

        var scriptId = Guid.NewGuid();
        var scriptVersionId = Guid.NewGuid();
        var newerScriptVersionId = Guid.NewGuid();

        var script = new Script
        {
            Id = scriptId,
            ContentItemId = contentItemId,
            ChannelId = channelId,
            Status = ScriptStatus.Approved,
            EstimatedDurationSeconds = 45.0,
            PacingWpm = 140,
            Language = "es"
        };

        var scene1 = new ScriptScene
        {
            Id = Guid.NewGuid(),
            ScriptId = scriptId,
            OrderIndex = 1,
            SceneType = SceneType.Hook,
            NarrationText = "La inteligencia artificial está transformando la forma en que trabajamos hoy.",
            WordCount = 12,
            EstimatedDurationSeconds = 5.0
        };

        var scene2 = new ScriptScene
        {
            Id = Guid.NewGuid(),
            ScriptId = scriptId,
            OrderIndex = 2,
            SceneType = SceneType.Problem,
            NarrationText = "Pero muchos profesionales temen quedarse atrás si no dominan estas nuevas herramientas.",
            WordCount = 13,
            EstimatedDurationSeconds = 5.5
        };

        script.Scenes.Add(scene1);
        script.Scenes.Add(scene2);

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
            ScriptVersionId = scriptVersionId,
            IsCurrent = true,
            Status = isApproved ? StoryboardStatus.Approved : StoryboardStatus.Draft,
            Title = "Test Storyboard",
            Version = 1,
            CreatedByEmail = "author@silverman.pro"
        };

        var assetPlan = new AssetPlan
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboardId,
            ContentItemId = contentItemId,
            Status = isApproved ? AssetPlanStatus.ReadyForGeneration : AssetPlanStatus.Planned
        };

        var voiceRequirement = new AssetRequirement
        {
            Id = Guid.NewGuid(),
            AssetPlanId = assetPlan.Id,
            AssetType = AssetType.TtsVoiceover,
            VoiceIntent = "Clear, modern, explanatory Spanish voiceover",
            TargetDurationSeconds = 45.0
        };
        assetPlan.Requirements.Add(voiceRequirement);
        storyboard.AssetPlan = assetPlan;

        var storyboardVersion = new StoryboardVersion
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboardId,
            ContentItemId = contentItemId,
            ScriptId = scriptId,
            ScriptVersionId = scriptVersionId,
            VersionNumber = 1,
            Status = isApproved ? StoryboardStatus.Approved : StoryboardStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "author@silverman.pro"
        };

        dbContext.ContentItems.Add(contentItem);
        dbContext.Scripts.Add(script);
        dbContext.ScriptVersions.Add(scriptVersion);
        dbContext.Storyboards.Add(storyboard);
        dbContext.StoryboardVersions.Add(storyboardVersion);
        await dbContext.SaveChangesAsync();

        return (contentItem, script, scriptVersion, storyboard, storyboardVersion, voiceRequirement);
    }

    [Fact]
    public async Task DispatchGenerationAsync_Succeeds_ForApprovedNonStaleStoryboard()
    {
        var (service, dbContext) = CreateTestSetup();
        var (contentItem, _, _, storyboard, _, req) = await SeedApprovedVoiceStoryboardAsync(dbContext, isApproved: true, isStale: false);

        var request = new DispatchVoiceGenerationRequest(
            AssetRequirementId: req.Id,
            CandidateCount: 2
        );

        var result = await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, request, "editor@silverman.pro");

        Assert.True(result.Success);
        Assert.Single(result.Jobs);
        Assert.Equal(JobStatus.Queued, result.Jobs[0].Status);
        Assert.Equal(JobTypes.GenerateVoiceAsset, result.Jobs[0].JobType);
        Assert.Equal(2, result.Jobs[0].CandidateCount);
    }

    [Fact]
    public async Task DispatchGenerationAsync_Fails_WhenStoryboardIsUnapproved()
    {
        var (service, dbContext) = CreateTestSetup();
        var (contentItem, _, _, storyboard, _, req) = await SeedApprovedVoiceStoryboardAsync(dbContext, isApproved: false, isStale: false);

        var request = new DispatchVoiceGenerationRequest(
            AssetRequirementId: req.Id,
            CandidateCount: 1
        );

        var result = await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, request, "editor@silverman.pro");

        Assert.False(result.Success);
        Assert.Contains("must be Approved", result.BlockerReason);
    }

    [Fact]
    public async Task DispatchGenerationAsync_Fails_WhenStoryboardIsStale()
    {
        var (service, dbContext) = CreateTestSetup();
        var (contentItem, _, _, storyboard, _, req) = await SeedApprovedVoiceStoryboardAsync(dbContext, isApproved: true, isStale: true);

        var request = new DispatchVoiceGenerationRequest(
            AssetRequirementId: req.Id,
            CandidateCount: 1
        );

        var result = await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, request, "editor@silverman.pro");

        Assert.False(result.Success);
        Assert.Contains("stale", result.BlockerReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DispatchGenerationAsync_IsIdempotent_ForAccidentalDuplicateDispatch()
    {
        var (service, dbContext) = CreateTestSetup();
        var (contentItem, _, _, storyboard, _, req) = await SeedApprovedVoiceStoryboardAsync(dbContext, isApproved: true, isStale: false);

        var request = new DispatchVoiceGenerationRequest(
            AssetRequirementId: req.Id,
            CandidateCount: 1
        );

        var result1 = await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, request, "editor@silverman.pro");
        var result2 = await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, request, "editor@silverman.pro");

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Jobs[0].Id, result2.Jobs[0].Id);
        Assert.Equal(1, await dbContext.Jobs.CountAsync(j => j.JobType == JobTypes.GenerateVoiceAsset));
    }

    [Fact]
    public async Task ProcessQueuedJobsAsync_ExecutesJob_AndCreatesGeneratedAssets()
    {
        var (service, dbContext) = CreateTestSetup();
        var (contentItem, _, _, storyboard, _, req) = await SeedApprovedVoiceStoryboardAsync(dbContext, isApproved: true, isStale: false);

        var request = new DispatchVoiceGenerationRequest(
            AssetRequirementId: req.Id,
            CandidateCount: 2
        );

        var dispatch = await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, request, "editor@silverman.pro");
        Assert.True(dispatch.Success);

        await service.ProcessQueuedJobsAsync();

        var job = await dbContext.Jobs.Include(j => j.Attempts).FirstAsync(j => j.Id == dispatch.Jobs[0].Id);
        Assert.Equal(JobStatus.Succeeded, job.Status);
        Assert.Single(job.Attempts);
        Assert.Equal(JobStatus.Succeeded, job.Attempts[0].Status);

        var generatedAssets = await dbContext.GeneratedAssets
            .Where(ga => ga.AssetRequirementId == req.Id)
            .OrderBy(ga => ga.VariantIndex)
            .ToListAsync();

        Assert.Equal(2, generatedAssets.Count);
        Assert.Equal(1, generatedAssets[0].VariantIndex);
        Assert.Equal(2, generatedAssets[1].VariantIndex);
        Assert.Equal("Audio", generatedAssets[0].MediaType);
        Assert.Equal(AssetType.TtsVoiceover, generatedAssets[0].AssetType);
        Assert.Equal("audio/wav", generatedAssets[0].ContentType);
        Assert.True(generatedAssets[0].DurationSeconds > 0);
        Assert.Equal(GeneratedAssetStatus.PendingReview, generatedAssets[0].Status);
        Assert.False(generatedAssets[0].IsSelectedForAssembly);
    }

    [Fact]
    public async Task ReviewCandidateAsync_Approve_SetsSelectedForAssembly_AndUnselectsSiblings()
    {
        var (service, dbContext) = CreateTestSetup();
        var (contentItem, _, _, storyboard, _, req) = await SeedApprovedVoiceStoryboardAsync(dbContext, isApproved: true, isStale: false);

        var request = new DispatchVoiceGenerationRequest(
            AssetRequirementId: req.Id,
            CandidateCount: 2
        );

        await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, request, "editor@silverman.pro");
        await service.ProcessQueuedJobsAsync();

        var candidates = await dbContext.GeneratedAssets
            .Where(ga => ga.AssetRequirementId == req.Id)
            .OrderBy(ga => ga.VariantIndex)
            .ToListAsync();

        var cand1 = candidates[0];
        var cand2 = candidates[1];

        // Approve Candidate 1
        var approved1 = await service.ReviewCandidateAsync(cand1.Id, new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Approved, null), "editor@silverman.pro");
        Assert.NotNull(approved1);
        Assert.Equal(GeneratedAssetStatus.Approved, approved1.Status);
        Assert.True(approved1.IsSelectedForAssembly);
        Assert.True(approved1.IsEligibleForAssembly);

        // Approve Candidate 2 -> should atomically unselect Candidate 1
        var approved2 = await service.ReviewCandidateAsync(cand2.Id, new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Approved, null), "editor@silverman.pro");
        Assert.NotNull(approved2);
        Assert.Equal(GeneratedAssetStatus.Approved, approved2.Status);
        Assert.True(approved2.IsSelectedForAssembly);

        var refreshedCand1 = await dbContext.GeneratedAssets.FirstAsync(ga => ga.Id == cand1.Id);
        Assert.False(refreshedCand1.IsSelectedForAssembly);
    }

    [Fact]
    public async Task ReviewCandidateAsync_Reject_RequiresReason()
    {
        var (service, dbContext) = CreateTestSetup();
        var (contentItem, _, _, storyboard, _, req) = await SeedApprovedVoiceStoryboardAsync(dbContext, isApproved: true, isStale: false);

        var request = new DispatchVoiceGenerationRequest(
            AssetRequirementId: req.Id,
            CandidateCount: 1
        );

        await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, request, "editor@silverman.pro");
        await service.ProcessQueuedJobsAsync();

        var cand = await dbContext.GeneratedAssets.FirstAsync(ga => ga.AssetRequirementId == req.Id);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ReviewCandidateAsync(cand.Id, new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Rejected, ""), "editor@silverman.pro")
        );

        var rejected = await service.ReviewCandidateAsync(cand.Id, new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Rejected, "Mispronounced acronym"), "editor@silverman.pro");
        Assert.NotNull(rejected);
        Assert.Equal(GeneratedAssetStatus.Rejected, rejected.Status);
        Assert.Equal("Mispronounced acronym", rejected.RejectionReason);
        Assert.False(rejected.IsSelectedForAssembly);
        Assert.False(rejected.IsEligibleForAssembly);
    }

    [Fact]
    public async Task VoiceSelection_RejectsPendingAndRejectedCandidates_AndKeepsSingleApprovedSelection()
    {
        var (service, dbContext) = CreateTestSetup();
        var (contentItem, _, _, storyboard, _, req) = await SeedApprovedVoiceStoryboardAsync(dbContext);
        await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, new DispatchVoiceGenerationRequest(req.Id, CandidateCount: 2), "editor@silverman.pro");
        await service.ProcessQueuedJobsAsync();
        var candidates = await dbContext.GeneratedAssets.Where(asset => asset.AssetRequirementId == req.Id).OrderBy(asset => asset.VariantIndex).ToListAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SelectCandidateForAssemblyAsync(candidates[0].Id, "editor@silverman.pro"));
        await service.ReviewCandidateAsync(candidates[0].Id, new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Approved, null), "editor@silverman.pro");
        await service.ReviewCandidateAsync(candidates[1].Id, new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Rejected, "Does not meet narration quality."), "editor@silverman.pro");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SelectCandidateForAssemblyAsync(candidates[1].Id, "editor@silverman.pro"));

        var selected = await service.SelectCandidateForAssemblyAsync(candidates[0].Id, "editor@silverman.pro");
        Assert.True(selected!.IsSelectedForAssembly);
        Assert.Equal(1, await dbContext.GeneratedAssets.CountAsync(asset => asset.AssetRequirementId == req.Id && asset.IsSelectedForAssembly));
    }

    [Fact]
    public async Task ConfigFingerprint_ChangesWhenVoiceProfileSemanticsChange()
    {
        var original = VoiceProfiles.Presets[VoiceProfiles.IaSimpleEsDefault.ProfileKey];
        try
        {
            var (service, dbContext) = CreateTestSetup();
            var (contentItem, _, _, storyboard, _, req) = await SeedApprovedVoiceStoryboardAsync(dbContext);
            await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, new DispatchVoiceGenerationRequest(req.Id, CandidateCount: 1), "editor@silverman.pro");
            var first = await dbContext.Jobs.SingleAsync();

            VoiceProfiles.Presets[original.ProfileKey] = original with { SpeakingStyle = "Documentary", Tone = "Measured", Warmth = "Low", Energy = "Calm" };
            await service.DispatchGenerationAsync(contentItem.Id, storyboard.Id, new DispatchVoiceGenerationRequest(req.Id, CandidateCount: 1, GenerationRevision: 2), "editor@silverman.pro");
            var second = await dbContext.Jobs.OrderByDescending(job => job.GenerationRevision).FirstAsync();

            Assert.NotEqual(first.IdempotencyKey, second.IdempotencyKey);
        }
        finally
        {
            VoiceProfiles.Presets[original.ProfileKey] = original;
        }
    }
}
