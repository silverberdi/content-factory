using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Discovery.Adapters;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Discovery;

public class DiscoveryService(
    AppDbContext dbContext,
    IAuditService auditService,
    IEnumerable<ISourceSyncAdapter> syncAdapters) : IDiscoveryService
{
    public async Task<List<DiscoverySourceDto>> GetSourcesAsync(Guid? channelId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.DiscoverySources.AsNoTracking().AsQueryable();
        if (channelId.HasValue && channelId.Value != Guid.Empty)
        {
            query = query.Where(s => s.ChannelId == channelId.Value);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        var sources = await query.OrderByDescending(s => s.CreatedAtUtc).ToListAsync(cancellationToken);
        var channelIds = sources.Select(s => s.ChannelId).Distinct().ToList();
        var channels = await dbContext.Channels.AsNoTracking()
            .Where(c => channelIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return sources.Select(s => MapToSourceDto(s, channels.GetValueOrDefault(s.ChannelId))).ToList();
    }

    public async Task<DiscoverySourceDto?> GetSourceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.DiscoverySources.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (source == null) return null;

        var channelName = await dbContext.Channels.AsNoTracking()
            .Where(c => c.Id == source.ChannelId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return MapToSourceDto(source, channelName);
    }

    public async Task<DiscoverySourceDto> CreateSourceAsync(CreateDiscoverySourceRequest request, Guid actorId, string actorEmail, CancellationToken cancellationToken = default)
    {
        if (request.ChannelId == Guid.Empty)
            throw new ArgumentException("ChannelId is required for discovery sources.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Source name is required.");
        if (string.IsNullOrWhiteSpace(request.OriginUrl))
            throw new ArgumentException("Origin URL is required.");

        var channel = await dbContext.Channels.FindAsync([request.ChannelId], cancellationToken);
        if (channel == null)
            throw new ArgumentException("Channel not found.");

        var trimmedUrl = request.OriginUrl.Trim();
        var exists = await dbContext.DiscoverySources.AnyAsync(s => s.ChannelId == request.ChannelId && s.OriginUrl == trimmedUrl, cancellationToken);
        if (exists)
            throw new InvalidOperationException("A discovery source with this URL already exists in this channel.");

        var sourceType = !string.IsNullOrWhiteSpace(request.SourceType) && SourceType.All.Contains(request.SourceType)
            ? request.SourceType
            : SourceType.Feed;

        var source = new DiscoverySource
        {
            Id = Guid.NewGuid(),
            ChannelId = request.ChannelId,
            Name = request.Name.Trim(),
            OriginUrl = trimmedUrl,
            SourceType = sourceType,
            Language = !string.IsNullOrWhiteSpace(request.Language) ? request.Language.Trim() : channel.Language,
            PollingIntervalMinutes = request.PollingIntervalMinutes is > 0 ? request.PollingIntervalMinutes.Value : 60,
            Status = DiscoverySourceStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.DiscoverySources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "DiscoverySource.Created",
            targetType: "DiscoverySource",
            targetId: source.Id.ToString(),
            actorUserId: actorId,
            actorEmail: actorEmail,
            cancellationToken: cancellationToken
        );

        return MapToSourceDto(source, channel.Name);
    }

    public async Task<DiscoverySourceDto> UpdateSourceAsync(Guid id, UpdateDiscoverySourceRequest request, Guid actorId, string actorEmail, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.DiscoverySources.FindAsync([id], cancellationToken);
        if (source == null)
            throw new InvalidOperationException("Discovery source not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Source name is required.");
        if (string.IsNullOrWhiteSpace(request.OriginUrl))
            throw new ArgumentException("Origin URL is required.");

        var trimmedUrl = request.OriginUrl.Trim();
        if (!string.Equals(source.OriginUrl, trimmedUrl, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await dbContext.DiscoverySources.AnyAsync(s => s.ChannelId == source.ChannelId && s.OriginUrl == trimmedUrl && s.Id != id, cancellationToken);
            if (exists)
                throw new InvalidOperationException("A discovery source with this URL already exists in this channel.");
        }

        source.Name = request.Name.Trim();
        source.OriginUrl = trimmedUrl;
        if (!string.IsNullOrWhiteSpace(request.SourceType) && SourceType.All.Contains(request.SourceType))
        {
            source.SourceType = request.SourceType;
        }
        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            source.Language = request.Language.Trim();
        }
        if (request.PollingIntervalMinutes is > 0)
        {
            source.PollingIntervalMinutes = request.PollingIntervalMinutes.Value;
        }
        if (!string.IsNullOrWhiteSpace(request.Status) && DiscoverySourceStatus.All.Contains(request.Status))
        {
            source.Status = request.Status;
        }
        source.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "DiscoverySource.Updated",
            targetType: "DiscoverySource",
            targetId: source.Id.ToString(),
            actorUserId: actorId,
            actorEmail: actorEmail,
            cancellationToken: cancellationToken
        );

        var channelName = await dbContext.Channels.AsNoTracking()
            .Where(c => c.Id == source.ChannelId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return MapToSourceDto(source, channelName);
    }

    public async Task DeleteSourceAsync(Guid id, Guid actorId, string actorEmail, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.DiscoverySources.FindAsync([id], cancellationToken);
        if (source == null)
            throw new InvalidOperationException("Discovery source not found.");

        dbContext.DiscoverySources.Remove(source);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "DiscoverySource.Deleted",
            targetType: "DiscoverySource",
            targetId: id.ToString(),
            actorUserId: actorId,
            actorEmail: actorEmail,
            cancellationToken: cancellationToken
        );
    }

    public async Task<int> SyncSourceAsync(Guid id, Guid actorId, string actorEmail, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.DiscoverySources.FindAsync([id], cancellationToken);
        if (source == null)
            throw new InvalidOperationException("Discovery source not found.");

        var adapter = syncAdapters.FirstOrDefault(a => a.CanHandle(source.SourceType));
        if (adapter == null)
            throw new NotSupportedException($"No ingestion adapter registered for source type '{source.SourceType}'.");

        int newItemsCount = 0;
        try
        {
            var items = await adapter.FetchAsync(source, cancellationToken);

            foreach (var item in items)
            {
                string? normalizedUrl = DiscoveryUrlNormalizer.Normalize(item.ExternalUrl);

                if (!string.IsNullOrEmpty(normalizedUrl))
                {
                    var existing = await dbContext.DiscoveryCandidates
                        .FirstOrDefaultAsync(c => c.ChannelId == source.ChannelId && c.NormalizedUrl == normalizedUrl, cancellationToken);

                    if (existing != null)
                    {
                        existing.DiscoveredAtUtc = DateTime.UtcNow;
                        continue;
                    }
                }

                var candidate = new DiscoveryCandidate
                {
                    Id = Guid.NewGuid(),
                    ChannelId = source.ChannelId,
                    DiscoverySourceId = source.Id,
                    ExternalUrl = item.ExternalUrl,
                    NormalizedUrl = normalizedUrl,
                    Title = item.Title,
                    Summary = item.Summary,
                    RawContent = item.RawContent,
                    Language = !string.IsNullOrWhiteSpace(item.Language) ? item.Language : source.Language,
                    Author = item.Author,
                    DiscoveredAtUtc = item.DiscoveredAtUtc,
                    Status = DiscoveryCandidateStatus.PendingReview,
                    OriginType = OriginType.Automated,
                    CreatedAtUtc = DateTime.UtcNow
                };

                dbContext.DiscoveryCandidates.Add(candidate);
                newItemsCount++;
            }

            source.LastSyncAtUtc = DateTime.UtcNow;
            source.NextSyncAtUtc = DateTime.UtcNow.AddMinutes(source.PollingIntervalMinutes);
            source.FailureCount = 0;
            source.LastErrorMessage = null;
            source.Status = DiscoverySourceStatus.Active;
            source.UpdatedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.RecordAsync(
                action: "DiscoverySource.Synced",
                targetType: "DiscoverySource",
                targetId: source.Id.ToString(),
                actorUserId: actorId,
                actorEmail: actorEmail,
                cancellationToken: cancellationToken
            );

            return newItemsCount;
        }
        catch (Exception ex)
        {
            source.FailureCount += 1;
            source.LastErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            if (source.FailureCount >= 3)
            {
                source.Status = DiscoverySourceStatus.Error;
            }
            source.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<DiscoveryCandidateDto>> GetCandidatesAsync(Guid? channelId = null, string? status = null, Guid? sourceId = null, string? search = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        var query = dbContext.DiscoveryCandidates.AsNoTracking().AsQueryable();

        if (channelId.HasValue && channelId.Value != Guid.Empty)
        {
            query = query.Where(c => c.ChannelId == channelId.Value);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }
        if (sourceId.HasValue && sourceId.Value != Guid.Empty)
        {
            query = query.Where(c => c.DiscoverySourceId == sourceId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Title.ToLower().Contains(term) || (c.Summary != null && c.Summary.ToLower().Contains(term)));
        }

        var candidates = await query
            .OrderByDescending(c => c.DiscoveredAtUtc)
            .Take(limit > 0 ? limit : 100)
            .ToListAsync(cancellationToken);

        var channelIds = candidates.Select(c => c.ChannelId).Distinct().ToList();
        var sourceIds = candidates.Where(c => c.DiscoverySourceId.HasValue).Select(c => c.DiscoverySourceId!.Value).Distinct().ToList();

        var channels = await dbContext.Channels.AsNoTracking()
            .Where(c => channelIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var sources = await dbContext.DiscoverySources.AsNoTracking()
            .Where(s => sourceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        return candidates.Select(c => MapToCandidateDto(
            c,
            channels.GetValueOrDefault(c.ChannelId),
            c.DiscoverySourceId.HasValue ? sources.GetValueOrDefault(c.DiscoverySourceId.Value) : null
        )).ToList();
    }

    public async Task<DiscoveryCandidateDto?> GetCandidateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.DiscoveryCandidates.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (candidate == null) return null;

        var channelName = await dbContext.Channels.AsNoTracking()
            .Where(c => c.Id == candidate.ChannelId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        string? sourceName = null;
        if (candidate.DiscoverySourceId.HasValue)
        {
            sourceName = await dbContext.DiscoverySources.AsNoTracking()
                .Where(s => s.Id == candidate.DiscoverySourceId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return MapToCandidateDto(candidate, channelName, sourceName);
    }

    public async Task<DiscoveryCandidateDto> QuickSubmitCandidateAsync(QuickSubmitCandidateRequest request, Guid actorId, string actorEmail, CancellationToken cancellationToken = default)
    {
        if (request.ChannelId == Guid.Empty)
            throw new ArgumentException("ChannelId is required for candidate submission.");

        var channel = await dbContext.Channels.FindAsync([request.ChannelId], cancellationToken);
        if (channel == null)
            throw new ArgumentException("Channel not found.");

        if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.ExternalUrl) && string.IsNullOrWhiteSpace(request.Summary))
            throw new ArgumentException("A title, URL, or note is required.");

        string? normalizedUrl = DiscoveryUrlNormalizer.Normalize(request.ExternalUrl);

        if (!string.IsNullOrEmpty(normalizedUrl))
        {
            var existing = await dbContext.DiscoveryCandidates
                .FirstOrDefaultAsync(c => c.ChannelId == request.ChannelId && c.NormalizedUrl == normalizedUrl, cancellationToken);

            if (existing != null)
            {
                existing.DiscoveredAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                return MapToCandidateDto(existing, channel.Name, null);
            }
        }

        var title = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title.Trim()
            : (!string.IsNullOrWhiteSpace(request.ExternalUrl) ? request.ExternalUrl.Trim() : "Manual Lead");

        var candidate = new DiscoveryCandidate
        {
            Id = Guid.NewGuid(),
            ChannelId = request.ChannelId,
            DiscoverySourceId = null,
            ExternalUrl = !string.IsNullOrWhiteSpace(request.ExternalUrl) ? request.ExternalUrl.Trim() : null,
            NormalizedUrl = normalizedUrl,
            Title = title,
            Summary = request.Summary?.Trim(),
            RawContent = request.Summary?.Trim(),
            Language = !string.IsNullOrWhiteSpace(request.Language) ? request.Language.Trim() : channel.Language,
            Author = actorEmail,
            DiscoveredAtUtc = DateTime.UtcNow,
            Status = DiscoveryCandidateStatus.PendingReview,
            OriginType = OriginType.Manual,
            SubmitterEmail = actorEmail,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.DiscoveryCandidates.Add(candidate);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "DiscoveryCandidate.Submitted",
            targetType: "DiscoveryCandidate",
            targetId: candidate.Id.ToString(),
            actorUserId: actorId,
            actorEmail: actorEmail,
            cancellationToken: cancellationToken
        );

        return MapToCandidateDto(candidate, channel.Name, null);
    }

    public async Task<DiscoveryCandidateDto> TriageCandidateAsync(Guid id, TriageCandidateRequest request, Guid actorId, string actorEmail, CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.DiscoveryCandidates.FindAsync([id], cancellationToken);
        if (candidate == null)
            throw new InvalidOperationException("Discovery candidate not found.");

        if (!DiscoveryCandidateStatus.All.Contains(request.Status))
            throw new ArgumentException($"Invalid status '{request.Status}'.");

        candidate.Status = request.Status;

        if (request.Status == DiscoveryCandidateStatus.Promoted)
        {
            candidate.PromotedAtUtc = DateTime.UtcNow;
            candidate.PromotedByEmail = actorEmail;
            candidate.EditorialNotes = request.EditorialNotes?.Trim();

            await auditService.RecordAsync(
                action: "DiscoveryCandidate.Promoted",
                targetType: "DiscoveryCandidate",
                targetId: candidate.Id.ToString(),
                actorUserId: actorId,
                actorEmail: actorEmail,
                cancellationToken: cancellationToken
            );
        }
        else if (request.Status == DiscoveryCandidateStatus.Dismissed)
        {
            candidate.DismissalReason = !string.IsNullOrWhiteSpace(request.DismissalReason)
                ? request.DismissalReason.Trim()
                : "Dismissed by operator";

            await auditService.RecordAsync(
                action: "DiscoveryCandidate.Dismissed",
                targetType: "DiscoveryCandidate",
                targetId: candidate.Id.ToString(),
                actorUserId: actorId,
                actorEmail: actorEmail,
                cancellationToken: cancellationToken
            );
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var channelName = await dbContext.Channels.AsNoTracking()
            .Where(c => c.Id == candidate.ChannelId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        string? sourceName = null;
        if (candidate.DiscoverySourceId.HasValue)
        {
            sourceName = await dbContext.DiscoverySources.AsNoTracking()
                .Where(s => s.Id == candidate.DiscoverySourceId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return MapToCandidateDto(candidate, channelName, sourceName);
    }

    public async Task<DiscoverySummaryDto> GetSummaryAsync(Guid? channelId = null, CancellationToken cancellationToken = default)
    {
        var candidateQuery = dbContext.DiscoveryCandidates.AsNoTracking().AsQueryable();
        var sourceQuery = dbContext.DiscoverySources.AsNoTracking().AsQueryable();

        if (channelId.HasValue && channelId.Value != Guid.Empty)
        {
            candidateQuery = candidateQuery.Where(c => c.ChannelId == channelId.Value);
            sourceQuery = sourceQuery.Where(s => s.ChannelId == channelId.Value);
        }

        var pending = await candidateQuery.CountAsync(c => c.Status == DiscoveryCandidateStatus.PendingReview, cancellationToken);
        var promoted = await candidateQuery.CountAsync(c => c.Status == DiscoveryCandidateStatus.Promoted, cancellationToken);
        var dismissed = await candidateQuery.CountAsync(c => c.Status == DiscoveryCandidateStatus.Dismissed, cancellationToken);

        var activeSources = await sourceQuery.CountAsync(s => s.Status == DiscoverySourceStatus.Active, cancellationToken);
        var pausedSources = await sourceQuery.CountAsync(s => s.Status == DiscoverySourceStatus.Paused, cancellationToken);
        var errorSources = await sourceQuery.CountAsync(s => s.Status == DiscoverySourceStatus.Error, cancellationToken);

        return new DiscoverySummaryDto(pending, promoted, dismissed, activeSources, pausedSources, errorSources);
    }

    private static DiscoverySourceDto MapToSourceDto(DiscoverySource s, string? channelName) =>
        new(
            s.Id,
            s.ChannelId,
            channelName,
            s.Name,
            s.OriginUrl,
            s.SourceType,
            s.Language,
            s.PollingIntervalMinutes,
            s.Status,
            s.LastSyncAtUtc,
            s.NextSyncAtUtc,
            s.FailureCount,
            s.LastErrorMessage,
            s.CreatedAtUtc,
            s.UpdatedAtUtc
        );

    private static DiscoveryCandidateDto MapToCandidateDto(DiscoveryCandidate c, string? channelName, string? sourceName) =>
        new(
            c.Id,
            c.ChannelId,
            channelName,
            c.DiscoverySourceId,
            sourceName,
            c.ExternalUrl,
            c.NormalizedUrl,
            c.Title,
            c.Summary,
            c.RawContent,
            c.Language,
            c.Author,
            c.DiscoveredAtUtc,
            c.Status,
            c.OriginType,
            c.SubmitterEmail,
            c.DismissalReason,
            c.EditorialNotes,
            c.PromotedAtUtc,
            c.PromotedByEmail,
            c.CreatedAtUtc
        );
}
