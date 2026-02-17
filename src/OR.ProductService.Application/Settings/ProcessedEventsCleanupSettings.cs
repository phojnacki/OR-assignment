namespace OR.ProductService.Application.Settings;

/// <summary>
/// Configuration settings for processed event cleanup.
/// </summary>
public class ProcessedEventsCleanupSettings
{
    public const string SectionName = "ProcessedEventsCleanup";

    /// <summary>
    /// Number of minutes to retain processed events. Events older than this will be deleted.
    /// Default: 30 minutes
    /// </summary>
    public double RetentionMinutes { get; set; } = 30;

    /// <summary>
    /// Interval in minutes between cleanup runs.
    /// Default: 24 minutes
    /// </summary>
    public double CleanupIntervalMinutes { get; set; } = 24;
}
