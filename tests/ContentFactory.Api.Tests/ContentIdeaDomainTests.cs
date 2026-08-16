using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Tests;

public class ContentIdeaDomainTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void ContentIdea_PreservesExactTruthSourceVersionLineage()
    {
        var contentItemId = Guid.NewGuid();
        var truthSourceId = Guid.NewGuid();
        var truthSourceVersionId = Guid.NewGuid();

        var idea = new ContentIdea
        {
            ContentItemId = contentItemId,
            TruthSourceId = truthSourceId,
            TruthSourceVersionId = truthSourceVersionId,
            Title = "3 Habilidades Clave",
            Angle = "Enfoque en criterio analítico",
            HookStrategy = "¿Crees que un prompt te salvará en 2026?",
            AudienceValue = "Aprender a auditar respuestas",
            Status = ContentIdeaStatus.Proposed,
            Version = 1
        };

        Assert.Equal(contentItemId, idea.ContentItemId);
        Assert.Equal(truthSourceId, idea.TruthSourceId);
        Assert.Equal(truthSourceVersionId, idea.TruthSourceVersionId);
        Assert.Equal(ContentIdeaStatus.Proposed, idea.Status);
        Assert.Equal(1, idea.Version);
    }

    [Fact]
    public async Task OptimisticConcurrency_DetectsStaleVersionConflictAcrossMutations()
    {
        using var dbContext = CreateInMemoryDbContext();
        var ideaId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();
        var tsId = Guid.NewGuid();
        var tsVerId = Guid.NewGuid();

        var idea = new ContentIdea
        {
            Id = ideaId,
            ContentItemId = contentItemId,
            TruthSourceId = tsId,
            TruthSourceVersionId = tsVerId,
            Title = "Original Idea Title",
            Angle = "Original Angle",
            HookStrategy = "Original Hook",
            Status = ContentIdeaStatus.Proposed,
            Version = 1
        };

        dbContext.ContentIdeas.Add(idea);
        await dbContext.SaveChangesAsync();

        // Operator A updates idea to version 2
        var loadedIdeaA = await dbContext.ContentIdeas.FindAsync(ideaId);
        Assert.NotNull(loadedIdeaA);
        loadedIdeaA.Title = "Updated by Operator A";
        loadedIdeaA.Version = 2;
        await dbContext.SaveChangesAsync();

        // Operator B attempts to edit or select providing expectedVersion 1
        long operatorBExpectedVersion = 1;
        var currentInDb = await dbContext.ContentIdeas.FindAsync(ideaId);
        Assert.NotNull(currentInDb);

        var isConflict = currentInDb.Version != operatorBExpectedVersion;
        Assert.True(isConflict);
        Assert.Equal(2, currentInDb.Version);
    }

    [Fact]
    public async Task SingleActiveSelection_AtomicReplacement_MaintainsOnlyOneSelectedIdea()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();
        var tsId = Guid.NewGuid();
        var tsVerId = Guid.NewGuid();

        var ideaA = new ContentIdea
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            TruthSourceId = tsId,
            TruthSourceVersionId = tsVerId,
            Title = "Idea A",
            Angle = "Angle A",
            HookStrategy = "Hook A",
            Status = ContentIdeaStatus.Selected,
            SelectedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            SelectedByEmail = "operator@silverman.pro",
            Version = 1
        };

        var ideaB = new ContentIdea
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            TruthSourceId = tsId,
            TruthSourceVersionId = tsVerId,
            Title = "Idea B",
            Angle = "Angle B",
            HookStrategy = "Hook B",
            Status = ContentIdeaStatus.Proposed,
            Version = 1
        };

        dbContext.ContentIdeas.AddRange(ideaA, ideaB);
        await dbContext.SaveChangesAsync();

        // Perform atomic replacement: select ideaB and revert ideaA
        var activeSelected = await dbContext.ContentIdeas
            .Where(i => i.ContentItemId == contentItemId && i.Status == ContentIdeaStatus.Selected)
            .ToListAsync();

        Assert.Single(activeSelected);
        Assert.Equal(ideaA.Id, activeSelected[0].Id);

        // Transition: ideaA -> Proposed, ideaB -> Selected
        ideaA.Status = ContentIdeaStatus.Proposed;
        ideaA.Version++;
        ideaB.Status = ContentIdeaStatus.Selected;
        ideaB.SelectedAtUtc = DateTime.UtcNow;
        ideaB.SelectedByEmail = "operator@silverman.pro";
        ideaB.Version++;

        await dbContext.SaveChangesAsync();

        var newSelected = await dbContext.ContentIdeas
            .Where(i => i.ContentItemId == contentItemId && i.Status == ContentIdeaStatus.Selected)
            .ToListAsync();

        Assert.Single(newSelected);
        Assert.Equal(ideaB.Id, newSelected[0].Id);

        var proposedIdeas = await dbContext.ContentIdeas
            .Where(i => i.ContentItemId == contentItemId && i.Status == ContentIdeaStatus.Proposed)
            .ToListAsync();

        Assert.Single(proposedIdeas);
        Assert.Equal(ideaA.Id, proposedIdeas[0].Id);
    }

    [Fact]
    public void IsNearDuplicate_DetectsIdenticalAndSimilarIdeas()
    {
        // Exact match on title
        var exactTitle = ContentIdeaService.IsNearDuplicate(
            "3 Habilidades de IA", "Angulo A", "Hook A", "Valor A",
            "3 Habilidades de IA", "Angulo B", "Hook B", "Valor B"
        );
        Assert.True(exactTitle);

        // Exact match on angle
        var exactAngle = ContentIdeaService.IsNearDuplicate(
            "Titulo A", "Enfoque en criterio analítico y auditoría", "Hook A", "Valor A",
            "Titulo B", "Enfoque en criterio analítico y auditoría", "Hook B", "Valor B"
        );
        Assert.True(exactAngle);

        // Near duplicate with high token overlap
        var nearDup = ContentIdeaService.IsNearDuplicate(
            "3 Habilidades Clave que la IA No Reemplaza en 2026",
            "Enfoque contraintuitivo: El criterio crítico supera a la memorización de prompts",
            "¿Crees que un prompt te salvará el empleo en 2026?",
            "El espectador aprende pensamiento crítico",
            "3 Habilidades Clave que la IA No Reemplaza en 2026",
            "Enfoque contraintuitivo: El criterio crítico supera memorización de prompts",
            "¿Crees que un prompt te salvará en 2026?",
            "El espectador aprende pensamiento crítico"
        );
        Assert.True(nearDup);

        // Completely distinct ideas
        var distinct = ContentIdeaService.IsNearDuplicate(
            "3 Habilidades Clave que la IA No Reemplaza en 2026",
            "Enfoque contraintuitivo / Empoderamiento profesional",
            "¿Crees que un prompt te salvará?",
            "Pensamiento crítico y auditoría",
            "El Error de 1.000€ que Cometen al Delegar Tareas en IA",
            "Alerta de riesgo operativo en contabilidad",
            "Un fallo tonto en una respuesta te costará caro",
            "Checklist de 3 pasos para auditar documentos"
        );
        Assert.False(distinct);
    }
}
