using System.Globalization;
using System.Text.RegularExpressions;
using ETLFramework.Core.Models;

namespace ETLFramework.Transformation.Mapping;

/// <summary>
/// String transformation for field mapping.
/// </summary>
public class StringFieldTransformation : IFieldTransformation
{
    private readonly Func<string?, string?> _transformFunc;

    /// <summary>
    /// Initializes a new instance of the StringFieldTransformation class.
    /// </summary>
    /// <param name="name">The transformation name</param>
    /// <param name="transformFunc">The transformation function</param>
    public StringFieldTransformation(string name, Func<string?, string?> transformFunc)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _transformFunc = transformFunc ?? throw new ArgumentNullException(nameof(transformFunc));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<object?> TransformAsync(object? value, DataRecord sourceRecord, CancellationToken cancellationToken = default)
    {
        var stringValue = value?.ToString();
        var result = _transformFunc(stringValue);
        return Task.FromResult<object?>(result);
    }

    /// <summary>
    /// Creates a transformation that converts to uppercase.
    /// </summary>
    /// <returns>Uppercase transformation</returns>
    public static StringFieldTransformation ToUpper()
    {
        return new StringFieldTransformation("ToUpper", s => s?.ToUpperInvariant());
    }

    /// <summary>
    /// Creates a transformation that converts to lowercase.
    /// </summary>
    /// <returns>Lowercase transformation</returns>
    public static StringFieldTransformation ToLower()
    {
        return new StringFieldTransformation("ToLower", s => s?.ToLowerInvariant());
    }

    /// <summary>
    /// Creates a transformation that trims whitespace.
    /// </summary>
    /// <returns>Trim transformation</returns>
    public static StringFieldTransformation Trim()
    {
        return new StringFieldTransformation("Trim", s => s?.Trim());
    }

    /// <summary>
    /// Creates a transformation that replaces text using regex.
    /// </summary>
    /// <param name="pattern">The regex pattern</param>
    /// <param name="replacement">The replacement text</param>
    /// <returns>Regex replace transformation</returns>
    public static StringFieldTransformation RegexReplace(string pattern, string replacement)
    {
        var regex = new Regex(pattern);
        return new StringFieldTransformation($"RegexReplace({pattern})", 
            s => s != null ? regex.Replace(s, replacement) : null);
    }

    /// <summary>
    /// Creates a transformation that formats strings.
    /// </summary>
    /// <param name="format">The format string</param>
    /// <returns>Format transformation</returns>
    public static StringFieldTransformation Format(string format)
    {
        return new StringFieldTransformation($"Format({format})", 
            s => s != null ? string.Format(format, s) : null);
    }
}

/// <summary>
/// Numeric transformation for field mapping.
/// </summary>
public class NumericFieldTransformation : IFieldTransformation
{
    private readonly Func<decimal?, decimal?> _transformFunc;

    /// <summary>
    /// Initializes a new instance of the NumericFieldTransformation class.
    /// </summary>
    /// <param name="name">The transformation name</param>
    /// <param name="transformFunc">The transformation function</param>
    public NumericFieldTransformation(string name, Func<decimal?, decimal?> transformFunc)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _transformFunc = transformFunc ?? throw new ArgumentNullException(nameof(transformFunc));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<object?> TransformAsync(object? value, DataRecord sourceRecord, CancellationToken cancellationToken = default)
    {
        var numericValue = ConvertToDecimal(value);
        var result = _transformFunc(numericValue);
        return Task.FromResult<object?>(result);
    }

    /// <summary>
    /// Converts a value to decimal.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>The decimal value or null</returns>
    private static decimal? ConvertToDecimal(object? value)
    {
        if (value == null) return null;
        if (value is decimal d) return d;
        if (decimal.TryParse(value.ToString(), out var result)) return result;
        return null;
    }

    /// <summary>
    /// Creates a transformation that rounds to specified decimal places.
    /// </summary>
    /// <param name="decimals">Number of decimal places</param>
    /// <returns>Round transformation</returns>
    public static NumericFieldTransformation Round(int decimals)
    {
        return new NumericFieldTransformation($"Round({decimals})", 
            n => n.HasValue ? Math.Round(n.Value, decimals) : null);
    }

    /// <summary>
    /// Creates a transformation that multiplies by a factor.
    /// </summary>
    /// <param name="factor">The multiplication factor</param>
    /// <returns>Multiply transformation</returns>
    public static NumericFieldTransformation Multiply(decimal factor)
    {
        return new NumericFieldTransformation($"Multiply({factor})", 
            n => n.HasValue ? n.Value * factor : null);
    }

    /// <summary>
    /// Creates a transformation that adds a constant.
    /// </summary>
    /// <param name="addend">The value to add</param>
    /// <returns>Add transformation</returns>
    public static NumericFieldTransformation Add(decimal addend)
    {
        return new NumericFieldTransformation($"Add({addend})", 
            n => n.HasValue ? n.Value + addend : null);
    }

