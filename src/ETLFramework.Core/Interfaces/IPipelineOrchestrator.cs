using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Orchestrates the execution of ETL pipelines, managing scheduling, monitoring, and lifecycle.
/// </summary>
public interface IPipelineOrchestrator
{
    /// <summary>
    /// Executes a pipeline asynchronously.
    /// </summary>
    /// <param name="pipeline">The pipeline to execute</param>
    /// <param name="context">The execution context</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The result of the pipeline execution</returns>
    Task<PipelineExecutionResult> ExecutePipelineAsync(IPipeline pipeline, IPipelineContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a pipeline for execution.
    /// </summary>
    /// <param name="pipeline">The pipeline to schedule</param>
    /// <param name="schedule">The schedule configuration</param>
    /// <returns>The scheduled job identifier</returns>
    Task<Guid> SchedulePipelineAsync(IPipeline pipeline, IScheduleConfiguration schedule);

    /// <summary>
    /// Cancels a scheduled pipeline execution.
    /// </summary>
    /// <param name="jobId">The job identifier to cancel</param>
    /// <returns>True if the job was cancelled, false if not found or already completed</returns>
    Task<bool> CancelScheduledPipelineAsync(Guid jobId);

    /// <summary>
    /// Gets the status of a pipeline execution.
    /// </summary>
    /// <param name="executionId">The execution identifier</param>
    /// <returns>The execution status, or null if not found</returns>
    Task<PipelineExecutionStatus?> GetExecutionStatusAsync(Guid executionId);

    /// <summary>
    /// Gets all active pipeline executions.
    /// </summary>
    /// <returns>Collection of active executions</returns>
    Task<IEnumerable<PipelineExecutionStatus>> GetActiveExecutionsAsync();

    /// <summary>
    /// Pauses a running pipeline execution.
    /// </summary>
    /// <param name="executionId">The execution identifier</param>
    /// <returns>True if the execution was paused, false if not found or not running</returns>
    Task<bool> PausePipelineAsync(Guid executionId);

    /// <summary>
    /// Resumes a paused pipeline execution.
    /// </summary>
    /// <param name="executionId">The execution identifier</param>
    /// <returns>True if the execution was resumed, false if not found or not paused</returns>
    Task<bool> ResumePipelineAsync(Guid executionId);

    /// <summary>
    /// Stops a running pipeline execution.
    /// </summary>
    /// <param name="executionId">The execution identifier</param>
    /// <param name="force">Whether to force stop the execution</param>
    /// <returns>True if the execution was stopped, false if not found</returns>
    Task<bool> StopPipelineAsync(Guid executionId, bool force = false);

    /// <summary>
    /// Gets the execution history for a pipeline.
    /// </summary>
    /// <param name="pipelineId">The pipeline identifier</param>
    /// <param name="limit">Maximum number of history entries to return</param>
    /// <returns>Collection of execution history entries</returns>
    Task<IEnumerable<PipelineExecutionHistory>> GetExecutionHistoryAsync(Guid pipelineId, int limit = 100);

    /// <summary>
    /// Event raised when a pipeline execution starts.
    /// </summary>
    event EventHandler<PipelineExecutionEventArgs> PipelineStarted;

    /// <summary>
    /// Event raised when a pipeline execution completes.
    /// </summary>
    event EventHandler<PipelineExecutionEventArgs> PipelineCompleted;

    /// <summary>
    /// Event raised when a pipeline execution fails.
    /// </summary>
    event EventHandler<PipelineExecutionEventArgs> PipelineFailed;

    /// <summary>
    /// Event raised when a pipeline execution is cancelled.
    /// </summary>
    event EventHandler<PipelineExecutionEventArgs> PipelineCancelled;
}
