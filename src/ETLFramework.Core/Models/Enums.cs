namespace ETLFramework.Core.Models;

/// <summary>
/// Represents the current status of a pipeline.
/// </summary>
public enum PipelineStatus
{
    /// <summary>
    /// Pipeline is ready to be executed.
    /// </summary>
    Ready,

    /// <summary>
    /// Pipeline is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// Pipeline execution completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Pipeline execution failed with errors.
    /// </summary>
    Failed,

    /// <summary>
    /// Pipeline execution was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Pipeline is paused and can be resumed.
    /// </summary>
    Paused
}

/// <summary>
/// Represents the type of pipeline stage.
/// </summary>
public enum StageType
{
    /// <summary>
    /// Stage extracts data from a source.
    /// </summary>
    Extract,

    /// <summary>
    /// Stage transforms data.
    /// </summary>
    Transform,

    /// <summary>
    /// Stage loads data to a destination.
    /// </summary>
    Load,

    /// <summary>
    /// Stage validates data quality.
    /// </summary>
    Validate,

    /// <summary>
    /// Custom stage type for specialized operations.
    /// </summary>
    Custom
}

/// <summary>
/// Represents the current status of a pipeline stage.
/// </summary>
public enum StageStatus
{
    /// <summary>
    /// Stage is ready to be executed.
    /// </summary>
    Ready,

    /// <summary>
    /// Stage is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// Stage execution completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Stage execution failed with errors.
    /// </summary>
    Failed,

    /// <summary>
    /// Stage execution was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Stage is skipped due to conditions.
    /// </summary>
    Skipped
}

/// <summary>
/// Represents the current status of a connector connection.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Connection is closed.
    /// </summary>
    Closed,

    /// <summary>
    /// Connection is open and ready.
    /// </summary>
    Open,

    /// <summary>
    /// Connection is in the process of opening.
    /// </summary>
    Opening,

    /// <summary>
    /// Connection is in the process of closing.
    /// </summary>
    Closing,

    /// <summary>
    /// Connection has failed.
    /// </summary>
    Failed
}

/// <summary>
/// Represents the write mode for destination connectors.
/// </summary>
public enum WriteMode
{
    /// <summary>
    /// Insert new records only.
    /// </summary>
    Insert,

    /// <summary>
    /// Update existing records only.
    /// </summary>
    Update,

    /// <summary>
    /// Insert new records or update existing ones (upsert).
    /// </summary>
    Upsert,

    /// <summary>
    /// Replace all existing data.
    /// </summary>
    Replace,

    /// <summary>
    /// Append to existing data.
    /// </summary>
    Append
}