    /// <summary>
    /// Creates a transformation that converts to absolute value.
    /// </summary>
    /// <returns>Absolute value transformation</returns>
    public static NumericFieldTransformation Abs()
    {
        return new NumericFieldTransformation("Abs", 
            n => n.HasValue ? Math.Abs(n.Value) : null);
    }
}

/// <summary>
/// Date/time transformation for field mapping.
/// </summary>
public class DateTimeFieldTransformation : IFieldTransformation
{
    private readonly Func<DateTime?, DateTime?> _transformFunc;

    /// <summary>
    /// Initializes a new instance of the DateTimeFieldTransformation class.
    /// </summary>
    /// <param name="name">The transformation name</param>
    /// <param name="transformFunc">The transformation function</param>
    public DateTimeFieldTransformation(string name, Func<DateTime?, DateTime?> transformFunc)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _transformFunc = transformFunc ?? throw new ArgumentNullException(nameof(transformFunc));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<object?> TransformAsync(object? value, DataRecord sourceRecord, CancellationToken cancellationToken = default)
    {
        var dateValue = ConvertToDateTime(value);
        var result = _transformFunc(dateValue);
        return Task.FromResult<object?>(result);
    }

    /// <summary>
    /// Converts a value to DateTime.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>The DateTime value or null</returns>
    private static DateTime? ConvertToDateTime(object? value)
    {
        if (value == null) return null;
        if (value is DateTime dt) return dt;
        if (DateTime.TryParse(value.ToString(), out var result)) return result;
        return null;
    }

    /// <summary>
    /// Creates a transformation that formats date to string.
    /// </summary>
    /// <param name="format">The date format</param>
    /// <returns>Format transformation</returns>
    public static IFieldTransformation Format(string format)
    {
        return new StringFieldTransformation($"DateFormat({format})", 
            s => ConvertToDateTime(s)?.ToString(format));
    }

    /// <summary>
    /// Creates a transformation that adds days.
    /// </summary>
    /// <param name="days">Number of days to add</param>
    /// <returns>Add days transformation</returns>
    public static DateTimeFieldTransformation AddDays(int days)
    {
        return new DateTimeFieldTransformation($"AddDays({days})", 
            dt => dt?.AddDays(days));
    }

    /// <summary>
    /// Creates a transformation that converts to UTC.
    /// </summary>
    /// <returns>UTC transformation</returns>
    public static DateTimeFieldTransformation ToUtc()
    {
        return new DateTimeFieldTransformation("ToUtc", 
            dt => dt?.ToUniversalTime());
    }

    /// <summary>
    /// Creates a transformation that gets the date part only.
    /// </summary>
    /// <returns>Date only transformation</returns>
    public static DateTimeFieldTransformation DateOnly()
    {
        return new DateTimeFieldTransformation("DateOnly", 
            dt => dt?.Date);
    }
}

/// <summary>
/// Lookup transformation for field mapping.
/// </summary>
public class LookupFieldTransformation : IFieldTransformation
{
    private readonly Dictionary<object, object> _lookupTable;
    private readonly object? _defaultValue;

    /// <summary>
    /// Initializes a new instance of the LookupFieldTransformation class.
    /// </summary>
    /// <param name="name">The transformation name</param>
    /// <param name="lookupTable">The lookup table</param>
    /// <param name="defaultValue">Default value if lookup fails</param>
    public LookupFieldTransformation(string name, Dictionary<object, object> lookupTable, object? defaultValue = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _lookupTable = lookupTable ?? throw new ArgumentNullException(nameof(lookupTable));
        _defaultValue = defaultValue;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<object?> TransformAsync(object? value, DataRecord sourceRecord, CancellationToken cancellationToken = default)
    {
        if (value != null && _lookupTable.TryGetValue(value, out var result))
        {
            return Task.FromResult<object?>(result);
        }

        return Task.FromResult(_defaultValue);
    }

    /// <summary>
    /// Creates a lookup transformation from a dictionary.
    /// </summary>
    /// <param name="lookupTable">The lookup table</param>
    /// <param name="defaultValue">Default value if lookup fails</param>
    /// <returns>Lookup transformation</returns>
    public static LookupFieldTransformation FromDictionary(Dictionary<object, object> lookupTable, object? defaultValue = null)
    {
        return new LookupFieldTransformation("Lookup", lookupTable, defaultValue);
    }
}

/// <summary>
/// Custom transformation with user-defined logic.
/// </summary>
public class CustomFieldTransformation : IFieldTransformation
{
    private readonly Func<object?, DataRecord, CancellationToken, Task<object?>> _transformFunc;

    /// <summary>
    /// Initializes a new instance of the CustomFieldTransformation class.
    /// </summary>
    /// <param name="name">The transformation name</param>
    /// <param name="transformFunc">The transformation function</param>
    public CustomFieldTransformation(string name, Func<object?, DataRecord, CancellationToken, Task<object?>> transformFunc)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _transformFunc = transformFunc ?? throw new ArgumentNullException(nameof(transformFunc));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<object?> TransformAsync(object? value, DataRecord sourceRecord, CancellationToken cancellationToken = default)
    {
        return await _transformFunc(value, sourceRecord, cancellationToken);
    }
}
