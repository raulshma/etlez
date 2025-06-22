using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ETLFramework.Pipeline;

/// <summary>
/// Implementation of pipeline orchestrator that manages pipeline execution, scheduling, and monitoring.
/// </summary>
public class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly ILogger<PipelineOrchestrator> _logger;
    private readonly ConcurrentDictionary<Guid, PipelineExecutionStatus> _activeExecutions;
    private readonly ConcurrentDictionary<Guid, PipelineExecutionHistory> _executionHistory;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokens;

    /// <summary>
    /// Initializes a new instance of the PipelineOrchestrator class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public PipelineOrchestrator(ILogger<PipelineOrchestrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activeExecutions = new ConcurrentDictionary<Guid, PipelineExecutionStatus>();
        _executionHistory = new ConcurrentDictionary<Guid, PipelineExecutionHistory>();
        _cancellationTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
    }

    /// <inheritdoc />
    public event EventHandler<PipelineExecutionEventArgs>? PipelineStarted;

    /// <inheritdoc />
    public event EventHandler<PipelineExecutionEventArgs>? PipelineCompleted;

    /// <inheritdoc />
    public event EventHandler<PipelineExecutionEventArgs>? PipelineFailed;

    /// <inheritdoc />
    public event EventHandler<PipelineExecutionEventArgs>? PipelineCancelled;

    /// <inheritdoc />
    public async Task<PipelineExecutionResult> ExecutePipelineAsync(IPipeline pipeline, IPipelineContext context, CancellationToken cancellationToken = default)
    {
        if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var executionId = context.ExecutionId;
        
        _logger.LogInformation("Starting pipeline execution: {PipelineName} (Execution: {ExecutionId})",
            pipeline.Name, executionId);

        // Create execution status
        var executionStatus = new PipelineExecutionStatus
        {
            ExecutionId = executionId,
            Status = PipelineStatus.Running,
            StartTime = DateTimeOffset.UtcNow,
            RecordsProcessed = 0
        };

        // Register the execution
        _activeExecutions[executionId] = executionStatus;

        // Create cancellation token source for this execution
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokens[executionId] = cts;

        PipelineExecutionResult? result = null;

        try
        {
            // Raise pipeline started event
            OnPipelineStarted(new PipelineExecutionEventArgs
            {
                ExecutionId = executionId,
                PipelineId = pipeline.Id
            });

            // Execute the pipeline
            result = await pipeline.ExecuteAsync(context, cts.Token);

            // Update execution status
            executionStatus.Status = result.IsSuccess ? PipelineStatus.Completed : PipelineStatus.Failed;
            executionStatus.RecordsProcessed = result.RecordsProcessed;

            // Add to execution history
            var historyEntry = new PipelineExecutionHistory
            {
                ExecutionId = executionId,
                StartTime = result.StartTime,
                EndTime = result.EndTime,
                Status = executionStatus.Status,
                RecordsProcessed = result.RecordsProcessed
            };
            _executionHistory[executionId] = historyEntry;

            if (result.IsSuccess)
            {
                _logger.LogInformation("Pipeline execution completed successfully: {PipelineName} (Execution: {ExecutionId})",
                    pipeline.Name, executionId);

                OnPipelineCompleted(new PipelineExecutionEventArgs
                {
                    ExecutionId = executionId,
                    PipelineId = pipeline.Id,
                    Result = result
                });
            }
            else
            {
                _logger.LogWarning("Pipeline execution completed with errors: {PipelineName} (Execution: {ExecutionId}, Errors: {ErrorCount})",
                    pipeline.Name, executionId, result.Errors.Count);

                OnPipelineFailed(new PipelineExecutionEventArgs
                {
                    ExecutionId = executionId,
                    PipelineId = pipeline.Id,
                    Result = result
                });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Pipeline execution was cancelled: {PipelineName} (Execution: {ExecutionId})",
                pipeline.Name, executionId);

            executionStatus.Status = PipelineStatus.Cancelled;

            OnPipelineCancelled(new PipelineExecutionEventArgs
            {
                ExecutionId = executionId,
                PipelineId = pipeline.Id,
                Result = result
            });

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline execution failed: {PipelineName} (Execution: {ExecutionId})",
                pipeline.Name, executionId);

            executionStatus.Status = PipelineStatus.Failed;

            OnPipelineFailed(new PipelineExecutionEventArgs
            {
                ExecutionId = executionId,
                PipelineId = pipeline.Id,
                Result = result
            });

            throw;
        }
        finally
        {
            // Clean up
            _activeExecutions.TryRemove(executionId, out _);
            _cancellationTokens.TryRemove(executionId, out var tokenSource);
            tokenSource?.Dispose();
        }

        return result;
    }

    /// <inheritdoc />
    public Task<Guid> SchedulePipelineAsync(IPipeline pipeline, IScheduleConfiguration schedule)
    {
        // TODO: Implement scheduling functionality
        // This would integrate with a job scheduler like Quartz.NET or similar
        throw new NotImplementedException("Pipeline scheduling will be implemented in a future version");
    }

    /// <inheritdoc />
    public Task<bool> CancelScheduledPipelineAsync(Guid jobId)
    {
        // TODO: Implement scheduled job cancellation
        throw new NotImplementedException("Pipeline scheduling will be implemented in a future version");
    }

    /// <inheritdoc />
    public Task<PipelineExecutionStatus?> GetExecutionStatusAsync(Guid executionId)
    {
        _activeExecutions.TryGetValue(executionId, out var status);
        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public Task<IEnumerable<PipelineExecutionStatus>> GetActiveExecutionsAsync()
    {
        return Task.FromResult(_activeExecutions.Values.AsEnumerable());
    }

    /// <inheritdoc />
    public Task<bool> PausePipelineAsync(Guid executionId)
    {
        // TODO: Implement pipeline pausing
        // This would require more sophisticated execution control
        _logger.LogWarning("Pipeline pausing is not yet implemented for execution: {ExecutionId}", executionId);
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<bool> ResumePipelineAsync(Guid executionId)
    {
        // TODO: Implement pipeline resuming
        _logger.LogWarning("Pipeline resuming is not yet implemented for execution: {ExecutionId}", executionId);
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<bool> StopPipelineAsync(Guid executionId, bool force = false)
    {
        if (_cancellationTokens.TryGetValue(executionId, out var cts))
        {
            _logger.LogInformation("Stopping pipeline execution: {ExecutionId} (Force: {Force})", executionId, force);

            if (force)
            {
                cts.Cancel();
            }
            else
            {
                cts.CancelAfter(TimeSpan.FromSeconds(30)); // Give it 30 seconds to stop gracefully
            }

            return Task.FromResult(true);
        }

        _logger.LogWarning("Cannot stop pipeline execution - execution not found: {ExecutionId}", executionId);
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<IEnumerable<PipelineExecutionHistory>> GetExecutionHistoryAsync(Guid pipelineId, int limit = 100)
    {
        var history = _executionHistory.Values
            .OrderByDescending(h => h.StartTime)
            .Take(limit);

        return Task.FromResult(history);
    }

    /// <summary>
    /// Creates a pipeline context for execution.
    /// </summary>
    /// <param name="configuration">The pipeline configuration</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A new pipeline context</returns>
    public IPipelineContext CreateContext(IPipelineConfiguration configuration, ILogger logger, CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid();
        return new PipelineContext(executionId, configuration, logger, cancellationToken);
    }

    /// <summary>
    /// Executes a pipeline with automatic context creation.
    /// </summary>
    /// <param name="pipeline">The pipeline to execute</param>
    /// <param name="configuration">The pipeline configuration</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The execution result</returns>
    public async Task<PipelineExecutionResult> ExecutePipelineAsync(
        IPipeline pipeline, 
        IPipelineConfiguration configuration, 
        ILogger logger, 
        CancellationToken cancellationToken = default)
    {
        var context = CreateContext(configuration, logger, cancellationToken);
        return await ExecutePipelineAsync(pipeline, context, cancellationToken);
    }

    /// <summary>
    /// Gets the total number of active executions.
    /// </summary>
    public int ActiveExecutionCount => _activeExecutions.Count;

    /// <summary>
    /// Gets the total number of executions in history.
    /// </summary>
    public int ExecutionHistoryCount => _executionHistory.Count;

    /// <summary>
    /// Clears the execution history.
    /// </summary>
    public void ClearExecutionHistory()
    {
        var count = _executionHistory.Count;
        _executionHistory.Clear();
        _logger.LogInformation("Cleared {Count} execution history entries", count);
    }

    /// <summary>
    /// Raises the PipelineStarted event.
    /// </summary>
    /// <param name="args">The event arguments</param>
    protected virtual void OnPipelineStarted(PipelineExecutionEventArgs args)
    {
        PipelineStarted?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the PipelineCompleted event.
    /// </summary>
    /// <param name="args">The event arguments</param>
    protected virtual void OnPipelineCompleted(PipelineExecutionEventArgs args)
    {
        PipelineCompleted?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the PipelineFailed event.
    /// </summary>
    /// <param name="args">The event arguments</param>
    protected virtual void OnPipelineFailed(PipelineExecutionEventArgs args)
    {
        PipelineFailed?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the PipelineCancelled event.
    /// </summary>
    /// <param name="args">The event arguments</param>
    protected virtual void OnPipelineCancelled(PipelineExecutionEventArgs args)
    {
        PipelineCancelled?.Invoke(this, args);
    }
}
