using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OR.ProductService.Application.Interfaces;
using OR.ProductService.Application.Settings;

namespace OR.ProductService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up old processed events to prevent unbounded growth.
/// </summary>
public class ProcessedEventCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessedEventCleanupService> _logger;
    private readonly ProcessedEventsCleanupSettings _settings;

    public ProcessedEventCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ProcessedEventCleanupService> logger,
        IOptions<ProcessedEventsCleanupSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ProcessedEvent cleanup service started. Retention: {RetentionMinutes} minutes, Interval: {IntervalMinutes} minutes",
            _settings.RetentionMinutes,
            _settings.CleanupIntervalMinutes);

        // Wait a bit before first cleanup to allow service to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during processed event cleanup");
            }

            // Wait for the configured interval before next cleanup
            await Task.Delay(TimeSpan.FromMinutes(_settings.CleanupIntervalMinutes), stoppingToken);
        }
    }

    private async Task CleanupOldEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var cutoffDate = DateTime.UtcNow.AddMinutes(-_settings.RetentionMinutes);
        
        _logger.LogInformation("Starting cleanup of processed events older than {CutoffDate:u}", cutoffDate);

        var deletedCount = await repository.CleanupOldProcessedEventsAsync(cutoffDate, cancellationToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation(
                "Deleted {DeletedCount} processed events older than {RetentionMinutes} minutes",
                deletedCount,
                _settings.RetentionMinutes);
        }
        else
        {
            _logger.LogDebug("No old processed events to clean up");
        }
    }
}
