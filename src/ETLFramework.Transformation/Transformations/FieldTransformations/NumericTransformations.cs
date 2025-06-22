using System.Globalization;
using ETLFramework.Core.Models;
using ETLFramework.Transformation.Interfaces;
using ETLFramework.Transformation.Helpers;

namespace ETLFramework.Transformation.Transformations.FieldTransformations;

/// <summary>
/// Base class for numeric field transformations.
/// </summary>
public abstract class BaseNumericTransformation : IFieldTransformation
{
    /// <summary>
    /// Initializes a new instance of the BaseNumericTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="targetField">The target field name</param>
    protected BaseNumericTransformation(string sourceField, string targetField)
    {
        Id = Guid.NewGuid().ToString();
        SourceField = sourceField;
        TargetField = targetField;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public TransformationType Type => TransformationType.Field;

    /// <inheritdoc />
    public bool SupportsParallelExecution => true;

    /// <inheritdoc />
    public string SourceField { get; }

    /// <inheritdoc />
    public string TargetField { get; }

    /// <inheritdoc />
    public virtual ValidationResult Validate(Interfaces.ITransformationContext context)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrEmpty(SourceField))
            result.AddError("Source field name is required", nameof(SourceField));

        if (string.IsNullOrEmpty(TargetField))
            result.AddError("Target field name is required", nameof(TargetField));

        return result;
    }

    /// <inheritdoc />
    public async Task<Core.Models.TransformationResult> TransformAsync(DataRecord record, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            var inputValue = record.GetField<object>(SourceField);
            var transformedValue = await TransformFieldAsync(inputValue, context, cancellationToken);
            
            var outputRecord = record.Clone();
            outputRecord.SetField(TargetField, transformedValue);

            var result = TransformationResultHelper.Success(outputRecord);
            context.UpdateStatistics(1, 1, DateTimeOffset.UtcNow - startTime);

            return result;
        }
        catch (Exception ex)
        {
            var result = TransformationResultHelper.Failure($"Numeric transformation failed: {ex.Message}", ex);
            context.AddError($"Numeric transformation failed for field '{SourceField}': {ex.Message}", ex, fieldName: SourceField);
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Core.Models.TransformationResult>> TransformBatchAsync(IEnumerable<DataRecord> records, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var results = new List<TransformationResult>();
        
        foreach (var record in records)
        {
            var result = await TransformAsync(record, context, cancellationToken);
            results.Add(result);
        }
        
        return results;
    }

    /// <inheritdoc />
    public abstract Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public TransformationMetadata GetMetadata()
    {
        return new TransformationMetadata
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Type = Type,
            Category = "Numeric",
            Tags = { "numeric", "field", "math" },
            RequiredInputFields = { SourceField },
            OutputFields = { TargetField },
            Performance = new TransformationPerformance
            {
                ExpectedThroughput = 15000,
                MemoryUsageMB = 1,
                IsCpuIntensive = false,
                IsMemoryIntensive = false,
                Complexity = ComplexityLevel.Low
            }
        };
    }

    /// <summary>
    /// Converts a value to a decimal number.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>The decimal value or null if conversion fails</returns>
    public static decimal? ToDecimal(object? value)
    {
        if (value == null)
            return null;

        if (value is decimal decimalValue)
            return decimalValue;

        if (decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Converts a value to a double number.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>The double value or null if conversion fails</returns>
    protected static double? ToDouble(object? value)
    {
        if (value == null)
            return null;

        if (value is double doubleValue)
            return doubleValue;

        if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }
}

/// <summary>
/// Rounds numeric values to a specified number of decimal places.
/// </summary>
public class RoundTransformation : BaseNumericTransformation
{
    private readonly int _decimals;

    /// <summary>
    /// Initializes a new instance of the RoundTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="decimals">The number of decimal places</param>
    /// <param name="targetField">The target field name</param>
    public RoundTransformation(string sourceField, int decimals = 2, string targetField = null) 
        : base(sourceField, targetField ?? sourceField)
    {
        _decimals = decimals;
    }

    /// <inheritdoc />
    public override string Name => "Round";

    /// <inheritdoc />
    public override string Description => $"Rounds numeric values to {_decimals} decimal places";

    /// <inheritdoc />
    public override Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var decimalValue = ToDecimal(value);
        if (decimalValue == null)
            return Task.FromResult<object?>(value);

        var rounded = Math.Round(decimalValue.Value, _decimals);
        return Task.FromResult<object?>(rounded);
    }
}

/// <summary>
/// Adds a constant value to numeric fields.
/// </summary>
public class AddTransformation : BaseNumericTransformation
{
    private readonly decimal _addend;

