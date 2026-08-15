using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContentFactory.Api.Modules.Content;
using ContentFactory.Api.Modules.Dashboard;
using ContentFactory.Api.Modules.Discovery;
using Xunit;

namespace ContentFactory.Api.Tests;

public class ContentAndTruthSourceApiIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task FullContentAndTruthSourceLifecycle_IntegrationTest()
    {
        // 1. Get Channels to locate IA Simple ES
        var channelsRes = await _client.GetAsync("/api/channels");
        Assert.Equal(HttpStatusCode.OK, channelsRes.StatusCode);
        var channels = await channelsRes.Content.ReadFromJsonAsync<List<ContentFactory.Api.Modules.Channels.ChannelDto>>();
        Assert.NotNull(channels);
        var pilot = channels.First(c => c.Slug == "ia-simple-es");

        // 2. Submit a manual discovery candidate
        var quickSubmitReq = new QuickSubmitCandidateRequest(
            pilot.Id,
            "https://example.com/ai-safety-breakthrough-2026",
            "Modelos de IA Segura para Empresas en 2026",
            "Resumen sobre técnicas de supervisión humana y mitigación de sesgos.",
            "es"
        );
        var submitRes = await _client.PostAsJsonAsync("/api/discovery/candidates/manual", quickSubmitReq);
        Assert.Equal(HttpStatusCode.Created, submitRes.StatusCode);
        var candidate = await submitRes.Content.ReadFromJsonAsync<DiscoveryCandidateDto>();
        Assert.NotNull(candidate);

        // 3. Initiate ContentItem from Candidate (tests candidate promotion + ContentItem creation + evidence capture)
        var initReq = new InitiateContentFromCandidateRequest(
            TitleOverride: "Modelos de IA Segura para Empresas en 2026"
        );
        var initRes = await _client.PostAsJsonAsync($"/api/discovery/candidates/{candidate.Id}/initiate-content", initReq);
        Assert.Equal(HttpStatusCode.OK, initRes.StatusCode);
        var contentItem = await initRes.Content.ReadFromJsonAsync<ContentItemDto>();
        Assert.NotNull(contentItem);
        Assert.Equal(ContentItemStage.DraftingEvidence, contentItem.Stage);
        Assert.Equal(1, contentItem.EvidenceCount);

        // 3b. Test duplicate initiation prevention (initiating again returns existing item instead of duplicating)
        var initAgainRes = await _client.PostAsJsonAsync($"/api/discovery/candidates/{candidate.Id}/initiate-content", initReq);
        Assert.Equal(HttpStatusCode.OK, initAgainRes.StatusCode);
        var duplicatedAttempt = await initAgainRes.Content.ReadFromJsonAsync<ContentItemDto>();
        Assert.NotNull(duplicatedAttempt);
        Assert.Equal(contentItem.Id, duplicatedAttempt.Id);

        // 4. Attach a secondary context note evidence
        var attachReq = new AttachEvidenceRequest(
            DiscoveryCandidateId: null,
            OriginUrl: null,
            Title: "Guía de mejores prácticas de gobernanza de datos",
            ContentText: "Las auditorías periódicas de datos de entrenamiento y las pruebas de robustez previenen alucinaciones críticas.",
            Role: EvidenceRole.SupportingEvidence,
            Notes: "Citar como fuente de contexto operativo."
        );
        var attachRes = await _client.PostAsJsonAsync($"/api/content-items/{contentItem.Id}/evidence", attachReq);
        Assert.Equal(HttpStatusCode.OK, attachRes.StatusCode);
        var attachedEvidence = await attachRes.Content.ReadFromJsonAsync<ContentItemEvidenceDto>();
        Assert.NotNull(attachedEvidence);
        Assert.Equal(EvidenceStatus.Captured, attachedEvidence.Status);

        // 5. Generate AI Draft for TruthSource
        var generateRes = await _client.PostAsync($"/api/content-items/{contentItem.Id}/truth-source/generate-ai-draft", null);
        Assert.Equal(HttpStatusCode.OK, generateRes.StatusCode);
        var truthSource = await generateRes.Content.ReadFromJsonAsync<TruthSourceDto>();
        Assert.NotNull(truthSource);
        Assert.Equal(TruthSourceStatus.Draft, truthSource.Status);
        Assert.False(string.IsNullOrWhiteSpace(truthSource.Summary));
        Assert.NotEmpty(truthSource.KeyIdeas);
        Assert.NotEmpty(truthSource.VerifiableClaims);
        Assert.NotEmpty(truthSource.DoNotSayConstraints);

        // 6. Manual edit with optimistic concurrency check
        var saveReq = new SaveTruthSourceRequest(
            Summary: "Resumen refinado por el operador sobre mitigación de sesgos y supervisión humana en 2026.",
            KeyIdeas: ["La supervisión humana continua es la clave de la adopción responsable", "Las auditorías previenen riesgos legales"],
            VerifiableClaims: truthSource.VerifiableClaims,
            EvidenceReferences: truthSource.EvidenceReferences,
            RiskNotes: "Evitar afirmaciones exageradas de seguridad absoluta.",
            DoNotSayConstraints: truthSource.DoNotSayConstraints,
            PossibleAngles: truthSource.PossibleAngles,
            LocalizationNotes: "Español directo y profesional.",
            ExpectedVersion: truthSource.Version,
            ChangeSummary: "Ajuste de tono y síntesis factual."
        );
        var saveRes = await _client.PutAsJsonAsync($"/api/content-items/{contentItem.Id}/truth-source", saveReq);
        Assert.Equal(HttpStatusCode.OK, saveRes.StatusCode);
        var updatedTs = await saveRes.Content.ReadFromJsonAsync<TruthSourceDto>();
        Assert.NotNull(updatedTs);
        Assert.Equal(truthSource.Version + 1, updatedTs.Version);

        // 7. Test Optimistic Concurrency Conflict (409)
        var staleSaveReq = saveReq with { ExpectedVersion = truthSource.Version }; // Stale version
        var conflictRes = await _client.PutAsJsonAsync($"/api/content-items/{contentItem.Id}/truth-source", staleSaveReq);
        Assert.Equal(HttpStatusCode.Conflict, conflictRes.StatusCode);

        // 8. Submit for Review (creates EditorialTask)
        var submitReviewRes = await _client.PostAsync($"/api/content-items/{contentItem.Id}/truth-source/submit-review", null);
        Assert.Equal(HttpStatusCode.OK, submitReviewRes.StatusCode);
        var underReviewTs = await submitReviewRes.Content.ReadFromJsonAsync<TruthSourceDto>();
        Assert.NotNull(underReviewTs);
        Assert.Equal(TruthSourceStatus.UnderReview, underReviewTs.Status);

        // Check EditorialTasks list
        var tasksRes = await _client.GetAsync($"/api/editorial-tasks?channelId={pilot.Id}");
        Assert.Equal(HttpStatusCode.OK, tasksRes.StatusCode);
        var tasks = await tasksRes.Content.ReadFromJsonAsync<List<EditorialTaskDto>>();
        Assert.NotNull(tasks);
        Assert.Contains(tasks, t => t.ContentItemId == contentItem.Id && t.TaskType == EditorialTaskType.ReviewTruthSource);

        // 9. Reject TruthSource with reason
        var rejectReq = new RejectTruthSourceRequest("Falta incluir referencia a costes de implementación.");
        var rejectRes = await _client.PostAsJsonAsync($"/api/content-items/{contentItem.Id}/truth-source/reject", rejectReq);
        Assert.Equal(HttpStatusCode.OK, rejectRes.StatusCode);
        var rejectedTs = await rejectRes.Content.ReadFromJsonAsync<TruthSourceDto>();
        Assert.NotNull(rejectedTs);
        Assert.Equal(TruthSourceStatus.Rejected, rejectedTs.Status);
        Assert.Equal(rejectReq.Reason, rejectedTs.RejectionReason);

        // 10. Re-approve TruthSource
        var approveRes = await _client.PostAsync($"/api/content-items/{contentItem.Id}/truth-source/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);
        var approvedTs = await approveRes.Content.ReadFromJsonAsync<TruthSourceDto>();
        Assert.NotNull(approvedTs);
        Assert.Equal(TruthSourceStatus.Approved, approvedTs.Status);

        // 11. Verify ContentItem detail reflects TruthSourceApproved stage
        var detailRes = await _client.GetAsync($"/api/content-items/{contentItem.Id}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);
        var detail = await detailRes.Content.ReadFromJsonAsync<ContentItemDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(ContentItemStage.TruthSourceApproved, detail.Stage);
        Assert.NotNull(detail.TruthSource);
        Assert.Equal(TruthSourceStatus.Approved, detail.TruthSource.Status);

        // 12. Verify Version History
        var versionsRes = await _client.GetAsync($"/api/content-items/{contentItem.Id}/truth-source/versions");
        Assert.Equal(HttpStatusCode.OK, versionsRes.StatusCode);
        var versions = await versionsRes.Content.ReadFromJsonAsync<List<TruthSourceVersionDto>>();
        Assert.NotNull(versions);
        Assert.NotEmpty(versions);
    }

    [Fact]
    public async Task GenerateAiDraft_RejectsWhenNoCapturedEvidence_AcceptsWhenCapturedEvidencePresent()
    {
        // 1. Create a ContentItem
        var createReq = new CreateContentItemRequest(
            ChannelId: Guid.Parse("00000000-0000-0000-0000-000000000010"),
            Title: "Gating Rule Verification Piece"
        );
        var createRes = await _client.PostAsJsonAsync("/api/content-items", createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var item = await createRes.Content.ReadFromJsonAsync<ContentItemDto>();
        Assert.NotNull(item);

        // 2. Attempt AI draft generation when 0 evidences exist -> Must reject with 400 Bad Request
        var failResEmpty = await _client.PostAsync($"/api/content-items/{item.Id}/truth-source/generate-ai-draft", null);
        Assert.Equal(HttpStatusCode.BadRequest, failResEmpty.StatusCode);

        // 3. Attach a failed URL evidence (404 simulator or unresolvable URL)
        var attachFailedReq = new AttachEvidenceRequest(
            DiscoveryCandidateId: null,
            OriginUrl: "https://example.invalid/dead-link-404",
            Title: "Dead Link Evidence",
            ContentText: null,
            Role: EvidenceRole.PrimaryLead,
            Notes: "Simulated failed URL"
        );
        var attachFailedRes = await _client.PostAsJsonAsync($"/api/content-items/{item.Id}/evidence", attachFailedReq);
        Assert.Equal(HttpStatusCode.OK, attachFailedRes.StatusCode);
        var failedEvidence = await attachFailedRes.Content.ReadFromJsonAsync<ContentItemEvidenceDto>();
        Assert.NotNull(failedEvidence);
        Assert.Equal(EvidenceStatus.CaptureFailed, failedEvidence.Status);

        // 4. Attempt AI draft generation when only CaptureFailed evidence exists -> Must reject with 400 Bad Request
        var failResOnlyCaptureFailed = await _client.PostAsync($"/api/content-items/{item.Id}/truth-source/generate-ai-draft", null);
        Assert.Equal(HttpStatusCode.BadRequest, failResOnlyCaptureFailed.StatusCode);
        var errorContent = await failResOnlyCaptureFailed.Content.ReadAsStringAsync();
        Assert.Contains("At least one successfully captured evidence item is required", errorContent);

        // 5. Attach a valid textual captured evidence
        var attachValidReq = new AttachEvidenceRequest(
            DiscoveryCandidateId: null,
            OriginUrl: null,
            Title: "Valid Manual Observation",
            ContentText: "Datos comprobados sobre optimización de pipelines.",
            Role: EvidenceRole.SupportingEvidence,
            Notes: "Verified note"
        );
        var attachValidRes = await _client.PostAsJsonAsync($"/api/content-items/{item.Id}/evidence", attachValidReq);
        Assert.Equal(HttpStatusCode.OK, attachValidRes.StatusCode);
        var validEvidence = await attachValidRes.Content.ReadFromJsonAsync<ContentItemEvidenceDto>();
        Assert.NotNull(validEvidence);
        Assert.Equal(EvidenceStatus.Captured, validEvidence.Status);

        // 6. Attempt AI draft generation now that 1 Captured evidence exists -> Must succeed with 200 OK
        var successRes = await _client.PostAsync($"/api/content-items/{item.Id}/truth-source/generate-ai-draft", null);
        Assert.Equal(HttpStatusCode.OK, successRes.StatusCode);
        var draft = await successRes.Content.ReadFromJsonAsync<TruthSourceDto>();
        Assert.NotNull(draft);
        Assert.Equal(TruthSourceStatus.Draft, draft.Status);
    }
}
