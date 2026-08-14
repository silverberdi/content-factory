using System.Net;
using System.Net.Http.Json;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Discovery;
using ContentFactory.Api.Modules.Discovery.Adapters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContentFactory.Api.Tests;

public class DiscoveryUrlNormalizerTests
{
    [Theory]
    [InlineData("https://example.com/article?utm_source=twitter&utm_medium=social", "https://example.com/article")]
    [InlineData("HTTPS://EXAMPLE.COM/Article/?fbclid=12345&ref=homepage", "https://example.com/Article")]
    [InlineData("http://techcrunch.com/2026/08/ai-news?id=99&utm_campaign=daily", "http://techcrunch.com/2026/08/ai-news?id=99")]
    [InlineData("https://xataka.com/ia#section-1", "https://xataka.com/ia")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void NormalizesUrlAndStripsTracking(string? raw, string? expected)
    {
        var normalized = DiscoveryUrlNormalizer.Normalize(raw);
        Assert.Equal(expected, normalized);
    }
}

public class DiscoveryServiceUnitTests
{
    private readonly AppDbContext _dbContext;
    private readonly DiscoveryService _discoveryService;
    private readonly Guid _channelId = Guid.NewGuid();

    public DiscoveryServiceUnitTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        var auditService = new AuditService(_dbContext, new NullLogger<AuditService>());

        var channel = new Channel
        {
            Id = _channelId,
            Slug = "ia-simple-es",
            Name = "IA Simple ES",
            Language = "es",
            Niche = "AI and future of work",
            Status = ChannelStatus.Pilot,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Channels.Add(channel);
        _dbContext.SaveChanges();

        var mockAdapter = new MockFeedAdapter();
        _discoveryService = new DiscoveryService(_dbContext, auditService, [mockAdapter]);
    }

    [Fact]
    public async Task CreateSource_DuplicateOriginUrlInSameChannel_ThrowsConflict()
    {
        var request = new CreateDiscoverySourceRequest(
            ChannelId: _channelId,
            Name: "Test Feed",
            OriginUrl: "https://example.com/feed.xml",
            SourceType: SourceType.Feed,
            Language: "es",
            PollingIntervalMinutes: 60
        );

        await _discoveryService.CreateSourceAsync(request, Guid.NewGuid(), "admin@example.com");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _discoveryService.CreateSourceAsync(request, Guid.NewGuid(), "admin@example.com"));

        Assert.Contains("already exists in this channel", ex.Message);
    }

    [Fact]
    public async Task QuickSubmit_WithUrl_NormalizesAndDeduplicatesWithinChannel()
    {
        var request1 = new QuickSubmitCandidateRequest(
            ChannelId: _channelId,
            ExternalUrl: "https://techcrunch.com/2026/08/ai-breakthrough?utm_source=feed",
            Title: "AI Breakthrough",
            Summary: "Summary text",
            Language: "es"
        );

        var first = await _discoveryService.QuickSubmitCandidateAsync(request1, Guid.NewGuid(), "operator@example.com");
        Assert.NotNull(first);
        Assert.Equal("https://techcrunch.com/2026/08/ai-breakthrough", first.NormalizedUrl);

        var request2 = new QuickSubmitCandidateRequest(
            ChannelId: _channelId,
            ExternalUrl: "https://techcrunch.com/2026/08/ai-breakthrough?utm_medium=email",
            Title: "AI Breakthrough Updated",
            Summary: "Another summary",
            Language: "es"
        );

        var second = await _discoveryService.QuickSubmitCandidateAsync(request2, Guid.NewGuid(), "operator@example.com");
        Assert.Equal(first.Id, second.Id);

        var totalCandidates = await _dbContext.DiscoveryCandidates.CountAsync();
        Assert.Equal(1, totalCandidates);
    }

    [Fact]
    public async Task QuickSubmit_TextOnlyWithoutUrl_PersistsAsValidManualCandidate()
    {
        var request1 = new QuickSubmitCandidateRequest(
            ChannelId: _channelId,
            ExternalUrl: null,
            Title: "Editorial idea on AI agents in customer service",
            Summary: "Key points to investigate: cost reduction vs customer satisfaction.",
            Language: "es"
        );

        var candidate1 = await _discoveryService.QuickSubmitCandidateAsync(request1, Guid.NewGuid(), "editor@example.com");
        Assert.NotNull(candidate1);
        Assert.Null(candidate1.ExternalUrl);
        Assert.Null(candidate1.NormalizedUrl);
        Assert.Equal(OriginType.Manual, candidate1.OriginType);
        Assert.Equal("editor@example.com", candidate1.SubmitterEmail);

        var request2 = new QuickSubmitCandidateRequest(
            ChannelId: _channelId,
            ExternalUrl: null,
            Title: "Second editorial idea on autonomous coding",
            Summary: "Follow up on new benchmarks.",
            Language: "es"
        );

        var candidate2 = await _discoveryService.QuickSubmitCandidateAsync(request2, Guid.NewGuid(), "editor@example.com");
        Assert.NotEqual(candidate1.Id, candidate2.Id);

        var total = await _dbContext.DiscoveryCandidates.CountAsync();
        Assert.Equal(2, total);
    }

