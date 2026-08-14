using ContentFactory.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Discovery;

public class DiscoveryBackgroundSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<DiscoveryBackgroundSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DiscoveryBackgroundSyncService started.");

        // Initial delay on startup to let DB initialize
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueSourcesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing due discovery sources in background runner.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        logger.LogInformation("DiscoveryBackgroundSyncService stopping.");
    }

    private async Task ProcessDueSourcesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var discoveryService = scope.ServiceProvider.GetRequiredService<IDiscoveryService>();

        var now = DateTime.UtcNow;
        var dueSources = await dbContext.DiscoverySources
            .Where(s => s.Status == DiscoverySourceStatus.Active && (s.NextSyncAtUtc == null || s.NextSyncAtUtc <= now))
            .Select(s => s.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var sourceId in dueSources)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                logger.LogInformation("Background syncing discovery source {SourceId}", sourceId);
                await discoveryService.SyncSourceAsync(sourceId, Guid.Empty, "system.sync@factory.silverman.pro", cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Background sync failed for discovery source {SourceId}", sourceId);
            }
        }
    }
}
