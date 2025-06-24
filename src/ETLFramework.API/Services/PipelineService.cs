using ETLFramework.API.Models;
using ETLFramework.Core.Interfaces;
using ETLFramework.Data.Entities;
using ETLFramework.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ETLFramework.API.Services;

/// <summary>
/// Implementation of pipeline service using repository pattern.
/// </summary>
public class PipelineService : IPipelineService
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IExecutionRepository _executionRepository;
    private readonly ILogger<PipelineService> _logger;
    private readonly Dictionary<string, CancellationTokenSource> _runningExecutions;
    private readonly object _runningExecutionsLock = new object();

    /// <summary>
    /// Initializes a new instance of the PipelineService class.
    /// </summary>
    /// <param name="orchestrator">The pipeline orchestrator</param>
    /// <param name="pipelineRepository">The pipeline repository</param>
    /// <param name="executionRepository">The execution repository</param>
    /// <param name="logger">The logger instance</param>
    public PipelineService(
        IPipelineOrchestrator orchestrator,
        IPipelineRepository pipelineRepository,
        IExecutionRepository executionRepository,
        ILogger<PipelineService> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _pipelineRepository = pipelineRepository ?? throw new ArgumentNullException(nameof(pipelineRepository));
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runningExecutions = new Dictionary<string, CancellationTokenSource>();
    }

    /// <inheritdoc />
    public async Task<PagedResult<PipelineResponse>> GetPipelinesAsync(int page, int pageSize, string? search, bool? isEnabled)
    {
        try
        {
            var pagedPipelines = await _pipelineRepository.GetPagedAsync(page, pageSize, search, isEnabled);

            var items = new List<PipelineResponse>();
            foreach (var pipeline in pagedPipelines.Items)
            {
                var response = await MapToPipelineResponseAsync(pipeline);
                items.Add(response);
            }

            return new PagedResult<PipelineResponse>
            {
                Items = items,
                TotalCount = pagedPipelines.TotalCount,
                Page = pagedPipelines.Page,
                PageSize = pagedPipelines.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipelines. Page: {Page}, PageSize: {PageSize}, Search: {Search}, IsEnabled: {IsEnabled}",
                page, pageSize, search, isEnabled);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PipelineResponse?> GetPipelineAsync(string id)
    {
        try
        {
            var pipeline = await _pipelineRepository.GetByIdAsync(id);
            if (pipeline == null)
            {
                return null;
            }

            return await MapToPipelineResponseAsync(pipeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipeline {PipelineId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PipelineResponse> CreatePipelineAsync(CreatePipelineRequest request)
    {
        try
        {
            var id = Guid.NewGuid().ToString();
            var now = DateTimeOffset.UtcNow;

            var pipeline = new Data.Entities.Pipeline
            {
                Id = id,
                Name = request.Name,
                Description = request.Description,
                SourceConnector = MapToDataConnector(request.SourceConnector),
                TargetConnector = MapToDataConnector(request.TargetConnector),
                Transformations = MapToDataTransformations(request.Transformations),
                Configuration = request.Configuration,
                IsEnabled = request.IsEnabled,
                CreatedAt = now,
                ModifiedAt = now
            };

            var createdPipeline = await _pipelineRepository.CreateAsync(pipeline);
            return await MapToPipelineResponseAsync(createdPipeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pipeline with name '{PipelineName}'", request.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PipelineResponse?> UpdatePipelineAsync(string id, UpdatePipelineRequest request)
    {
        try
        {
            var existingPipeline = await _pipelineRepository.GetByIdAsync(id);
            if (existingPipeline == null)
            {
                return null;
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
                existingPipeline.Name = request.Name;

            if (request.Description != null)
                existingPipeline.Description = request.Description;

            if (request.SourceConnector != null)
                existingPipeline.SourceConnector = MapToDataConnector(request.SourceConnector);

            if (request.TargetConnector != null)
                existingPipeline.TargetConnector = MapToDataConnector(request.TargetConnector);

            if (request.Transformations != null)
                existingPipeline.Transformations = MapToDataTransformations(request.Transformations);

            if (request.Configuration != null)
                existingPipeline.Configuration = request.Configuration;

            if (request.IsEnabled.HasValue)
                existingPipeline.IsEnabled = request.IsEnabled.Value;

            var updatedPipeline = await _pipelineRepository.UpdateAsync(existingPipeline);
            return updatedPipeline != null ? await MapToPipelineResponseAsync(updatedPipeline) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pipeline {PipelineId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeletePipelineAsync(string id)
    {
        try
        {
            var deleted = await _pipelineRepository.DeleteAsync(id);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pipeline {PipelineId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ExecutePipelineResponse?> ExecutePipelineAsync(string id, ExecutePipelineRequest request)
    {
        try
        {
            var pipeline = await _pipelineRepository.GetByIdAsync(id);
            if (pipeline == null)
            {
                return null;
            }

            if (!pipeline.IsEnabled)
            {
                throw new ArgumentException("Pipeline is disabled");
            }

            var executionId = Guid.NewGuid().ToString();
            var startTime = DateTimeOffset.UtcNow;

            var execution = new Data.Entities.Execution
            {
                ExecutionId = executionId,
                PipelineId = id,
                Status = "Running",
                StartTime = startTime,
                Parameters = request.Parameters
            };

            // Create execution record
            var createdExecution = await _executionRepository.CreateAsync(execution);

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
                _ = Task.Run(async () => await ExecutePipelineInternalAsync(pipeline, createdExecution, request));
                return response;
            }
            else
            {
                // Execute synchronously
                await ExecutePipelineInternalAsync(pipeline, createdExecution, request);
                return MapToExecutionResponse(createdExecution);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing pipeline {PipelineId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<ExecutePipelineResponse>?> GetPipelineExecutionsAsync(string id, int page, int pageSize)
    {
        try
        {
            // Check if pipeline exists
            var pipelineExists = await _pipelineRepository.ExistsAsync(id);
            if (!pipelineExists)
            {
                return null;
            }

            var pagedExecutions = await _executionRepository.GetByPipelineIdAsync(id, page, pageSize);

            var items = pagedExecutions.Items.Select(MapToExecutionResponse).ToList();

            return new PagedResult<ExecutePipelineResponse>
            {
                Items = items,
                TotalCount = pagedExecutions.TotalCount,
                Page = pagedExecutions.Page,
                PageSize = pagedExecutions.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting executions for pipeline {PipelineId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ExecutePipelineResponse?> GetPipelineExecutionAsync(string id, string executionId)
    {
        try
        {
            var execution = await _executionRepository.GetByPipelineAndExecutionIdAsync(id, executionId);
            return execution != null ? MapToExecutionResponse(execution) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution {ExecutionId} for pipeline {PipelineId}", executionId, id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CancelPipelineExecutionAsync(string id, string executionId)
    {
        try
        {
            CancellationTokenSource? cts = null;
            lock (_runningExecutionsLock)
            {
                if (_runningExecutions.TryGetValue(executionId, out cts))
                {
                    _runningExecutions.Remove(executionId);
                }
            }

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();

                // Update execution status in database
                var execution = await _executionRepository.GetByExecutionIdAsync(executionId);
                if (execution != null)
                {
                    execution.Status = "Cancelled";
                    execution.EndTime = DateTimeOffset.UtcNow;
                    await _executionRepository.UpdateAsync(execution);
                }

                _logger.LogInformation("Cancelled pipeline execution {ExecutionId} for pipeline {PipelineId}", executionId, id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling execution {ExecutionId} for pipeline {PipelineId}", executionId, id);
            throw;
        }
    }

    /// <summary>
    /// Executes a pipeline internally.
    /// </summary>
    /// <param name="pipeline">The pipeline entity</param>
    /// <param name="execution">The execution entity</param>
    /// <param name="request">The execution request</param>
    /// <returns>A task representing the async operation</returns>
    private async Task ExecutePipelineInternalAsync(Data.Entities.Pipeline pipeline, Data.Entities.Execution execution, ExecutePipelineRequest request)
    {
        var cts = new CancellationTokenSource();
        if (request.TimeoutSeconds.HasValue)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds.Value));
        }

        lock (_runningExecutionsLock)
        {
            _runningExecutions[execution.ExecutionId] = cts;
        }

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

            // Update execution in database
            await _executionRepository.UpdateAsync(execution);

            _logger.LogInformation("Completed execution {ExecutionId} for pipeline {PipelineId}. " +
                                 "Processed {RecordsProcessed} records in {Duration}ms",
                execution.ExecutionId, pipeline.Id, execution.RecordsProcessed,
                execution.Duration?.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            execution.Status = "Cancelled";
            execution.EndTime = DateTimeOffset.UtcNow;
            await _executionRepository.UpdateAsync(execution);
            _logger.LogInformation("Execution {ExecutionId} for pipeline {PipelineId} was cancelled",
                execution.ExecutionId, pipeline.Id);
        }
        catch (Exception ex)
        {
            execution.Status = "Failed";
            execution.EndTime = DateTimeOffset.UtcNow;
            execution.ErrorMessage = ex.Message;
            await _executionRepository.UpdateAsync(execution);
            _logger.LogError(ex, "Execution {ExecutionId} for pipeline {PipelineId} failed",
                execution.ExecutionId, pipeline.Id);
        }
        finally
        {
            lock (_runningExecutionsLock)
            {
                _runningExecutions.Remove(execution.ExecutionId);
            }
            cts.Dispose();

            // Update pipeline last execution time
            if (execution.EndTime.HasValue)
            {
                await _pipelineRepository.UpdateLastExecutedAsync(pipeline.Id, execution.EndTime.Value);
            }
        }
    }

    /// <summary>
    /// Maps pipeline entity to response model.
    /// </summary>
    /// <param name="pipeline">The pipeline entity</param>
    /// <returns>Pipeline response</returns>
    private async Task<PipelineResponse> MapToPipelineResponseAsync(Data.Entities.Pipeline pipeline)
    {
        var statistics = await _executionRepository.GetStatisticsAsync(pipeline.Id);

        return new PipelineResponse
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            Description = pipeline.Description,
            SourceConnector = MapToApiConnector(pipeline.SourceConnector),
            TargetConnector = MapToApiConnector(pipeline.TargetConnector),
            Transformations = MapToApiTransformations(pipeline.Transformations),
            Configuration = pipeline.Configuration ?? new Dictionary<string, object>(),
            IsEnabled = pipeline.IsEnabled,
            CreatedAt = pipeline.CreatedAt,
            ModifiedAt = pipeline.ModifiedAt,
            LastExecutedAt = pipeline.LastExecutedAt,
            Statistics = MapToApiStatistics(statistics)
        };
    }

    /// <summary>
    /// Maps execution entity to response model.
    /// </summary>
    /// <param name="execution">The execution entity</param>
    /// <returns>Execution response</returns>
    private static ExecutePipelineResponse MapToExecutionResponse(Data.Entities.Execution execution)
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
            Details = execution.Parameters ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Maps data connector to API connector.
    /// </summary>
    /// <param name="dataConnector">The data connector</param>
    /// <returns>API connector</returns>
    private static ConnectorConfigurationDto MapToApiConnector(Data.Models.ConnectorConfigurationDto? dataConnector)
    {
        if (dataConnector == null)
            return new ConnectorConfigurationDto();

        return new ConnectorConfigurationDto
        {
            Type = dataConnector.Type,
            Configuration = dataConnector.Configuration
        };
    }

    /// <summary>
    /// Maps API connector to data connector.
    /// </summary>
    /// <param name="apiConnector">The API connector</param>
    /// <returns>Data connector</returns>
    private static Data.Models.ConnectorConfigurationDto MapToDataConnector(ConnectorConfigurationDto apiConnector)
    {
        return new Data.Models.ConnectorConfigurationDto
        {
            Type = apiConnector.Type,
            Configuration = apiConnector.Configuration
        };
    }

    /// <summary>
    /// Maps data transformations to API transformations.
    /// </summary>
    /// <param name="dataTransformations">The data transformations</param>
    /// <returns>API transformations</returns>
    private static List<TransformationConfigurationDto> MapToApiTransformations(List<Data.Models.TransformationConfigurationDto>? dataTransformations)
    {
        if (dataTransformations == null)
            return new List<TransformationConfigurationDto>();

        return dataTransformations.Select(dt => new TransformationConfigurationDto
        {
            Id = dt.Id,
            Name = dt.Name,
            Type = dt.Type,
            Configuration = dt.Configuration,
            Order = dt.Order
        }).ToList();
    }

    /// <summary>
    /// Maps API transformations to data transformations.
    /// </summary>
    /// <param name="apiTransformations">The API transformations</param>
    /// <returns>Data transformations</returns>
    private static List<Data.Models.TransformationConfigurationDto> MapToDataTransformations(List<TransformationConfigurationDto> apiTransformations)
    {
        return apiTransformations.Select(at => new Data.Models.TransformationConfigurationDto
        {
            Id = at.Id,
            Name = at.Name,
            Type = at.Type,
            Configuration = at.Configuration,
            Order = at.Order
        }).ToList();
    }

    /// <summary>
    /// Maps execution statistics to API statistics.
    /// </summary>
    /// <param name="statistics">The execution statistics</param>
    /// <returns>API statistics</returns>
    private static PipelineStatisticsDto? MapToApiStatistics(Data.Repositories.Interfaces.ExecutionStatistics statistics)
    {
        if (statistics.TotalExecutions == 0) return null;

        return new PipelineStatisticsDto
        {
            TotalExecutions = statistics.TotalExecutions,
            SuccessfulExecutions = statistics.SuccessfulExecutions,
            FailedExecutions = statistics.FailedExecutions,
            AverageExecutionTime = statistics.AverageExecutionTime,
            TotalRecordsProcessed = statistics.TotalRecordsProcessed,
            AverageThroughput = statistics.AverageThroughput,
            LastExecutionStatus = statistics.LastExecutionStatus
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
