namespace ETLFramework.Core.Exceptions;

/// <summary>
/// Exception thrown when an error occurs during pipeline execution.
/// </summary>
public class PipelineExecutionException : ETLFrameworkException
{
    /// <summary>
    /// Initializes a new instance of the PipelineExecutionException class.
    /// </summary>
    public PipelineExecutionException()
    {
        Component = "Pipeline";
    }

    /// <summary>
    /// Initializes a new instance of the PipelineExecutionException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public PipelineExecutionException(string message) : base(message)
    {
        Component = "Pipeline";
    }

    /// <summary>
    /// Initializes a new instance of the PipelineExecutionException class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public PipelineExecutionException(string message, Exception innerException) : base(message, innerException)
    {
        Component = "Pipeline";
    }

    /// <summary>
    /// Gets or sets the pipeline identifier associated with this exception.
    /// </summary>
    public Guid? PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the execution identifier associated with this exception.
    /// </summary>
    public Guid? ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the stage identifier where the exception occurred.
    /// </summary>
    public Guid? StageId { get; set; }

    /// <summary>
    /// Gets or sets the name of the stage where the exception occurred.
    /// </summary>
    public string? StageName { get; set; }

    /// <summary>
    /// Gets or sets the record number being processed when the exception occurred.
    /// </summary>
    public long? RecordNumber { get; set; }

    /// <summary>
    /// Creates a pipeline execution exception with pipeline context.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="pipelineId">The pipeline identifier</param>
    /// <param name="executionId">The execution identifier</param>
    /// <returns>A new PipelineExecutionException instance</returns>
    public static PipelineExecutionException Create(string message, Guid pipelineId, Guid executionId)
    {
        return new PipelineExecutionException(message)
        {
            PipelineId = pipelineId,
            ExecutionId = executionId
        };
    }

    /// <summary>
    /// Creates a pipeline execution exception with stage context.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="pipelineId">The pipeline identifier</param>
    /// <param name="executionId">The execution identifier</param>
    /// <param name="stageId">The stage identifier</param>
    /// <param name="stageName">The stage name</param>
    /// <returns>A new PipelineExecutionException instance</returns>
    public static PipelineExecutionException CreateForStage(string message, Guid pipelineId, Guid executionId, Guid stageId, string stageName)
    {
        return new PipelineExecutionException(message)
        {
            PipelineId = pipelineId,
            ExecutionId = executionId,
            StageId = stageId,
            StageName = stageName
        };
    }

    /// <summary>
    /// Creates a pipeline execution exception with record context.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="pipelineId">The pipeline identifier</param>
    /// <param name="executionId">The execution identifier</param>
    /// <param name="recordNumber">The record number</param>
    /// <returns>A new PipelineExecutionException instance</returns>
    public static PipelineExecutionException CreateForRecord(string message, Guid pipelineId, Guid executionId, long recordNumber)
    {
        return new PipelineExecutionException(message)
        {
            PipelineId = pipelineId,
            ExecutionId = executionId,
            RecordNumber = recordNumber
        };
    }
}
