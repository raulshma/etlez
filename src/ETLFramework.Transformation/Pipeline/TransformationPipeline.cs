using System.Diagnostics;
using ETLFramework.Core.Models;
using ETLFramework.Transformation.Interfaces;
using ETLFramework.Transformation.Helpers;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Transformation.Pipeline;

/// <summary>
/// Implementation of transformation pipeline.
/// </summary>
public class TransformationPipeline : ITransformationPipeline
{
    private readonly ILogger<TransformationPipeline> _logger;
    private readonly ITransformationProcessor _processor;
    private readonly TransformationPipelineStatistics _statistics;
    private readonly object _statsLock = new object();

    /// <summary>
    /// Initializes a new instance of the TransformationPipeline class.
    /// </summary>
    /// <param name="name">The pipeline name</param>
    /// <param name="processor">The transformation processor</param>
    /// <param name="logger">The logger instance</param>
    public TransformationPipeline(string name, ITransformationProcessor processor, ILogger<TransformationPipeline> logger)
    {
        Name = name;
        _processor = processor;
        _logger = logger;
        Stages = new List<ITransformationStage>();
        Configuration = new Dictionary<string, object>();
        _statistics = new TransformationPipelineStatistics();
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IList<ITransformationStage> Stages { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Configuration { get; }

    /// <inheritdoc />
    public async Task<IEnumerable<TransformationResult>> ExecuteAsync(
        IEnumerable<DataRecord> records, 
        Interfaces.ITransformationContext context, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var recordList = records.ToList();
        var results = new List<TransformationResult>();
        var currentRecords = recordList;

        try
        {
            _logger.LogInformation("Executing pipeline {PipelineName} with {RecordCount} records and {StageCount} stages", 
                Name, recordList.Count, Stages.Count);

            context.SetMetadata("PipelineName", Name);
            context.SetMetadata("TotalStages", Stages.Count);
            context.SetTotalRecords(recordList.Count);

            var orderedStages = Stages.Where(s => s.IsEnabled).OrderBy(s => s.Order).ToList();

            for (int stageIndex = 0; stageIndex < orderedStages.Count; stageIndex++)
            {
                var stage = orderedStages[stageIndex];
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Executing stage {StageIndex}/{TotalStages}: {StageName}", 
                    stageIndex + 1, orderedStages.Count, stage.Name);

                context.SetMetadata("CurrentStage", stage.Name);
                context.SetMetadata("CurrentStageIndex", stageIndex + 1);

                try
                {
                    var stageResults = await stage.ExecuteAsync(currentRecords, context, cancellationToken);
                    results.AddRange(stageResults);

                    // Update current records for next stage
                    currentRecords = stageResults.GetOutputRecords().ToList();

                    _logger.LogDebug("Stage {StageName} completed. {OutputRecords} records for next stage", 
                        stage.Name, currentRecords.Count);

                    if (currentRecords.Count == 0)
                    {
                        _logger.LogWarning("No records remaining after stage {StageName}. Pipeline execution stopped", stage.Name);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing stage {StageName}", stage.Name);

                    if (!stage.ContinueOnError)
                    {
                        _logger.LogError("Stage {StageName} failed and ContinueOnError is false. Stopping pipeline execution", stage.Name);
                        
                        var errorResult = TransformationResultHelper.Failure(
                            $"Stage '{stage.Name}' failed: {ex.Message}",
                            ex);

                        results.Add(errorResult);
                        break;
                    }

                    context.AddError($"Stage '{stage.Name}' failed but pipeline continues", ex);
                }
            }

            lock (_statsLock)
            {
                _statistics.TotalExecutions++;
                _statistics.TotalRecordsProcessed += recordList.Count;
                _statistics.TotalExecutionTime = _statistics.TotalExecutionTime.Add(stopwatch.Elapsed);
                
                if (results.Any(r => !r.IsSuccessful))
                    _statistics.FailedExecutions++;
                else
                    _statistics.SuccessfulExecutions++;

                _statistics.CalculateDerivedStatistics();
            }

            _logger.LogInformation("Pipeline {PipelineName} completed in {ElapsedMs}ms. {ResultCount} results generated", 
                Name, stopwatch.ElapsedMilliseconds, results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline {PipelineName} execution failed", Name);

            lock (_statsLock)
            {
                _statistics.TotalExecutions++;
                _statistics.FailedExecutions++;
                _statistics.CalculateDerivedStatistics();
            }

            var errorResult = TransformationResultHelper.Failure(
                $"Pipeline execution failed: {ex.Message}",
                ex);

            return new[] { errorResult };
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrEmpty(Name))
            result.AddError("Pipeline name is required", nameof(Name));

        if (Stages.Count == 0)
            result.AddError("Pipeline must have at least one stage", nameof(Stages));

        // Validate stage order
        var enabledStages = Stages.Where(s => s.IsEnabled).ToList();
        var orders = enabledStages.Select(s => s.Order).ToList();
        
        if (orders.Count != orders.Distinct().Count())
            result.AddError("Stage orders must be unique", nameof(Stages));

        // Validate each stage
        foreach (var stage in Stages)
        {
            var stageResult = stage.Validate();
            if (!stageResult.IsValid)
            {
                foreach (var error in stageResult.Errors)
                {
                    result.AddError($"Stage '{stage.Name}': {error}", stage.Name);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public TransformationPipelineStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            var stats = new TransformationPipelineStatistics
            {
                TotalExecutions = _statistics.TotalExecutions,
                TotalRecordsProcessed = _statistics.TotalRecordsProcessed,
                TotalExecutionTime = _statistics.TotalExecutionTime,
                SuccessfulExecutions = _statistics.SuccessfulExecutions,
                FailedExecutions = _statistics.FailedExecutions
            };

            foreach (var kvp in _statistics.StageStatistics)
            {
                stats.StageStatistics[kvp.Key] = kvp.Value;
            }

            stats.CalculateDerivedStatistics();
            return stats;
        }
    }

    /// <summary>
    /// Adds a stage to the pipeline.
    /// </summary>
    /// <param name="stage">The stage to add</param>
    public void AddStage(ITransformationStage stage)
    {
        Stages.Add(stage);
        _logger.LogDebug("Added stage {StageName} to pipeline {PipelineName}", stage.Name, Name);
    }

    /// <summary>
    /// Removes a stage from the pipeline.
    /// </summary>
    /// <param name="stageName">The name of the stage to remove</param>
    /// <returns>True if the stage was removed</returns>
    public bool RemoveStage(string stageName)
    {
        var stage = Stages.FirstOrDefault(s => s.Name.Equals(stageName, StringComparison.OrdinalIgnoreCase));
        if (stage != null)
        {
            Stages.Remove(stage);
            _logger.LogDebug("Removed stage {StageName} from pipeline {PipelineName}", stageName, Name);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a stage by name.
    /// </summary>
    /// <param name="stageName">The stage name</param>
    /// <returns>The stage or null if not found</returns>
    public ITransformationStage? GetStage(string stageName)
    {
        return Stages.FirstOrDefault(s => s.Name.Equals(stageName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="value">The configuration value</param>
    public void SetConfiguration(string key, object value)
    {
        Configuration[key] = value;
    }

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value</param>
    /// <returns>The configuration value</returns>
    public T GetConfiguration<T>(string key, T defaultValue = default!)
    {
        if (Configuration.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
}

/// <summary>
/// Implementation of transformation stage.
/// </summary>
public class TransformationStage : ITransformationStage
{
    private readonly ILogger<TransformationStage> _logger;
    private readonly ITransformationProcessor _processor;

    /// <summary>
    /// Initializes a new instance of the TransformationStage class.
    /// </summary>
    /// <param name="name">The stage name</param>
    /// <param name="order">The stage order</param>
    /// <param name="processor">The transformation processor</param>
    /// <param name="logger">The logger instance</param>
    public TransformationStage(string name, int order, ITransformationProcessor processor, ILogger<TransformationStage> logger)
    {
        Name = name;
        Order = order;
        _processor = processor;
        _logger = logger;
        Transformations = new List<ITransformation>();
        IsEnabled = true;
        ExecutionStrategy = StageExecutionStrategy.Sequential;
        ContinueOnError = false;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public int Order { get; }

    /// <inheritdoc />
    public bool IsEnabled { get; set; }

    /// <inheritdoc />
    public IList<ITransformation> Transformations { get; }

    /// <inheritdoc />
    public StageExecutionStrategy ExecutionStrategy { get; set; }

    /// <inheritdoc />
    public bool ContinueOnError { get; set; }

    /// <inheritdoc />
    public async Task<IEnumerable<TransformationResult>> ExecuteAsync(
        IEnumerable<DataRecord> records, 
        Interfaces.ITransformationContext context, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var recordList = records.ToList();

        try
        {
            _logger.LogDebug("Executing stage {StageName} with {RecordCount} records and {TransformationCount} transformations", 
                Name, recordList.Count, Transformations.Count);

            if (Transformations.Count == 0)
            {
                _logger.LogWarning("Stage {StageName} has no transformations. Records will pass through unchanged", Name);
                
                return recordList.Select(record =>
                    TransformationResultHelper.Success(record));
            }

            var results = await _processor.ProcessRecordsAsync(recordList, Transformations, context, cancellationToken);

            _logger.LogDebug("Stage {StageName} completed in {ElapsedMs}ms", Name, stopwatch.ElapsedMilliseconds);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stage {StageName}", Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrEmpty(Name))
            result.AddError("Stage name is required", nameof(Name));

        if (Order < 0)
            result.AddError("Stage order must be non-negative", nameof(Order));

        return result;
    }

    /// <summary>
    /// Adds a transformation to the stage.
    /// </summary>
    /// <param name="transformation">The transformation to add</param>
    public void AddTransformation(ITransformation transformation)
    {
        Transformations.Add(transformation);
        _logger.LogDebug("Added transformation {TransformationName} to stage {StageName}", transformation.Name, Name);
    }

    /// <summary>
    /// Removes a transformation from the stage.
    /// </summary>
    /// <param name="transformationId">The transformation ID to remove</param>
    /// <returns>True if the transformation was removed</returns>
    public bool RemoveTransformation(string transformationId)
    {
        var transformation = Transformations.FirstOrDefault(t => t.Id == transformationId);
        if (transformation != null)
        {
            Transformations.Remove(transformation);
            _logger.LogDebug("Removed transformation {TransformationId} from stage {StageName}", transformationId, Name);
            return true;
        }
        return false;
    }
}
