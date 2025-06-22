namespace ETLFramework.Messaging.Events;

/// <summary>
/// Event published when a pipeline fails during execution.
/// </summary>
public class PipelineFailedEvent : PipelineEvent
{
    /// <summary>
    /// Initializes a new instance of the PipelineFailedEvent class.
    /// </summary>
    public PipelineFailedEvent()
    {
        EventType = "PipelineFailed";
    }

    /// <summary>
    /// Gets or sets the primary error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stack trace of the error.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Gets or sets the name of the stage where the failure occurred.
    /// </summary>
    public string? FailedStage { get; set; }

    /// <summary>
    /// Gets or sets the error code or category.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the severity of the error.
    /// </summary>
    public string Severity { get; set; } = "Error";

    /// <summary>
    /// Gets or sets whether the pipeline can be retried.
    /// </summary>
    public bool IsRetryable { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of retry attempts made.
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets additional error details.
    /// </summary>
    public Dictionary<string, object> ErrorDetails { get; set; } = new();

    /// <summary>
    /// Gets or sets the execution duration before failure.
    /// </summary>
    public TimeSpan? ExecutionDuration { get; set; }

    /// <summary>
    /// Gets or sets the number of records processed before failure.
    /// </summary>
    public long RecordsProcessedBeforeFailure { get; set; }
}
