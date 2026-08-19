using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContentFactory.Api.Tests;

public class ScriptApiIntegrationTests
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

    private static (ScriptService scriptService, AppDbContext dbContext) CreateTestService()
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
        var scriptService = new ScriptService(dbContext, aiRouter, auditService, NullLogger<ScriptService>.Instance);
        return (scriptService, dbContext);
    }

    private static async Task<(ContentItem item, ContentIdea idea, TruthSource ts, TruthSourceVersion tsVer)> SeedPrerequisitesAsync(AppDbContext dbContext)
    {
        var channel = new Channel { Id = Guid.NewGuid(), Name = "IA Simple ES", Slug = "ia-simple-es", Language = "es", Niche = "AI" };
        var contentItem = new ContentItem { Id = Guid.NewGuid(), ChannelId = channel.Id, Title = "Piece 1", Slug = "piece-1", Stage = ContentItemStage.IdeaSelected };
        var truthSource = new TruthSource
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItem.Id,
            Status = TruthSourceStatus.Approved,
            Summary = "Approved factual basis",
            KeyIdeasJson = "[\"Key idea 1\"]",
            VerifiableClaimsJson = "[{\"claim\":\"Claim 1\",\"sourceCitation\":\"Citation 1\",\"evidenceId\":\"00000000-0000-0000-0000-000000000001\"}]",
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
        var idea = new ContentIdea
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItem.Id,
            TruthSourceId = truthSource.Id,
            TruthSourceVersionId = tsVersion.Id,
            Title = "Selected Idea",
            Angle = "Analytical angle",
            HookStrategy = "Hook pattern",
            Status = ContentIdeaStatus.Selected,
            Version = 1
        };

        dbContext.Channels.Add(channel);
        dbContext.ContentItems.Add(contentItem);
        dbContext.TruthSources.Add(truthSource);
        dbContext.TruthSourceVersions.Add(tsVersion);
        dbContext.ContentIdeas.Add(idea);
        await dbContext.SaveChangesAsync();

        return (contentItem, idea, truthSource, tsVersion);
    }

    [Fact]
    public async Task CreateScript_CalculatesAggregates_AndAdvancesStageToScriptDrafted()
    {
        var (scriptService, dbContext) = CreateTestService();
        var (contentItem, idea, _, _) = await SeedPrerequisitesAsync(dbContext);

        var request = new CreateScriptRequest(
            Title: "3 Claves de IA",
            TargetDurationSeconds: 45,
            PacingWpm: 140,
            Language: "es-ES",
            Scenes:
            [
                new(null, 1, SceneType.Hook, "¿Crees que la IA te reemplazará?", "Primer plano", null),
                new(null, 2, SceneType.Problem, "Muchos memorizan prompts inútiles.", "B-roll oficina", null),
                new(null, 3, SceneType.Insight, "El criterio analítico marca la diferencia.", "Animación", null),
                new(null, 4, SceneType.Climax, "Aprende a auditar respuestas.", "Gráfico 3 pasos", null),
                new(null, 5, SceneType.CallToAction, "Guarda este video.", "Botón guardar", null)
            ]
        );

        var created = await scriptService.CreateScriptAsync(contentItem.Id, request, "operator@silverman.pro");

        Assert.NotNull(created);
        Assert.Equal(1, created.Version);
        Assert.Equal(ScriptStatus.Draft, created.Status);
        Assert.Equal(5, created.Scenes.Count);
        Assert.True(created.TotalWordCount > 0);
        Assert.True(created.EstimatedDurationSeconds > 0);

        var refreshedItem = await dbContext.ContentItems.FindAsync(contentItem.Id);
        Assert.NotNull(refreshedItem);
        Assert.Equal(ContentItemStage.ScriptDrafted, refreshedItem.Stage);
    }

    [Fact]
    public async Task GenerateAiScript_CreatesStructuredScript_With5ScenesAndClaims()
    {
        var (scriptService, dbContext) = CreateTestService();
        var (contentItem, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        var options = new GenerateScriptOptions(TargetDurationSeconds: 45, PacingWpm: 140);
        var generated = await scriptService.GenerateAiScriptAsync(contentItem.Id, options, "operator@silverman.pro");

        Assert.NotNull(generated);
        Assert.Equal(5, generated.Scenes.Count);
        Assert.Equal(SceneType.Hook, generated.Scenes[0].SceneType);
        Assert.Equal(SceneType.CallToAction, generated.Scenes[4].SceneType);
        Assert.Equal(140, generated.PacingWpm);
        Assert.Equal(1, generated.Version);
        Assert.Equal(ScriptStatus.Draft, generated.Status);

        // Verify that recommendation record was saved in AI telemetry
        var rec = await dbContext.AiRecommendations.FirstOrDefaultAsync(r => r.Capability == AiCapabilities.GenerateScript);
        Assert.NotNull(rec);
        Assert.Equal(contentItem.Id, rec.ContentItemId);
    }

    [Fact]
    public async Task FullLifecycle_Draft_Submit_Approve_CompletesReviewTask()
    {
        var (scriptService, dbContext) = CreateTestService();
        var (contentItem, _, _, _) = await SeedPrerequisitesAsync(dbContext);

        // 1. Create Script
        var createRequest = new CreateScriptRequest("Script Lifecycle", 45, 140, "es-ES", null);
        var script = await scriptService.CreateScriptAsync(contentItem.Id, createRequest, "operator@silverman.pro");

        // 2. Submit for review
        var submitRequest = new SubmitScriptForReviewRequest(ExpectedVersion: 1);
        var submitted = await scriptService.SubmitForReviewAsync(contentItem.Id, script.Id, submitRequest, "operator@silverman.pro");

        Assert.Equal(ScriptStatus.UnderReview, submitted.Status);
        Assert.Equal(2, submitted.Version);

        var itemUnderReview = await dbContext.ContentItems.FindAsync(contentItem.Id);
        Assert.NotNull(itemUnderReview);
        Assert.Equal(ContentItemStage.ScriptUnderReview, itemUnderReview.Stage);

        // Editorial task should have been automatically created
        var task = await dbContext.EditorialTasks.FirstOrDefaultAsync(t => t.ContentItemId == contentItem.Id && t.TaskType == EditorialTaskType.ReviewScript);
        Assert.NotNull(task);
        Assert.Equal(EditorialTaskStatus.Pending, task.Status);

        // 3. Approve script
        var approveRequest = new ApproveScriptRequest(ExpectedVersion: 2);
        var approved = await scriptService.ApproveScriptAsync(contentItem.Id, script.Id, approveRequest, "editorial.lead@silverman.pro");

        Assert.Equal(ScriptStatus.Approved, approved.Status);
        Assert.Equal(3, approved.Version);

        var itemApproved = await dbContext.ContentItems.FindAsync(contentItem.Id);
        Assert.NotNull(itemApproved);
        Assert.Equal(ContentItemStage.ScriptApproved, itemApproved.Stage);

        // Editorial task should now be Completed
        var completedTask = await dbContext.EditorialTasks.FindAsync(task.Id);
        Assert.NotNull(completedTask);
        Assert.Equal(EditorialTaskStatus.Completed, completedTask.Status);
    }

    [Fact]
    public async Task StaleScript_CannotBeApproved_ThrowsInvalidOperationException()
    {
        var (scriptService, dbContext) = CreateTestService();
        var (contentItem, idea, _, _) = await SeedPrerequisitesAsync(dbContext);

        var createRequest = new CreateScriptRequest("Stale Test Script", 45, 140, "es-ES", null);
        var script = await scriptService.CreateScriptAsync(contentItem.Id, createRequest, "operator@silverman.pro");

        // Now modify selected idea to a new one
        var newIdea = new ContentIdea
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItem.Id,
            TruthSourceId = idea.TruthSourceId,
            TruthSourceVersionId = idea.TruthSourceVersionId,
            Title = "New Selected Idea",
            Angle = "New Angle",
            HookStrategy = "New Hook",
            Status = ContentIdeaStatus.Selected,
            Version = 1
        };
        idea.Status = ContentIdeaStatus.Proposed;
        dbContext.ContentIdeas.Add(newIdea);
        await dbContext.SaveChangesAsync();

        // Attempting to approve the stale script must be blocked
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await scriptService.ApproveScriptAsync(contentItem.Id, script.Id, new ApproveScriptRequest(ExpectedVersion: 1), "editorial.lead@silverman.pro");
        });
    }
}
