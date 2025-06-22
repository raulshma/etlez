using System.Collections.Concurrent;
using System.Diagnostics;
using ETLFramework.Core.Models;
using ETLFramework.Transformation.Interfaces;
using ETLFramework.Transformation.Helpers;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Transformation.Processors;

/// <summary>
/// Default implementation of transformation processor.
/// </summary>
public class TransformationProcessor : ITransformationProcessor
{
    private readonly ILogger<TransformationProcessor> _logger;
    private readonly TransformationProcessorStatistics _statistics;
    private readonly object _statsLock = new object();

    /// <summary>
    /// Initializes a new instance of the TransformationProcessor class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public TransformationProcessor(ILogger<TransformationProcessor> logger)
    {
        _logger = logger;
        _statistics = new TransformationProcessorStatistics();
    }

    /// <inheritdoc />
    public string Name => "Default Transformation Processor";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public bool SupportsParallelExecution => true;

    /// <inheritdoc />
    public async Task<IEnumerable<TransformationResult>> ProcessRecordAsync(
        DataRecord record, 
        IEnumerable<ITransformation> transformations, 
        Interfaces.ITransformationContext context, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<TransformationResult>();
        var currentRecords = new List<DataRecord> { record };

        try
        {
            _logger.LogDebug("Processing record {RecordId} with {TransformationCount} transformations", 
                record.Id, transformations.Count());

            foreach (var transformation in transformations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var transformationResults = new List<TransformationResult>();

                foreach (var currentRecord in currentRecords)
                {
                    try
                    {
                        var result = await transformation.TransformAsync(currentRecord, context, cancellationToken);
                        transformationResults.Add(result);

                        lock (_statsLock)
                        {
                            _statistics.TotalTransformationsExecuted++;
                            if (result.IsSuccessful)
                                _statistics.SuccessfulTransformations++;
                            else
                                _statistics.FailedTransformations++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Transformation {TransformationId} failed for record {RecordId}", 
                            transformation.Id, currentRecord.Id);

                        var errorResult = TransformationResultHelper.Failure(
                            $"Transformation failed: {ex.Message}",
                            ex);

                        transformationResults.Add(errorResult);
                        context.AddError($"Transformation '{transformation.Name}' failed", ex, fieldName: transformation.Id);

                        lock (_statsLock)
                        {
                            _statistics.TotalTransformationsExecuted++;
                            _statistics.FailedTransformations++;
                        }
                    }
                }

                results.AddRange(transformationResults);

                // Update current records for next transformation
                currentRecords = transformationResults.GetOutputRecords().ToList();

                if (currentRecords.Count == 0)
                {
                    _logger.LogWarning("No records remaining after transformation {TransformationId}", transformation.Id);
                    break;
                }
            }

            lock (_statsLock)
            {
                _statistics.TotalRecordsProcessed++;
                _statistics.TotalProcessingTime = _statistics.TotalProcessingTime.Add(stopwatch.Elapsed);
                _statistics.CalculateDerivedStatistics();
            }

            _logger.LogDebug("Completed processing record {RecordId} in {ElapsedMs}ms", 
                record.Id, stopwatch.ElapsedMilliseconds);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing record {RecordId}", record.Id);
            
            var errorResult = TransformationResultHelper.Failure(
                $"Record processing failed: {ex.Message}",
                ex);

            return new[] { errorResult };
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TransformationResult>> ProcessRecordsAsync(
        IEnumerable<DataRecord> records, 
        IEnumerable<ITransformation> transformations, 
        Interfaces.ITransformationContext context, 
        CancellationToken cancellationToken = default)
    {
        var recordList = records.ToList();
        var transformationList = transformations.ToList();

        _logger.LogInformation("Processing {RecordCount} records with {TransformationCount} transformations", 
            recordList.Count, transformationList.Count);

        if (SupportsParallelExecution && transformationList.All(t => t.SupportsParallelExecution))
        {
            return await ProcessRecordsParallelAsync(recordList, transformationList, context, cancellationToken);
        }
        else
        {
            return await ProcessRecordsSequentialAsync(recordList, transformationList, context, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TransformationResult>> ProcessWithRulesAsync(
        IEnumerable<DataRecord> records, 
        ITransformationRuleSet ruleSet, 
        Interfaces.ITransformationContext context, 
        CancellationToken cancellationToken = default)
    {
        var recordList = records.ToList();
        var results = new List<TransformationResult>();

        _logger.LogInformation("Processing {RecordCount} records with rule set {RuleSetName}", 
            recordList.Count, ruleSet.Name);

        foreach (var record in recordList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.AdvanceRecord(record);

            try
            {
                var ruleResults = await ruleSet.ApplyAsync(record, context, cancellationToken);
                results.AddRange(ruleResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying rules to record {RecordId}", record.Id);
                
                var errorResult = TransformationResultHelper.Failure(
                    $"Rule application failed: {ex.Message}",
                    ex);

                results.Add(errorResult);
                context.AddError($"Rule set '{ruleSet.Name}' failed", ex);
            }
        }

        return results;
    }

    /// <inheritdoc />
    public ValidationResult ValidateTransformations(IEnumerable<ITransformation> transformations, Interfaces.ITransformationContext context)
    {
        var result = new ValidationResult { IsValid = true };
        var transformationList = transformations.ToList();

        _logger.LogDebug("Validating {TransformationCount} transformations", transformationList.Count);

        foreach (var transformation in transformationList)
        {
            try
            {
                var transformationResult = transformation.Validate(context);
                if (!transformationResult.IsValid)
                {
                    foreach (var error in transformationResult.Errors)
                    {
                        result.AddError($"Transformation '{transformation.Name}': {error}", transformation.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Validation failed for transformation '{transformation.Name}': {ex.Message}", transformation.Id);
                _logger.LogError(ex, "Validation error for transformation {TransformationId}", transformation.Id);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public TransformationProcessorStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            var stats = new TransformationProcessorStatistics
            {
                TotalRecordsProcessed = _statistics.TotalRecordsProcessed,
                TotalTransformationsExecuted = _statistics.TotalTransformationsExecuted,
                TotalProcessingTime = _statistics.TotalProcessingTime,
                SuccessfulTransformations = _statistics.SuccessfulTransformations,
                FailedTransformations = _statistics.FailedTransformations,
                MemoryUsageBytes = _statistics.MemoryUsageBytes
            };

            foreach (var kvp in _statistics.CustomMetrics)
            {
                stats.CustomMetrics[kvp.Key] = kvp.Value;
            }

            stats.CalculateDerivedStatistics();
            return stats;
        }
    }

    /// <inheritdoc />
    public void ResetStatistics()
    {
        lock (_statsLock)
        {
            _statistics.TotalRecordsProcessed = 0;
            _statistics.TotalTransformationsExecuted = 0;
            _statistics.TotalProcessingTime = TimeSpan.Zero;
            _statistics.SuccessfulTransformations = 0;
            _statistics.FailedTransformations = 0;
            _statistics.MemoryUsageBytes = 0;
            _statistics.CustomMetrics.Clear();
        }

        _logger.LogInformation("Transformation processor statistics reset");
    }

    /// <summary>
    /// Processes records sequentially.
    /// </summary>
    private async Task<IEnumerable<TransformationResult>> ProcessRecordsSequentialAsync(
        IList<DataRecord> records, 
        IList<ITransformation> transformations, 
        Interfaces.ITransformationContext context, 
        CancellationToken cancellationToken)
    {
        var results = new List<TransformationResult>();

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.AdvanceRecord(record);

            var recordResults = await ProcessRecordAsync(record, transformations, context, cancellationToken);
            results.AddRange(recordResults);
        }

        return results;
    }

    /// <summary>
    /// Processes records in parallel.
    /// </summary>
    private async Task<IEnumerable<TransformationResult>> ProcessRecordsParallelAsync(
        IList<DataRecord> records, 
        IList<ITransformation> transformations, 
        Interfaces.ITransformationContext context, 
        CancellationToken cancellationToken)
    {
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        var results = new ConcurrentBag<TransformationResult>();

        await Parallel.ForEachAsync(records, parallelOptions, async (record, ct) =>
        {
            var childContext = context.CreateChildContext($"Record-{record.Id}");
            childContext.AdvanceRecord(record);

            var recordResults = await ProcessRecordAsync(record, transformations, childContext, ct);
            
            foreach (var result in recordResults)
            {
                results.Add(result);
            }
        });

        return results.ToList();
    }
}
