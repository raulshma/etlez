using ETLFramework.Core.Models;
using ETLFramework.Transformation.Interfaces;
using ETLFramework.Transformation.Helpers;

namespace ETLFramework.Transformation.Transformations.AdvancedTransformations;

/// <summary>
/// Base class for lookup transformations.
/// </summary>
public abstract class BaseLookupTransformation : ITransformation
{
    /// <summary>
    /// Initializes a new instance of the BaseLookupTransformation class.
    /// </summary>
    /// <param name="id">The transformation ID</param>
    /// <param name="name">The transformation name</param>
    /// <param name="lookupField">The field to use for lookup</param>
    /// <param name="targetField">The target field for the result</param>
    protected BaseLookupTransformation(string id, string name, string lookupField, string targetField)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        LookupField = lookupField ?? throw new ArgumentNullException(nameof(lookupField));
        TargetField = targetField ?? throw new ArgumentNullException(nameof(targetField));
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description => $"Lookup transformation for field {LookupField}";

    /// <inheritdoc />
    public TransformationType Type => TransformationType.Field;

    /// <inheritdoc />
    public bool SupportsParallelExecution => true;

    /// <summary>
    /// Gets the field to use for lookup.
    /// </summary>
    public string LookupField { get; }

    /// <summary>
    /// Gets the target field for the result.
    /// </summary>
    public string TargetField { get; }

    /// <summary>
    /// Gets or sets the default value to use when lookup fails.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets whether to fail transformation when lookup fails.
    /// </summary>
    public bool FailOnMissingLookup { get; set; } = false;

