using ETLFramework.Core.Models;
using ETLFramework.Core.Interfaces;

namespace ETLFramework.Transformation.Interfaces;

/// <summary>
/// Base interface for all data transformations.
/// </summary>
public interface ITransformation
{
    /// <summary>
    /// Gets the unique identifier for this transformation.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of this transformation.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this transformation.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the transformation type.
    /// </summary>
    TransformationType Type { get; }

    /// <summary>
    /// Gets whether this transformation can be executed in parallel.
    /// </summary>
    bool SupportsParallelExecution { get; }

    /// <summary>
    /// Validates the transformation configuration.
    /// </summary>
    /// <param name="context">The transformation context</param>
    /// <returns>A validation result</returns>
    ValidationResult Validate(ITransformationContext context);

    /// <summary>
    /// Applies the transformation to a data record.
    /// </summary>
    /// <param name="record">The input record</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation result</returns>
    Task<TransformationResult> TransformAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the transformation to multiple data records.
    /// </summary>
    /// <param name="records">The input records</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation results</returns>
    Task<IEnumerable<TransformationResult>> TransformBatchAsync(IEnumerable<DataRecord> records, ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata about this transformation.
    /// </summary>
    /// <returns>Transformation metadata</returns>
    TransformationMetadata GetMetadata();
}

/// <summary>
/// Interface for field-level transformations.
/// </summary>
public interface IFieldTransformation : ITransformation
{
    /// <summary>
    /// Gets the source field name.
    /// </summary>
    string SourceField { get; }

    /// <summary>
    /// Gets the target field name.
    /// </summary>
    string TargetField { get; }

    /// <summary>
    /// Transforms a field value.
    /// </summary>
    /// <param name="value">The input value</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformed value</returns>
    Task<object?> TransformFieldAsync(object? value, ITransformationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for record-level transformations.
/// </summary>
public interface IRecordTransformation : ITransformation
{
    /// <summary>
    /// Gets whether this transformation can produce multiple output records from a single input record.
    /// </summary>
    bool CanProduceMultipleRecords { get; }

    /// <summary>
    /// Gets whether this transformation can filter out records.
    /// </summary>
    bool CanFilterRecords { get; }
}

/// <summary>
/// Interface for conditional transformations.
/// </summary>
public interface IConditionalTransformation : ITransformation
{
    /// <summary>
    /// Evaluates whether the transformation should be applied to the given record.
    /// </summary>
    /// <param name="record">The input record</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the transformation should be applied</returns>
    Task<bool> ShouldApplyAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for aggregate transformations that work across multiple records.
/// </summary>
public interface IAggregateTransformation : ITransformation
{
    /// <summary>
    /// Gets the aggregation window size.
    /// </summary>
    int WindowSize { get; }

    /// <summary>
    /// Initializes the aggregation.
    /// </summary>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    Task InitializeAsync(ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes the aggregation and produces final results.
    /// </summary>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The final aggregation results</returns>
    Task<IEnumerable<TransformationResult>> FinalizeAsync(ITransformationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the type of transformation.
/// </summary>
public enum TransformationType
{
    /// <summary>
    /// Field-level transformation.
    /// </summary>
    Field,

    /// <summary>
    /// Record-level transformation.
    /// </summary>
    Record,

    /// <summary>
    /// Conditional transformation.
    /// </summary>
    Conditional,

    /// <summary>
    /// Aggregate transformation.
    /// </summary>
    Aggregate,

    /// <summary>
    /// Custom transformation.
    /// </summary>
    Custom
}

/// <summary>
/// Represents metadata about a transformation.
/// </summary>
public class TransformationMetadata
{
    /// <summary>
    /// Gets or sets the transformation ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation type.
    /// </summary>
    public TransformationType Type { get; set; }

    /// <summary>
    /// Gets or sets the transformation version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the transformation author.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation tags.
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the input schema requirements.
    /// </summary>
    public List<string> RequiredInputFields { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the output schema information.
    /// </summary>
    public List<string> OutputFields { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the configuration parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets performance characteristics.
    /// </summary>
    public TransformationPerformance? Performance { get; set; }
}

/// <summary>
/// Represents performance characteristics of a transformation.
/// </summary>
public class TransformationPerformance
{
    /// <summary>
    /// Gets or sets the expected throughput in records per second.
    /// </summary>
    public int? ExpectedThroughput { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in MB.
    /// </summary>
    public int? MemoryUsageMB { get; set; }

    /// <summary>
    /// Gets or sets whether the transformation is CPU intensive.
    /// </summary>
    public bool IsCpuIntensive { get; set; }

    /// <summary>
    /// Gets or sets whether the transformation is memory intensive.
    /// </summary>
    public bool IsMemoryIntensive { get; set; }

    /// <summary>
    /// Gets or sets the complexity level.
    /// </summary>
    public ComplexityLevel Complexity { get; set; }
}

/// <summary>
/// Represents the complexity level of a transformation.
/// </summary>
public enum ComplexityLevel
{
    /// <summary>
    /// Low complexity - simple operations.
    /// </summary>
    Low,

    /// <summary>
    /// Medium complexity - moderate operations.
    /// </summary>
    Medium,

    /// <summary>
    /// High complexity - complex operations.
    /// </summary>
    High,

    /// <summary>
    /// Very high complexity - very complex operations.
    /// </summary>
    VeryHigh
}
