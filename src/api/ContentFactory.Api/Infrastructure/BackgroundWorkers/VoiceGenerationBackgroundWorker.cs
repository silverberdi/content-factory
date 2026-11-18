using ContentFactory.Api.Modules.Content;

namespace ContentFactory.Api.Infrastructure.BackgroundWorkers;

public class VoiceGenerationBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<VoiceGenerationBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("VoiceGenerationBackgroundWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var voiceService = scope.ServiceProvider.GetRequiredService<IVoiceGenerationService>();
                await voiceService.ProcessQueuedJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing queued voice generation jobs in background worker.");
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

        logger.LogInformation("VoiceGenerationBackgroundWorker stopped.");
    }
}
