using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Discovery;

namespace ContentFactory.Api.Modules.Dashboard;

public record FactoryHealthDto(
    string Status, // healthy, degraded, attention-required
    int ActiveChannelsCount,
    int PilotChannelsCount,
    int TotalChannelsCount,
    string DatabaseStatus,
    string BackupStatus,
    string Environment
);

public record AttentionItemDto(
    Guid Id,
    string Severity, // info, warning, critical
    string Title,
    string Description,
    string? ActionPath,
    bool IsRepresentativeDemo,
    DateTime TimestampUtc
);

public record DashboardSummaryDto(
    FactoryHealthDto FactoryHealth,
    List<ChannelDto> Channels,
    List<AttentionItemDto> AttentionItems,
    DiscoverySummaryDto? Discovery = null
);

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
