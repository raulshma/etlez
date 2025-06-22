namespace ETLFramework.Core.Models;

/// <summary>
/// Configuration options for the ETL Framework.
/// </summary>
public class FrameworkOptions
{
    /// <summary>
    /// Gets or sets the maximum number of concurrent pipeline executions.
    /// </summary>
    public int MaxConcurrentPipelines { get; set; } = 10;

    /// <summary>
    /// Gets or sets the default timeout for pipeline execution.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets whether to enable performance metrics collection.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the metrics collection interval.
    /// </summary>
    public TimeSpan MetricsInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets whether to enable detailed logging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the default batch size for data processing.
    /// </summary>
    public int DefaultBatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum memory usage limit.
    /// </summary>
    public long? MaxMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the temporary directory for processing files.
    /// </summary>
    public string? TempDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether to enable plugin loading.
    /// </summary>
    public bool EnablePlugins { get; set; } = true;

    /// <summary>
    /// Gets or sets the plugin directory path.
    /// </summary>
    public string? PluginDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether to enable event publishing.
    /// </summary>
    public bool EnableEventPublishing { get; set; } = true;

    /// <summary>
    /// Gets or sets the default event topic for pipeline events.
    /// </summary>
    public string DefaultEventTopic { get; set; } = "pipeline.events";

    /// <summary>
    /// Gets or sets custom framework properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}
