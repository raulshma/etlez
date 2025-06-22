namespace ETLFramework.Messaging.Events;

/// <summary>
/// Event published when a pipeline stage completes execution.
/// </summary>
public class StageCompletedEvent : PipelineEvent
{
    /// <summary>
    /// Initializes a new instance of the StageCompletedEvent class.
    /// </summary>
    public StageCompletedEvent()
    {
        EventType = "StageCompleted";
    }

    /// <summary>
    /// Gets or sets the name of the completed stage.
    /// </summary>
    public string StageName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the stage.
    /// </summary>
    public string StageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution order of the stage.
    /// </summary>
    public int StageOrder { get; set; }

    /// <summary>
    /// Gets or sets the stage execution duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the number of records processed by the stage.
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
    /// Gets or sets the processing rate for this stage (records per second).
    /// </summary>
    public double ProcessingRate { get; set; }

    /// <summary>
    /// Gets or sets the memory usage during stage execution.
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets stage-specific metrics.
    /// </summary>
    public Dictionary<string, object> StageMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the stage completed successfully.
    /// </summary>
    public bool IsSuccessful { get; set; } = true;

    /// <summary>
    /// Gets or sets any warnings generated during stage execution.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
