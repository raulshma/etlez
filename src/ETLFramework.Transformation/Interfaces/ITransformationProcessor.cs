using ETLFramework.Core.Models;

namespace ETLFramework.Transformation.Interfaces;

/// <summary>
/// Interface for transformation processors that execute transformations.
/// </summary>
public interface ITransformationProcessor
{
    /// <summary>
    /// Gets the processor name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the processor version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets whether the processor supports parallel execution.
    /// </summary>
    bool SupportsParallelExecution { get; }

    /// <summary>
    /// Processes a single data record through transformations.
    /// </summary>
    /// <param name="record">The input record</param>
    /// <param name="transformations">The transformations to apply</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation results</returns>
    Task<IEnumerable<Core.Models.TransformationResult>> ProcessRecordAsync(
        DataRecord record,
        IEnumerable<ITransformation> transformations,
        ITransformationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes multiple data records through transformations.
    /// </summary>
    /// <param name="records">The input records</param>
    /// <param name="transformations">The transformations to apply</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation results</returns>
    Task<IEnumerable<Core.Models.TransformationResult>> ProcessRecordsAsync(
        IEnumerable<DataRecord> records,
        IEnumerable<ITransformation> transformations,
        ITransformationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes data records using transformation rules.
    /// </summary>
    /// <param name="records">The input records</param>
    /// <param name="ruleSet">The transformation rule set</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation results</returns>
    Task<IEnumerable<Core.Models.TransformationResult>> ProcessWithRulesAsync(
        IEnumerable<DataRecord> records,
        ITransformationRuleSet ruleSet,
        ITransformationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates transformations before processing.
    /// </summary>
    /// <param name="transformations">The transformations to validate</param>
    /// <param name="context">The transformation context</param>
    /// <returns>A validation result</returns>
    ValidationResult ValidateTransformations(IEnumerable<ITransformation> transformations, ITransformationContext context);

    /// <summary>
    /// Gets processor statistics.
    /// </summary>
    /// <returns>Processor statistics</returns>
    TransformationProcessorStatistics GetStatistics();

    /// <summary>
    /// Resets processor statistics.
    /// </summary>
    void ResetStatistics();
}

/// <summary>
/// Interface for transformation pipeline that chains multiple processors.
/// </summary>
public interface ITransformationPipeline
{
    /// <summary>
    /// Gets the pipeline name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the pipeline stages.
    /// </summary>
    IList<ITransformationStage> Stages { get; }

    /// <summary>
    /// Gets the pipeline configuration.
    /// </summary>
    IDictionary<string, object> Configuration { get; }

    /// <summary>
    /// Executes the transformation pipeline.
    /// </summary>
    /// <param name="records">The input records</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation results</returns>
    Task<IEnumerable<Core.Models.TransformationResult>> ExecuteAsync(
        IEnumerable<DataRecord> records,
        ITransformationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the pipeline configuration.
    /// </summary>
    /// <returns>A validation result</returns>
    ValidationResult Validate();

    /// <summary>
    /// Gets pipeline statistics.
    /// </summary>
    /// <returns>Pipeline statistics</returns>
    TransformationPipelineStatistics GetStatistics();
}

/// <summary>
/// Interface for transformation pipeline stages.
/// </summary>
public interface ITransformationStage
{
    /// <summary>
    /// Gets the stage name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the stage order.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets whether the stage is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the transformations in this stage.
    /// </summary>
    IList<ITransformation> Transformations { get; }

    /// <summary>
    /// Gets the stage execution strategy.
    /// </summary>
    StageExecutionStrategy ExecutionStrategy { get; }

    /// <summary>
    /// Gets whether to continue on error.
    /// </summary>
    bool ContinueOnError { get; }

    /// <summary>
    /// Executes the stage transformations.
    /// </summary>
    /// <param name="records">The input records</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation results</returns>
    Task<IEnumerable<Core.Models.TransformationResult>> ExecuteAsync(
        IEnumerable<DataRecord> records,
        ITransformationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the stage configuration.
    /// </summary>
    /// <returns>A validation result</returns>
    ValidationResult Validate();
}

/// <summary>
/// Represents stage execution strategies.
/// </summary>
public enum StageExecutionStrategy
{
    /// <summary>
    /// Execute transformations sequentially.
    /// </summary>
    Sequential,

    /// <summary>
    /// Execute transformations in parallel.
    /// </summary>
    Parallel,

    /// <summary>
    /// Execute transformations in batches.
    /// </summary>
    Batch
}

/// <summary>
/// Represents transformation processor statistics.
/// </summary>
public class TransformationProcessorStatistics
{
    /// <summary>
    /// Gets or sets the total number of records processed.
    /// </summary>
    public long TotalRecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of transformations executed.
    /// </summary>
    public long TotalTransformationsExecuted { get; set; }

    /// <summary>
    /// Gets or sets the total processing time.
    /// </summary>
    public TimeSpan TotalProcessingTime { get; set; }

    /// <summary>
    /// Gets or sets the average processing time per record.
    /// </summary>
    public TimeSpan AverageProcessingTimePerRecord { get; set; }

    /// <summary>
    /// Gets or sets the throughput in records per second.
    /// </summary>
    public double ThroughputRecordsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the number of successful transformations.
    /// </summary>
    public long SuccessfulTransformations { get; set; }

    /// <summary>
    /// Gets or sets the number of failed transformations.
    /// </summary>
    public long FailedTransformations { get; set; }

    /// <summary>
    /// Gets or sets the success rate percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets custom metrics.
    /// </summary>
    public Dictionary<string, object> CustomMetrics { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Calculates derived statistics.
    /// </summary>
    public void CalculateDerivedStatistics()
    {
        if (TotalRecordsProcessed > 0)
        {
            if (TotalProcessingTime.TotalSeconds > 0)
            {
                AverageProcessingTimePerRecord = TimeSpan.FromTicks(TotalProcessingTime.Ticks / TotalRecordsProcessed);
                ThroughputRecordsPerSecond = TotalRecordsProcessed / TotalProcessingTime.TotalSeconds;
            }

            if (TotalTransformationsExecuted > 0)
            {
                SuccessRate = (double)SuccessfulTransformations / TotalTransformationsExecuted * 100;
            }
        }
    }
}

/// <summary>
/// Represents transformation pipeline statistics.
/// </summary>
public class TransformationPipelineStatistics
{
    /// <summary>
    /// Gets or sets the total number of pipeline executions.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the total number of records processed.
    /// </summary>
    public long TotalRecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total execution time.
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the average execution time.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the stage statistics.
    /// </summary>
    public Dictionary<string, TransformationStageStatistics> StageStatistics { get; set; } = new Dictionary<string, TransformationStageStatistics>();

    /// <summary>
    /// Gets or sets the number of successful executions.
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions.
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// Gets or sets the success rate percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Calculates derived statistics.
    /// </summary>
    public void CalculateDerivedStatistics()
    {
        if (TotalExecutions > 0)
        {
            AverageExecutionTime = TimeSpan.FromTicks(TotalExecutionTime.Ticks / TotalExecutions);
            SuccessRate = (double)SuccessfulExecutions / TotalExecutions * 100;
        }
    }
}

/// <summary>
/// Represents transformation stage statistics.
/// </summary>
public class TransformationStageStatistics
{
    /// <summary>
    /// Gets or sets the stage name.
    /// </summary>
    public string StageName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total execution time.
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the number of records processed.
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of successful transformations.
    /// </summary>
    public long SuccessfulTransformations { get; set; }

    /// <summary>
    /// Gets or sets the number of failed transformations.
    /// </summary>
    public long FailedTransformations { get; set; }

    /// <summary>
    /// Gets or sets the success rate percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Calculates derived statistics.
    /// </summary>
    public void CalculateDerivedStatistics()
    {
        var totalTransformations = SuccessfulTransformations + FailedTransformations;
        if (totalTransformations > 0)
        {
            SuccessRate = (double)SuccessfulTransformations / totalTransformations * 100;
        }
    }
}
