using ETLFramework.Data.Models;
using ETLFramework.Data.Entities;

namespace ETLFramework.Data.Repositories.Interfaces;

/// <summary>
/// Repository interface for execution operations.
/// </summary>
public interface IExecutionRepository
{
    /// <summary>
    /// Gets an execution by execution ID.
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The execution or null if not found</returns>
    Task<Execution?> GetByExecutionIdAsync(string executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an execution by pipeline ID and execution ID.
    /// </summary>
    /// <param name="pipelineId">The pipeline ID</param>
    /// <param name="executionId">The execution ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The execution or null if not found</returns>
    Task<Execution?> GetByPipelineAndExecutionIdAsync(string pipelineId, string executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions for a pipeline with pagination.
    /// </summary>
    /// <param name="pipelineId">The pipeline ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of executions</returns>
    Task<PagedResult<Execution>> GetByPipelineIdAsync(
        string pipelineId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new execution.
    /// </summary>
    /// <param name="execution">The execution to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created execution</returns>
    Task<Execution> CreateAsync(Execution execution, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing execution.
    /// </summary>
    /// <param name="execution">The execution to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated execution or null if not found</returns>
    Task<Execution?> UpdateAsync(Execution execution, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest execution for a pipeline.
    /// </summary>
    /// <param name="pipelineId">The pipeline ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The latest execution or null if none found</returns>
    Task<Execution?> GetLatestByPipelineIdAsync(string pipelineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets running executions for a pipeline.
    /// </summary>
    /// <param name="pipelineId">The pipeline ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of running executions</returns>
    Task<List<Execution>> GetRunningExecutionsAsync(string pipelineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution statistics for a pipeline.
    /// </summary>
    /// <param name="pipelineId">The pipeline ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution statistics</returns>
    Task<ExecutionStatistics> GetStatisticsAsync(string pipelineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes executions older than the specified date.
    /// </summary>
    /// <param name="olderThan">Delete executions older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted executions</returns>
    Task<int> DeleteOldExecutionsAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Execution statistics for a pipeline.
/// </summary>
public class ExecutionStatistics
{
    /// <summary>
    /// Gets or sets the total number of executions.
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of successful executions.
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions.
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of running executions.
    /// </summary>
    public int RunningExecutions { get; set; }

    /// <summary>
    /// Gets or sets the average execution time.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the total records processed.
    /// </summary>
    public long TotalRecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the average throughput in records per second.
    /// </summary>
    public double AverageThroughput { get; set; }

    /// <summary>
    /// Gets or sets the last execution status.
    /// </summary>
    public string? LastExecutionStatus { get; set; }

    /// <summary>
    /// Gets or sets the last execution date.
    /// </summary>
    public DateTimeOffset? LastExecutionDate { get; set; }
}
