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
}
