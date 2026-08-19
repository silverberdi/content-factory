using ContentFactory.Api.Modules.Content;

namespace ContentFactory.Api.Infrastructure.BackgroundWorkers;

public class VisualGenerationBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<VisualGenerationBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("VisualGenerationBackgroundWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var visualService = scope.ServiceProvider.GetRequiredService<IVisualGenerationService>();
                await visualService.ProcessQueuedJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing queued visual generation jobs in background worker.");
            }

            try
            {
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("VisualGenerationBackgroundWorker stopped.");
    }
}
