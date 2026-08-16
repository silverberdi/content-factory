using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using ContentFactory.Api.Modules.Dashboard;
using ContentFactory.Api.Modules.Discovery;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Infrastructure;

public class DashboardService(AppDbContext dbContext, IWebHostEnvironment environment) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var channels = await dbContext.Channels
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ChannelDto(
                c.Id,
                c.Slug,
                c.Name,
                c.Language,
                c.Niche,
                c.Status,
                c.CreatedAtUtc,
                c.UpdatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        var activeCount = channels.Count(c => c.Status == ChannelStatus.Active);
        var pilotCount = channels.Count(c => c.Status == ChannelStatus.Pilot);

        var dbStatus = dbContext.Database.IsInMemory() 
            ? "InMemory (Test/Fallback)" 
            : "Connected (MySQL/content_factory_dev)";

        var healthStatus = channels.Count > 0 ? "healthy" : "attention-required";

        var factoryHealth = new FactoryHealthDto(
            Status: healthStatus,
            ActiveChannelsCount: activeCount,
            PilotChannelsCount: pilotCount,
            TotalChannelsCount: channels.Count,
            DatabaseStatus: dbStatus,
            BackupStatus: "Not Configured (CF-001 Scope)",
            Environment: environment.EnvironmentName
        );

        // Discovery summary & attention items
        var pendingCandidates = await dbContext.DiscoveryCandidates
            .CountAsync(c => c.Status == DiscoveryCandidateStatus.PendingReview, cancellationToken);
        var promotedCandidates = await dbContext.DiscoveryCandidates
            .CountAsync(c => c.Status == DiscoveryCandidateStatus.Promoted, cancellationToken);
        var dismissedCandidates = await dbContext.DiscoveryCandidates
            .CountAsync(c => c.Status == DiscoveryCandidateStatus.Dismissed, cancellationToken);

        var activeSources = await dbContext.DiscoverySources
            .CountAsync(s => s.Status == DiscoverySourceStatus.Active, cancellationToken);
        var pausedSources = await dbContext.DiscoverySources
            .CountAsync(s => s.Status == DiscoverySourceStatus.Paused, cancellationToken);
        var errorSources = await dbContext.DiscoverySources
            .CountAsync(s => s.Status == DiscoverySourceStatus.Error, cancellationToken);

        var discoverySummary = new DiscoverySummaryDto(
            PendingCandidatesCount: pendingCandidates,
            PromotedCandidatesCount: promotedCandidates,
            DismissedCandidatesCount: dismissedCandidates,
            ActiveSourcesCount: activeSources,
            PausedSourcesCount: pausedSources,
            ErrorSourcesCount: errorSources
        );

        // Content Pipeline summary & attention items
        var totalContentItems = await dbContext.ContentItems.CountAsync(cancellationToken);
        var draftingEvidenceCount = await dbContext.ContentItems
            .CountAsync(c => c.Stage == ContentItemStage.DraftingEvidence, cancellationToken);
        var truthSourceApprovedCount = await dbContext.ContentItems
            .CountAsync(c => c.Stage == ContentItemStage.TruthSourceApproved, cancellationToken);
        var ideaSelectedCount = await dbContext.ContentItems
            .CountAsync(c => c.Stage == ContentItemStage.IdeaSelected, cancellationToken);
        var underReviewTruthSources = await dbContext.TruthSources
            .CountAsync(t => t.Status == TruthSourceStatus.UnderReview, cancellationToken);
        var pendingEditorialTasks = await dbContext.EditorialTasks
            .CountAsync(t => t.Status == EditorialTaskStatus.Pending, cancellationToken);

        var contentPipelineSummary = new ContentPipelineSummaryDto(
            TotalContentItemsCount: totalContentItems,
            DraftingEvidenceCount: draftingEvidenceCount,
            TruthSourceApprovedCount: truthSourceApprovedCount,
            IdeaSelectedCount: ideaSelectedCount,
            UnderReviewTruthSourcesCount: underReviewTruthSources,
            PendingEditorialTasksCount: pendingEditorialTasks
        );

        var attentionItems = new List<AttentionItemDto>();

        if (underReviewTruthSources > 0)
        {
            attentionItems.Add(new AttentionItemDto(
                Id: Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Severity: "warning",
                Title: "TruthSources Awaiting Review",
                Description: $"{underReviewTruthSources} truth source{(underReviewTruthSources > 1 ? "s" : "")} awaiting editorial review and verification.",
                ActionPath: "/content/items",
                IsRepresentativeDemo: false,
                TimestampUtc: DateTime.UtcNow
            ));
        }

        if (truthSourceApprovedCount > 0)
        {
            var firstApprovedItem = await dbContext.ContentItems
                .Where(c => c.Stage == ContentItemStage.TruthSourceApproved)
                .OrderByDescending(c => c.UpdatedAtUtc)
                .Select(c => new { c.Id, c.Title })
                .FirstOrDefaultAsync(cancellationToken);

            var actionPath = firstApprovedItem != null
                ? $"/content/items/{firstApprovedItem.Id}/ideas"
                : "/content/items";

            attentionItems.Add(new AttentionItemDto(
                Id: Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Severity: "info",
                Title: "Approved TruthSources Ready for Ideas",
                Description: $"{truthSourceApprovedCount} piece{(truthSourceApprovedCount > 1 ? "s" : "")} with approved TruthSource ready for creative idea generation and selection.",
                ActionPath: actionPath,
                IsRepresentativeDemo: false,
                TimestampUtc: DateTime.UtcNow
            ));
        }

        if (pendingCandidates > 0)
        {
            attentionItems.Add(new AttentionItemDto(
                Id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Severity: "info",
                Title: "Candidates Awaiting Triage",
                Description: $"{pendingCandidates} discovery lead{(pendingCandidates > 1 ? "s" : "")} awaiting editorial review.",
                ActionPath: "/discovery/triage",
                IsRepresentativeDemo: false,
                TimestampUtc: DateTime.UtcNow
            ));
        }

        if (errorSources > 0)
        {
            attentionItems.Add(new AttentionItemDto(
                Id: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Severity: "warning",
                Title: "Discovery Source Error",
                Description: $"{errorSources} discovery source{(errorSources > 1 ? "s" : "")} encountered feed/sync failures.",
                ActionPath: "/discovery/sources",
                IsRepresentativeDemo: false,
                TimestampUtc: DateTime.UtcNow
            ));
        }

        // In Development, provide representative attention items if needed
        if (environment.IsDevelopment() && channels.Any(c => c.Status == ChannelStatus.Pilot) && attentionItems.Count == 0)
        {
            attentionItems.Add(new AttentionItemDto(
                Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Severity: "info",
                Title: "Pilot Channel Active",
                Description: "Pilot channel 'IA Simple ES' is active and receiving discovery feeds.",
                ActionPath: "/channels",
                IsRepresentativeDemo: true,
                TimestampUtc: DateTime.UtcNow.AddMinutes(-15)
            ));
        }

        return new DashboardSummaryDto(
            factoryHealth,
            channels,
            attentionItems,
            discoverySummary,
            contentPipelineSummary
        );
    }
}
