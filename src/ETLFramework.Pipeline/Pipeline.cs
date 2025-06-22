using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Pipeline;

/// <summary>
/// Implementation of an ETL pipeline that can execute multiple stages in sequence.
/// </summary>
public class Pipeline : IPipeline
{
    private readonly ILogger<Pipeline> _logger;
    private readonly List<IPipelineStage> _stages;
    private PipelineStatus _status;

    /// <summary>
    /// Initializes a new instance of the Pipeline class.
    /// </summary>
    /// <param name="id">The unique pipeline identifier</param>
    /// <param name="name">The pipeline name</param>
    /// <param name="description">The pipeline description</param>
    /// <param name="logger">The logger instance</param>
    public Pipeline(Guid id, string name, string description, ILogger<Pipeline> logger)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stages = new List<IPipelineStage>();
        _status = PipelineStatus.Ready;
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public IReadOnlyList<IPipelineStage> Stages => _stages.AsReadOnly();

    /// <inheritdoc />
    public PipelineStatus Status => _status;

    /// <inheritdoc />
    public async Task<PipelineExecutionResult> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var executionResult = new PipelineExecutionResult
        {
            ExecutionId = context.ExecutionId,
            PipelineId = Id,
            StartTime = DateTimeOffset.UtcNow,
            IsSuccess = false
        };

        try
        {
            _logger.LogInformation("Starting pipeline execution: {PipelineName} (ID: {PipelineId}, Execution: {ExecutionId})",
                Name, Id, context.ExecutionId);

            _status = PipelineStatus.Running;

            // Validate pipeline before execution
            var validationResult = await ValidateAsync();
            if (!validationResult.IsValid)
            {
                var errorMessage = $"Pipeline validation failed: {string.Join("; ", validationResult.Errors.Select(e => e.Message))}";
                throw PipelineExecutionException.Create(errorMessage, Id, context.ExecutionId);
            }

            // Execute stages in order
            var enabledStages = _stages.Where(s => s.Status != StageStatus.Skipped).OrderBy(s => s.Order).ToList();
            
            _logger.LogInformation("Executing {StageCount} stages", enabledStages.Count);

            foreach (var stage in enabledStages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogInformation("Executing stage: {StageName} (Order: {Order}, Type: {StageType})",
                        stage.Name, stage.Order, stage.StageType);

                    // Create stage-specific context
                    var stageContext = context.CreateStageContext(stage);

                    // Prepare stage
                    await stage.PrepareAsync(stageContext, cancellationToken);

                    // Execute stage
                    var stageResult = await stage.ExecuteAsync(stageContext, cancellationToken);

                    // Update execution statistics
                    executionResult.RecordsProcessed += stageResult.RecordsProcessed;
                    
                    if (!stageResult.IsSuccess)
                    {
                        executionResult.RecordsFailed += stageResult.RecordsProcessed;
                        
                        // Add stage errors to execution result
                        foreach (var error in stageResult.Errors)
                        {
                            executionResult.Errors.Add(error);
                        }

                        // Check error handling configuration
                        if (context.Configuration.ErrorHandling.StopOnError)
                        {
                            throw PipelineExecutionException.CreateForStage(
                                $"Stage '{stage.Name}' failed and pipeline is configured to stop on error",
                                Id, context.ExecutionId, stage.Id, stage.Name);
                        }
                    }

                    // Cleanup stage
                    await stage.CleanupAsync(stageContext, cancellationToken);

                    _logger.LogInformation("Completed stage: {StageName} - Success: {IsSuccess}, Records: {RecordsProcessed}",
                        stage.Name, stageResult.IsSuccess, stageResult.RecordsProcessed);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Pipeline execution was cancelled during stage: {StageName}", stage.Name);
                    _status = PipelineStatus.Cancelled;
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing stage: {StageName}", stage.Name);
                    
                    var executionError = new ExecutionError
                    {
                        Message = ex.Message,
                        Exception = ex,
                        Source = $"Stage: {stage.Name}",
                        ErrorCode = "STAGE_EXECUTION_ERROR"
                    };
                    
                    executionResult.Errors.Add(executionError);
                    context.AddError(executionError);

                    // Check if we should stop on error
                    if (context.Configuration.ErrorHandling.StopOnError)
                    {
                        throw;
                    }
                }
            }

            // Check if we have too many errors
            if (executionResult.Errors.Count > context.Configuration.ErrorHandling.MaxErrors)
            {
                throw new PipelineExecutionException(
                    $"Pipeline exceeded maximum allowed errors ({context.Configuration.ErrorHandling.MaxErrors})")
                {
                    PipelineId = Id,
                    ExecutionId = context.ExecutionId
                };
            }

            executionResult.IsSuccess = executionResult.Errors.Count == 0;
            _status = executionResult.IsSuccess ? PipelineStatus.Completed : PipelineStatus.Failed;