    /// <summary>
    /// Initializes a new instance of the AddTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="addend">The value to add</param>
    /// <param name="targetField">The target field name</param>
    public AddTransformation(string sourceField, decimal addend, string targetField = null) 
        : base(sourceField, targetField ?? sourceField)
    {
        _addend = addend;
    }

    /// <inheritdoc />
    public override string Name => "Add";

    /// <inheritdoc />
    public override string Description => $"Adds {_addend} to numeric values";

    /// <inheritdoc />
    public override Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var decimalValue = ToDecimal(value);
        if (decimalValue == null)
            return Task.FromResult<object?>(value);

        var result = decimalValue.Value + _addend;
        return Task.FromResult<object?>(result);
    }
}

/// <summary>
/// Multiplies numeric values by a constant factor.
/// </summary>
public class MultiplyTransformation : BaseNumericTransformation
{
    private readonly decimal _multiplier;

    /// <summary>
    /// Initializes a new instance of the MultiplyTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="multiplier">The multiplication factor</param>
    /// <param name="targetField">The target field name</param>
    public MultiplyTransformation(string sourceField, decimal multiplier, string targetField = null) 
        : base(sourceField, targetField ?? sourceField)
    {
        _multiplier = multiplier;
    }

    /// <inheritdoc />
    public override string Name => "Multiply";

    /// <inheritdoc />
    public override string Description => $"Multiplies numeric values by {_multiplier}";

    /// <inheritdoc />
    public override Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var decimalValue = ToDecimal(value);
        if (decimalValue == null)
            return Task.FromResult<object?>(value);

        var result = decimalValue.Value * _multiplier;
        return Task.FromResult<object?>(result);
    }
}

/// <summary>
/// Formats numeric values as strings with specified format.
/// </summary>
public class FormatNumberTransformation : BaseNumericTransformation
{
    private readonly string _format;

    /// <summary>
    /// Initializes a new instance of the FormatNumberTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="format">The format string (e.g., "C", "N2", "P")</param>
    /// <param name="targetField">The target field name</param>
    public FormatNumberTransformation(string sourceField, string format, string targetField = null) 
        : base(sourceField, targetField ?? sourceField)
    {
        _format = format;
    }

    /// <inheritdoc />
    public override string Name => "Format Number";

    /// <inheritdoc />
    public override string Description => $"Formats numeric values using format '{_format}'";

    /// <inheritdoc />
    public override Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var decimalValue = ToDecimal(value);
        if (decimalValue == null)
            return Task.FromResult<object?>(value);

        try
        {
            var formatted = decimalValue.Value.ToString(_format, CultureInfo.InvariantCulture);
            return Task.FromResult<object?>(formatted);
        }
        catch (FormatException)
        {
            // If format is invalid, return original value
            return Task.FromResult<object?>(value);
        }
    }

    /// <inheritdoc />
    public override ValidationResult Validate(Interfaces.ITransformationContext context)
    {
        var result = base.Validate(context);

        if (string.IsNullOrEmpty(_format))
            result.AddError("Format string is required", nameof(_format));

        return result;
    }
}

/// <summary>
/// Calculates the absolute value of numeric fields.
/// </summary>
public class AbsoluteValueTransformation : BaseNumericTransformation
{
    /// <summary>
    /// Initializes a new instance of the AbsoluteValueTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="targetField">The target field name</param>
    public AbsoluteValueTransformation(string sourceField, string targetField = null) 
        : base(sourceField, targetField ?? sourceField)
    {
    }

    /// <inheritdoc />
    public override string Name => "Absolute Value";

    /// <inheritdoc />
    public override string Description => "Calculates the absolute value of numeric fields";

    /// <inheritdoc />
    public override Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var decimalValue = ToDecimal(value);
        if (decimalValue == null)
            return Task.FromResult<object?>(value);

        var result = Math.Abs(decimalValue.Value);
        return Task.FromResult<object?>(result);
    }
}

/// <summary>
/// Calculates mathematical expressions involving two fields.
/// </summary>
public class CalculateTransformation : IFieldTransformation
{
    private readonly string _leftField;
    private readonly string _rightField;
    private readonly MathOperation _operation;

