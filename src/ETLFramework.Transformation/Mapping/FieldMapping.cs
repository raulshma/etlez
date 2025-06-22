using ETLFramework.Core.Models;

namespace ETLFramework.Transformation.Mapping;

/// <summary>
/// Default implementation of field mapping.
/// </summary>
public class FieldMapping : IFieldMapping
{
    /// <summary>
    /// Initializes a new instance of the FieldMapping class.
    /// </summary>
    /// <param name="id">The mapping ID</param>
    /// <param name="name">The mapping name</param>
    /// <param name="sourcePath">The source field path</param>
    /// <param name="targetPath">The target field path</param>
    /// <param name="mappingType">The mapping type</param>
    public FieldMapping(string id, string name, string sourcePath, string targetPath, MappingType mappingType = MappingType.Direct)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
        TargetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
        MappingType = mappingType;
        IsRequired = false;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string SourcePath { get; }

    /// <inheritdoc />
    public string TargetPath { get; }

    /// <inheritdoc />
    public MappingType MappingType { get; }

    /// <inheritdoc />
    public IFieldTransformation? Transformation { get; set; }

    /// <inheritdoc />
    public object? DefaultValue { get; set; }

    /// <inheritdoc />
    public bool IsRequired { get; set; }

    /// <inheritdoc />
    public async Task MapValueAsync(DataRecord sourceRecord, DataRecord targetRecord, CancellationToken cancellationToken = default)
    {
        object? value = null;

        switch (MappingType)
        {
            case MappingType.Direct:
                value = GetSourceValue(sourceRecord, SourcePath);
                break;

            case MappingType.Transform:
                var sourceValue = GetSourceValue(sourceRecord, SourcePath);
                if (Transformation != null)
                {
                    value = await Transformation.TransformAsync(sourceValue, sourceRecord, cancellationToken);
                }
                else
                {
                    value = sourceValue;
                }
                break;

            case MappingType.Constant:
                value = DefaultValue;
                break;

            case MappingType.Conditional:
                value = await ApplyConditionalMappingAsync(sourceRecord, cancellationToken);
                break;

            case MappingType.Lookup:
                value = await ApplyLookupMappingAsync(sourceRecord, cancellationToken);
                break;

            case MappingType.Aggregate:
                value = await ApplyAggregateMappingAsync(sourceRecord, cancellationToken);
                break;

            case MappingType.Custom:
                value = await ApplyCustomMappingAsync(sourceRecord, cancellationToken);
                break;

            default:
                throw new NotSupportedException($"Mapping type {MappingType} is not supported");
        }

        // Use default value if source value is null and default is specified
        if (value == null && DefaultValue != null)
        {
            value = DefaultValue;
        }

        // Set the target value
        SetTargetValue(targetRecord, TargetPath, value);
    }

    /// <summary>
    /// Gets a value from the source record using the specified path.
    /// </summary>
    /// <param name="record">The source record</param>
    /// <param name="path">The field path</param>
    /// <returns>The field value</returns>
    protected virtual object? GetSourceValue(DataRecord record, string path)
    {
        // Support nested field paths using dot notation
        var pathParts = path.Split('.');
        
        if (pathParts.Length == 1)
        {
            return record.GetField<object>(path);
        }

        // For nested paths, we would need to implement nested object navigation
        // For now, just return the direct field value
        return record.GetField<object>(pathParts[0]);
    }

    /// <summary>
    /// Sets a value in the target record using the specified path.
    /// </summary>
    /// <param name="record">The target record</param>
    /// <param name="path">The field path</param>
    /// <param name="value">The value to set</param>
    protected virtual void SetTargetValue(DataRecord record, string path, object? value)
    {
        // Support nested field paths using dot notation
        var pathParts = path.Split('.');
        
        if (pathParts.Length == 1)
        {
            record.SetField(path, value);
            return;
        }

        // For nested paths, we would need to implement nested object creation
        // For now, just set the direct field value using the full path
        record.SetField(path, value);
    }

    /// <summary>
    /// Applies conditional mapping logic.
    /// </summary>
    /// <param name="sourceRecord">The source record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The mapped value</returns>
    protected virtual Task<object?> ApplyConditionalMappingAsync(DataRecord sourceRecord, CancellationToken cancellationToken)
    {
        // Default implementation - override in derived classes for specific conditional logic
        return Task.FromResult(GetSourceValue(sourceRecord, SourcePath));
    }

