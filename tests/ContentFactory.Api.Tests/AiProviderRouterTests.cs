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
}