    /// <summary>
    /// Initializes a new instance of the CalculateTransformation class.
    /// </summary>
    /// <param name="leftField">The left operand field</param>
    /// <param name="rightField">The right operand field</param>
    /// <param name="operation">The mathematical operation</param>
    /// <param name="targetField">The target field name</param>
    public CalculateTransformation(string leftField, string rightField, MathOperation operation, string targetField)
    {
        Id = Guid.NewGuid().ToString();
        _leftField = leftField;
        _rightField = rightField;
        _operation = operation;
        TargetField = targetField;
        SourceField = $"{leftField},{rightField}";
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name => "Calculate";

    /// <inheritdoc />
    public string Description => $"Calculates {_leftField} {GetOperationSymbol(_operation)} {_rightField}";

    /// <inheritdoc />
    public TransformationType Type => TransformationType.Field;

    /// <inheritdoc />
    public bool SupportsParallelExecution => true;

    /// <inheritdoc />
    public string SourceField { get; }

    /// <inheritdoc />
    public string TargetField { get; }

    /// <inheritdoc />
    public ValidationResult Validate(Interfaces.ITransformationContext context)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrEmpty(_leftField))
            result.AddError("Left field name is required", nameof(_leftField));

        if (string.IsNullOrEmpty(_rightField))
            result.AddError("Right field name is required", nameof(_rightField));

        if (string.IsNullOrEmpty(TargetField))
            result.AddError("Target field name is required", nameof(TargetField));

        return result;
    }

    /// <inheritdoc />
    public async Task<TransformationResult> TransformAsync(DataRecord record, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            var leftValue = BaseNumericTransformation.ToDecimal(record.GetField<object>(_leftField));
            var rightValue = BaseNumericTransformation.ToDecimal(record.GetField<object>(_rightField));
            
            if (leftValue == null || rightValue == null)
            {
                var result = TransformationResultHelper.Skipped(record, "One or both operands are not numeric");
                context.SkipRecord();
                return result;
            }

            decimal calculatedValue = _operation switch
            {
                MathOperation.Add => leftValue.Value + rightValue.Value,
                MathOperation.Subtract => leftValue.Value - rightValue.Value,
                MathOperation.Multiply => leftValue.Value * rightValue.Value,
                MathOperation.Divide => rightValue.Value != 0 ? leftValue.Value / rightValue.Value : 0,
                MathOperation.Modulo => rightValue.Value != 0 ? leftValue.Value % rightValue.Value : 0,
                _ => throw new InvalidOperationException($"Unsupported operation: {_operation}")
            };
            
            var outputRecord = record.Clone();
            outputRecord.SetField(TargetField, calculatedValue);
            
            var transformResult = TransformationResultHelper.Success(outputRecord);
            context.UpdateStatistics(1, 2, DateTimeOffset.UtcNow - startTime);
            
            return transformResult;
        }
        catch (Exception ex)
        {
            var result = TransformationResultHelper.Failure($"Calculation failed: {ex.Message}", ex);
            context.AddError($"Calculation failed: {ex.Message}", ex);
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TransformationResult>> TransformBatchAsync(IEnumerable<DataRecord> records, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var results = new List<TransformationResult>();
        
        foreach (var record in records)
        {
            var result = await TransformAsync(record, context, cancellationToken);
            results.Add(result);
        }
        
        return results;
    }

    /// <inheritdoc />
    public Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use TransformAsync for calculation transformation");
    }

    /// <inheritdoc />
    public TransformationMetadata GetMetadata()
    {
        return new TransformationMetadata
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Type = Type,
            Category = "Numeric",
            Tags = { "numeric", "field", "math", "calculate" },
            RequiredInputFields = { _leftField, _rightField },
            OutputFields = { TargetField },
            Parameters = { ["operation"] = _operation.ToString() },
            Performance = new TransformationPerformance
            {
                ExpectedThroughput = 12000,
                MemoryUsageMB = 1,
                IsCpuIntensive = false,
                IsMemoryIntensive = false,
                Complexity = ComplexityLevel.Low
            }
        };
    }

    private static string GetOperationSymbol(MathOperation operation)
    {
        return operation switch
        {
            MathOperation.Add => "+",
            MathOperation.Subtract => "-",
            MathOperation.Multiply => "*",
            MathOperation.Divide => "/",
            MathOperation.Modulo => "%",
            _ => "?"
        };
    }
}

/// <summary>
/// Represents mathematical operations.
/// </summary>
public enum MathOperation
{
    /// <summary>
    /// Addition operation.
    /// </summary>
    Add,

    /// <summary>
    /// Subtraction operation.
    /// </summary>
    Subtract,

    /// <summary>
    /// Multiplication operation.
    /// </summary>
    Multiply,

    /// <summary>
    /// Division operation.
    /// </summary>
    Divide,

    /// <summary>
    /// Modulo operation.
    /// </summary>
    Modulo
}
