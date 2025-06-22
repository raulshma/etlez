namespace ETLFramework.Messaging.Events;

/// <summary>
/// Event published when a pipeline completes successfully.
/// </summary>
public class PipelineCompletedEvent : PipelineEvent
{
    /// <summary>
    /// Initializes a new instance of the PipelineCompletedEvent class.
    /// </summary>
    public PipelineCompletedEvent()
    {
        EventType = "PipelineCompleted";
    }

    /// <summary>
    /// Gets or sets the total execution duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the total number of records processed.
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of records processed successfully.
    /// </summary>
    public long RecordsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the number of records that failed processing.
    /// </summary>
    public long RecordsFailed { get; set; }

    /// <summary>
    /// Gets or sets the average processing rate (records per second).
    /// </summary>
    public double ProcessingRate { get; set; }

    /// <summary>
    /// Gets or sets the peak memory usage during execution.
    /// </summary>
    public long PeakMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the average CPU usage during execution.
    /// </summary>
    public double AverageCpuUsage { get; set; }

    /// <summary>
    /// Gets or sets performance metrics for the execution.
    /// </summary>
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the execution result summary.
    /// </summary>
    public string? ResultSummary { get; set; }
}
