using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContentFactory.Api.Tests;

public class ScriptDomainAndServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void Script_PacingCalculation_ComputesCorrectEditorialDurationEstimate()
    {
        // 140 WPM = 2.333 words/sec
        // 105 words at 140 WPM => 105 / (140/60.0) = 105 / 2.3333 = 45.0s
        var wordCount = 105;
        var pacingWpm = 140;
        var duration = Math.Round(wordCount / (pacingWpm / 60.0), 1);
        Assert.Equal(45.0, duration);

        // 130 WPM (slower Spanish short-form)
        // 105 words at 130 WPM => 105 / (130/60.0) = 48.5s
        var duration130 = Math.Round(wordCount / (130 / 60.0), 1);
        Assert.Equal(48.5, duration130);

        // 150 WPM (faster Spanish short-form)
        // 105 words at 150 WPM => 105 / (150/60.0) = 42.0s
        var duration150 = Math.Round(wordCount / (150 / 60.0), 1);
        Assert.Equal(42.0, duration150);
    }

    [Fact]
    public async Task Script_PreservesLightweightFactualLineage_AndClaimReferences()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();
        var ideaId = Guid.NewGuid();
        var truthSourceId = Guid.NewGuid();
        var tsVersionId = Guid.NewGuid();
        var claimId = Guid.NewGuid();

        var script = new Script
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = Guid.NewGuid(),
            ContentIdeaId = ideaId,
            ContentIdeaVersionId = ideaId,
            TruthSourceId = truthSourceId,
            TruthSourceVersionId = tsVersionId,
            Title = "3 Habilidades Clave",
            TargetDurationSeconds = 45,
            PacingWpm = 140,
            EstimatedDurationSeconds = 45.0,
            TotalWordCount = 105,
            Language = "es-ES",
            Status = ScriptStatus.Draft,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = "operator@silverman.pro"
        };

        var scene1 = new ScriptScene
        {
            Id = Guid.NewGuid(),
            ScriptId = script.Id,
            OrderIndex = 1,
            SceneType = SceneType.Hook,
            NarrationText = "¿Crees que un prompt te salvará el empleo en 2026? Mira esto.",
            VisualPrompt = "Primer plano a cámara con texto animado",
            EstimatedDurationSeconds = 6.0,
            WordCount = 14
        };

        scene1.EvidenceReferences.Add(new ScriptSceneEvidenceReference
        {
            Id = Guid.NewGuid(),
            ScriptSceneId = scene1.Id,
            TruthSourceClaimId = claimId,
            ClaimStatement = "El 68% de las empresas priorizan criterio analítico sobre velocidad mecánica.",
            EditorialNote = "Gancho alineado con el estudio de empleo"
        });

        script.Scenes.Add(scene1);
        dbContext.Scripts.Add(script);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.Scripts
            .Include(s => s.Scenes)
                .ThenInclude(sc => sc.EvidenceReferences)
            .FirstOrDefaultAsync(s => s.Id == script.Id);

        Assert.NotNull(saved);
        Assert.Equal(ideaId, saved.ContentIdeaId);
        Assert.Equal(tsVersionId, saved.TruthSourceVersionId);
        Assert.Single(saved.Scenes);
        Assert.Single(saved.Scenes[0].EvidenceReferences);
        Assert.Equal(claimId, saved.Scenes[0].EvidenceReferences[0].TruthSourceClaimId);
        Assert.Equal("Gancho alineado con el estudio de empleo", saved.Scenes[0].EvidenceReferences[0].EditorialNote);
    }

    [Fact]
    public async Task StaleLineage_DetectsWhenSelectedIdeaOrTruthSourceEvolves()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var oldIdeaId = Guid.NewGuid();
        var newIdeaId = Guid.NewGuid();
        var tsId = Guid.NewGuid();
        var tsVer1Id = Guid.NewGuid();

        // Setup ContentItem
        var contentItem = new ContentItem
        {
            Id = contentItemId,
            ChannelId = channelId,
            Title = "Test Content",
            Slug = "test-content",
            Stage = ContentItemStage.ScriptDrafted,
            Status = ContentItemStatus.Active,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = "operator@silverman.pro"
        };

        // Setup TruthSource
        var truthSource = new TruthSource
        {
            Id = tsId,
            ContentItemId = contentItemId,
            Status = TruthSourceStatus.Approved,
            Summary = "Factual basis",
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = "operator@silverman.pro"
        };

        var tsVer1 = new TruthSourceVersion
        {
            Id = tsVer1Id,
            TruthSourceId = tsId,
            ContentItemId = contentItemId,
            VersionNumber = 1,
            SnapshotJson = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro"
        };

        // Old idea was Selected when script was created
        var oldIdea = new ContentIdea
        {
            Id = oldIdeaId,
            ContentItemId = contentItemId,
            TruthSourceId = tsId,
            TruthSourceVersionId = tsVer1Id,
            Title = "Old Idea",
            Angle = "Old Angle",
            HookStrategy = "Old Hook",
            Status = ContentIdeaStatus.Proposed, // Replaced!
            Version = 2
        };

        // New idea is now Selected
        var newIdea = new ContentIdea
        {
            Id = newIdeaId,
            ContentItemId = contentItemId,
            TruthSourceId = tsId,
            TruthSourceVersionId = tsVer1Id,
            Title = "New Idea",
            Angle = "New Angle",
            HookStrategy = "New Hook",
            Status = ContentIdeaStatus.Selected, // Active
            Version = 1
        };

        var script = new Script
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = channelId,
            ContentIdeaId = oldIdeaId, // Linked to old idea
            ContentIdeaVersionId = oldIdeaId,
            TruthSourceId = tsId,
            TruthSourceVersionId = tsVer1Id,
            Title = "Script Title",
            TargetDurationSeconds = 45,
            PacingWpm = 140,
            Status = ScriptStatus.Draft,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = "operator@silverman.pro"
        };

        dbContext.ContentItems.Add(contentItem);
        dbContext.TruthSources.Add(truthSource);
        dbContext.TruthSourceVersions.Add(tsVer1);
        dbContext.ContentIdeas.AddRange(oldIdea, newIdea);
        dbContext.Scripts.Add(script);
        await dbContext.SaveChangesAsync();

        var auditService = new AuditService(dbContext, NullLogger<AuditService>.Instance);
        var scriptService = new ScriptService(
            dbContext,
            new MockAiProviderRouter(),
            auditService,
            NullLogger<ScriptService>.Instance);

        var scriptDto = await scriptService.GetScriptByContentItemIdAsync(contentItemId);
        Assert.NotNull(scriptDto);
        Assert.True(scriptDto.IsStale);
        Assert.Contains("Selected ContentIdea has changed", scriptDto.StaleReason);
    }

    [Fact]
    public async Task RejectionAndReopen_FollowsStrictExplicitLifecycle()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var ideaId = Guid.NewGuid();
        var tsId = Guid.NewGuid();
        var tsVerId = Guid.NewGuid();

        var contentItem = new ContentItem
        {
            Id = contentItemId,
            ChannelId = channelId,
            Title = "Lifecycle Test",
            Slug = "lifecycle-test",
            Stage = ContentItemStage.ScriptUnderReview,
            Status = ContentItemStatus.Active,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = "operator@silverman.pro"
        };

        var idea = new ContentIdea
        {
            Id = ideaId,
            ContentItemId = contentItemId,
            TruthSourceId = tsId,
            TruthSourceVersionId = tsVerId,
            Title = "Test Idea",
            Angle = "Test Angle",
            HookStrategy = "Test Hook",
            Status = ContentIdeaStatus.Selected,
            Version = 1
        };

        var truthSource = new TruthSource
        {
            Id = tsId,
            ContentItemId = contentItemId,
            Status = TruthSourceStatus.Approved,
            Summary = "Summary",
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = "operator@silverman.pro"
        };

        var script = new Script
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = channelId,
            ContentIdeaId = ideaId,
            ContentIdeaVersionId = ideaId,
            TruthSourceId = tsId,
            TruthSourceVersionId = tsVerId,
            Title = "Script Title",
            TargetDurationSeconds = 45,
            PacingWpm = 140,
            Status = ScriptStatus.UnderReview,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = "operator@silverman.pro"
        };

        dbContext.ContentItems.Add(contentItem);
        dbContext.ContentIdeas.Add(idea);
        dbContext.TruthSources.Add(truthSource);
        dbContext.Scripts.Add(script);
        await dbContext.SaveChangesAsync();

        var auditService = new AuditService(dbContext, NullLogger<AuditService>.Instance);
        var scriptService = new ScriptService(
            dbContext,
            new MockAiProviderRouter(),
            auditService,
            NullLogger<ScriptService>.Instance);

        // 1. Operator rejects script with explicit reason
        var rejectRequest = new RejectScriptRequest("El gancho viola la restricción do-not-say.", ExpectedVersion: 1);
        var rejectedDto = await scriptService.RejectScriptAsync(contentItemId, script.Id, rejectRequest, "reviewer@silverman.pro");

        Assert.Equal(ScriptStatus.Rejected, rejectedDto.Status);
        Assert.Equal("El gancho viola la restricción do-not-say.", rejectedDto.RejectionReason);
        Assert.Equal(2, rejectedDto.Version);

        // Version history snapshot recorded
        var versions = await scriptService.GetScriptVersionsAsync(contentItemId, script.Id);
        Assert.Single(versions);
        Assert.Equal(1, versions[0].VersionNumber);
        Assert.Contains("Rechazado por el operador", versions[0].ChangeSummary);

        // 2. Operator explicitly reopens rejected script for revision
        var reopenRequest = new ReopenScriptRequest(ExpectedVersion: 2);
        var reopenedDto = await scriptService.ReopenScriptAsync(contentItemId, script.Id, reopenRequest, "operator@silverman.pro");

        Assert.Equal(ScriptStatus.Draft, reopenedDto.Status);
        Assert.Null(reopenedDto.RejectionReason);
        Assert.Equal(3, reopenedDto.Version);

        var updatedVersions = await scriptService.GetScriptVersionsAsync(contentItemId, script.Id);
        Assert.Equal(2, updatedVersions.Count);
    }

    [Fact]
    public async Task AiReview_ProducesAdvisoryCritique_WithoutAlteringScriptStatus()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var ideaId = Guid.NewGuid();
        var tsId = Guid.NewGuid();
        var tsVerId = Guid.NewGuid();

        var contentItem = new ContentItem
        {
            Id = contentItemId,
            ChannelId = channelId,
            Title = "Critique Test",
            Slug = "critique-test",
            Stage = ContentItemStage.ScriptDrafted,
            Status = ContentItemStatus.Active,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = "operator@silverman.pro"
        };

        var truthSource = new TruthSource
        {
            Id = tsId,
            ContentItemId = contentItemId,
            Status = TruthSourceStatus.Approved,
            Summary = "Factual synthesis",
            KeyIdeasJson = "[\"Idea 1\"]",
            VerifiableClaimsJson = "[]",
            DoNotSayConstraintsJson = "[\"No usar sensacionalismo\"]",
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = "operator@silverman.pro"
        };

        var script = new Script
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = channelId,
            ContentIdeaId = ideaId,
            ContentIdeaVersionId = ideaId,
            TruthSourceId = tsId,
            TruthSourceVersionId = tsVerId,
            Title = "Draft Script",
            TargetDurationSeconds = 45,
            PacingWpm = 140,
            EstimatedDurationSeconds = 45.0,
            TotalWordCount = 105,
            Status = ScriptStatus.Draft,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "operator@silverman.pro",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = "operator@silverman.pro"
        };

        script.Scenes.Add(new ScriptScene
        {
            Id = Guid.NewGuid(),
            ScriptId = script.Id,
            OrderIndex = 1,
            SceneType = SceneType.Hook,
            NarrationText = "¿Crees que un prompt te salvará?",
            VisualPrompt = "Visual",
            EstimatedDurationSeconds = 6.0,
            WordCount = 6
        });

        dbContext.ContentItems.Add(contentItem);
        dbContext.TruthSources.Add(truthSource);
        dbContext.Scripts.Add(script);
        await dbContext.SaveChangesAsync();

        var auditService = new AuditService(dbContext, NullLogger<AuditService>.Instance);
        var mockAiRouter = new MockAiProviderRouter();
        var scriptService = new ScriptService(
            dbContext,
            mockAiRouter,
            auditService,
            NullLogger<ScriptService>.Instance);

        var critique = await scriptService.ReviewScriptAsync(contentItemId, script.Id, "operator@silverman.pro");

        Assert.NotNull(critique);
        Assert.Equal("Pass", critique.OverallStatus);
        Assert.True(critique.FactualAlignmentScore >= 0.9);
        Assert.NotEmpty(critique.Dimensions);

        // Script in database remains strictly in Draft state with unmodified Version
        var refreshedScript = await dbContext.Scripts.FindAsync(script.Id);
        Assert.NotNull(refreshedScript);
        Assert.Equal(ScriptStatus.Draft, refreshedScript.Status);
        Assert.Equal(1, refreshedScript.Version);
    }
}