    [Fact]
    public async Task TriageCandidate_Promote_SetsExactHandoffStateAndProvenance()
    {
        var request = new QuickSubmitCandidateRequest(
            ChannelId: _channelId,
            ExternalUrl: "https://example.com/source1",
            Title: "Promote me",
            Summary: "Evidence summary",
            Language: "es"
        );

        var candidate = await _discoveryService.QuickSubmitCandidateAsync(request, Guid.NewGuid(), "editor@example.com");

        var triageRequest = new TriageCandidateRequest(
            Status: DiscoveryCandidateStatus.Promoted,
            DismissalReason: null,
            EditorialNotes: "Angle: Focus on local business impact"
        );

        var promoted = await _discoveryService.TriageCandidateAsync(candidate.Id, triageRequest, Guid.NewGuid(), "senior.editor@example.com");

        Assert.Equal(DiscoveryCandidateStatus.Promoted, promoted.Status);
        Assert.Equal("senior.editor@example.com", promoted.PromotedByEmail);
        Assert.NotNull(promoted.PromotedAtUtc);
        Assert.Equal("Angle: Focus on local business impact", promoted.EditorialNotes);
    }

    [Fact]
    public async Task TriageCandidate_Dismiss_SetsDismissalReason()
    {
        var request = new QuickSubmitCandidateRequest(
            ChannelId: _channelId,
            ExternalUrl: "https://example.com/source2",
            Title: "Dismiss me",
            Summary: "Low quality clickbait",
            Language: "es"
        );

        var candidate = await _discoveryService.QuickSubmitCandidateAsync(request, Guid.NewGuid(), "editor@example.com");

        var triageRequest = new TriageCandidateRequest(
            Status: DiscoveryCandidateStatus.Dismissed,
            DismissalReason: "Low Quality",
            EditorialNotes: null
        );

        var dismissed = await _discoveryService.TriageCandidateAsync(candidate.Id, triageRequest, Guid.NewGuid(), "editor@example.com");

        Assert.Equal(DiscoveryCandidateStatus.Dismissed, dismissed.Status);
        Assert.Equal("Low Quality", dismissed.DismissalReason);
    }

    private class MockFeedAdapter : ISourceSyncAdapter
    {
        public bool CanHandle(string sourceType) => sourceType == SourceType.Feed;

        public Task<IReadOnlyList<DiscoveredItem>> FetchAsync(DiscoverySource source, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<DiscoveredItem> items =
            [
                new DiscoveredItem(
                    Title: "Mock Lead 1",
                    ExternalUrl: "https://mockfeed.com/item1?utm_source=rss",
                    Summary: "Mock summary 1",
                    RawContent: "Full content 1",
                    Author: "Mock Author",
                    DiscoveredAtUtc: DateTime.UtcNow,
                    Language: "es"
                )
            ];
            return Task.FromResult(items);
        }
    }
}

public class DiscoveryApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DiscoveryApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSources_Returns200WithArray()
    {
        var response = await _client.GetAsync("/api/discovery/sources");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sources = await response.Content.ReadFromJsonAsync<List<DiscoverySourceDto>>();
        Assert.NotNull(sources);
    }

    [Fact]
    public async Task GetCandidates_Returns200WithArray()
    {
        var response = await _client.GetAsync("/api/discovery/candidates");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var candidates = await response.Content.ReadFromJsonAsync<List<DiscoveryCandidateDto>>();
        Assert.NotNull(candidates);
    }

    [Fact]
    public async Task GetSummary_Returns200WithCounters()
    {
        var response = await _client.GetAsync("/api/discovery/summary");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Failed with {response.StatusCode}: {body}");

        var summary = await response.Content.ReadFromJsonAsync<DiscoverySummaryDto>();
        Assert.NotNull(summary);
    }
}
