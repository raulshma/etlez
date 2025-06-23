using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using ETLFramework.Configuration.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ETLFramework.Pipeline;

/// <summary>
/// Implementation of pipeline orchestrator that manages pipeline execution, scheduling, and monitoring.
/// </summary>
public class PipelineOrchestrator : IPipelineOrchestrator, IDisposable
{
    private readonly ILogger<PipelineOrchestrator> _logger;
    private readonly ConcurrentDictionary<Guid, PipelineExecutionStatus> _activeExecutions;
    private readonly ConcurrentDictionary<Guid, PipelineExecutionHistory> _executionHistory;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokens;
    private readonly ConcurrentDictionary<Guid, ScheduledJob> _scheduledJobs;
    private readonly Timer _schedulerTimer;

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
        _scheduledJobs = new ConcurrentDictionary<Guid, ScheduledJob>();

        // Initialize scheduler timer to check for scheduled jobs every minute
        _schedulerTimer = new Timer(CheckScheduledJobs, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
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
        if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
        if (schedule == null) throw new ArgumentNullException(nameof(schedule));

        var jobId = Guid.NewGuid();
        var scheduledJob = new ScheduledJob
        {
            Id = jobId,
            Pipeline = pipeline,
            Schedule = schedule,
            NextRunTime = CalculateNextRunTime(schedule),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _scheduledJobs.TryAdd(jobId, scheduledJob);
        _logger.LogInformation("Scheduled pipeline {PipelineId} with job ID {JobId}. Next run: {NextRun}",
            pipeline.Id, jobId, scheduledJob.NextRunTime);

        return Task.FromResult(jobId);
    }

    /// <inheritdoc />
    public Task<bool> CancelScheduledPipelineAsync(Guid jobId)
    {
        if (_scheduledJobs.TryGetValue(jobId, out var job))
        {
            lock (job)
            {
                job.IsActive = false;
            }

            if (_scheduledJobs.TryRemove(jobId, out _))
            {
                _logger.LogInformation("Cancelled scheduled job {JobId} for pipeline {PipelineId}", jobId, job.Pipeline.Id);
                return Task.FromResult(true);
            }
        }

        _logger.LogWarning("Cannot cancel scheduled job - job not found: {JobId}", jobId);
        return Task.FromResult(false);
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

    /// <summary>
    /// Checks for scheduled jobs that need to be executed.
    /// </summary>
    /// <param name="state">Timer state (not used)</param>
    private void CheckScheduledJobs(object? state)
    {
        var now = DateTimeOffset.UtcNow;
        var jobsToRun = _scheduledJobs.Values
            .Where(job => job.IsActive && job.NextRunTime <= now)
            .ToList();

        foreach (var job in jobsToRun)
        {
            try
            {
                _logger.LogInformation("Executing scheduled job {JobId} for pipeline {PipelineId}", job.Id, job.Pipeline.Id);

                // Create a basic pipeline context for scheduled execution
                var config = new PipelineConfiguration
                {
                    Id = job.Pipeline.Id,
                    Name = job.Pipeline.Name,
                    Description = job.Pipeline.Description
                };
                var context = new PipelineContext(job.Pipeline.Id, config, _logger);

                // Execute the pipeline asynchronously (fire and forget for scheduled jobs)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ExecutePipelineAsync(job.Pipeline, context);

                        // Update job properties atomically
                        lock (job)
                        {
                            job.LastRunTime = now;
                            job.NextRunTime = CalculateNextRunTime(job.Schedule, now);
                        }

                        _logger.LogInformation("Scheduled job {JobId} completed successfully. Next run: {NextRun}",
                            job.Id, job.NextRunTime);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Scheduled job {JobId} failed", job.Id);

                        // Update job properties atomically
                        lock (job)
                        {
                            job.LastRunTime = now;
                            job.NextRunTime = CalculateNextRunTime(job.Schedule, now);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting scheduled job {JobId}", job.Id);
            }
        }
    }

    /// <summary>
    /// Calculates the next run time for a schedule.
    /// </summary>
    /// <param name="schedule">The schedule configuration</param>
    /// <param name="fromTime">The time to calculate from (defaults to now)</param>
    /// <returns>The next run time</returns>
    private DateTimeOffset CalculateNextRunTime(IScheduleConfiguration schedule, DateTimeOffset? fromTime = null)
    {
        var baseTime = fromTime ?? DateTimeOffset.UtcNow;

        if (!schedule.IsEnabled)
        {
            // If schedule is disabled, set next run time far in the future
            return DateTimeOffset.MaxValue;
        }

        if (!string.IsNullOrEmpty(schedule.CronExpression))
        {
            return CalculateNextCronTime(schedule.CronExpression, baseTime);
        }

        // Default to running every hour if no cron expression is provided
        return baseTime.AddHours(1);
    }

    /// <summary>
    /// Calculates the next run time for a cron expression (simplified implementation).
    /// </summary>
    /// <param name="cronExpression">The cron expression</param>
    /// <param name="fromTime">The time to calculate from</param>
    /// <returns>The next run time</returns>
    private DateTimeOffset CalculateNextCronTime(string? cronExpression, DateTimeOffset fromTime)
    {
        // This is a simplified implementation. In a production system, you would use a proper cron parser
        // like NCrontab or similar library
        if (string.IsNullOrEmpty(cronExpression))
            return fromTime.AddHours(1);

        // For now, just add an hour as a placeholder
        // TODO: Implement proper cron expression parsing
        return fromTime.AddHours(1);
    }

    /// <summary>
    /// Disposes the orchestrator and its resources.
    /// </summary>
    public void Dispose()
    {
        _schedulerTimer?.Dispose();

        foreach (var cts in _cancellationTokens.Values)
        {
            cts?.Dispose();
        }

        _cancellationTokens.Clear();
        _scheduledJobs.Clear();
    }
}

/// <summary>
/// Represents a scheduled pipeline job.
/// </summary>
internal class ScheduledJob
{
    /// <summary>
    /// Gets or sets the job identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the pipeline to execute.
    /// </summary>
    public IPipeline Pipeline { get; set; } = null!;

    /// <summary>
    /// Gets or sets the schedule configuration.
    /// </summary>
    public IScheduleConfiguration Schedule { get; set; } = null!;

    /// <summary>
    /// Gets or sets the next run time.
    /// </summary>
    public DateTimeOffset NextRunTime { get; set; }

    /// <summary>
    /// Gets or sets the last run time.
    /// </summary>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>
    /// Gets or sets whether the job is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets when the job was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
