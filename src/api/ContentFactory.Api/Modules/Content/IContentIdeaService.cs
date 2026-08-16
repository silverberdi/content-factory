using System.Text.Json;
using System.Text.RegularExpressions;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public interface IContentIdeaService
{
    Task<List<ContentIdeaDto>> GetIdeasByContentItemIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default);

    Task<ContentIdeaDto?> GetIdeaByIdAsync(
        Guid contentItemId,
        Guid ideaId,
        CancellationToken cancellationToken = default);

    Task<List<ContentIdeaVersionDto>> GetIdeaVersionsAsync(
        Guid contentItemId,
        Guid ideaId,
        CancellationToken cancellationToken = default);

    Task<List<ContentIdeaDto>> GenerateAiIdeasAsync(
        Guid contentItemId,
        GenerateIdeasOptions? options,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ContentIdeaDto> CreateManualIdeaAsync(
        Guid contentItemId,
        CreateIdeaRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ContentIdeaDto> UpdateIdeaAsync(
        Guid contentItemId,
        Guid ideaId,
        UpdateIdeaRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ContentIdeaDto> SelectIdeaAsync(
        Guid contentItemId,
        Guid ideaId,
        SelectIdeaRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ContentIdeaDto> DismissIdeaAsync(
        Guid contentItemId,
        Guid ideaId,
        DismissIdeaRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ContentIdeaDto> ReopenIdeaAsync(
        Guid contentItemId,
        Guid ideaId,
        ReopenIdeaRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);
}

public partial class ContentIdeaService(
    AppDbContext dbContext,
    IAiProviderRouter aiProviderRouter,
    IAuditService auditService,
    ILogger<ContentIdeaService> logger) : IContentIdeaService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "la", "el", "en", "y", "a", "los", "las", "un", "una", "unos", "unas",
        "que", "por", "para", "con", "no", "es", "son", "su", "sus", "al", "del",
        "se", "lo", "como", "mas", "más", "pero", "este", "esta", "estos", "estas",
        "the", "in", "to", "and", "of", "a", "for", "is", "on", "that", "by", "this", "with"
    };

    public async Task<List<ContentIdeaDto>> GetIdeasByContentItemIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var ideas = await dbContext.ContentIdeas
            .AsNoTracking()
            .Where(i => i.ContentItemId == contentItemId)
            .OrderByDescending(i => i.Status == ContentIdeaStatus.Selected)
            .ThenByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return ideas.Select(MapToDto).ToList();
    }

    public async Task<ContentIdeaDto?> GetIdeaByIdAsync(
        Guid contentItemId,
        Guid ideaId,
        CancellationToken cancellationToken = default)
    {
        var idea = await dbContext.ContentIdeas
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ContentItemId == contentItemId && i.Id == ideaId, cancellationToken);

        return idea != null ? MapToDto(idea) : null;
    }

    public async Task<List<ContentIdeaVersionDto>> GetIdeaVersionsAsync(
        Guid contentItemId,
        Guid ideaId,
        CancellationToken cancellationToken = default)
    {
        var versions = await dbContext.ContentIdeaVersions
            .AsNoTracking()
            .Where(v => v.ContentItemId == contentItemId && v.ContentIdeaId == ideaId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.Select(MapToVersionDto).ToList();
    }

    public async Task<List<ContentIdeaDto>> GenerateAiIdeasAsync(
        Guid contentItemId,
        GenerateIdeasOptions? options,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken)
            ?? throw new InvalidOperationException("ContentIdea generation requires an approved TruthSource.");

        if (truthSource.Status != TruthSourceStatus.Approved)
        {
            throw new InvalidOperationException("ContentIdea generation requires an approved TruthSource.");
        }

        // Fetch latest approved TruthSourceVersion for exact lineage reference
        var latestTruthSourceVersion = await dbContext.TruthSourceVersions
            .Where(v => v.TruthSourceId == truthSource.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var truthSourceVersionId = latestTruthSourceVersion?.Id ?? Guid.NewGuid();

        var channel = await dbContext.Channels
            .AsNoTracking()
            .FirstOrDefaultAsync(ch => ch.Id == contentItem.ChannelId, cancellationToken);

        var keyIdeas = DeserializeJsonList(truthSource.KeyIdeasJson);
        var verifiableClaims = DeserializeClaimsList(truthSource.VerifiableClaimsJson);
        var doNotSay = DeserializeJsonList(truthSource.DoNotSayConstraintsJson);
        var possibleAngles = DeserializeJsonList(truthSource.PossibleAnglesJson);

        var request = new GenerateIdeasRequest(
            ChannelId: contentItem.ChannelId,
            ChannelName: channel?.Name ?? "General",
            ChannelLanguage: channel?.Language ?? "es",
            ChannelNiche: channel?.Niche ?? "Technology",
            TruthSourceId: truthSource.Id,
            TruthSourceVersionId: truthSourceVersionId,
            Summary: truthSource.Summary,
            KeyIdeas: keyIdeas,
            VerifiableClaims: verifiableClaims,
            DoNotSayConstraints: doNotSay,
            PossibleAngles: possibleAngles,
            Count: options?.Count ?? 3,
            TargetAudience: options?.TargetAudience,
            FocusAngleStyle: options?.FocusAngleStyle
        );

        var context = new AiRoutingContext(contentItem.ChannelId, contentItemId);
        var aiResult = await aiProviderRouter.GenerateIdeasAsync(request, context, cancellationToken);

        if (!aiResult.Success || aiResult.Data == null || aiResult.Data.Ideas.Count == 0)
        {
            throw new InvalidOperationException($"AI idea generation failed: {aiResult.ErrorMessage ?? "No ideas generated."}");
        }

        // Fetch existing active ideas for near-duplicate filtering
        var existingActiveIdeas = await dbContext.ContentIdeas
            .Where(i => i.ContentItemId == contentItemId && i.Status != ContentIdeaStatus.Dismissed)
            .ToListAsync(cancellationToken);

        var addedIdeas = new List<ContentIdea>();

        foreach (var proposed in aiResult.Data.Ideas)
        {
            // Deterministic near-duplicate comparison against existing active ideas & newly added ideas in this batch
            var isDuplicate = existingActiveIdeas.Concat(addedIdeas).Any(existing =>
                IsNearDuplicate(
                    proposed.Title, proposed.Angle, proposed.HookStrategy, proposed.AudienceValue,
                    existing.Title, existing.Angle, existing.HookStrategy, existing.AudienceValue));

            if (isDuplicate)
            {
                logger.LogInformation("Filtered duplicate/near-duplicate idea proposal '{Title}' on ContentItem {ContentItemId}",
                    proposed.Title, contentItemId);
                continue;
            }

            var idea = new ContentIdea
            {
                Id = Guid.NewGuid(),
                ContentItemId = contentItemId,
                TruthSourceId = truthSource.Id,
                TruthSourceVersionId = truthSourceVersionId,
                Title = proposed.Title.Trim(),
                Angle = proposed.Angle.Trim(),
                HookStrategy = proposed.HookStrategy.Trim(),
                AudienceValue = proposed.AudienceValue.Trim(),
                Format = string.IsNullOrWhiteSpace(proposed.Format) ? "YouTube Short 30-60s" : proposed.Format.Trim(),
                IntendedOutcome = string.IsNullOrWhiteSpace(proposed.IntendedOutcome) ? "Educational" : proposed.IntendedOutcome.Trim(),
                FreshnessClass = string.IsNullOrWhiteSpace(proposed.FreshnessClass) ? IdeaFreshnessClass.Timely : proposed.FreshnessClass.Trim(),
                Priority = string.IsNullOrWhiteSpace(proposed.Priority) ? IdeaPriority.Normal : proposed.Priority.Trim(),
                Rationale = proposed.Rationale?.Trim() ?? string.Empty,
                Status = ContentIdeaStatus.Proposed,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByEmail = actorEmail,
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedByEmail = actorEmail
            };

            var versionSnapshot = new ContentIdeaVersion
            {
                Id = Guid.NewGuid(),
                ContentIdeaId = idea.Id,
                ContentItemId = contentItemId,
                TruthSourceId = truthSource.Id,
                TruthSourceVersionId = truthSourceVersionId,
                VersionNumber = 1,
                Title = idea.Title,
                Angle = idea.Angle,
                HookStrategy = idea.HookStrategy,
                AudienceValue = idea.AudienceValue,
                Format = idea.Format,
                IntendedOutcome = idea.IntendedOutcome,
                FreshnessClass = idea.FreshnessClass,
                Priority = idea.Priority,
                Rationale = idea.Rationale,
                Status = idea.Status,
                EditedByEmail = actorEmail,
                EditedAtUtc = DateTime.UtcNow,
                ChangeSummary = "Generado por IA a partir de TruthSource aprobado."
            };

            addedIdeas.Add(idea);
            dbContext.ContentIdeas.Add(idea);
            dbContext.ContentIdeaVersions.Add(versionSnapshot);
        }

        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "ContentIdea.AiGenerated",
            "ContentItem",
            contentItemId.ToString(),
            $"Generated {addedIdeas.Count} novel ideas from TruthSource v{truthSourceVersionId.ToString()[..8]}.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return await GetIdeasByContentItemIdAsync(contentItemId, cancellationToken);
    }

    public async Task<ContentIdeaDto> CreateManualIdeaAsync(
        Guid contentItemId,
        CreateIdeaRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");
        if (string.IsNullOrWhiteSpace(request.Angle))
            throw new ArgumentException("Angle is required.");
        if (string.IsNullOrWhiteSpace(request.HookStrategy))
            throw new ArgumentException("HookStrategy is required.");

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken)
            ?? throw new InvalidOperationException("ContentIdea creation requires an approved TruthSource.");

        if (truthSource.Status != TruthSourceStatus.Approved)
        {
            throw new InvalidOperationException("ContentIdea creation requires an approved TruthSource.");
        }

        var latestTruthSourceVersion = await dbContext.TruthSourceVersions
            .Where(v => v.TruthSourceId == truthSource.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var truthSourceVersionId = latestTruthSourceVersion?.Id ?? Guid.NewGuid();

        // Check for duplicate / near-duplicate against existing active ideas
        var existingActiveIdeas = await dbContext.ContentIdeas
            .Where(i => i.ContentItemId == contentItemId && i.Status != ContentIdeaStatus.Dismissed)
            .ToListAsync(cancellationToken);

        var isNearDuplicate = existingActiveIdeas.Any(existing =>
            IsNearDuplicate(
                request.Title, request.Angle, request.HookStrategy, request.AudienceValue ?? string.Empty,
                existing.Title, existing.Angle, existing.HookStrategy, existing.AudienceValue));

        if (isNearDuplicate)
        {
            throw new InvalidOperationException("An idea with a materially equivalent angle or hook already exists for this piece.");
        }

        var idea = new ContentIdea
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            TruthSourceId = truthSource.Id,
            TruthSourceVersionId = truthSourceVersionId,
            Title = request.Title.Trim(),
            Angle = request.Angle.Trim(),
            HookStrategy = request.HookStrategy.Trim(),
            AudienceValue = request.AudienceValue?.Trim() ?? string.Empty,
            Format = string.IsNullOrWhiteSpace(request.Format) ? "YouTube Short 30-60s" : request.Format.Trim(),
            IntendedOutcome = string.IsNullOrWhiteSpace(request.IntendedOutcome) ? "Educational" : request.IntendedOutcome.Trim(),
            FreshnessClass = string.IsNullOrWhiteSpace(request.FreshnessClass) ? IdeaFreshnessClass.Timely : request.FreshnessClass.Trim(),
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? IdeaPriority.Normal : request.Priority.Trim(),
            Rationale = request.Rationale?.Trim() ?? string.Empty,
            Status = ContentIdeaStatus.Proposed,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = actorEmail
        };

        var versionSnapshot = new ContentIdeaVersion
        {
            Id = Guid.NewGuid(),
            ContentIdeaId = idea.Id,
            ContentItemId = contentItemId,
            TruthSourceId = truthSource.Id,
            TruthSourceVersionId = truthSourceVersionId,
            VersionNumber = 1,
            Title = idea.Title,
            Angle = idea.Angle,
            HookStrategy = idea.HookStrategy,
            AudienceValue = idea.AudienceValue,
            Format = idea.Format,
            IntendedOutcome = idea.IntendedOutcome,
            FreshnessClass = idea.FreshnessClass,
            Priority = idea.Priority,
            Rationale = idea.Rationale,
            Status = idea.Status,
            EditedByEmail = actorEmail,
            EditedAtUtc = DateTime.UtcNow,
            ChangeSummary = "Creación manual por el operador."
        };

        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        dbContext.ContentIdeas.Add(idea);
        dbContext.ContentIdeaVersions.Add(versionSnapshot);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "ContentIdea.Created",
            "ContentIdea",
            idea.Id.ToString(),
            $"Created manual idea '{idea.Title}' against TruthSource v{truthSourceVersionId.ToString()[..8]}.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(idea);
    }

    public async Task<ContentIdeaDto> UpdateIdeaAsync(
        Guid contentItemId,
        Guid ideaId,
        UpdateIdeaRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");
        if (string.IsNullOrWhiteSpace(request.Angle))
            throw new ArgumentException("Angle is required.");
        if (string.IsNullOrWhiteSpace(request.HookStrategy))
            throw new ArgumentException("HookStrategy is required.");

        var idea = await dbContext.ContentIdeas
            .FirstOrDefaultAsync(i => i.ContentItemId == contentItemId && i.Id == ideaId, cancellationToken)
            ?? throw new ArgumentException("ContentIdea not found.");

        if (idea.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The idea was modified by another operator. Please reload latest changes.",
                idea.Version);
        }

        // Archive previous state into ContentIdeaVersion
        var versionSnapshot = new ContentIdeaVersion
        {
            Id = Guid.NewGuid(),
            ContentIdeaId = idea.Id,
            ContentItemId = contentItemId,
            TruthSourceId = idea.TruthSourceId,
            TruthSourceVersionId = idea.TruthSourceVersionId,
            VersionNumber = idea.Version,
            Title = idea.Title,
            Angle = idea.Angle,
            HookStrategy = idea.HookStrategy,
            AudienceValue = idea.AudienceValue,
            Format = idea.Format,
            IntendedOutcome = idea.IntendedOutcome,
            FreshnessClass = idea.FreshnessClass,
            Priority = idea.Priority,
            Rationale = idea.Rationale,
            Status = idea.Status,
            DismissalNotes = idea.DismissalNotes,
            EditedByEmail = actorEmail,
            EditedAtUtc = DateTime.UtcNow,
            ChangeSummary = string.IsNullOrWhiteSpace(request.ChangeSummary) ? "Edición manual por el operador." : request.ChangeSummary.Trim()
        };
        dbContext.ContentIdeaVersions.Add(versionSnapshot);

        idea.Title = request.Title.Trim();
        idea.Angle = request.Angle.Trim();
        idea.HookStrategy = request.HookStrategy.Trim();
        idea.AudienceValue = request.AudienceValue?.Trim() ?? string.Empty;
        idea.Format = string.IsNullOrWhiteSpace(request.Format) ? idea.Format : request.Format.Trim();
        idea.IntendedOutcome = string.IsNullOrWhiteSpace(request.IntendedOutcome) ? idea.IntendedOutcome : request.IntendedOutcome.Trim();
        idea.FreshnessClass = string.IsNullOrWhiteSpace(request.FreshnessClass) ? idea.FreshnessClass : request.FreshnessClass.Trim();
        idea.Priority = string.IsNullOrWhiteSpace(request.Priority) ? idea.Priority : request.Priority.Trim();
        idea.Rationale = request.Rationale?.Trim() ?? string.Empty;
        idea.Version += 1;
        idea.UpdatedAtUtc = DateTime.UtcNow;
        idea.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "ContentIdea.Updated",
            "ContentIdea",
            idea.Id.ToString(),
            $"Updated idea '{idea.Title}' to version {idea.Version}.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(idea);
    }

    public async Task<ContentIdeaDto> SelectIdeaAsync(
        Guid contentItemId,
        Guid ideaId,
        SelectIdeaRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var targetIdea = await dbContext.ContentIdeas
            .FirstOrDefaultAsync(i => i.ContentItemId == contentItemId && i.Id == ideaId, cancellationToken)
            ?? throw new ArgumentException("ContentIdea not found.");

        if (targetIdea.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The idea was modified by another operator. Please reload latest changes.",
                targetIdea.Version);
        }

        // Find existing selected idea if any
        var currentSelectedIdeas = await dbContext.ContentIdeas
            .Where(i => i.ContentItemId == contentItemId && i.Status == ContentIdeaStatus.Selected && i.Id != ideaId)
            .ToListAsync(cancellationToken);

        foreach (var priorSelected in currentSelectedIdeas)
        {
            // Snapshot prior selection state
            var priorVersion = new ContentIdeaVersion
            {
                Id = Guid.NewGuid(),
                ContentIdeaId = priorSelected.Id,
                ContentItemId = contentItemId,
                TruthSourceId = priorSelected.TruthSourceId,
                TruthSourceVersionId = priorSelected.TruthSourceVersionId,
                VersionNumber = priorSelected.Version,
                Title = priorSelected.Title,
                Angle = priorSelected.Angle,
                HookStrategy = priorSelected.HookStrategy,
                AudienceValue = priorSelected.AudienceValue,
                Format = priorSelected.Format,
                IntendedOutcome = priorSelected.IntendedOutcome,
                FreshnessClass = priorSelected.FreshnessClass,
                Priority = priorSelected.Priority,
                Rationale = priorSelected.Rationale,
                Status = priorSelected.Status,
                DismissalNotes = priorSelected.DismissalNotes,
                EditedByEmail = actorEmail,
                EditedAtUtc = DateTime.UtcNow,
                ChangeSummary = "Reemplazada como selección activa por otra idea."
            };
            dbContext.ContentIdeaVersions.Add(priorVersion);

            priorSelected.Status = ContentIdeaStatus.Proposed;
            priorSelected.Version += 1;
            priorSelected.UpdatedAtUtc = DateTime.UtcNow;
            priorSelected.UpdatedByEmail = actorEmail;

            await auditService.RecordAsync(
                "ContentIdea.SelectionReplaced",
                "ContentIdea",
                priorSelected.Id.ToString(),
                $"Idea '{priorSelected.Title}' was un-selected in favor of new selection.",
                actorUserId: null,
                actorEmail: actorEmail,
                correlationId: null,
                cancellationToken: cancellationToken);
        }

        // Snapshot target idea before setting to Selected
        var targetSnapshot = new ContentIdeaVersion
        {
            Id = Guid.NewGuid(),
            ContentIdeaId = targetIdea.Id,
            ContentItemId = contentItemId,
            TruthSourceId = targetIdea.TruthSourceId,
            TruthSourceVersionId = targetIdea.TruthSourceVersionId,
            VersionNumber = targetIdea.Version,
            Title = targetIdea.Title,
            Angle = targetIdea.Angle,
            HookStrategy = targetIdea.HookStrategy,
            AudienceValue = targetIdea.AudienceValue,
            Format = targetIdea.Format,
            IntendedOutcome = targetIdea.IntendedOutcome,
            FreshnessClass = targetIdea.FreshnessClass,
            Priority = targetIdea.Priority,
            Rationale = targetIdea.Rationale,
            Status = targetIdea.Status,
            DismissalNotes = targetIdea.DismissalNotes,
            EditedByEmail = actorEmail,
            EditedAtUtc = DateTime.UtcNow,
            ChangeSummary = "Seleccionada como base creativa para guionización."
        };
        dbContext.ContentIdeaVersions.Add(targetSnapshot);

        targetIdea.Status = ContentIdeaStatus.Selected;
        targetIdea.DismissalNotes = null;
        targetIdea.SelectedAtUtc = DateTime.UtcNow;
        targetIdea.SelectedByEmail = actorEmail;
        targetIdea.Version += 1;
        targetIdea.UpdatedAtUtc = DateTime.UtcNow;
        targetIdea.UpdatedByEmail = actorEmail;

        // Advance parent ContentItem lifecycle stage to IdeaSelected
        contentItem.Stage = ContentItemStage.IdeaSelected;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "ContentIdea.Selected",
            "ContentIdea",
            targetIdea.Id.ToString(),
            $"Idea '{targetIdea.Title}' selected for ContentItem '{contentItem.Title}'. Lifecycle stage is now IdeaSelected.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(targetIdea);
    }

    public async Task<ContentIdeaDto> DismissIdeaAsync(
        Guid contentItemId,
        Guid ideaId,
        DismissIdeaRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var idea = await dbContext.ContentIdeas
            .FirstOrDefaultAsync(i => i.ContentItemId == contentItemId && i.Id == ideaId, cancellationToken)
            ?? throw new ArgumentException("ContentIdea not found.");

        if (idea.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The idea was modified by another operator. Please reload latest changes.",
                idea.Version);
        }

        var versionSnapshot = new ContentIdeaVersion
        {
            Id = Guid.NewGuid(),
            ContentIdeaId = idea.Id,
            ContentItemId = contentItemId,
            TruthSourceId = idea.TruthSourceId,
            TruthSourceVersionId = idea.TruthSourceVersionId,
            VersionNumber = idea.Version,
            Title = idea.Title,
            Angle = idea.Angle,
            HookStrategy = idea.HookStrategy,
            AudienceValue = idea.AudienceValue,
            Format = idea.Format,
            IntendedOutcome = idea.IntendedOutcome,
            FreshnessClass = idea.FreshnessClass,
            Priority = idea.Priority,
            Rationale = idea.Rationale,
            Status = idea.Status,
            DismissalNotes = idea.DismissalNotes,
            EditedByEmail = actorEmail,
            EditedAtUtc = DateTime.UtcNow,
            ChangeSummary = string.IsNullOrWhiteSpace(request.Notes) ? "Descartada por el operador." : $"Descartada: {request.Notes.Trim()}"
        };
        dbContext.ContentIdeaVersions.Add(versionSnapshot);

        idea.Status = ContentIdeaStatus.Dismissed;
        idea.DismissalNotes = request.Notes?.Trim();
        idea.Version += 1;
        idea.UpdatedAtUtc = DateTime.UtcNow;
        idea.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "ContentIdea.Dismissed",
            "ContentIdea",
            idea.Id.ToString(),
            $"Dismissed idea '{idea.Title}'.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(idea);
    }

    public async Task<ContentIdeaDto> ReopenIdeaAsync(
        Guid contentItemId,
        Guid ideaId,
        ReopenIdeaRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var idea = await dbContext.ContentIdeas
            .FirstOrDefaultAsync(i => i.ContentItemId == contentItemId && i.Id == ideaId, cancellationToken)
            ?? throw new ArgumentException("ContentIdea not found.");

        if (idea.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The idea was modified by another operator. Please reload latest changes.",
                idea.Version);
        }

        var versionSnapshot = new ContentIdeaVersion
        {
            Id = Guid.NewGuid(),
            ContentIdeaId = idea.Id,
            ContentItemId = contentItemId,
            TruthSourceId = idea.TruthSourceId,
            TruthSourceVersionId = idea.TruthSourceVersionId,
            VersionNumber = idea.Version,
            Title = idea.Title,
            Angle = idea.Angle,
            HookStrategy = idea.HookStrategy,
            AudienceValue = idea.AudienceValue,
            Format = idea.Format,
            IntendedOutcome = idea.IntendedOutcome,
            FreshnessClass = idea.FreshnessClass,
            Priority = idea.Priority,
            Rationale = idea.Rationale,
            Status = idea.Status,
            DismissalNotes = idea.DismissalNotes,
            EditedByEmail = actorEmail,
            EditedAtUtc = DateTime.UtcNow,
            ChangeSummary = "Reabierta a estado Propuesta."
        };
        dbContext.ContentIdeaVersions.Add(versionSnapshot);

        idea.Status = ContentIdeaStatus.Proposed;
        idea.DismissalNotes = null;
        idea.Version += 1;
        idea.UpdatedAtUtc = DateTime.UtcNow;
        idea.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "ContentIdea.Reopened",
            "ContentIdea",
            idea.Id.ToString(),
            $"Reopened idea '{idea.Title}' to Proposed.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(idea);
    }

    public static bool IsNearDuplicate(
        string titleA, string angleA, string hookA, string audienceA,
        string titleB, string angleB, string hookB, string audienceB)
    {
        var normTitleA = NormalizeString(titleA);
        var normTitleB = NormalizeString(titleB);
        var normAngleA = NormalizeString(angleA);
        var normAngleB = NormalizeString(angleB);

        // 1. Exact match on normalized title or angle
        if (!string.IsNullOrWhiteSpace(normTitleA) && normTitleA == normTitleB) return true;
        if (!string.IsNullOrWhiteSpace(normAngleA) && normAngleA == normAngleB) return true;

        // 2. Title token similarity (>= 60%)
        var titleTokensA = ExtractTokens(titleA);
        var titleTokensB = ExtractTokens(titleB);
        if (titleTokensA.Count > 0 && titleTokensB.Count > 0)
        {
            var titleJaccard = (double)titleTokensA.Intersect(titleTokensB).Count() / titleTokensA.Union(titleTokensB).Count();
            if (titleJaccard >= 0.60) return true;
        }

        // 3. Angle token similarity (>= 60%)
        var angleTokensA = ExtractTokens(angleA);
        var angleTokensB = ExtractTokens(angleB);
        if (angleTokensA.Count > 0 && angleTokensB.Count > 0)
        {
            var angleJaccard = (double)angleTokensA.Intersect(angleTokensB).Count() / angleTokensA.Union(angleTokensB).Count();
            if (angleJaccard >= 0.60) return true;
        }

        // 4. Combined fields token overlap (>= 50%)
        var tokensA = ExtractTokens($"{titleA} {angleA} {hookA} {audienceA}");
        var tokensB = ExtractTokens($"{titleB} {angleB} {hookB} {audienceB}");

        if (tokensA.Count == 0 || tokensB.Count == 0) return false;

        var intersection = tokensA.Intersect(tokensB).Count();
        var union = tokensA.Union(tokensB).Count();
        var jaccard = (double)intersection / union;

        return jaccard >= 0.50;
    }

    private static HashSet<string> ExtractTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        var words = TokenRegex().Split(text.Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant())
            .Select(w => w.Trim())
            .Where(w => w.Length > 2 && !StopWords.Contains(w));
        return new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeString(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var normalized = text.Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant().Trim();
        normalized = NonAlphanumericRegex().Replace(normalized, " ");
        return WhitespaceRegex().Replace(normalized, " ").Trim();
    }

    private static ContentIdeaDto MapToDto(ContentIdea idea) =>
        new(
            idea.Id,
            idea.ContentItemId,
            idea.TruthSourceId,
            idea.TruthSourceVersionId,
            idea.Title,
            idea.Angle,
            idea.HookStrategy,
            idea.AudienceValue,
            idea.Format,
            idea.IntendedOutcome,
            idea.FreshnessClass,
            idea.Priority,
            idea.Rationale,
            idea.Status,
            idea.DismissalNotes,
            idea.SelectedAtUtc,
            idea.SelectedByEmail,
            idea.Version,
            idea.CreatedAtUtc,
            idea.CreatedByEmail,
            idea.UpdatedAtUtc,
            idea.UpdatedByEmail
        );

    private static ContentIdeaVersionDto MapToVersionDto(ContentIdeaVersion v) =>
        new(
            v.Id,
            v.ContentIdeaId,
            v.ContentItemId,
            v.TruthSourceId,
            v.TruthSourceVersionId,
            v.VersionNumber,
            v.Title,
            v.Angle,
            v.HookStrategy,
            v.AudienceValue,
            v.Format,
            v.IntendedOutcome,
            v.FreshnessClass,
            v.Priority,
            v.Rationale,
            v.Status,
            v.DismissalNotes,
            v.EditedByEmail,
            v.EditedAtUtc,
            v.ChangeSummary
        );

    private static List<string> DeserializeJsonList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }

    private static List<VerifiableClaimDto> DeserializeClaimsList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<VerifiableClaimDto>>(json) ?? []; }
        catch { return []; }
    }

    [GeneratedRegex(@"[^a-z0-9áéíóúüñ]+")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"[^a-z0-9áéíóúüñ\s]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
