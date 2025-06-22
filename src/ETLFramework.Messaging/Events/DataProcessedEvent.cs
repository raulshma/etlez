namespace ETLFramework.Messaging.Events;

/// <summary>
/// Event published when data is processed during pipeline execution.
/// </summary>
public class DataProcessedEvent : PipelineEvent
{
    /// <summary>
    /// Initializes a new instance of the DataProcessedEvent class.
    /// </summary>
    public DataProcessedEvent()
    {
        EventType = "DataProcessed";
    }

    /// <summary>
    /// Gets or sets the name of the stage processing the data.
    /// </summary>
    public string StageName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the current batch being processed.
    /// </summary>
    public long BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of records processed so far.
    /// </summary>
    public long TotalRecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the current processing rate (records per second).
    /// </summary>
    public double RecordsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the estimated total number of records to process.
    /// </summary>
    public long? EstimatedTotalRecords { get; set; }

    /// <summary>
    /// Gets or sets the completion percentage (0-100).
    /// </summary>
    public double CompletionPercentage { get; set; }

    /// <summary>
    /// Gets or sets the estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Gets or sets the current memory usage.
    /// </summary>
    public long CurrentMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the current CPU usage percentage.
    /// </summary>
    public double CurrentCpuUsage { get; set; }

    /// <summary>
    /// Gets or sets additional processing metrics.
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets data quality metrics for the processed batch.
    /// </summary>
    public Dictionary<string, object> DataQualityMetrics { get; set; } = new();
}