    /// <inheritdoc />
    public virtual ValidationResult Validate(ETLFramework.Transformation.Interfaces.ITransformationContext context)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(LookupField))
        {
            result.AddError("Lookup field cannot be empty");
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
            RequiredInputFields = new List<string> { LookupField },
            OutputFields = new List<string> { TargetField }
        };
    }

    /// <inheritdoc />
    public async Task<Core.Models.TransformationResult> TransformAsync(DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var lookupValue = record.GetField<object>(LookupField);
            var lookupResult = await PerformLookupAsync(lookupValue, record, context, cancellationToken);

            object? resultValue;
            if (lookupResult.Found)
            {
                resultValue = lookupResult.Value;
            }
            else if (DefaultValue != null)
            {
                resultValue = DefaultValue;
            }
            else if (FailOnMissingLookup)
            {
                context.AddError($"Lookup failed for value '{lookupValue}' in field '{LookupField}'", null, fieldName: LookupField);
                return TransformationResultHelper.Failure($"Lookup failed for value '{lookupValue}'");
            }
            else
            {
                resultValue = null;
            }

            var outputRecord = record.Clone();
            outputRecord.SetField(TargetField, resultValue);

            context.UpdateStatistics(1, 1, DateTimeOffset.UtcNow - startTime);
            return TransformationResultHelper.Success(outputRecord);
        }
        catch (Exception ex)
        {
            context.AddError($"Lookup transformation failed for field '{LookupField}': {ex.Message}", ex, fieldName: LookupField);
            return TransformationResultHelper.Failure($"Lookup transformation failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Core.Models.TransformationResult>> TransformBatchAsync(IEnumerable<DataRecord> records, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
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
    /// Performs the lookup operation.
    /// </summary>
    /// <param name="lookupValue">The value to lookup</param>
    /// <param name="record">The source record for context</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The lookup result</returns>
    protected abstract Task<LookupResult> PerformLookupAsync(object? lookupValue, DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Dictionary-based lookup transformation.
/// </summary>
public class DictionaryLookupTransformation : BaseLookupTransformation
{
    private readonly Dictionary<object, object> _lookupTable;

    /// <summary>
    /// Initializes a new instance of the DictionaryLookupTransformation class.
    /// </summary>
    /// <param name="lookupField">The field to use for lookup</param>
    /// <param name="targetField">The target field for the result</param>
    /// <param name="lookupTable">The lookup table</param>
    public DictionaryLookupTransformation(string lookupField, string targetField, Dictionary<object, object> lookupTable)
        : base($"dict_lookup_{lookupField}", $"Dictionary Lookup {lookupField}", lookupField, targetField)
    {
        _lookupTable = lookupTable ?? throw new ArgumentNullException(nameof(lookupTable));
    }

    /// <inheritdoc />
    protected override Task<LookupResult> PerformLookupAsync(object? lookupValue, DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken)
    {
        if (lookupValue != null && _lookupTable.TryGetValue(lookupValue, out var result))
        {
            return Task.FromResult(new LookupResult { Found = true, Value = result });
        }

        return Task.FromResult(new LookupResult { Found = false });
    }

    /// <summary>
    /// Gets statistics about the lookup table.
    /// </summary>
    /// <returns>Lookup statistics</returns>
    public LookupStatistics GetStatistics()
    {
        return new LookupStatistics
        {
            TotalEntries = _lookupTable.Count,
            LookupType = "Dictionary"
        };
    }
}

/// <summary>
/// Function-based lookup transformation.
/// </summary>
public class FunctionLookupTransformation : BaseLookupTransformation
{
    private readonly Func<object?, DataRecord, CancellationToken, Task<LookupResult>> _lookupFunction;

    /// <summary>
    /// Initializes a new instance of the FunctionLookupTransformation class.
    /// </summary>
    /// <param name="lookupField">The field to use for lookup</param>
    /// <param name="targetField">The target field for the result</param>
    /// <param name="lookupFunction">The lookup function</param>
    public FunctionLookupTransformation(string lookupField, string targetField, Func<object?, DataRecord, CancellationToken, Task<LookupResult>> lookupFunction)
        : base($"func_lookup_{lookupField}", $"Function Lookup {lookupField}", lookupField, targetField)
    {
        _lookupFunction = lookupFunction ?? throw new ArgumentNullException(nameof(lookupFunction));
    }

    /// <inheritdoc />
    protected override async Task<LookupResult> PerformLookupAsync(object? lookupValue, DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken)
    {
        return await _lookupFunction(lookupValue, record, cancellationToken);
    }
}

/// <summary>
/// Cache-enabled lookup transformation.
/// </summary>
public class CachedLookupTransformation : BaseLookupTransformation
{
    private readonly Func<object?, DataRecord, CancellationToken, Task<LookupResult>> _lookupFunction;
    private readonly Dictionary<object, LookupResult> _cache;
    private readonly int _maxCacheSize;

    /// <summary>
    /// Initializes a new instance of the CachedLookupTransformation class.
    /// </summary>
    /// <param name="lookupField">The field to use for lookup</param>
    /// <param name="targetField">The target field for the result</param>
    /// <param name="lookupFunction">The lookup function</param>
    /// <param name="maxCacheSize">Maximum cache size</param>
    public CachedLookupTransformation(string lookupField, string targetField, Func<object?, DataRecord, CancellationToken, Task<LookupResult>> lookupFunction, int maxCacheSize = 1000)
        : base($"cached_lookup_{lookupField}", $"Cached Lookup {lookupField}", lookupField, targetField)
    {
        _lookupFunction = lookupFunction ?? throw new ArgumentNullException(nameof(lookupFunction));
        _cache = new Dictionary<object, LookupResult>();
        _maxCacheSize = maxCacheSize;
    }

    /// <inheritdoc />
    protected override async Task<LookupResult> PerformLookupAsync(object? lookupValue, DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken)
    {
        if (lookupValue == null)
        {
            return new LookupResult { Found = false };
        }

        // Check cache first
        if (_cache.TryGetValue(lookupValue, out var cachedResult))
        {
            return cachedResult;
        }

        // Perform lookup
        var result = await _lookupFunction(lookupValue, record, cancellationToken);

        // Add to cache if not full
        if (_cache.Count < _maxCacheSize)
        {
            _cache[lookupValue] = result;
        }

        return result;
    }

    /// <summary>
    /// Clears the lookup cache.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Cache statistics</returns>
    public CacheStatistics GetCacheStatistics()
    {
        return new CacheStatistics
        {
            CacheSize = _cache.Count,
            MaxCacheSize = _maxCacheSize,
            CacheUtilization = (double)_cache.Count / _maxCacheSize * 100
        };
    }
}

/// <summary>
/// Multi-field lookup transformation.
/// </summary>
public class MultiFieldLookupTransformation : BaseLookupTransformation
{
    private readonly string[] _lookupFields;
    private readonly Dictionary<string, object> _lookupTable;

    /// <summary>
    /// Initializes a new instance of the MultiFieldLookupTransformation class.
    /// </summary>
    /// <param name="lookupFields">The fields to use for lookup</param>
    /// <param name="targetField">The target field for the result</param>
    /// <param name="lookupTable">The lookup table with composite keys</param>
    public MultiFieldLookupTransformation(string[] lookupFields, string targetField, Dictionary<string, object> lookupTable)
        : base($"multi_lookup_{string.Join("_", lookupFields)}", $"Multi-Field Lookup", string.Join(",", lookupFields), targetField)
    {
        _lookupFields = lookupFields ?? throw new ArgumentNullException(nameof(lookupFields));
        _lookupTable = lookupTable ?? throw new ArgumentNullException(nameof(lookupTable));
    }

    /// <inheritdoc />
    protected override Task<LookupResult> PerformLookupAsync(object? lookupValue, DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken)
    {
        // Create composite key from multiple fields
        var keyParts = _lookupFields.Select(field => record.GetField<object>(field)?.ToString() ?? "").ToArray();
        var compositeKey = string.Join("|", keyParts);

        if (_lookupTable.TryGetValue(compositeKey, out var result))
        {
            return Task.FromResult(new LookupResult { Found = true, Value = result });
        }

        return Task.FromResult(new LookupResult { Found = false });
    }
}

/// <summary>
/// Result of a lookup operation.
/// </summary>
public class LookupResult
{
    /// <summary>
    /// Gets or sets whether the lookup was successful.
    /// </summary>
    public bool Found { get; set; }

    /// <summary>
    /// Gets or sets the lookup result value.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// Statistics about lookup operations.
/// </summary>
public class LookupStatistics
{
    /// <summary>
    /// Gets or sets the total number of entries.
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Gets or sets the lookup type.
    /// </summary>
    public string LookupType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of successful lookups.
    /// </summary>
    public int SuccessfulLookups { get; set; }

    /// <summary>
    /// Gets or sets the number of failed lookups.
    /// </summary>
    public int FailedLookups { get; set; }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => (SuccessfulLookups + FailedLookups) > 0 
        ? (double)SuccessfulLookups / (SuccessfulLookups + FailedLookups) * 100 
        : 0;
}

/// <summary>
/// Statistics about cache performance.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Gets or sets the current cache size.
    /// </summary>
    public int CacheSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum cache size.
    /// </summary>
    public int MaxCacheSize { get; set; }

    /// <summary>
    /// Gets or sets the cache utilization percentage.
    /// </summary>
    public double CacheUtilization { get; set; }

    /// <summary>
    /// Gets or sets the number of cache hits.
    /// </summary>
    public int CacheHits { get; set; }

    /// <summary>
    /// Gets or sets the number of cache misses.
    /// </summary>
    public int CacheMisses { get; set; }

    /// <summary>
    /// Gets the cache hit rate as a percentage.
    /// </summary>
    public double CacheHitRate => (CacheHits + CacheMisses) > 0
        ? (double)CacheHits / (CacheHits + CacheMisses) * 100
        : 0;
}
