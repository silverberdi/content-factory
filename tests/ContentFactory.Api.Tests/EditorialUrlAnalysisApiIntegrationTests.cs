using System.Net;
using System.Net.Http.Json;
using ContentFactory.Api.Modules.Content;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ContentFactory.Api.Tests;

public sealed class EditorialUrlAnalysisApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient client;

    public EditorialUrlAnalysisApiIntegrationTests(CustomWebApplicationFactory factory) => client = factory.CreateClient();

    [Fact]
    public async Task ManualTextAnalysis_IsIdempotent_AndReadDoesNotNeedAnotherProviderCall()
    {
        var request = new EditorialUrlAnalysisRequest(
            Guid.Parse("00000000-0000-0000-0000-000000000010"),
            "https://example.com/editorial/article#section",
            "productividad",
            "La automatización responsable mejora tareas repetitivas cuando existe revisión humana.",
            "Artículo de prueba");

        var firstResponse = await client.PostAsJsonAsync("/api/editorial-url-analyses", request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<EditorialUrlAnalysisDto>();
        Assert.NotNull(first);
        Assert.Equal(EditorialUrlAnalysisStatus.Completed, first.Status);
        Assert.Equal(TruthSourceStatus.Draft, first.Draft?.Status);
        Assert.NotEmpty(first.Findings);

        var secondResponse = await client.PostAsJsonAsync("/api/editorial-url-analyses", request);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var second = await secondResponse.Content.ReadFromJsonAsync<EditorialUrlAnalysisDto>();
        Assert.NotNull(second);
        Assert.Equal(first.AnalysisId, second.AnalysisId);
        Assert.Equal(first.EvidenceId, second.EvidenceId);

        var readResponse = await client.GetAsync($"/api/editorial-url-analyses/{first.AnalysisId}");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var read = await readResponse.Content.ReadFromJsonAsync<EditorialUrlAnalysisDto>();
        Assert.NotNull(read);
        Assert.Equal(first.AnalysisId, read.AnalysisId);
        Assert.Equal(first.Findings.Count, read.Findings.Count);
        Assert.Equal(first.Findings.Select(f => f.Id), read.Findings.Select(f => f.Id));
        Assert.Equal("Mock", read.Provider);
        Assert.True(read.IsSimulated);
    }

    [Fact]
    public async Task ConcurrentEquivalentRequests_ReuseOneAnalysis()
    {
        var request = new EditorialUrlAnalysisRequest(
            Guid.Parse("00000000-0000-0000-0000-000000000010"),
            $"https://example.com/concurrent/{Guid.NewGuid():N}",
            ManualText: "Una sola entrada textual para comprobar idempotencia concurrente.");

        var responses = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => client.PostAsJsonAsync("/api/editorial-url-analyses", request)));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<EditorialUrlAnalysisDto>()));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Single(results.Select(result => result!.AnalysisId).Distinct());
        Assert.Single(results.Select(result => result!.EvidenceId).Distinct());
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://127.0.0.1/private")]
    [InlineData("http://localhost/private")]
    public async Task UnsafeUrl_IsRejectedBeforeCreatingContent(string url)
    {
        var response = await client.PostAsJsonAsync("/api/editorial-url-analyses", new EditorialUrlAnalysisRequest(
            Guid.Parse("00000000-0000-0000-0000-000000000010"), url));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingText_IsRecoverableAndDoesNotCreateFictitiousFindings()
    {
        var response = await client.PostAsJsonAsync("/api/editorial-url-analyses", new EditorialUrlAnalysisRequest(
            Guid.Parse("00000000-0000-0000-0000-000000000010"),
            $"https://example.com/video/{Guid.NewGuid():N}",
            ManualText: " "));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EditorialUrlAnalysisDto>();
        Assert.NotNull(result);
        Assert.Contains(result.Status, new[] { EditorialUrlAnalysisStatus.Failed, EditorialUrlAnalysisStatus.NotAnalyzable });
        Assert.Empty(result.Findings);
    }
}
