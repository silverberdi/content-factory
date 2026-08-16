using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContentFactory.Api.Tests;

public class ContentIdeaApiIntegrationTests
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

    private static (ContentIdeaService ideaService, AppDbContext dbContext) CreateTestService()
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
        var ideaService = new ContentIdeaService(dbContext, aiRouter, auditService, NullLogger<ContentIdeaService>.Instance);
        return (ideaService, dbContext);
    }

    [Fact]
    public async Task GenerateAiIdeas_RequiresApprovedTruthSource_ThrowsIfNotApproved()
    {
        var (ideaService, dbContext) = CreateTestService();

        var channel = new Channel { Id = Guid.NewGuid(), Name = "IA Simple ES", Slug = "ia-simple-es", Language = "es", Niche = "AI" };
        var contentItem = new ContentItem { Id = Guid.NewGuid(), ChannelId = channel.Id, Title = "Draft Piece", Stage = ContentItemStage.DraftingEvidence };
        var truthSource = new TruthSource { Id = Guid.NewGuid(), ContentItemId = contentItem.Id, Status = TruthSourceStatus.Draft, Summary = "Draft TS" };

        dbContext.Channels.Add(channel);
        dbContext.ContentItems.Add(contentItem);
        dbContext.TruthSources.Add(truthSource);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ideaService.GenerateAiIdeasAsync(contentItem.Id, new GenerateIdeasOptions(3), "operator@silverman.pro");
        });
    }

    [Fact]
    public async Task GenerateAiIdeas_OnApprovedTruthSource_GeneratesNovelIdeasAndFiltersDuplicates()
    {
        var (ideaService, dbContext) = CreateTestService();

        var channel = new Channel { Id = Guid.NewGuid(), Name = "IA Simple ES", Slug = "ia-simple-es", Language = "es", Niche = "AI" };
        var contentItem = new ContentItem { Id = Guid.NewGuid(), ChannelId = channel.Id, Title = "AI in Office", Stage = ContentItemStage.TruthSourceApproved };
        var truthSource = new TruthSource
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItem.Id,
            Status = TruthSourceStatus.Approved,
            Summary = "Evaluación de modelos de razonamiento en tareas de oficina.",
            KeyIdeasJson = "[\"Menos errores en resúmenes\", \"Supervisión humana clave\"]",
            VerifiableClaimsJson = "[]",
            DoNotSayConstraintsJson = "[\"No prometer 100% de infalibilidad\"]",
            PossibleAnglesJson = "[\"3 claves para usar razonamiento\"]"
        };
        var tsVersion = new TruthSourceVersion
        {
            Id = Guid.NewGuid(),
            TruthSourceId = truthSource.Id,
            ContentItemId = contentItem.Id,
            VersionNumber = 1,
            SnapshotJson = "{}",
            SupportingEvidenceIdsJson = "[]",
            CreatedByEmail = "operator@silverman.pro"
        };

        dbContext.Channels.Add(channel);
        dbContext.ContentItems.Add(contentItem);
        dbContext.TruthSources.Add(truthSource);
        dbContext.TruthSourceVersions.Add(tsVersion);
        await dbContext.SaveChangesAsync();

        var ideas = await ideaService.GenerateAiIdeasAsync(contentItem.Id, new GenerateIdeasOptions(3), "operator@silverman.pro");

        Assert.NotEmpty(ideas);
        Assert.Equal(3, ideas.Count);
        Assert.All(ideas, idea =>
        {
            Assert.Equal(contentItem.Id, idea.ContentItemId);
            Assert.Equal(truthSource.Id, idea.TruthSourceId);
            Assert.Equal(tsVersion.Id, idea.TruthSourceVersionId);
            Assert.Equal(ContentIdeaStatus.Proposed, idea.Status);
            Assert.Equal(1, idea.Version);
        });

        // Triggering generation again should filter near-duplicate proposals
        var countBefore = (await ideaService.GetIdeasByContentItemIdAsync(contentItem.Id)).Count;
        Assert.Equal(3, countBefore);

        var ideasAfterSecondGen = await ideaService.GenerateAiIdeasAsync(contentItem.Id, new GenerateIdeasOptions(3), "operator@silverman.pro");
        Assert.Equal(countBefore, ideasAfterSecondGen.Count);
    }

    [Fact]
    public async Task ManualIdea_NearDuplicateCheck_RejectsEquivalentProposals()
    {
        var (ideaService, dbContext) = CreateTestService();

        var channel = new Channel { Id = Guid.NewGuid(), Name = "IA Simple ES", Slug = "ia-simple-es", Language = "es", Niche = "AI" };
        var contentItem = new ContentItem { Id = Guid.NewGuid(), ChannelId = channel.Id, Title = "Piece 1", Stage = ContentItemStage.TruthSourceApproved };
        var truthSource = new TruthSource { Id = Guid.NewGuid(), ContentItemId = contentItem.Id, Status = TruthSourceStatus.Approved, Summary = "Approved TS" };
        var tsVersion = new TruthSourceVersion { Id = Guid.NewGuid(), TruthSourceId = truthSource.Id, ContentItemId = contentItem.Id, VersionNumber = 1, SnapshotJson = "{}" };

        dbContext.Channels.Add(channel);
        dbContext.ContentItems.Add(contentItem);
        dbContext.TruthSources.Add(truthSource);
        dbContext.TruthSourceVersions.Add(tsVersion);
        await dbContext.SaveChangesAsync();

        var request1 = new CreateIdeaRequest(
            Title: "3 Habilidades Clave de IA en 2026",
            Angle: "Enfoque en criterio analítico y auditoría",
            HookStrategy: "¿Crees que un prompt te salvará el empleo?",
            AudienceValue: "Aprender auditoría práctica",
            Format: "YouTube Short 30-60s",
            IntendedOutcome: "Educativo",
            FreshnessClass: IdeaFreshnessClass.Timely,
            Priority: IdeaPriority.High,
            Rationale: "Basado en estudio laboral"
        );

        var idea1 = await ideaService.CreateManualIdeaAsync(contentItem.Id, request1, "operator@silverman.pro");
        Assert.NotNull(idea1);

        // Attempting to create materially equivalent idea should be rejected
        var requestDuplicate = new CreateIdeaRequest(
            Title: "3 Habilidades Clave de IA en 2026",
            Angle: "Enfoque en criterio analítico y auditoría",
            HookStrategy: "¿Crees que un prompt te salvará?",
            AudienceValue: "Aprender auditoría práctica",
            Format: "YouTube Short 30-60s",
            IntendedOutcome: "Educativo",
            FreshnessClass: IdeaFreshnessClass.Timely,
            Priority: IdeaPriority.High,
            Rationale: "Basado en estudio laboral"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ideaService.CreateManualIdeaAsync(contentItem.Id, requestDuplicate, "operator@silverman.pro");
        });
    }

    [Fact]
    public async Task FullSpectrumMutationConcurrency_DetectsConflictOnUpdateSelectDismissReopen()
    {
        var (ideaService, dbContext) = CreateTestService();

        var channel = new Channel { Id = Guid.NewGuid(), Name = "IA Simple ES", Slug = "ia-simple-es", Language = "es", Niche = "AI" };
        var contentItem = new ContentItem { Id = Guid.NewGuid(), ChannelId = channel.Id, Title = "Piece 1", Stage = ContentItemStage.TruthSourceApproved };
        var truthSource = new TruthSource { Id = Guid.NewGuid(), ContentItemId = contentItem.Id, Status = TruthSourceStatus.Approved, Summary = "Approved TS" };
        var tsVersion = new TruthSourceVersion { Id = Guid.NewGuid(), TruthSourceId = truthSource.Id, ContentItemId = contentItem.Id, VersionNumber = 1, SnapshotJson = "{}" };

        dbContext.Channels.Add(channel);
        dbContext.ContentItems.Add(contentItem);
        dbContext.TruthSources.Add(truthSource);
        dbContext.TruthSourceVersions.Add(tsVersion);
        await dbContext.SaveChangesAsync();

        var created = await ideaService.CreateManualIdeaAsync(contentItem.Id, new CreateIdeaRequest(
            "Idea Original", "Angulo 1", "Hook 1", "Valor 1", null, null, null, null, null), "operator@silverman.pro");

        Assert.Equal(1, created.Version);

        // 1. Valid update increments to version 2
        var updated = await ideaService.UpdateIdeaAsync(contentItem.Id, created.Id, new UpdateIdeaRequest(
            "Idea Modificada", "Angulo 1", "Hook 1", "Valor 1", null, null, null, null, null, "Cambio de título", 1), "operator@silverman.pro");

        Assert.Equal(2, updated.Version);
        Assert.Equal("Idea Modificada", updated.Title);

        // 2. Stale update with version 1 throws ConcurrencyConflictException
        await Assert.ThrowsAsync<ConcurrencyConflictException>(async () =>
        {
            await ideaService.UpdateIdeaAsync(contentItem.Id, created.Id, new UpdateIdeaRequest(
                "Stale Update", "Angulo 1", "Hook 1", "Valor 1", null, null, null, null, null, null, 1), "operator@silverman.pro");
        });

        // 3. Stale select with version 1 throws ConcurrencyConflictException
        await Assert.ThrowsAsync<ConcurrencyConflictException>(async () =>
        {
            await ideaService.SelectIdeaAsync(contentItem.Id, created.Id, new SelectIdeaRequest(1), "operator@silverman.pro");
        });

        // 4. Stale dismiss with version 1 throws ConcurrencyConflictException
        await Assert.ThrowsAsync<ConcurrencyConflictException>(async () =>
        {
            await ideaService.DismissIdeaAsync(contentItem.Id, created.Id, new DismissIdeaRequest("nota", 1), "operator@silverman.pro");
        });

        // 5. Stale reopen with version 1 throws ConcurrencyConflictException
        await Assert.ThrowsAsync<ConcurrencyConflictException>(async () =>
        {
            await ideaService.ReopenIdeaAsync(contentItem.Id, created.Id, new ReopenIdeaRequest(1), "operator@silverman.pro");
        });
    }

    [Fact]
    public async Task SingleActiveSelection_AtomicReplacement_AdvancesStageToIdeaSelected()
    {
        var (ideaService, dbContext) = CreateTestService();

        var channel = new Channel { Id = Guid.NewGuid(), Name = "IA Simple ES", Slug = "ia-simple-es", Language = "es", Niche = "AI" };
        var contentItem = new ContentItem { Id = Guid.NewGuid(), ChannelId = channel.Id, Title = "Piece 1", Stage = ContentItemStage.TruthSourceApproved };
        var truthSource = new TruthSource { Id = Guid.NewGuid(), ContentItemId = contentItem.Id, Status = TruthSourceStatus.Approved, Summary = "Approved TS" };
        var tsVersion = new TruthSourceVersion { Id = Guid.NewGuid(), TruthSourceId = truthSource.Id, ContentItemId = contentItem.Id, VersionNumber = 1, SnapshotJson = "{}" };

        dbContext.Channels.Add(channel);
        dbContext.ContentItems.Add(contentItem);
        dbContext.TruthSources.Add(truthSource);
        dbContext.TruthSourceVersions.Add(tsVersion);
        await dbContext.SaveChangesAsync();

        var ideaA = await ideaService.CreateManualIdeaAsync(contentItem.Id, new CreateIdeaRequest(
            "3 Habilidades que la IA no Reemplaza", "Enfoque analítico", "Crees que un prompt te salvará", "Aprender auditoría", null, null, null, null, null), "operator@silverman.pro");

        var ideaB = await ideaService.CreateManualIdeaAsync(contentItem.Id, new CreateIdeaRequest(
            "El Error Costoso al Usar Automatizaciones", "Riesgos operativos y legales", "Un fallo tonto en resumen", "Checklist de prevención", null, null, null, null, null), "operator@silverman.pro");

        // Select Idea A
        var selectedA = await ideaService.SelectIdeaAsync(contentItem.Id, ideaA.Id, new SelectIdeaRequest(ideaA.Version), "operator@silverman.pro");
        Assert.Equal(ContentIdeaStatus.Selected, selectedA.Status);

        var itemAfterA = await dbContext.ContentItems.FindAsync(contentItem.Id);
        Assert.NotNull(itemAfterA);
        Assert.Equal(ContentItemStage.IdeaSelected, itemAfterA.Stage);

        // Select Idea B -> should atomically revert Idea A to Proposed
        var selectedB = await ideaService.SelectIdeaAsync(contentItem.Id, ideaB.Id, new SelectIdeaRequest(ideaB.Version), "operator@silverman.pro");
        Assert.Equal(ContentIdeaStatus.Selected, selectedB.Status);

        var refreshedA = await ideaService.GetIdeaByIdAsync(contentItem.Id, ideaA.Id);
        Assert.NotNull(refreshedA);
        Assert.Equal(ContentIdeaStatus.Proposed, refreshedA.Status);

        var itemAfterB = await dbContext.ContentItems.FindAsync(contentItem.Id);
        Assert.NotNull(itemAfterB);
        Assert.Equal(ContentItemStage.IdeaSelected, itemAfterB.Stage);

        // Verify version history snapshots exist for both
        var versionsA = await ideaService.GetIdeaVersionsAsync(contentItem.Id, ideaA.Id);
        Assert.NotEmpty(versionsA);

        var versionsB = await ideaService.GetIdeaVersionsAsync(contentItem.Id, ideaB.Id);
        Assert.NotEmpty(versionsB);
    }
}