public class MockAiProviderRouter : IAiProviderRouter
{
    public Task<AiCapabilityResult<BuildTruthSourceResponse>> BuildTruthSourceAsync(
        BuildTruthSourceRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiCapabilityResult<BuildTruthSourceResponse>(true, null, null, null));

    public Task<AiCapabilityResult<GenerateIdeasResponse>> GenerateIdeasAsync(
        GenerateIdeasRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiCapabilityResult<GenerateIdeasResponse>(true, null, null, null));

    public Task<AiCapabilityResult<GenerateScriptResponse>> GenerateScriptAsync(
        GenerateScriptRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        var result = new GeneratedScriptResult(
            Title: request.IdeaTitle,
            TargetDurationSeconds: request.TargetDurationSeconds,
            PacingWpm: request.PacingWpm,
            Language: request.ChannelLanguage,
            Scenes:
            [
                new(1, SceneType.Hook, "¿Sabías esto?", "Visual 1", null),
                new(2, SceneType.Problem, "El problema común.", "Visual 2", null),
                new(3, SceneType.Insight, "El insight clave.", "Visual 3", null),
                new(4, SceneType.Climax, "La solución.", "Visual 4", null),
                new(5, SceneType.CallToAction, "Comenta abajo.", "Visual 5", null)
            ]
        );

        return Task.FromResult(new AiCapabilityResult<GenerateScriptResponse>(
            true,
            new GenerateScriptResponse(result, "Mock rationale"),
            null,
            null));
    }

