using System.Text.RegularExpressions;
using ETLFramework.Core.Models;
using ETLFramework.Transformation.Interfaces;
using ETLFramework.Transformation.Helpers;

namespace ETLFramework.Transformation.Transformations.FieldTransformations;

/// <summary>
/// Base class for string field transformations.
/// </summary>
public abstract class BaseStringTransformation : IFieldTransformation
{
    /// <summary>
    /// Initializes a new instance of the BaseStringTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="targetField">The target field name</param>
    protected BaseStringTransformation(string sourceField, string targetField)
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
            var result = TransformationResultHelper.Failure($"String transformation failed: {ex.Message}", ex);
            context.AddError($"String transformation failed for field '{SourceField}': {ex.Message}", ex, fieldName: SourceField);
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
            Category = "String",
            Tags = { "string", "field", "text" },
            RequiredInputFields = { SourceField },
            OutputFields = { TargetField },
            Performance = new TransformationPerformance
            {
                ExpectedThroughput = 10000,
                MemoryUsageMB = 1,
                IsCpuIntensive = false,
                IsMemoryIntensive = false,
                Complexity = ComplexityLevel.Low
            }
        };
    }
}

/// <summary>
/// Transforms text to uppercase.
/// </summary>
public class UppercaseTransformation : BaseStringTransformation
{
    /// <summary>
    /// Initializes a new instance of the UppercaseTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="targetField">The target field name</param>
    public UppercaseTransformation(string sourceField, string targetField = null) 
        : base(sourceField, targetField ?? sourceField)
    {
    }

    /// <inheritdoc />
    public override string Name => "Uppercase";

    /// <inheritdoc />
    public override string Description => "Converts text to uppercase";

    /// <inheritdoc />
    public override Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        if (value == null)
            return Task.FromResult<object?>(null);

        var stringValue = value.ToString();
        return Task.FromResult<object?>(stringValue?.ToUpperInvariant());
    }
}

/// <summary>
/// Transforms text to lowercase.
/// </summary>
public class LowercaseTransformation : BaseStringTransformation
{
    /// <summary>
    /// Initializes a new instance of the LowercaseTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="targetField">The target field name</param>
    public LowercaseTransformation(string sourceField, string targetField = null) 
        : base(sourceField, targetField ?? sourceField)
    {
    }

    /// <inheritdoc />
    public override string Name => "Lowercase";

    /// <inheritdoc />
    public override string Description => "Converts text to lowercase";

    /// <inheritdoc />
    public override Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        if (value == null)
            return Task.FromResult<object?>(null);

        var stringValue = value.ToString();
        return Task.FromResult<object?>(stringValue?.ToLowerInvariant());
    }
}

/// <summary>
/// Trims whitespace from text.
/// </summary>
public class TrimTransformation : BaseStringTransformation
{
    /// <summary>
    /// Initializes a new instance of the TrimTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="targetField">The target field name</param>
    public TrimTransformation(string sourceField, string targetField = null) 
        : base(sourceField, targetField ?? sourceField)
    {
    }

    /// <inheritdoc />
    public override string Name => "Trim";

    /// <inheritdoc />
    public override string Description => "Removes leading and trailing whitespace";

    /// <inheritdoc />
    public override Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        if (value == null)
            return Task.FromResult<object?>(null);

        var stringValue = value.ToString();
        return Task.FromResult<object?>(stringValue?.Trim());
    }
}

/// <summary>
/// Replaces text using regular expressions.
/// </summary>
public class RegexReplaceTransformation : BaseStringTransformation
{
    private readonly string _pattern;
    private readonly string _replacement;
    private readonly Regex _regex;

    /// <summary>
    /// Initializes a new instance of the RegexReplaceTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="pattern">The regex pattern</param>
    /// <param name="replacement">The replacement text</param>
    /// <param name="targetField">The target field name</param>
    public RegexReplaceTransformation(string sourceField, string pattern, string replacement, string targetField = null) 
        : base(sourceField, targetField ?? sourceField)
    {
        _pattern = pattern;
        _replacement = replacement;
        _regex = new Regex(pattern, RegexOptions.Compiled);
    }

    /// <inheritdoc />
    public override string Name => "Regex Replace";

    /// <inheritdoc />
    public override string Description => $"Replaces text matching pattern '{_pattern}' with '{_replacement}'";

    /// <inheritdoc />
    public override Task<object?> TransformFieldAsync(object? value, Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        if (value == null)
            return Task.FromResult<object?>(null);

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
            return Task.FromResult<object?>(stringValue);

        var result = _regex.Replace(stringValue, _replacement);
        return Task.FromResult<object?>(result);
    }

    /// <inheritdoc />
    public override ValidationResult Validate(Interfaces.ITransformationContext context)
    {
        var result = base.Validate(context);

        if (string.IsNullOrEmpty(_pattern))
            result.AddError("Regex pattern is required", nameof(_pattern));

        if (_replacement == null)
            result.AddError("Replacement text is required", nameof(_replacement));

        try
        {
            _ = new Regex(_pattern);
        }
        catch (ArgumentException ex)
        {
            result.AddError($"Invalid regex pattern: {ex.Message}", nameof(_pattern));
        }

        return result;
    }
}

/// <summary>
/// Concatenates multiple fields into a single field.
/// </summary>
public class ConcatenateTransformation : IFieldTransformation
{
    private readonly string[] _sourceFields;
    private readonly string _separator;

    /// <summary>
    /// Initializes a new instance of the ConcatenateTransformation class.
    /// </summary>
    /// <param name="sourceFields">The source field names</param>
    /// <param name="targetField">The target field name</param>
    /// <param name="separator">The separator between values</param>
    public ConcatenateTransformation(string[] sourceFields, string targetField, string separator = "")
    {
        Id = Guid.NewGuid().ToString();
        _sourceFields = sourceFields;
        TargetField = targetField;
        _separator = separator;
        SourceField = string.Join(",", sourceFields);
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name => "Concatenate";

    /// <inheritdoc />
    public string Description => $"Concatenates fields {string.Join(", ", _sourceFields)} with separator '{_separator}'";

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

        if (_sourceFields == null || _sourceFields.Length == 0)
            result.AddError("At least one source field is required", nameof(_sourceFields));

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
            var values = _sourceFields.Select(field => record.GetField<object>(field)?.ToString() ?? "").ToArray();
            var concatenatedValue = string.Join(_separator, values);
            
            var outputRecord = record.Clone();
            outputRecord.SetField(TargetField, concatenatedValue);
            
            var result = TransformationResultHelper.Success(outputRecord);
            context.UpdateStatistics(1, _sourceFields.Length, DateTimeOffset.UtcNow - startTime);
            
            return result;
        }
        catch (Exception ex)
        {
            var result = TransformationResultHelper.Failure($"Concatenation failed: {ex.Message}", ex);
            context.AddError($"Concatenation failed: {ex.Message}", ex);
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
        // This method is not used for concatenation as it works with multiple fields
        throw new NotSupportedException("Use TransformAsync for concatenation transformation");
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
            Category = "String",
            Tags = { "string", "field", "concatenate", "combine" },
            RequiredInputFields = _sourceFields.ToList(),
            OutputFields = { TargetField },
            Parameters = { ["separator"] = _separator },
            Performance = new TransformationPerformance
            {
                ExpectedThroughput = 8000,
                MemoryUsageMB = 1,
                IsCpuIntensive = false,
                IsMemoryIntensive = false,
                Complexity = ComplexityLevel.Low
            }
        };
    }
}
