using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Dashboard;
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

        // Real runtime signals available in CF-001 slice
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

        var attentionItems = new List<AttentionItemDto>();

        // In Development, provide representative attention items to verify the Attention widget
        if (environment.IsDevelopment())
        {
            if (channels.Any(c => c.Status == ChannelStatus.Pilot))
            {
                attentionItems.Add(new AttentionItemDto(
                    Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Severity: "info",
                    Title: "Pilot Channel Initialized",
                    Description: "Pilot channel 'IA Simple ES' is registered and awaiting editorial idea discovery.",
                    ActionPath: "/channels",
                    IsRepresentativeDemo: true,
                    TimestampUtc: DateTime.UtcNow.AddMinutes(-15)
                ));

                attentionItems.Add(new AttentionItemDto(
                    Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Severity: "warning",
                    Title: "Channel Configuration Check",
                    Description: "Verify target audience profile and language parameters for Spanish AI niche.",
                    ActionPath: "/channels",
                    IsRepresentativeDemo: true,
                    TimestampUtc: DateTime.UtcNow.AddHours(-1)
                ));
            }
        }

        return new DashboardSummaryDto(
            factoryHealth,
            channels,
            attentionItems
        );
    }
}
