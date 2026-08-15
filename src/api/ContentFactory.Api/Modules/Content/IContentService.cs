using System.Text.Json;
using System.Text.RegularExpressions;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Discovery;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public interface IContentService
{
    Task<List<ContentItemDto>> GetContentItemsAsync(
        Guid? channelId = null,
        string? stage = null,
        string? status = null,
        string? searchQuery = null,
        CancellationToken cancellationToken = default);

    Task<ContentItemDetailDto?> GetContentItemDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ContentItemDto> CreateContentItemAsync(
        CreateContentItemRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ContentItemDto?> UpdateContentItemAsync(
        Guid id,
        UpdateContentItemRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ContentItemEvidenceDto> AttachEvidenceAsync(
        Guid contentItemId,
        AttachEvidenceRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<bool> DetachOrExcludeEvidenceAsync(
        Guid contentItemId,
        Guid evidenceId,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ContentItemEvidenceDto?> RetryEvidenceCaptureAsync(
        Guid contentItemId,
        Guid evidenceId,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ContentItemDto> InitiateContentFromCandidateAsync(
        Guid candidateId,
        InitiateContentFromCandidateRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ContentItemEvidenceDto> AttachCandidateToContentAsync(
        Guid candidateId,
        AttachCandidateToContentRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);
}

public class ConcurrencyConflictException(string message, long currentVersion) : Exception(message)
{
    public long CurrentVersion { get; } = currentVersion;
}

public partial class ContentService(
    AppDbContext dbContext,
    IEvidenceCaptureService evidenceCaptureService,
    IAuditService auditService,
    ILogger<ContentService> logger) : IContentService
{
    public async Task<List<ContentItemDto>> GetContentItemsAsync(
        Guid? channelId = null,
        string? stage = null,
        string? status = null,
        string? searchQuery = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ContentItems.AsNoTracking().AsQueryable();

        if (channelId.HasValue && channelId.Value != Guid.Empty)
        {
            query = query.Where(c => c.ChannelId == channelId.Value);
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(c => c.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim();
            query = query.Where(c => c.Title.Contains(search) || c.Slug.Contains(search));
        }

        var items = await query
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        var channelIds = items.Select(i => i.ChannelId).Distinct().ToList();
        var channels = await dbContext.Channels
            .Where(ch => channelIds.Contains(ch.Id))
            .ToDictionaryAsync(ch => ch.Id, ch => ch.Name, cancellationToken);

        var itemIds = items.Select(i => i.Id).ToList();
        var evidenceCounts = await dbContext.ContentItemEvidences
            .Where(e => itemIds.Contains(e.ContentItemId) && e.Status != EvidenceStatus.Excluded)
            .GroupBy(e => e.ContentItemId)
            .Select(g => new { ContentItemId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ContentItemId, g => g.Count, cancellationToken);

        var truthSources = await dbContext.TruthSources
            .Where(t => itemIds.Contains(t.ContentItemId))
            .ToDictionaryAsync(t => t.ContentItemId, t => new { t.Status, t.Version }, cancellationToken);

        return items.Select(item =>
        {
            channels.TryGetValue(item.ChannelId, out var channelName);
            evidenceCounts.TryGetValue(item.Id, out var count);
            truthSources.TryGetValue(item.Id, out var tsInfo);

            return new ContentItemDto(
                item.Id,
                item.ChannelId,
                channelName,
                item.Title,
                item.Slug,
                item.Stage,
                item.Status,
                item.Version,
                count,
                tsInfo?.Status,
                tsInfo?.Version,
                item.CreatedAtUtc,
                item.CreatedByEmail,
                item.UpdatedAtUtc,
                item.UpdatedByEmail
            );
        }).ToList();
    }

    public async Task<ContentItemDetailDto?> GetContentItemDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.ContentItems
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (item == null) return null;

        var channel = await dbContext.Channels
            .AsNoTracking()
            .FirstOrDefaultAsync(ch => ch.Id == item.ChannelId, cancellationToken);

        var evidences = await dbContext.ContentItemEvidences
            .AsNoTracking()
            .Where(e => e.ContentItemId == id)
            .OrderBy(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var truthSource = await dbContext.TruthSources
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ContentItemId == id, cancellationToken);

        TruthSourceDto? truthSourceDto = null;
        if (truthSource != null)
        {
            truthSourceDto = MapTruthSourceToDto(truthSource);
        }

        var evidenceDtos = evidences.Select(e => new ContentItemEvidenceDto(
            e.Id,
            e.ContentItemId,
            e.DiscoveryCandidateId,
            e.OriginUrl,
            e.Title,
            e.Role,
            e.Status,
            e.RawContent,
            e.ObjectStorageKey,
            e.ExtractedText,
            e.ContentHash,
            e.ErrorMessage,
            e.Notes,
            e.Author,
            e.CapturedAtUtc,
            e.CreatedAtUtc,
            e.CreatedByEmail
        )).ToList();

        return new ContentItemDetailDto(
            item.Id,
            item.ChannelId,
            channel?.Name,
            item.Title,
            item.Slug,
            item.Stage,
            item.Status,
            item.Version,
            item.CreatedAtUtc,
            item.CreatedByEmail,
            item.UpdatedAtUtc,
            item.UpdatedByEmail,
            evidenceDtos,
            truthSourceDto
        );
    }

    public async Task<ContentItemDto> CreateContentItemAsync(
        CreateContentItemRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (request.ChannelId == Guid.Empty)
        {
            throw new ArgumentException("ChannelId is required.");
        }

        var channelExists = await dbContext.Channels.AnyAsync(c => c.Id == request.ChannelId, cancellationToken);
        if (!channelExists)
        {
            throw new ArgumentException("Channel not found.");
        }

        var title = string.IsNullOrWhiteSpace(request.Title) ? "Nuevo Contenido" : request.Title.Trim();
        var slug = GenerateSlug(title);

        var item = new ContentItem
        {
            Id = Guid.NewGuid(),
            ChannelId = request.ChannelId,
            Title = title,
            Slug = slug,
            Stage = ContentItemStage.DraftingEvidence,
            Status = ContentItemStatus.Active,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = actorEmail
        };

        dbContext.ContentItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "ContentItem.Created",
            "ContentItem",
            item.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var channel = await dbContext.Channels.FindAsync([request.ChannelId], cancellationToken);

        return new ContentItemDto(
            item.Id,
            item.ChannelId,
            channel?.Name,
            item.Title,
            item.Slug,
            item.Stage,
            item.Status,
            item.Version,
            0,
            null,
            null,
            item.CreatedAtUtc,
            item.CreatedByEmail,
            item.UpdatedAtUtc,
            item.UpdatedByEmail
        );
    }

    public async Task<ContentItemDto?> UpdateContentItemAsync(
        Guid id,
        UpdateContentItemRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.ContentItems.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (item == null) return null;

        if (item.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The ContentItem was modified by another operator. Please reload latest changes.",
                item.Version);
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            item.Title = request.Title.Trim();
            item.Slug = GenerateSlug(item.Title);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && ContentItemStatus.All.Contains(request.Status))
        {
            item.Status = request.Status;
        }

        item.Version = request.ExpectedVersion + 1;
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "ContentItem.Updated",
            "ContentItem",
            item.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var channel = await dbContext.Channels.FindAsync([item.ChannelId], cancellationToken);
        var evidenceCount = await dbContext.ContentItemEvidences
            .CountAsync(e => e.ContentItemId == id && e.Status != EvidenceStatus.Excluded, cancellationToken);
        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == id, cancellationToken);

        return new ContentItemDto(
            item.Id,
            item.ChannelId,
            channel?.Name,
            item.Title,
            item.Slug,
            item.Stage,
            item.Status,
            item.Version,
            evidenceCount,
            truthSource?.Status,
            truthSource?.Version,
            item.CreatedAtUtc,
            item.CreatedByEmail,
            item.UpdatedAtUtc,
            item.UpdatedByEmail
        );
    }

    public async Task<ContentItemEvidenceDto> AttachEvidenceAsync(
        Guid contentItemId,
        AttachEvidenceRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.ContentItems.FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken);
        if (item == null)
        {
            throw new ArgumentException("ContentItem not found.");
        }

        var result = await evidenceCaptureService.CaptureEvidenceAsync(
            contentItemId,
            request.DiscoveryCandidateId,
            request.OriginUrl,
            request.Title,
            request.ContentText,
            request.Role ?? EvidenceRole.SupportingEvidence,
            request.Notes,
            actorEmail,
            cancellationToken);

        item.UpdatedAtUtc = DateTime.UtcNow;
        item.UpdatedByEmail = actorEmail;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "ContentItem.EvidenceAttached",
            "ContentItemEvidence",
            result.Evidence.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var e = result.Evidence;
        return new ContentItemEvidenceDto(
            e.Id,
            e.ContentItemId,
            e.DiscoveryCandidateId,
            e.OriginUrl,
            e.Title,
            e.Role,
            e.Status,
            e.RawContent,
            e.ObjectStorageKey,
            e.ExtractedText,
            e.ContentHash,
            e.ErrorMessage,
            e.Notes,
            e.Author,
            e.CapturedAtUtc,
            e.CreatedAtUtc,
            e.CreatedByEmail
        );
    }

    public async Task<bool> DetachOrExcludeEvidenceAsync(
        Guid contentItemId,
        Guid evidenceId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var evidence = await dbContext.ContentItemEvidences
            .FirstOrDefaultAsync(e => e.Id == evidenceId && e.ContentItemId == contentItemId, cancellationToken);

        if (evidence == null) return false;

        // Check if this evidence has contributed to an existing TruthSource version
        var isUsedInVersion = await dbContext.TruthSourceVersions
            .AnyAsync(v => v.ContentItemId == contentItemId && v.SupportingEvidenceIdsJson.Contains(evidenceId.ToString()), cancellationToken);

        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken);

        var isUsedInActiveTs = truthSource != null && truthSource.EvidenceReferencesJson.Contains(evidenceId.ToString());

        if (isUsedInVersion || isUsedInActiveTs)
        {
            // Non-destructive exclusion
            evidence.Status = EvidenceStatus.Excluded;
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.RecordAsync(
                "ContentItem.EvidenceExcluded",
                "ContentItemEvidence",
                evidenceId.ToString(),
                detailsJson: null,
                actorUserId: null,
                actorEmail: actorEmail,
                correlationId: null,
                cancellationToken: cancellationToken);
        }
        else
        {
            // Uncommitted evidence can be detached physically
            dbContext.ContentItemEvidences.Remove(evidence);
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.RecordAsync(
                "ContentItem.EvidenceDetached",
                "ContentItemEvidence",
                evidenceId.ToString(),
                detailsJson: null,
                actorUserId: null,
                actorEmail: actorEmail,
                correlationId: null,
                cancellationToken: cancellationToken);
        }

        return true;
    }

    public async Task<ContentItemEvidenceDto?> RetryEvidenceCaptureAsync(
        Guid contentItemId,
        Guid evidenceId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var result = await evidenceCaptureService.RetryCaptureAsync(contentItemId, evidenceId, actorEmail, cancellationToken);
        if (result.Evidence == null) return null;

        var e = result.Evidence;
        return new ContentItemEvidenceDto(
            e.Id,
            e.ContentItemId,
            e.DiscoveryCandidateId,
            e.OriginUrl,
            e.Title,
            e.Role,
            e.Status,
            e.RawContent,
            e.ObjectStorageKey,
            e.ExtractedText,
            e.ContentHash,
            e.ErrorMessage,
            e.Notes,
            e.Author,
            e.CapturedAtUtc,
            e.CreatedAtUtc,
            e.CreatedByEmail
        );
    }

    public async Task<ContentItemDto> InitiateContentFromCandidateAsync(
        Guid candidateId,
        InitiateContentFromCandidateRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.DiscoveryCandidates.FirstOrDefaultAsync(c => c.Id == candidateId, cancellationToken);
        if (candidate == null)
        {
            throw new ArgumentException("DiscoveryCandidate not found.");
        }

        // Duplicate prevention: check if candidate is already primary lead on an existing active ContentItem
        var existingLead = await dbContext.ContentItemEvidences
            .Where(e => e.DiscoveryCandidateId == candidateId && e.Role == EvidenceRole.PrimaryLead && e.Status != EvidenceStatus.Excluded)
            .Select(e => e.ContentItemId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingLead != Guid.Empty)
        {
            var existingItem = await dbContext.ContentItems.FindAsync([existingLead], cancellationToken);
            if (existingItem != null)
            {
                var ch = await dbContext.Channels.FindAsync([existingItem.ChannelId], cancellationToken);
                var count = await dbContext.ContentItemEvidences.CountAsync(e => e.ContentItemId == existingItem.Id, cancellationToken);
                var ts = await dbContext.TruthSources.FirstOrDefaultAsync(t => t.ContentItemId == existingItem.Id, cancellationToken);

                return new ContentItemDto(
                    existingItem.Id,
                    existingItem.ChannelId,
                    ch?.Name,
                    existingItem.Title,
                    existingItem.Slug,
                    existingItem.Stage,
                    existingItem.Status,
                    existingItem.Version,
                    count,
                    ts?.Status,
                    ts?.Version,
                    existingItem.CreatedAtUtc,
                    existingItem.CreatedByEmail,
                    existingItem.UpdatedAtUtc,
                    existingItem.UpdatedByEmail
                );
            }
        }

        var title = !string.IsNullOrWhiteSpace(request.TitleOverride) ? request.TitleOverride.Trim() : candidate.Title;
        var slug = GenerateSlug(title);

        var newItem = new ContentItem
        {
            Id = Guid.NewGuid(),
            ChannelId = candidate.ChannelId,
            Title = title,
            Slug = slug,
            Stage = ContentItemStage.DraftingEvidence,
            Status = ContentItemStatus.Active,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = actorEmail
        };

        dbContext.ContentItems.Add(newItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Capture evidence
        await evidenceCaptureService.CaptureEvidenceAsync(
            newItem.Id,
            candidate.Id,
            candidate.ExternalUrl,
            candidate.Title,
            candidate.RawContent ?? candidate.Summary,
            EvidenceRole.PrimaryLead,
            candidate.EditorialNotes,
            actorEmail,
            cancellationToken);

        // Update candidate status to Promoted if it was pending
        if (candidate.Status != DiscoveryCandidateStatus.Promoted)
        {
            candidate.Status = DiscoveryCandidateStatus.Promoted;
            candidate.PromotedAtUtc = DateTime.UtcNow;
            candidate.PromotedByEmail = actorEmail;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "DiscoveryCandidate.InitiatedContentItem",
            "ContentItem",
            newItem.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var channel = await dbContext.Channels.FindAsync([newItem.ChannelId], cancellationToken);

        return new ContentItemDto(
            newItem.Id,
            newItem.ChannelId,
            channel?.Name,
            newItem.Title,
            newItem.Slug,
            newItem.Stage,
            newItem.Status,
            newItem.Version,
            1,
            null,
            null,
            newItem.CreatedAtUtc,
            newItem.CreatedByEmail,
            newItem.UpdatedAtUtc,
            newItem.UpdatedByEmail
        );
    }

    public async Task<ContentItemEvidenceDto> AttachCandidateToContentAsync(
        Guid candidateId,
        AttachCandidateToContentRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.DiscoveryCandidates.FirstOrDefaultAsync(c => c.Id == candidateId, cancellationToken);
        if (candidate == null)
        {
            throw new ArgumentException("DiscoveryCandidate not found.");
        }

        var item = await dbContext.ContentItems.FirstOrDefaultAsync(c => c.Id == request.ContentItemId, cancellationToken);
        if (item == null)
        {
            throw new ArgumentException("Target ContentItem not found.");
        }

        var result = await evidenceCaptureService.CaptureEvidenceAsync(
            item.Id,
            candidate.Id,
            candidate.ExternalUrl,
            candidate.Title,
            candidate.RawContent ?? candidate.Summary,
            request.Role ?? EvidenceRole.SupportingEvidence,
            request.Notes ?? candidate.EditorialNotes,
            actorEmail,
            cancellationToken);

        if (candidate.Status != DiscoveryCandidateStatus.Promoted)
        {
            candidate.Status = DiscoveryCandidateStatus.Promoted;
            candidate.PromotedAtUtc = DateTime.UtcNow;
            candidate.PromotedByEmail = actorEmail;
        }

        item.UpdatedAtUtc = DateTime.UtcNow;
        item.UpdatedByEmail = actorEmail;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "DiscoveryCandidate.AttachedToContentItem",
            "ContentItemEvidence",
            result.Evidence.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var e = result.Evidence;
        return new ContentItemEvidenceDto(
            e.Id,
            e.ContentItemId,
            e.DiscoveryCandidateId,
            e.OriginUrl,
            e.Title,
            e.Role,
            e.Status,
            e.RawContent,
            e.ObjectStorageKey,
            e.ExtractedText,
            e.ContentHash,
            e.ErrorMessage,
            e.Notes,
            e.Author,
            e.CapturedAtUtc,
            e.CreatedAtUtc,
            e.CreatedByEmail
        );
    }

    private static TruthSourceDto MapTruthSourceToDto(TruthSource ts)
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

    private static string GenerateSlug(string title)
    {
        var normalized = title.ToLowerInvariant().Trim();
        normalized = SlugRegex().Replace(normalized, "-");
        normalized = TrimHyphenRegex().Replace(normalized, "");
        if (normalized.Length > 100) normalized = normalized[..100];
        return string.IsNullOrWhiteSpace(normalized) ? $"content-{Guid.NewGuid():N}"[..16] : normalized;
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"^-+|-+$")]
    private static partial Regex TrimHyphenRegex();
}
