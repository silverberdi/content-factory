using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContentFactory.Api.Tests;

public class AiProviderRouterTests
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

    [Fact]
    public async Task BuildTruthSource_WithMockProvider_ReturnsStructuredResponse()
    {
        using var dbContext = CreateInMemoryDbContext();
        var config = new ConfigurationBuilder().Build();
        var router = new AiProviderRouter(
            dbContext,
            new TestHttpClientFactory(),
            config,
            NullLogger<AiProviderRouter>.Instance);

        var channelId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();
        var evidence1Id = Guid.NewGuid();
        var evidence2Id = Guid.NewGuid();

        var request = new BuildTruthSourceRequest(
            ChannelName: "IA Simple ES",
            ChannelLanguage: "es",
            ChannelNiche: "AI and future of work",
            Evidences:
            [
                new EvidenceSnippetDto(
                    evidence1Id,
                    "Modelos de Razonamiento en Empresas",
                    "https://example.com/reasoning",
                    EvidenceRole.PrimaryLead,
                    "Los nuevos modelos verifican cada paso antes de responder."
                ),
                new EvidenceSnippetDto(
                    evidence2Id,
                    "Nota editorial sobre auditoría humana",
                    null,
                    EvidenceRole.SupportingEvidence,
                    "La supervisión humana evita errores en tareas críticas."
                )
            ]
        );

        var context = new AiRoutingContext(channelId, contentItemId, PreferredProvider: AiProviders.Mock);

        var result = await router.BuildTruthSourceAsync(request, context);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.Summary));
        Assert.NotEmpty(result.Data.KeyIdeas);
        Assert.NotEmpty(result.Data.VerifiableClaims);
        Assert.Contains(result.Data.VerifiableClaims, c => c.EvidenceId == evidence1Id);
        Assert.NotEmpty(result.Data.DoNotSayConstraints);
        Assert.NotEmpty(result.Data.PossibleAngles);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.LocalizationNotes));

        // Verify recommendation persisted
        Assert.NotNull(result.Recommendation);
        var recommendationInDb = await dbContext.AiRecommendations.FirstOrDefaultAsync(r => r.Id == result.Recommendation.Id);
        Assert.NotNull(recommendationInDb);
        Assert.Equal(AiCapabilities.BuildTruthSource, recommendationInDb.Capability);
        Assert.Equal(AiProviders.Mock, recommendationInDb.Provider);
        Assert.False(string.IsNullOrWhiteSpace(recommendationInDb.StructuredOutputJson));
        Assert.False(string.IsNullOrWhiteSpace(recommendationInDb.Rationale));
    }

    [Fact]
    public async Task GenerateIdeas_WithMockProvider_ReturnsStructuredProposalsAndLogsRecommendation()
    {
        using var dbContext = CreateInMemoryDbContext();
        var config = new ConfigurationBuilder().Build();
        var router = new AiProviderRouter(
            dbContext,
            new TestHttpClientFactory(),
            config,
            NullLogger<AiProviderRouter>.Instance);

        var channelId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();
        var truthSourceId = Guid.NewGuid();
        var truthSourceVersionId = Guid.NewGuid();

        var request = new GenerateIdeasRequest(
            ChannelId: channelId,
            ChannelName: "IA Simple ES",
            ChannelLanguage: "es",
            ChannelNiche: "AI and future of work",
            TruthSourceId: truthSourceId,
            TruthSourceVersionId: truthSourceVersionId,
            Summary: "Síntesis factual sobre cómo el criterio analítico y la capacidad de auditar respuestas diferencian a los profesionales.",
            KeyIdeas: ["El criterio analítico supera a la memorización de prompts", "Las empresas buscan perfiles híbridos"],
            VerifiableClaims: [new VerifiableClaimDto("68% de las empresas priorizan criterio sobre velocidad", "El País", null)],
            DoNotSayConstraints: ["No usar sensacionalismo", "No prometer fórmulas mágicas"],
            PossibleAngles: ["3 habilidades que la IA no reemplaza", "Cómo auditar respuestas"],
            Count: 3
        );

        var context = new AiRoutingContext(channelId, contentItemId, PreferredProvider: AiProviders.Mock);

        var result = await router.GenerateIdeasAsync(request, context);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.Ideas.Count);
        Assert.All(result.Data.Ideas, idea =>
        {
            Assert.False(string.IsNullOrWhiteSpace(idea.Title));
            Assert.False(string.IsNullOrWhiteSpace(idea.Angle));
            Assert.False(string.IsNullOrWhiteSpace(idea.HookStrategy));
            Assert.False(string.IsNullOrWhiteSpace(idea.AudienceValue));
            Assert.Equal("YouTube Short 30-60s", idea.Format);
            Assert.False(string.IsNullOrWhiteSpace(idea.IntendedOutcome));
        });

        // Verify recommendation persisted with TruthSourceVersionId
        Assert.NotNull(result.Recommendation);
        var recommendationInDb = await dbContext.AiRecommendations.FirstOrDefaultAsync(r => r.Id == result.Recommendation.Id);
        Assert.NotNull(recommendationInDb);
        Assert.Equal(AiCapabilities.GenerateIdeas, recommendationInDb.Capability);
        Assert.Equal(AiProviders.Mock, recommendationInDb.Provider);
        Assert.Equal(truthSourceVersionId, recommendationInDb.TruthSourceVersionId);
        Assert.Equal(contentItemId, recommendationInDb.ContentItemId);
        Assert.False(string.IsNullOrWhiteSpace(recommendationInDb.StructuredOutputJson));
        Assert.False(string.IsNullOrWhiteSpace(recommendationInDb.Rationale));
    }

    [Fact]
    public async Task PlanStoryboard_WithMockProvider_ReturnsStructuredFramesAndLogsRecommendation()
    {
        using var dbContext = CreateInMemoryDbContext();
        var config = new ConfigurationBuilder().Build();
        var router = new AiProviderRouter(
            dbContext,
            new TestHttpClientFactory(),
            config,
            NullLogger<AiProviderRouter>.Instance);

        var channelId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();
        var scriptId = Guid.NewGuid();
        var scriptVersionId = Guid.NewGuid();
        var truthSourceId = Guid.NewGuid();
        var tsVersionId = Guid.NewGuid();

        var scenes = new List<ScriptSceneDto>
        {
            new(Guid.NewGuid(), scriptId, 1, SceneType.Hook, "El 70% de los profesionales usan mal la IA.", "Plano cercano de creador", 8.0, 18, []),
            new(Guid.NewGuid(), scriptId, 2, SceneType.Problem, "Copian y pegan sin auditar.", "Oficina con alertas", 10.0, 24, []),
            new(Guid.NewGuid(), scriptId, 3, SceneType.Insight, "El secreto es el criterio analítico.", "Visualización de red neuronal", 12.0, 28, []),
            new(Guid.NewGuid(), scriptId, 4, SceneType.Climax, "Quien audita, lidera.", "Primer plano de microchip", 8.0, 18, []),
            new(Guid.NewGuid(), scriptId, 5, SceneType.CallToAction, "Síguenos para más consejos.", "Plano amplio ciudad", 7.0, 16, [])
        };

        var request = new PlanStoryboardRequest(
            ChannelId: channelId,
            ChannelName: "IA Simple ES",
            ChannelLanguage: "es",
            ChannelNiche: "AI and future of work",
            ScriptId: scriptId,
            ScriptVersionId: scriptVersionId,
            TruthSourceId: truthSourceId,
            TruthSourceVersionId: tsVersionId,
            ScriptTitle: "3 Habilidades Clave",
            TargetDurationSeconds: 45,
            PacingWpm: 140,
            Scenes: scenes,
            VisualStylePreset: "Cyber-Tech Minimalist 9:16"
        );

        var context = new AiRoutingContext(channelId, contentItemId, PreferredProvider: AiProviders.Mock);

        var result = await router.PlanStoryboardAsync(request, context);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Storyboard.Frames);
        Assert.All(result.Data.Storyboard.Frames, frame =>
        {
            Assert.False(string.IsNullOrWhiteSpace(frame.FramingIntent));
            Assert.False(string.IsNullOrWhiteSpace(frame.CameraMotionIntent));
            Assert.False(string.IsNullOrWhiteSpace(frame.VisualPrompt));
            Assert.False(string.IsNullOrWhiteSpace(frame.AudioCue));
            Assert.True(frame.EstimatedDurationSeconds > 0);
            Assert.False(string.IsNullOrWhiteSpace(frame.TransitionIntent));
        });

        // Verify recommendation persisted
        Assert.NotNull(result.Recommendation);
        var recommendationInDb = await dbContext.AiRecommendations.FirstOrDefaultAsync(r => r.Id == result.Recommendation.Id);
        Assert.NotNull(recommendationInDb);
        Assert.Equal(AiCapabilities.PlanStoryboard, recommendationInDb.Capability);
        Assert.Equal(AiProviders.Mock, recommendationInDb.Provider);
        Assert.Equal(tsVersionId, recommendationInDb.TruthSourceVersionId);
        Assert.Equal(contentItemId, recommendationInDb.ContentItemId);
    }

    [Fact]
    public async Task ReviewStoryboard_WithMockProvider_ReturnsAdvisoryCritiqueAndDimensions()
    {
        using var dbContext = CreateInMemoryDbContext();
        var config = new ConfigurationBuilder().Build();
        var router = new AiProviderRouter(
            dbContext,
            new TestHttpClientFactory(),
            config,
            NullLogger<AiProviderRouter>.Instance);

        var channelId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();
        var scriptId = Guid.NewGuid();
        var scriptVersionId = Guid.NewGuid();

        var scriptScenes = new List<ScriptSceneDto>
        {
            new(Guid.NewGuid(), scriptId, 1, SceneType.Hook, "Gancho inicial", "Visual", 8.0, 18, []),
            new(Guid.NewGuid(), scriptId, 2, SceneType.Problem, "Problema", "Visual", 10.0, 24, []),
            new(Guid.NewGuid(), scriptId, 3, SceneType.Insight, "Insight", "Visual", 12.0, 28, []),
            new(Guid.NewGuid(), scriptId, 4, SceneType.Climax, "Climax", "Visual", 8.0, 18, []),
            new(Guid.NewGuid(), scriptId, 5, SceneType.CallToAction, "CTA", "Visual", 7.0, 16, [])
        };

        var frames = new List<StoryboardFrameDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), 1, scriptScenes[0].Id, 1, FramingIntent.CloseUp, "Comp", CameraMotionIntent.SlowZoomIn, "Subj", "Env", "Style", "Prompt 1", "Neg", "Audio 1", 8.0, "Text 1", TransitionIntent.Cut, DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), 2, scriptScenes[1].Id, 2, FramingIntent.MediumShot, "Comp", CameraMotionIntent.TrackingShot, "Subj", "Env", "Style", "Prompt 2", "Neg", "Audio 2", 10.0, "Text 2", TransitionIntent.Dissolve, DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), 3, scriptScenes[2].Id, 3, FramingIntent.MotionGraphic, "Comp", CameraMotionIntent.SlowZoomIn, "Subj", "Env", "Style", "Prompt 3", "Neg", "Audio 3", 12.0, "Text 3", TransitionIntent.ZoomIn, DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), 4, scriptScenes[3].Id, 4, FramingIntent.ExtremeCloseUp, "Comp", CameraMotionIntent.Static, "Subj", "Env", "Style", "Prompt 4", "Neg", "Audio 4", 8.0, "Text 4", TransitionIntent.Wipe, DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), 5, scriptScenes[4].Id, 5, FramingIntent.WideShot, "Comp", CameraMotionIntent.SlowZoomIn, "Subj", "Env", "Style", "Prompt 5", "Neg", "Audio 5", 7.0, "Text 5", TransitionIntent.Cut, DateTime.UtcNow, DateTime.UtcNow)
        };

        var request = new ReviewStoryboardRequest(
            ChannelId: channelId,
            ChannelName: "IA Simple ES",
            ChannelLanguage: "es",
            ScriptId: scriptId,
            ScriptVersionId: scriptVersionId,
            ScriptTitle: "3 Habilidades Clave",
            ScriptTargetDurationSeconds: 45,
            ScriptScenes: scriptScenes,
            StoryboardTitle: "3 Habilidades Clave - Storyboard",
            TargetDurationSeconds: 45,
            Frames: frames
        );

        var context = new AiRoutingContext(channelId, contentItemId, PreferredProvider: AiProviders.Mock);

        var result = await router.ReviewStoryboardAsync(request, context);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Pass", result.Data.ReviewResult.OverallStatus);
        Assert.NotEmpty(result.Data.ReviewResult.Dimensions);
        Assert.Equal(5, result.Data.ReviewResult.FrameCritiques.Count);
        Assert.NotEmpty(result.Data.ReviewResult.ActionableRecommendations);

        // Verify recommendation persisted
        Assert.NotNull(result.Recommendation);
        var recommendationInDb = await dbContext.AiRecommendations.FirstOrDefaultAsync(r => r.Id == result.Recommendation.Id);
        Assert.NotNull(recommendationInDb);
        Assert.Equal(AiCapabilities.ReviewStoryboard, recommendationInDb.Capability);
        Assert.Equal(AiProviders.Mock, recommendationInDb.Provider);
    }
}
