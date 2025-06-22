namespace ETLFramework.Core.Models;

/// <summary>
/// Represents the result of a pipeline execution.
/// </summary>
public class PipelineExecutionResult
{
    /// <summary>
    /// Gets or sets the unique identifier for this execution.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the pipeline identifier.
    /// </summary>
    public Guid PipelineId { get; set; }

    /// <summary>
    /// Gets or sets whether the execution was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the execution start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the execution end time.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Gets the duration of the execution.
    /// </summary>
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);

    /// <summary>
    /// Gets or sets the number of records processed.
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of records that failed processing.
    /// </summary>
    public long RecordsFailed { get; set; }

    /// <summary>
    /// Gets or sets the execution statistics.
    /// </summary>
    public ExecutionStatistics Statistics { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of errors that occurred during execution.
    /// </summary>
    public IList<ExecutionError> Errors { get; set; } = new List<ExecutionError>();

    /// <summary>
    /// Gets or sets the collection of warnings that occurred during execution.
    /// </summary>
    public IList<ExecutionWarning> Warnings { get; set; } = new List<ExecutionWarning>();

    /// <summary>
    /// Gets or sets additional execution metadata.
    /// </summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents the result of a stage execution.
/// </summary>
public class StageExecutionResult
{
    /// <summary>
    /// Gets or sets the stage identifier.
    /// </summary>
    public Guid StageId { get; set; }

    /// <summary>
    /// Gets or sets whether the stage execution was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the stage execution start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the stage execution end time.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Gets the duration of the stage execution.
    /// </summary>
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);

    /// <summary>
    /// Gets or sets the number of records processed by this stage.
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the collection of errors that occurred during stage execution.
    /// </summary>
    public IList<ExecutionError> Errors { get; set; } = new List<ExecutionError>();

    /// <summary>
    /// Gets or sets the collection of warnings that occurred during stage execution.
    /// </summary>
    public IList<ExecutionWarning> Warnings { get; set; } = new List<ExecutionWarning>();

    /// <summary>
    /// Gets or sets additional stage execution metadata.
    /// </summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents an execution error.
/// </summary>
public class ExecutionError
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused this error.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets when the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the source of the error (stage name, connector name, etc.).
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets additional context information about the error.
    /// </summary>
    public IDictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the severity of the error.
    /// </summary>
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
}

/// <summary>
/// Represents an execution warning.
/// </summary>
public class ExecutionWarning
{
    /// <summary>
    /// Gets or sets the warning message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the warning code.
    /// </summary>
    public string? WarningCode { get; set; }

    /// <summary>
    /// Gets or sets when the warning occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the source of the warning.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets additional context information about the warning.
    /// </summary>
    public IDictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents execution statistics.
/// </summary>
public class ExecutionStatistics : IExecutionStatistics
{
    /// <summary>
    /// Gets or sets the total number of records processed.
    /// </summary>
    public long TotalRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of records successfully processed.
    /// </summary>
    public long SuccessfulRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of records that failed processing.
    /// </summary>
    public long FailedRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of records skipped.
    /// </summary>
    public long SkippedRecords { get; set; }

    /// <summary>
    /// Gets or sets the processing rate (records per second).
    /// </summary>
    public double ProcessingRate { get; set; }

    /// <summary>
    /// Gets or sets the peak memory usage during execution.
    /// </summary>
    public long PeakMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the average memory usage during execution.
    /// </summary>
    public long AverageMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets additional performance metrics.
    /// </summary>
    public IDictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Interface for execution statistics.
/// </summary>
public interface IExecutionStatistics
{
    /// <summary>
    /// Gets or sets the total number of records processed.
    /// </summary>
    long TotalRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of records successfully processed.
    /// </summary>
    long SuccessfulRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of records that failed processing.
    /// </summary>
    long FailedRecords { get; set; }

    /// <summary>
    /// Gets or sets the processing rate (records per second).
    /// </summary>
    double ProcessingRate { get; set; }
}

/// <summary>
/// Represents progress information for an operation.
/// </summary>
public class ProgressInfo
{
    /// <summary>
    /// Gets or sets the current progress percentage (0-100).
    /// </summary>
    public double PercentComplete { get; set; }

    /// <summary>
    /// Gets or sets the current status message.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets the current step or operation being performed.
    /// </summary>
    public string? CurrentStep { get; set; }

    /// <summary>
    /// Gets or sets the number of items processed so far.
    /// </summary>
    public long ItemsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of items to process.
    /// </summary>
    public long? TotalItems { get; set; }

    /// <summary>
    /// Gets or sets when this progress update was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents the severity of an error.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Low severity error.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity error.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity error.
    /// </summary>
    High,

    /// <summary>
    /// Error level.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error level.
    /// </summary>
    Critical
}
