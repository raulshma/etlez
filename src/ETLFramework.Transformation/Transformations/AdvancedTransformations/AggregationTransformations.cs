using ETLFramework.Core.Models;
using ETLFramework.Transformation.Interfaces;
using ETLFramework.Transformation.Helpers;
using ETLFramework.Core.Interfaces;

namespace ETLFramework.Transformation.Transformations.AdvancedTransformations;

/// <summary>
/// Base class for aggregation transformations.
/// </summary>
public abstract class BaseAggregationTransformation : ITransformation
{
    /// <summary>
    /// Initializes a new instance of the BaseAggregationTransformation class.
    /// </summary>
    /// <param name="id">The transformation ID</param>
    /// <param name="name">The transformation name</param>
    /// <param name="sourceFields">The source fields to aggregate</param>
    /// <param name="targetField">The target field for the result</param>
    protected BaseAggregationTransformation(string id, string name, string[] sourceFields, string targetField)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        SourceFields = sourceFields ?? throw new ArgumentNullException(nameof(sourceFields));
        TargetField = targetField ?? throw new ArgumentNullException(nameof(targetField));
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description => $"Aggregation transformation for fields {string.Join(", ", SourceFields)}";

    /// <inheritdoc />
    public TransformationType Type => TransformationType.Aggregate;

    /// <inheritdoc />
    public bool SupportsParallelExecution => false; // Aggregations typically need sequential processing

    /// <summary>
    /// Gets the source fields to aggregate.
    /// </summary>
    public string[] SourceFields { get; }

    /// <summary>
    /// Gets the target field for the result.
    /// </summary>
    public string TargetField { get; }

    /// <summary>
    /// Gets or sets whether to skip null values in aggregation.
    /// </summary>
    public bool SkipNullValues { get; set; } = true;

    /// <inheritdoc />
    public virtual ValidationResult Validate(ITransformationContext context)
    {
        var result = new ValidationResult { IsValid = true };

        if (SourceFields.Length == 0)
        {
            result.AddError("At least one source field must be specified");
        }

        if (string.IsNullOrWhiteSpace(TargetField))
        {
            result.AddError("Target field cannot be empty");
        }

        return result;
    }

    /// <inheritdoc />
    public virtual TransformationMetadata GetMetadata()
    {
        return new TransformationMetadata
        {
            Id = Id,
            Name = Name,
            Type = Type,
            Description = Description,
            RequiredInputFields = new List<string>(SourceFields),
            OutputFields = new List<string> { TargetField }
        };
    }

    /// <inheritdoc />
    public async Task<TransformationResult> TransformAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var values = new List<object?>();
            
            foreach (var field in SourceFields)
            {
                var value = record.GetField<object>(field);
                if (!SkipNullValues || value != null)
                {
                    values.Add(value);
                }
            }

            var aggregatedValue = await AggregateValuesAsync(values, record, context, cancellationToken);

            var outputRecord = record.Clone();
            outputRecord.SetField(TargetField, aggregatedValue);