            _logger.LogInformation("Pipeline execution completed: {PipelineName} - Success: {IsSuccess}, Records: {RecordsProcessed}, Errors: {ErrorCount}",
                Name, executionResult.IsSuccess, executionResult.RecordsProcessed, executionResult.Errors.Count);
        }
        catch (OperationCanceledException)
        {
            _status = PipelineStatus.Cancelled;
            executionResult.IsSuccess = false;
            _logger.LogWarning("Pipeline execution was cancelled: {PipelineName}", Name);
            throw;
        }
        catch (Exception ex)
        {
            _status = PipelineStatus.Failed;
            executionResult.IsSuccess = false;
            
            _logger.LogError(ex, "Pipeline execution failed: {PipelineName}", Name);
            
            if (ex is not PipelineExecutionException)
            {
                // Wrap non-pipeline exceptions
                throw new PipelineExecutionException($"Pipeline execution failed: {ex.Message}", ex)
                {
                    PipelineId = Id,
                    ExecutionId = context.ExecutionId
                };
            }
            throw;
        }
        finally
        {
            executionResult.EndTime = DateTimeOffset.UtcNow;
            
            // Copy statistics from context
            if (context.Statistics is ExecutionStatistics stats)
            {
                executionResult.Statistics = stats;
            }

            // Copy errors and warnings from context
            foreach (var error in context.Errors)
            {
                if (!executionResult.Errors.Contains(error))
                {
                    executionResult.Errors.Add(error);
                }
            }

            foreach (var warning in context.Warnings)
            {
                if (!executionResult.Warnings.Contains(warning))
                {
                    executionResult.Warnings.Add(warning);
                }
            }
        }

        return executionResult;
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateAsync()
    {
        var result = new ValidationResult { IsValid = true };

        // Validate pipeline properties
        if (string.IsNullOrWhiteSpace(Name))
        {
            result.AddError("Pipeline name is required", nameof(Name));
        }

        // Validate stages
        if (_stages.Count == 0)
        {
            result.AddWarning("Pipeline has no stages", nameof(Stages));
        }
        else
        {
            // Check for duplicate orders
            var orders = _stages.Select(s => s.Order).ToList();
            var duplicateOrders = orders.GroupBy(o => o).Where(g => g.Count() > 1).Select(g => g.Key);
            
            foreach (var order in duplicateOrders)
            {
                result.AddError($"Duplicate stage order: {order}", nameof(Stages));
            }

            // Validate each stage
            foreach (var stage in _stages)
            {
                // Stage validation would be implemented when we have concrete stage implementations
                // For now, just check basic properties
                if (string.IsNullOrWhiteSpace(stage.Name))
                {
                    result.AddError($"Stage at order {stage.Order} has no name", nameof(Stages));
                }
            }
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public void AddStage(IPipelineStage stage)
    {
        if (stage == null) throw new ArgumentNullException(nameof(stage));

        if (_status == PipelineStatus.Running)
        {
            throw new InvalidOperationException("Cannot add stages while pipeline is running");
        }

        _stages.Add(stage);
        _logger.LogDebug("Added stage: {StageName} (Order: {Order})", stage.Name, stage.Order);
    }

    /// <inheritdoc />
    public bool RemoveStage(Guid stageId)
    {
        if (_status == PipelineStatus.Running)
        {
            throw new InvalidOperationException("Cannot remove stages while pipeline is running");
        }

        var stage = _stages.FirstOrDefault(s => s.Id == stageId);
        if (stage != null)
        {
            _stages.Remove(stage);
            _logger.LogDebug("Removed stage: {StageName} (ID: {StageId})", stage.Name, stageId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a stage by its identifier.
    /// </summary>
    /// <param name="stageId">The stage identifier</param>
    /// <returns>The stage if found, null otherwise</returns>
    public IPipelineStage? GetStage(Guid stageId)
    {
        return _stages.FirstOrDefault(s => s.Id == stageId);
    }

    /// <summary>
    /// Gets a stage by its name.
    /// </summary>
    /// <param name="stageName">The stage name</param>
    /// <returns>The stage if found, null otherwise</returns>
    public IPipelineStage? GetStage(string stageName)
    {
        return _stages.FirstOrDefault(s => s.Name.Equals(stageName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clears all stages from the pipeline.
    /// </summary>
    public void ClearStages()
    {
        if (_status == PipelineStatus.Running)
        {
            throw new InvalidOperationException("Cannot clear stages while pipeline is running");
        }

        var count = _stages.Count;
        _stages.Clear();
        _logger.LogDebug("Cleared {StageCount} stages from pipeline", count);
    }

    /// <summary>
    /// Returns a string representation of the pipeline.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        return $"Pipeline[{Name}, Stages={_stages.Count}, Status={_status}]";
    }
}