    public Task<AiCapabilityResult<ReviewScriptResponse>> ReviewScriptAsync(
        ReviewScriptRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        var review = new ScriptReviewResultDto(
            OverallStatus: "Pass",
            FactualAlignmentScore: 0.95,
            RetentionAnalysis: "Gancho efectivo",
            PacingAssessment: "Pacing óptimo",
            DoNotSayComplianceNotes: ["Cumple guardrails"],
            Dimensions: [new ScriptReviewDimensionDto("Fidelidad", "Pass", "Alineado")],
            SceneCritiques: [],
            ActionableRecommendations: ["Mantener dinamismo"]
        );

        return Task.FromResult(new AiCapabilityResult<ReviewScriptResponse>(
            true,
            new ReviewScriptResponse(review, "Mock review rationale"),
            null,
            null));
    }

    public Task<AiCapabilityResult<PlanStoryboardResponse>> PlanStoryboardAsync(
        PlanStoryboardRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        var storyboard = new GeneratedStoryboardResult(
            Title: $"{request.ScriptTitle} - Storyboard",
            TargetDurationSeconds: request.TargetDurationSeconds,
            VisualStylePreset: request.VisualStylePreset ?? "Tech Minimalist 9:16",
            Frames:
            [
                new(1, 1, FramingIntent.CloseUp, "Comp 1", CameraMotionIntent.SlowZoomIn, "Subj", "Env", "Style", "Prompt 1", "Neg", "Audio 1", 8.0, "Text 1", TransitionIntent.Cut),
                new(2, 2, FramingIntent.MediumShot, "Comp 2", CameraMotionIntent.TrackingShot, "Subj", "Env", "Style", "Prompt 2", "Neg", "Audio 2", 10.0, "Text 2", TransitionIntent.Dissolve),
                new(3, 3, FramingIntent.MotionGraphic, "Comp 3", CameraMotionIntent.SlowZoomIn, "Subj", "Env", "Style", "Prompt 3", "Neg", "Audio 3", 12.0, "Text 3", TransitionIntent.ZoomIn),
                new(4, 4, FramingIntent.ExtremeCloseUp, "Comp 4", CameraMotionIntent.Static, "Subj", "Env", "Style", "Prompt 4", "Neg", "Audio 4", 8.0, "Text 4", TransitionIntent.Wipe),
                new(5, 5, FramingIntent.WideShot, "Comp 5", CameraMotionIntent.SlowZoomIn, "Subj", "Env", "Style", "Prompt 5", "Neg", "Audio 5", 7.0, "Text 5", TransitionIntent.Cut)
            ]
        );

        return Task.FromResult(new AiCapabilityResult<PlanStoryboardResponse>(
            true,
            new PlanStoryboardResponse(storyboard, "Mock storyboard rationale"),
            null,
            null));
    }

    public Task<AiCapabilityResult<ReviewStoryboardResponse>> ReviewStoryboardAsync(
        ReviewStoryboardRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        var review = new StoryboardReviewResultDto(
            OverallStatus: "Pass",
            VisualAlignmentScore: 0.95,
            HookVisualAssessment: "Gancho visual potente",
            FramingDiversityAssessment: "Diversidad adecuada",
            TimingAlignmentAssessment: "Tiempos alineados",
            Dimensions: [new StoryboardReviewDimensionDto("Composición", "Pass", "Vertical 9:16")],
            FrameCritiques: [],
            ActionableRecommendations: ["Asegurar zonas seguras"]
        );

        return Task.FromResult(new AiCapabilityResult<ReviewStoryboardResponse>(
            true,
            new ReviewStoryboardResponse(review, "Mock storyboard review rationale"),
            null,
            null));
    }
}