            context.UpdateStatistics(1, 1, DateTimeOffset.UtcNow - startTime);
            return TransformationResultHelper.Success(outputRecord);
        }
        catch (Exception ex)
        {
            context.AddError($"Aggregation transformation failed: {ex.Message}", ex);
            return TransformationResultHelper.Failure($"Aggregation transformation failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Core.Models.TransformationResult>> TransformBatchAsync(IEnumerable<DataRecord> records, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var results = new List<Core.Models.TransformationResult>();

        foreach (var record in records)
        {
            var result = await TransformAsync(record, context, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Aggregates the values.
    /// </summary>
    /// <param name="values">The values to aggregate</param>
    /// <param name="record">The source record for context</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregated value</returns>
    protected abstract Task<object?> AggregateValuesAsync(IEnumerable<object?> values, DataRecord record, ITransformationContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Converts values to decimal for numeric operations.
    /// </summary>
    /// <param name="values">The values to convert</param>
    /// <returns>The converted decimal values</returns>
    protected static IEnumerable<decimal> ConvertToDecimals(IEnumerable<object?> values)
    {
        foreach (var value in values)
        {
            if (value != null && decimal.TryParse(value.ToString(), out var decimalValue))
            {
                yield return decimalValue;
            }
        }
    }
}

/// <summary>
/// Sum aggregation transformation.
/// </summary>
public class SumAggregationTransformation : BaseAggregationTransformation
{
    /// <summary>
    /// Initializes a new instance of the SumAggregationTransformation class.
    /// </summary>
    /// <param name="sourceFields">The source fields to sum</param>
    /// <param name="targetField">The target field for the result</param>
    public SumAggregationTransformation(string[] sourceFields, string targetField)
        : base($"sum_{string.Join("_", sourceFields)}", $"Sum {string.Join(", ", sourceFields)}", sourceFields, targetField)
    {
    }

    /// <inheritdoc />
    protected override Task<object?> AggregateValuesAsync(IEnumerable<object?> values, DataRecord record, ITransformationContext context, CancellationToken cancellationToken)
    {
        var numericValues = ConvertToDecimals(values);
        var sum = numericValues.Sum();
        return Task.FromResult<object?>(sum);
    }
}

/// <summary>
/// Average aggregation transformation.
/// </summary>
public class AverageAggregationTransformation : BaseAggregationTransformation
{
    /// <summary>
    /// Initializes a new instance of the AverageAggregationTransformation class.
    /// </summary>
    /// <param name="sourceFields">The source fields to average</param>
    /// <param name="targetField">The target field for the result</param>
    public AverageAggregationTransformation(string[] sourceFields, string targetField)
        : base($"avg_{string.Join("_", sourceFields)}", $"Average {string.Join(", ", sourceFields)}", sourceFields, targetField)
    {
    }

    /// <inheritdoc />
    protected override Task<object?> AggregateValuesAsync(IEnumerable<object?> values, DataRecord record, ITransformationContext context, CancellationToken cancellationToken)
    {
        var numericValues = ConvertToDecimals(values).ToList();
        if (numericValues.Count == 0) return Task.FromResult<object?>(null);
        
        var average = numericValues.Average();
        return Task.FromResult<object?>(average);
    }
}

/// <summary>
/// Count aggregation transformation.
/// </summary>
public class CountAggregationTransformation : BaseAggregationTransformation
{
    /// <summary>
    /// Initializes a new instance of the CountAggregationTransformation class.
    /// </summary>
    /// <param name="sourceFields">The source fields to count</param>
    /// <param name="targetField">The target field for the result</param>
    public CountAggregationTransformation(string[] sourceFields, string targetField)
        : base($"count_{string.Join("_", sourceFields)}", $"Count {string.Join(", ", sourceFields)}", sourceFields, targetField)
    {
    }

    /// <inheritdoc />
    protected override Task<object?> AggregateValuesAsync(IEnumerable<object?> values, DataRecord record, ITransformationContext context, CancellationToken cancellationToken)
    {
        var count = values.Count();
        return Task.FromResult<object?>(count);
    }
}

/// <summary>
/// Min/Max aggregation transformation.
/// </summary>
public class MinMaxAggregationTransformation : BaseAggregationTransformation
{
    private readonly bool _isMin;

    /// <summary>
    /// Initializes a new instance of the MinMaxAggregationTransformation class.
    /// </summary>
    /// <param name="sourceFields">The source fields to aggregate</param>
    /// <param name="targetField">The target field for the result</param>
    /// <param name="isMin">True for min, false for max</param>
    public MinMaxAggregationTransformation(string[] sourceFields, string targetField, bool isMin)
        : base($"{(isMin ? "min" : "max")}_{string.Join("_", sourceFields)}", $"{(isMin ? "Min" : "Max")} {string.Join(", ", sourceFields)}", sourceFields, targetField)
    {
        _isMin = isMin;
    }

    /// <inheritdoc />
    protected override Task<object?> AggregateValuesAsync(IEnumerable<object?> values, DataRecord record, ITransformationContext context, CancellationToken cancellationToken)
    {
        var numericValues = ConvertToDecimals(values).ToList();
        if (numericValues.Count == 0) return Task.FromResult<object?>(null);
        
        var result = _isMin ? numericValues.Min() : numericValues.Max();
        return Task.FromResult<object?>(result);
    }

    /// <summary>
    /// Creates a min aggregation transformation.
    /// </summary>
    /// <param name="sourceFields">The source fields</param>
    /// <param name="targetField">The target field</param>
    /// <returns>Min aggregation transformation</returns>
    public static MinMaxAggregationTransformation Min(string[] sourceFields, string targetField)
    {
        return new MinMaxAggregationTransformation(sourceFields, targetField, true);
    }

    /// <summary>
    /// Creates a max aggregation transformation.
    /// </summary>
    /// <param name="sourceFields">The source fields</param>
    /// <param name="targetField">The target field</param>
    /// <returns>Max aggregation transformation</returns>
    public static MinMaxAggregationTransformation Max(string[] sourceFields, string targetField)
    {
        return new MinMaxAggregationTransformation(sourceFields, targetField, false);
    }
}

/// <summary>
/// Concatenation aggregation transformation.
/// </summary>
public class ConcatenationAggregationTransformation : BaseAggregationTransformation
{
    private readonly string _separator;

    /// <summary>
    /// Initializes a new instance of the ConcatenationAggregationTransformation class.
    /// </summary>
    /// <param name="sourceFields">The source fields to concatenate</param>
    /// <param name="targetField">The target field for the result</param>
    /// <param name="separator">The separator to use</param>
    public ConcatenationAggregationTransformation(string[] sourceFields, string targetField, string separator = " ")
        : base($"concat_{string.Join("_", sourceFields)}", $"Concatenate {string.Join(", ", sourceFields)}", sourceFields, targetField)
    {
        _separator = separator;
    }

    /// <inheritdoc />
    protected override Task<object?> AggregateValuesAsync(IEnumerable<object?> values, DataRecord record, ITransformationContext context, CancellationToken cancellationToken)
    {
        var stringValues = values.Where(v => v != null).Select(v => v!.ToString());
        var result = string.Join(_separator, stringValues);
        return Task.FromResult<object?>(result);
    }
}

/// <summary>
/// Custom aggregation transformation with user-defined logic.
/// </summary>
public class CustomAggregationTransformation : BaseAggregationTransformation
{
    private readonly Func<IEnumerable<object?>, DataRecord, CancellationToken, Task<object?>> _aggregationFunction;

    /// <summary>
    /// Initializes a new instance of the CustomAggregationTransformation class.
    /// </summary>
    /// <param name="sourceFields">The source fields to aggregate</param>
    /// <param name="targetField">The target field for the result</param>
    /// <param name="aggregationFunction">The custom aggregation function</param>
    /// <param name="name">The transformation name</param>
    public CustomAggregationTransformation(string[] sourceFields, string targetField, Func<IEnumerable<object?>, DataRecord, CancellationToken, Task<object?>> aggregationFunction, string? name = null)
        : base($"custom_{string.Join("_", sourceFields)}", name ?? $"Custom Aggregation {string.Join(", ", sourceFields)}", sourceFields, targetField)
    {
        _aggregationFunction = aggregationFunction ?? throw new ArgumentNullException(nameof(aggregationFunction));
    }

    /// <inheritdoc />
    protected override async Task<object?> AggregateValuesAsync(IEnumerable<object?> values, DataRecord record, ITransformationContext context, CancellationToken cancellationToken)
    {
        return await _aggregationFunction(values, record, cancellationToken);
    }
}
