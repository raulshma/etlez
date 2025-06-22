using System.Collections.Concurrent;
using ETLFramework.API.Models;
using ETLFramework.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ETLFramework.API.Services;

/// <summary>
/// Implementation of pipeline service.
/// </summary>
public class PipelineService : IPipelineService
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<PipelineService> _logger;
    private readonly ConcurrentDictionary<string, PipelineInfo> _pipelines;
    private readonly ConcurrentDictionary<string, List<ExecutionInfo>> _executions;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningExecutions;

    /// <summary>
    /// Initializes a new instance of the PipelineService class.
    /// </summary>
    /// <param name="orchestrator">The pipeline orchestrator</param>
    /// <param name="logger">The logger instance</param>
    public PipelineService(IPipelineOrchestrator orchestrator, ILogger<PipelineService> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipelines = new ConcurrentDictionary<string, PipelineInfo>();
        _executions = new ConcurrentDictionary<string, List<ExecutionInfo>>();
        _runningExecutions = new ConcurrentDictionary<string, CancellationTokenSource>();
    }

    /// <inheritdoc />
    public Task<PagedResult<PipelineResponse>> GetPipelinesAsync(int page, int pageSize, string? search, bool? isEnabled)
    {
        var query = _pipelines.Values.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                   (p.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Apply enabled filter
        if (isEnabled.HasValue)
        {
            query = query.Where(p => p.IsEnabled == isEnabled.Value);
        }

        var totalCount = query.Count();
        var items = query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToPipelineResponse)
            .ToList();

        var result = new PagedResult<PipelineResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<PipelineResponse?> GetPipelineAsync(string id)
    {
        if (_pipelines.TryGetValue(id, out var pipeline))
        {
            return Task.FromResult<PipelineResponse?>(MapToPipelineResponse(pipeline));
        }

        return Task.FromResult<PipelineResponse?>(null);
    }

    /// <inheritdoc />
    public Task<PipelineResponse> CreatePipelineAsync(CreatePipelineRequest request)
    {
        var id = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;

        var pipeline = new PipelineInfo
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            SourceConnector = request.SourceConnector,
            TargetConnector = request.TargetConnector,
            Transformations = request.Transformations,
            Configuration = request.Configuration,
            IsEnabled = request.IsEnabled,
            CreatedAt = now,
            ModifiedAt = now
        };

        _pipelines.TryAdd(id, pipeline);
        _executions.TryAdd(id, new List<ExecutionInfo>());

        _logger.LogInformation("Created pipeline {PipelineId} with name '{PipelineName}'", id, request.Name);

        return Task.FromResult(MapToPipelineResponse(pipeline));
    }

    /// <inheritdoc />
    public Task<PipelineResponse?> UpdatePipelineAsync(string id, UpdatePipelineRequest request)
    {
        if (!_pipelines.TryGetValue(id, out var pipeline))
        {
            return Task.FromResult<PipelineResponse?>(null);
        }

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
            pipeline.Name = request.Name;

        if (request.Description != null)
            pipeline.Description = request.Description;

        if (request.SourceConnector != null)
            pipeline.SourceConnector = request.SourceConnector;

        if (request.TargetConnector != null)
            pipeline.TargetConnector = request.TargetConnector;

        if (request.Transformations != null)
            pipeline.Transformations = request.Transformations;

        if (request.Configuration != null)
            pipeline.Configuration = request.Configuration;

        if (request.IsEnabled.HasValue)
            pipeline.IsEnabled = request.IsEnabled.Value;

        pipeline.ModifiedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation("Updated pipeline {PipelineId}", id);

        return Task.FromResult<PipelineResponse?>(MapToPipelineResponse(pipeline));
    }

    /// <inheritdoc />
    public Task<bool> DeletePipelineAsync(string id)
    {
        var removed = _pipelines.TryRemove(id, out _);
        if (removed)
        {
            _executions.TryRemove(id, out _);
            _logger.LogInformation("Deleted pipeline {PipelineId}", id);
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public async Task<ExecutePipelineResponse?> ExecutePipelineAsync(string id, ExecutePipelineRequest request)
    {
        if (!_pipelines.TryGetValue(id, out var pipeline))
        {
            return null;
        }

        if (!pipeline.IsEnabled)
        {
            throw new ArgumentException("Pipeline is disabled");
        }

        var executionId = Guid.NewGuid().ToString();
        var startTime = DateTimeOffset.UtcNow;

        var execution = new ExecutionInfo
        {
            ExecutionId = executionId,
            PipelineId = id,
            Status = "Running",
            StartTime = startTime,
            Parameters = request.Parameters
        };

        // Add to execution history
        _executions.AddOrUpdate(id, 
            new List<ExecutionInfo> { execution },
            (key, existing) => 
            {
                existing.Add(execution);
                return existing;
            });

        var response = new ExecutePipelineResponse
        {
            ExecutionId = executionId,
            PipelineId = id,
            Status = "Running",
            StartTime = startTime
        };

        if (request.Async)
        {
            // Start async execution
            _ = Task.Run(async () => await ExecutePipelineInternalAsync(pipeline, execution, request));
            return response;
        }
        else
        {
            // Execute synchronously
            await ExecutePipelineInternalAsync(pipeline, execution, request);
            return MapToExecutionResponse(execution);
        }
    }

    /// <inheritdoc />
    public Task<PagedResult<ExecutePipelineResponse>?> GetPipelineExecutionsAsync(string id, int page, int pageSize)
    {
        if (!_pipelines.ContainsKey(id))
        {
            return Task.FromResult<PagedResult<ExecutePipelineResponse>?>(null);
        }

        var executions = _executions.GetValueOrDefault(id, new List<ExecutionInfo>());
        var totalCount = executions.Count;
        var items = executions
            .OrderByDescending(e => e.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToExecutionResponse)
            .ToList();

        var result = new PagedResult<ExecutePipelineResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Task.FromResult<PagedResult<ExecutePipelineResponse>?>(result);
    }

    /// <inheritdoc />
    public Task<ExecutePipelineResponse?> GetPipelineExecutionAsync(string id, string executionId)
    {
        if (!_executions.TryGetValue(id, out var executions))
        {
            return Task.FromResult<ExecutePipelineResponse?>(null);
        }

        var execution = executions.FirstOrDefault(e => e.ExecutionId == executionId);
        return Task.FromResult(execution != null ? MapToExecutionResponse(execution) : null);
    }

    /// <inheritdoc />
    public Task<bool> CancelPipelineExecutionAsync(string id, string executionId)
    {
        if (_runningExecutions.TryRemove(executionId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();

            // Update execution status
            if (_executions.TryGetValue(id, out var executions))
            {
                var execution = executions.FirstOrDefault(e => e.ExecutionId == executionId);
                if (execution != null)
                {
                    execution.Status = "Cancelled";
                    execution.EndTime = DateTimeOffset.UtcNow;
                }
            }

            _logger.LogInformation("Cancelled pipeline execution {ExecutionId} for pipeline {PipelineId}", executionId, id);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Executes a pipeline internally.
    /// </summary>
    /// <param name="pipeline">The pipeline info</param>
    /// <param name="execution">The execution info</param>
    /// <param name="request">The execution request</param>
    /// <returns>A task representing the async operation</returns>
    private async Task ExecutePipelineInternalAsync(PipelineInfo pipeline, ExecutionInfo execution, ExecutePipelineRequest request)
    {
        var cts = new CancellationTokenSource();
        if (request.TimeoutSeconds.HasValue)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds.Value));
        }

        _runningExecutions.TryAdd(execution.ExecutionId, cts);

        try
        {
            _logger.LogInformation("Starting execution {ExecutionId} for pipeline {PipelineId}", 
                execution.ExecutionId, pipeline.Id);

            // Simulate pipeline execution
            await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);

            // Simulate processing records
            execution.RecordsProcessed = Random.Shared.Next(100, 1000);
            execution.SuccessfulRecords = execution.RecordsProcessed - Random.Shared.Next(0, 10);
            execution.FailedRecords = execution.RecordsProcessed - execution.SuccessfulRecords;

            execution.Status = "Completed";
            execution.EndTime = DateTimeOffset.UtcNow;

            _logger.LogInformation("Completed execution {ExecutionId} for pipeline {PipelineId}. " +
                                 "Processed {RecordsProcessed} records in {Duration}ms", 
                execution.ExecutionId, pipeline.Id, execution.RecordsProcessed, 
                execution.Duration?.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            execution.Status = "Cancelled";
            execution.EndTime = DateTimeOffset.UtcNow;
            _logger.LogInformation("Execution {ExecutionId} for pipeline {PipelineId} was cancelled", 
                execution.ExecutionId, pipeline.Id);
        }
        catch (Exception ex)
        {
            execution.Status = "Failed";
            execution.EndTime = DateTimeOffset.UtcNow;
            execution.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Execution {ExecutionId} for pipeline {PipelineId} failed", 
                execution.ExecutionId, pipeline.Id);
        }
        finally
        {
            _runningExecutions.TryRemove(execution.ExecutionId, out _);
            cts.Dispose();

            // Update pipeline last execution time
            pipeline.LastExecutedAt = execution.EndTime;
        }
    }

    /// <summary>
    /// Maps pipeline info to response model.
    /// </summary>
    /// <param name="pipeline">The pipeline info</param>
    /// <returns>Pipeline response</returns>
    private PipelineResponse MapToPipelineResponse(PipelineInfo pipeline)
    {
        var executions = _executions.GetValueOrDefault(pipeline.Id, new List<ExecutionInfo>());
        var statistics = CalculateStatistics(executions);

        return new PipelineResponse
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            Description = pipeline.Description,
            SourceConnector = pipeline.SourceConnector,
            TargetConnector = pipeline.TargetConnector,
            Transformations = pipeline.Transformations,
            Configuration = pipeline.Configuration,
            IsEnabled = pipeline.IsEnabled,
            CreatedAt = pipeline.CreatedAt,
            ModifiedAt = pipeline.ModifiedAt,
            LastExecutedAt = pipeline.LastExecutedAt,
            Statistics = statistics
        };
    }

    /// <summary>
    /// Maps execution info to response model.
    /// </summary>
    /// <param name="execution">The execution info</param>
    /// <returns>Execution response</returns>
    private static ExecutePipelineResponse MapToExecutionResponse(ExecutionInfo execution)
    {
        return new ExecutePipelineResponse
        {
            ExecutionId = execution.ExecutionId,
            PipelineId = execution.PipelineId,
            Status = execution.Status,
            StartTime = execution.StartTime,
            EndTime = execution.EndTime,
            Duration = execution.Duration,
            RecordsProcessed = execution.RecordsProcessed,
            SuccessfulRecords = execution.SuccessfulRecords,
            FailedRecords = execution.FailedRecords,
            ErrorMessage = execution.ErrorMessage,
            Details = execution.Parameters
        };
    }

    /// <summary>
    /// Calculates pipeline statistics.
    /// </summary>
    /// <param name="executions">The execution list</param>
    /// <returns>Pipeline statistics</returns>
    private static PipelineStatisticsDto? CalculateStatistics(List<ExecutionInfo> executions)
    {
        if (executions.Count == 0) return null;

        var completedExecutions = executions.Where(e => e.Duration.HasValue).ToList();
        
        return new PipelineStatisticsDto
        {
            TotalExecutions = executions.Count,
            SuccessfulExecutions = executions.Count(e => e.Status == "Completed"),
            FailedExecutions = executions.Count(e => e.Status == "Failed"),
            AverageExecutionTime = completedExecutions.Count > 0 
                ? TimeSpan.FromTicks((long)completedExecutions.Average(e => e.Duration!.Value.Ticks))
                : TimeSpan.Zero,
            TotalRecordsProcessed = executions.Sum(e => e.RecordsProcessed),
            AverageThroughput = completedExecutions.Count > 0 
                ? completedExecutions.Average(e => e.Duration!.Value.TotalSeconds > 0 ? e.RecordsProcessed / e.Duration.Value.TotalSeconds : 0)
                : 0,
            LastExecutionStatus = executions.OrderByDescending(e => e.StartTime).FirstOrDefault()?.Status
        };
    }
}

/// <summary>
/// Internal pipeline information.
/// </summary>
internal class PipelineInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ConnectorConfigurationDto SourceConnector { get; set; } = new();
    public ConnectorConfigurationDto TargetConnector { get; set; } = new();
    public List<TransformationConfigurationDto> Transformations { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public DateTimeOffset? LastExecutedAt { get; set; }
}

/// <summary>
/// Internal execution information.
/// </summary>
internal class ExecutionInfo
{
    public string ExecutionId { get; set; } = string.Empty;
    public string PipelineId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public long RecordsProcessed { get; set; }
    public long SuccessfulRecords { get; set; }
    public long FailedRecords { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();

    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
}