    /// <summary>
    /// Applies lookup mapping logic.
    /// </summary>
    /// <param name="sourceRecord">The source record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The mapped value</returns>
    protected virtual Task<object?> ApplyLookupMappingAsync(DataRecord sourceRecord, CancellationToken cancellationToken)
    {
        // Default implementation - override in derived classes for specific lookup logic
        return Task.FromResult(GetSourceValue(sourceRecord, SourcePath));
    }

    /// <summary>
    /// Applies aggregate mapping logic.
    /// </summary>
    /// <param name="sourceRecord">The source record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The mapped value</returns>
    protected virtual Task<object?> ApplyAggregateMappingAsync(DataRecord sourceRecord, CancellationToken cancellationToken)
    {
        // Default implementation - override in derived classes for specific aggregate logic
        return Task.FromResult(GetSourceValue(sourceRecord, SourcePath));
    }

    /// <summary>
    /// Applies custom mapping logic.
    /// </summary>
    /// <param name="sourceRecord">The source record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The mapped value</returns>
    protected virtual Task<object?> ApplyCustomMappingAsync(DataRecord sourceRecord, CancellationToken cancellationToken)
    {
        // Default implementation - override in derived classes for specific custom logic
        return Task.FromResult(GetSourceValue(sourceRecord, SourcePath));
    }
}

/// <summary>
/// Field mapping with custom transformation function.
/// </summary>
public class CustomFieldMapping : FieldMapping
{
    private readonly Func<DataRecord, CancellationToken, Task<object?>> _customMappingFunc;

    /// <summary>
    /// Initializes a new instance of the CustomFieldMapping class.
    /// </summary>
    /// <param name="id">The mapping ID</param>
    /// <param name="name">The mapping name</param>
    /// <param name="sourcePath">The source field path</param>
    /// <param name="targetPath">The target field path</param>
    /// <param name="customMappingFunc">The custom mapping function</param>
    public CustomFieldMapping(
        string id, 
        string name, 
        string sourcePath, 
        string targetPath, 
        Func<DataRecord, CancellationToken, Task<object?>> customMappingFunc)
        : base(id, name, sourcePath, targetPath, MappingType.Custom)
    {
        _customMappingFunc = customMappingFunc ?? throw new ArgumentNullException(nameof(customMappingFunc));
    }

    /// <inheritdoc />
    protected override async Task<object?> ApplyCustomMappingAsync(DataRecord sourceRecord, CancellationToken cancellationToken)
    {
        return await _customMappingFunc(sourceRecord, cancellationToken);
    }
}

/// <summary>
/// Field mapping for constant values.
/// </summary>
public class ConstantFieldMapping : FieldMapping
{
    /// <summary>
    /// Initializes a new instance of the ConstantFieldMapping class.
    /// </summary>
    /// <param name="id">The mapping ID</param>
    /// <param name="name">The mapping name</param>
    /// <param name="targetPath">The target field path</param>
    /// <param name="constantValue">The constant value</param>
    public ConstantFieldMapping(string id, string name, string targetPath, object? constantValue)
        : base(id, name, string.Empty, targetPath, MappingType.Constant)
    {
        DefaultValue = constantValue;
    }
}

/// <summary>
/// Field mapping with conditional logic.
/// </summary>
public class ConditionalFieldMapping : FieldMapping
{
    private readonly Func<DataRecord, bool> _condition;
    private readonly string _trueSourcePath;
    private readonly string _falseSourcePath;

    /// <summary>
    /// Initializes a new instance of the ConditionalFieldMapping class.
    /// </summary>
    /// <param name="id">The mapping ID</param>
    /// <param name="name">The mapping name</param>
    /// <param name="targetPath">The target field path</param>
    /// <param name="condition">The condition function</param>
    /// <param name="trueSourcePath">Source path when condition is true</param>
    /// <param name="falseSourcePath">Source path when condition is false</param>
    public ConditionalFieldMapping(
        string id, 
        string name, 
        string targetPath, 
        Func<DataRecord, bool> condition,
        string trueSourcePath,
        string falseSourcePath)
        : base(id, name, trueSourcePath, targetPath, MappingType.Conditional)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _trueSourcePath = trueSourcePath ?? throw new ArgumentNullException(nameof(trueSourcePath));
        _falseSourcePath = falseSourcePath ?? throw new ArgumentNullException(nameof(falseSourcePath));
    }

    /// <inheritdoc />
    protected override Task<object?> ApplyConditionalMappingAsync(DataRecord sourceRecord, CancellationToken cancellationToken)
    {
        var sourcePath = _condition(sourceRecord) ? _trueSourcePath : _falseSourcePath;
        var value = GetSourceValue(sourceRecord, sourcePath);
        return Task.FromResult(value);
    }
}
