using System.Text.Json;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public interface ITruthSourceService
{
    Task<TruthSourceDto?> GetTruthSourceAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default);

    Task<TruthSourceDto> GenerateAiDraftAsync(
        Guid contentItemId,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<TruthSourceDto> SaveTruthSourceAsync(
        Guid contentItemId,
        SaveTruthSourceRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<TruthSourceDto> SubmitForReviewAsync(
        Guid contentItemId,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<TruthSourceDto> ApproveTruthSourceAsync(
        Guid contentItemId,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<TruthSourceDto> RejectTruthSourceAsync(
        Guid contentItemId,
        RejectTruthSourceRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<List<TruthSourceVersionDto>> GetVersionHistoryAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateTruthSourceApprovedAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default);
}

public class TruthSourceService(
    AppDbContext dbContext,
    IAiProviderRouter aiProviderRouter,
    IAuditService auditService,
    ILogger<TruthSourceService> logger) : ITruthSourceService
{
    public async Task<TruthSourceDto?> GetTruthSourceAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var ts = await dbContext.TruthSources
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken);

        return ts != null ? MapToDto(ts) : null;
    }

    public async Task<TruthSourceDto> GenerateAiDraftAsync(
        Guid contentItemId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken);

        if (contentItem == null)
        {
            throw new ArgumentException("ContentItem not found.");
        }

        var channel = await dbContext.Channels
            .AsNoTracking()
            .FirstOrDefaultAsync(ch => ch.Id == contentItem.ChannelId, cancellationToken);

        var evidences = await dbContext.ContentItemEvidences
            .Where(e => e.ContentItemId == contentItemId && e.Status == EvidenceStatus.Captured)
            .OrderBy(e => e.Role == EvidenceRole.PrimaryLead ? 0 : 1)
            .ToListAsync(cancellationToken);

        if (evidences.Count == 0)
        {
            throw new InvalidOperationException("At least one successfully captured evidence item is required for TruthSource synthesis.");
        }

        var buildRequest = new BuildTruthSourceRequest(
            ChannelName: channel?.Name ?? "General",
            ChannelLanguage: channel?.Language ?? "es",
            ChannelNiche: channel?.Niche ?? "Technology",
            Evidences: evidences.Select(e => new EvidenceSnippetDto(
                e.Id,
                e.Title,
                e.OriginUrl,
                e.Role,
                e.ExtractedText ?? e.RawContent ?? e.Title
            )).ToList()
        );

        var routingContext = new AiRoutingContext(contentItem.ChannelId, contentItemId);
        var aiResult = await aiProviderRouter.BuildTruthSourceAsync(buildRequest, routingContext, cancellationToken);

        if (!aiResult.Success || aiResult.Data == null)
        {
            throw new InvalidOperationException($"AI synthesis failed: {aiResult.ErrorMessage ?? "Unknown error"}");
        }

        var data = aiResult.Data;
        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken);

        if (truthSource == null)
        {
            truthSource = new TruthSource
            {
                Id = Guid.NewGuid(),
                ContentItemId = contentItemId,
                Status = TruthSourceStatus.Draft,
                Summary = data.Summary,
                KeyIdeasJson = JsonSerializer.Serialize(data.KeyIdeas),
                VerifiableClaimsJson = JsonSerializer.Serialize(data.VerifiableClaims),
                EvidenceReferencesJson = JsonSerializer.Serialize(data.EvidenceReferences),
                RiskNotes = data.RiskNotes,
                DoNotSayConstraintsJson = JsonSerializer.Serialize(data.DoNotSayConstraints),
                PossibleAnglesJson = JsonSerializer.Serialize(data.PossibleAngles),
                LocalizationNotes = data.LocalizationNotes,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByEmail = actorEmail,
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedByEmail = actorEmail
            };
            dbContext.TruthSources.Add(truthSource);
        }
        else
        {
            // Archive previous version if it was reviewed or had version history
            if (truthSource.Version > 0)
            {
                var previousSnapshot = new TruthSourceVersion
                {
                    Id = Guid.NewGuid(),
                    TruthSourceId = truthSource.Id,
                    ContentItemId = contentItemId,
                    VersionNumber = truthSource.Version,
                    SnapshotJson = JsonSerializer.Serialize(truthSource),
                    SupportingEvidenceIdsJson = truthSource.EvidenceReferencesJson,
                    ChangeSummary = "Regenerado automáticamente por IA.",
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByEmail = actorEmail
                };
                dbContext.TruthSourceVersions.Add(previousSnapshot);
            }

            truthSource.Status = TruthSourceStatus.Draft;
            truthSource.Summary = data.Summary;
            truthSource.KeyIdeasJson = JsonSerializer.Serialize(data.KeyIdeas);
            truthSource.VerifiableClaimsJson = JsonSerializer.Serialize(data.VerifiableClaims);
            truthSource.EvidenceReferencesJson = JsonSerializer.Serialize(data.EvidenceReferences);
            truthSource.RiskNotes = data.RiskNotes;
            truthSource.DoNotSayConstraintsJson = JsonSerializer.Serialize(data.DoNotSayConstraints);
            truthSource.PossibleAnglesJson = JsonSerializer.Serialize(data.PossibleAngles);
            truthSource.LocalizationNotes = data.LocalizationNotes;
            truthSource.RejectionReason = null;
            truthSource.RejectedAtUtc = null;
            truthSource.RejectedByEmail = null;
            truthSource.Version += 1;
            truthSource.UpdatedAtUtc = DateTime.UtcNow;
            truthSource.UpdatedByEmail = actorEmail;
        }

        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "TruthSource.GeneratedByAI",
            "TruthSource",
            truthSource.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(truthSource);
    }

    public async Task<TruthSourceDto> SaveTruthSourceAsync(
        Guid contentItemId,
        SaveTruthSourceRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken);

        if (truthSource == null)
        {
            throw new ArgumentException("TruthSource not found.");
        }

        if (truthSource.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The TruthSource was modified by another operator. Please reload latest changes.",
                truthSource.Version);
        }

        // Archive previous state into TruthSourceVersion
        var versionSnapshot = new TruthSourceVersion
        {
            Id = Guid.NewGuid(),
            TruthSourceId = truthSource.Id,
            ContentItemId = contentItemId,
            VersionNumber = truthSource.Version,
            SnapshotJson = JsonSerializer.Serialize(truthSource),
            SupportingEvidenceIdsJson = truthSource.EvidenceReferencesJson,
            ChangeSummary = string.IsNullOrWhiteSpace(request.ChangeSummary) ? "Edición manual por el operador." : request.ChangeSummary.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail
        };
        dbContext.TruthSourceVersions.Add(versionSnapshot);

        // Update fields
        truthSource.Summary = request.Summary.Trim();
        truthSource.KeyIdeasJson = JsonSerializer.Serialize(request.KeyIdeas);
        truthSource.VerifiableClaimsJson = JsonSerializer.Serialize(request.VerifiableClaims);
        truthSource.EvidenceReferencesJson = JsonSerializer.Serialize(request.EvidenceReferences);
        truthSource.RiskNotes = request.RiskNotes.Trim();
        truthSource.DoNotSayConstraintsJson = JsonSerializer.Serialize(request.DoNotSayConstraints);
        truthSource.PossibleAnglesJson = JsonSerializer.Serialize(request.PossibleAngles);
        truthSource.LocalizationNotes = request.LocalizationNotes.Trim();
        truthSource.Version = request.ExpectedVersion + 1;
        truthSource.UpdatedAtUtc = DateTime.UtcNow;
        truthSource.UpdatedByEmail = actorEmail;

        var contentItem = await dbContext.ContentItems.FindAsync([contentItemId], cancellationToken);
        if (contentItem != null)
        {
            contentItem.UpdatedAtUtc = DateTime.UtcNow;
            contentItem.UpdatedByEmail = actorEmail;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "TruthSource.Edited",
            "TruthSource",
            truthSource.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(truthSource);
    }

    public async Task<TruthSourceDto> SubmitForReviewAsync(
        Guid contentItemId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken);

        if (truthSource == null)
        {
            throw new ArgumentException("TruthSource not found.");
        }

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken);

        if (contentItem == null)
        {
            throw new ArgumentException("ContentItem not found.");
        }

        truthSource.Status = TruthSourceStatus.UnderReview;
        truthSource.UpdatedAtUtc = DateTime.UtcNow;
        truthSource.UpdatedByEmail = actorEmail;

        // Create or update EditorialTask of type ReviewTruthSource
        var existingTask = await dbContext.EditorialTasks
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId && t.TaskType == EditorialTaskType.ReviewTruthSource && t.Status == EditorialTaskStatus.Pending, cancellationToken);

        if (existingTask == null)
        {
            var task = new EditorialTask
            {
                Id = Guid.NewGuid(),
                ChannelId = contentItem.ChannelId,
                ContentItemId = contentItemId,
                TaskType = EditorialTaskType.ReviewTruthSource,
                Priority = EditorialTaskPriority.Normal,
                Status = EditorialTaskStatus.Pending,
                DueDateUtc = DateTime.UtcNow.AddHours(24),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedByEmail = actorEmail
            };
            dbContext.EditorialTasks.Add(task);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "TruthSource.SubmittedForReview",
            "TruthSource",
            truthSource.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(truthSource);
    }

    public async Task<TruthSourceDto> ApproveTruthSourceAsync(
        Guid contentItemId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken);

        if (truthSource == null)
        {
            throw new ArgumentException("TruthSource not found.");
        }

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken);

        if (contentItem == null)
        {
            throw new ArgumentException("ContentItem not found.");
        }

        truthSource.Status = TruthSourceStatus.Approved;
        truthSource.ApprovedAtUtc = DateTime.UtcNow;
        truthSource.ApprovedByEmail = actorEmail;
        truthSource.RejectionReason = null;
        truthSource.UpdatedAtUtc = DateTime.UtcNow;
        truthSource.UpdatedByEmail = actorEmail;

        contentItem.Stage = ContentItemStage.TruthSourceApproved;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        // Complete any pending review tasks for this content item
        var pendingTasks = await dbContext.EditorialTasks
            .Where(t => t.ContentItemId == contentItemId && t.TaskType == EditorialTaskType.ReviewTruthSource && (t.Status == EditorialTaskStatus.Pending || t.Status == EditorialTaskStatus.InProgress))
            .ToListAsync(cancellationToken);

        foreach (var task in pendingTasks)
        {
            task.Status = EditorialTaskStatus.Completed;
            task.CompletedAtUtc = DateTime.UtcNow;
            task.CompletedByEmail = actorEmail;
            task.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "TruthSource.Approved",
            "TruthSource",
            truthSource.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(truthSource);
    }

    public async Task<TruthSourceDto> RejectTruthSourceAsync(
        Guid contentItemId,
        RejectTruthSourceRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Rejection reason is required and cannot be empty.");
        }

        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken);

        if (truthSource == null)
        {
            throw new ArgumentException("TruthSource not found.");
        }

        truthSource.Status = TruthSourceStatus.Rejected;
        truthSource.RejectionReason = request.Reason.Trim();
        truthSource.RejectedAtUtc = DateTime.UtcNow;
        truthSource.RejectedByEmail = actorEmail;
        truthSource.UpdatedAtUtc = DateTime.UtcNow;
        truthSource.UpdatedByEmail = actorEmail;

        // Complete / resolve the review task
        var pendingTasks = await dbContext.EditorialTasks
            .Where(t => t.ContentItemId == contentItemId && t.TaskType == EditorialTaskType.ReviewTruthSource && (t.Status == EditorialTaskStatus.Pending || t.Status == EditorialTaskStatus.InProgress))
            .ToListAsync(cancellationToken);

        foreach (var task in pendingTasks)
        {
            task.Status = EditorialTaskStatus.Completed;
            task.CompletedAtUtc = DateTime.UtcNow;
            task.CompletedByEmail = actorEmail;
            task.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "TruthSource.Rejected",
            "TruthSource",
            truthSource.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(truthSource);
    }

    public async Task<List<TruthSourceVersionDto>> GetVersionHistoryAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var versions = await dbContext.TruthSourceVersions
            .AsNoTracking()
            .Where(v => v.ContentItemId == contentItemId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.Select(v => new TruthSourceVersionDto(
            v.Id,
            v.TruthSourceId,
            v.ContentItemId,
            v.VersionNumber,
            v.SnapshotJson,
            JsonSerializer.Deserialize<List<Guid>>(v.SupportingEvidenceIdsJson) ?? [],
            v.ChangeSummary,
            v.CreatedAtUtc,
            v.CreatedByEmail
        )).ToList();
    }

    public async Task<bool> ValidateTruthSourceApprovedAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var truthSource = await dbContext.TruthSources
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken);

        return truthSource != null && truthSource.Status == TruthSourceStatus.Approved;
    }

    private static TruthSourceDto MapToDto(TruthSource ts)
    {
        var keyIdeas = JsonSerializer.Deserialize<List<string>>(ts.KeyIdeasJson) ?? [];
        var claims = JsonSerializer.Deserialize<List<VerifiableClaimDto>>(ts.VerifiableClaimsJson) ?? [];
        var refs = JsonSerializer.Deserialize<List<Guid>>(ts.EvidenceReferencesJson) ?? [];
        var constraints = JsonSerializer.Deserialize<List<string>>(ts.DoNotSayConstraintsJson) ?? [];
        var angles = JsonSerializer.Deserialize<List<string>>(ts.PossibleAnglesJson) ?? [];

        return new TruthSourceDto(
            ts.Id,
            ts.ContentItemId,
            ts.Status,
            ts.Summary,
            keyIdeas,
            claims,
            refs,
            ts.RiskNotes,
            constraints,
            angles,
            ts.LocalizationNotes,
            ts.RejectionReason,
            ts.RejectedAtUtc,
            ts.RejectedByEmail,
            ts.ApprovedAtUtc,
            ts.ApprovedByEmail,
            ts.Version,
            ts.CreatedAtUtc,
            ts.CreatedByEmail,
            ts.UpdatedAtUtc,
            ts.UpdatedByEmail
        );
    }
}
