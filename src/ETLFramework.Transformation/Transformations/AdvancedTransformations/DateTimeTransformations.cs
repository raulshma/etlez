using System.Globalization;
using ETLFramework.Core.Models;
using ETLFramework.Transformation.Interfaces;
using ETLFramework.Transformation.Helpers;
using ETLFramework.Core.Interfaces;

namespace ETLFramework.Transformation.Transformations.AdvancedTransformations;

/// <summary>
/// Base class for date/time transformations.
/// </summary>
public abstract class BaseDateTimeTransformation : ITransformation
{
    /// <summary>
    /// Initializes a new instance of the BaseDateTimeTransformation class.
    /// </summary>
    /// <param name="id">The transformation ID</param>
    /// <param name="name">The transformation name</param>
    /// <param name="sourceField">The source field name</param>
    /// <param name="targetField">The target field name</param>
    protected BaseDateTimeTransformation(string id, string name, string sourceField, string? targetField = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        SourceField = sourceField ?? throw new ArgumentNullException(nameof(sourceField));
        TargetField = targetField ?? sourceField;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description => $"DateTime transformation for field {SourceField}";

    /// <inheritdoc />
    public TransformationType Type => TransformationType.Field;

    /// <inheritdoc />
    public bool SupportsParallelExecution => true;

    /// <summary>
    /// Gets the source field name.
    /// </summary>
    public string SourceField { get; }

    /// <summary>
    /// Gets the target field name.
    /// </summary>
    public string TargetField { get; }

    /// <inheritdoc />
    public virtual ValidationResult Validate(ITransformationContext context)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(SourceField))
        {
            result.AddError("Source field cannot be empty");
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
            RequiredInputFields = new List<string> { SourceField },
            OutputFields = new List<string> { TargetField }
        };
    }

    /// <inheritdoc />
    public async Task<Core.Models.TransformationResult> TransformAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var sourceValue = record.GetField<object>(SourceField);
            var dateTime = ConvertToDateTime(sourceValue);

            if (dateTime == null)
            {
                context.SkipRecord();
                return TransformationResultHelper.Skipped(record, $"Could not convert '{sourceValue}' to DateTime");
            }

            var transformedValue = await TransformDateTimeAsync(dateTime.Value, record, context, cancellationToken);

            var outputRecord = record.Clone();
            outputRecord.SetField(TargetField, transformedValue);

            context.UpdateStatistics(1, 1, DateTimeOffset.UtcNow - startTime);
            return TransformationResultHelper.Success(outputRecord);
        }
        catch (Exception ex)
        {
            context.AddError($"DateTime transformation failed for field '{SourceField}': {ex.Message}", ex, fieldName: SourceField);
            return TransformationResultHelper.Failure($"DateTime transformation failed: {ex.Message}", ex);
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
    /// Transforms a DateTime value.
    /// </summary>
    /// <param name="dateTime">The DateTime to transform</param>
    /// <param name="record">The source record for context</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformed value</returns>
    protected abstract Task<object?> TransformDateTimeAsync(DateTime dateTime, DataRecord record, ITransformationContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Converts a value to DateTime.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>The DateTime value or null if conversion fails</returns>
    protected static DateTime? ConvertToDateTime(object? value)
    {
        if (value == null) return null;
        if (value is DateTime dt) return dt;
        if (value is DateTimeOffset dto) return dto.DateTime;
        if (DateTime.TryParse(value.ToString(), out var result)) return result;
        return null;
    }
}

/// <summary>
/// Transformation to format DateTime values.
/// </summary>
public class DateTimeFormatTransformation : BaseDateTimeTransformation
{
    private readonly string _format;
    private readonly CultureInfo _culture;

    /// <summary>
    /// Initializes a new instance of the DateTimeFormatTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="format">The DateTime format string</param>
    /// <param name="targetField">The target field name</param>
    /// <param name="culture">The culture for formatting</param>
    public DateTimeFormatTransformation(string sourceField, string format, string? targetField = null, CultureInfo? culture = null)
        : base($"datetime_format_{sourceField}", $"Format DateTime {sourceField}", sourceField, targetField)
    {
        _format = format ?? throw new ArgumentNullException(nameof(format));
        _culture = culture ?? CultureInfo.InvariantCulture;
    }

    /// <inheritdoc />
    protected override Task<object?> TransformDateTimeAsync(DateTime dateTime, DataRecord record, ITransformationContext context, CancellationToken cancellationToken)
    {
        var formatted = dateTime.ToString(_format, _culture);
        return Task.FromResult<object?>(formatted);
    }
}

/// <summary>
/// Transformation to add time to DateTime values.
/// </summary>
public class DateTimeAddTransformation : BaseDateTimeTransformation
{
    private readonly TimeSpan _timeToAdd;

    /// <summary>
    /// Initializes a new instance of the DateTimeAddTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="timeToAdd">The time to add</param>
    /// <param name="targetField">The target field name</param>
    public DateTimeAddTransformation(string sourceField, TimeSpan timeToAdd, string? targetField = null)
        : base($"datetime_add_{sourceField}", $"Add Time to {sourceField}", sourceField, targetField)
    {
        _timeToAdd = timeToAdd;
    }

    /// <inheritdoc />
    protected override Task<object?> TransformDateTimeAsync(DateTime dateTime, DataRecord record, ITransformationContext context, CancellationToken cancellationToken)
    {
        var result = dateTime.Add(_timeToAdd);
        return Task.FromResult<object?>(result);
    }

    /// <summary>
    /// Creates a transformation that adds days.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="days">Number of days to add</param>
    /// <param name="targetField">The target field name</param>
    /// <returns>Add days transformation</returns>
    public static DateTimeAddTransformation AddDays(string sourceField, int days, string? targetField = null)
    {
        return new DateTimeAddTransformation(sourceField, TimeSpan.FromDays(days), targetField);
    }

    /// <summary>
    /// Creates a transformation that adds hours.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="hours">Number of hours to add</param>
    /// <param name="targetField">The target field name</param>
    /// <returns>Add hours transformation</returns>
    public static DateTimeAddTransformation AddHours(string sourceField, int hours, string? targetField = null)
    {
        return new DateTimeAddTransformation(sourceField, TimeSpan.FromHours(hours), targetField);
    }
}

/// <summary>
/// Transformation to convert DateTime to different time zones.
/// </summary>
public class DateTimeTimeZoneTransformation : BaseDateTimeTransformation
{
    private readonly TimeZoneInfo _sourceTimeZone;
    private readonly TimeZoneInfo _targetTimeZone;

    /// <summary>
    /// Initializes a new instance of the DateTimeTimeZoneTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="sourceTimeZone">The source time zone</param>
    /// <param name="targetTimeZone">The target time zone</param>
    /// <param name="targetField">The target field name</param>
    public DateTimeTimeZoneTransformation(string sourceField, TimeZoneInfo sourceTimeZone, TimeZoneInfo targetTimeZone, string? targetField = null)
        : base($"datetime_timezone_{sourceField}", $"Convert TimeZone {sourceField}", sourceField, targetField)
    {
        _sourceTimeZone = sourceTimeZone ?? throw new ArgumentNullException(nameof(sourceTimeZone));
        _targetTimeZone = targetTimeZone ?? throw new ArgumentNullException(nameof(targetTimeZone));
    }

    /// <inheritdoc />
    protected override Task<object?> TransformDateTimeAsync(DateTime dateTime, DataRecord record, ITransformationContext context, CancellationToken cancellationToken)
    {
        var sourceDateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, _sourceTimeZone);
        var targetDateTime = TimeZoneInfo.ConvertTimeFromUtc(sourceDateTime, _targetTimeZone);
        return Task.FromResult<object?>(targetDateTime);
    }

    /// <summary>
    /// Creates a transformation that converts to UTC.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="sourceTimeZone">The source time zone</param>
    /// <param name="targetField">The target field name</param>
    /// <returns>Convert to UTC transformation</returns>
    public static DateTimeTimeZoneTransformation ToUtc(string sourceField, TimeZoneInfo sourceTimeZone, string? targetField = null)
    {
        return new DateTimeTimeZoneTransformation(sourceField, sourceTimeZone, TimeZoneInfo.Utc, targetField);
    }
}

/// <summary>
/// Transformation to extract date parts.
/// </summary>
public class DateTimePartTransformation : BaseDateTimeTransformation
{
    private readonly DateTimePart _part;

    /// <summary>
    /// Initializes a new instance of the DateTimePartTransformation class.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="part">The date part to extract</param>
    /// <param name="targetField">The target field name</param>
    public DateTimePartTransformation(string sourceField, DateTimePart part, string? targetField = null)
        : base($"datetime_part_{sourceField}", $"Extract {part} from {sourceField}", sourceField, targetField)
    {
        _part = part;
    }

    /// <inheritdoc />
    protected override Task<object?> TransformDateTimeAsync(DateTime dateTime, DataRecord record, ITransformationContext context, CancellationToken cancellationToken)
    {
        object result = _part switch
        {
            DateTimePart.Year => dateTime.Year,
            DateTimePart.Month => dateTime.Month,
            DateTimePart.Day => dateTime.Day,
            DateTimePart.Hour => dateTime.Hour,
            DateTimePart.Minute => dateTime.Minute,
            DateTimePart.Second => dateTime.Second,
            DateTimePart.DayOfWeek => (int)dateTime.DayOfWeek,
            DateTimePart.DayOfYear => dateTime.DayOfYear,
            DateTimePart.Date => dateTime.Date,
            DateTimePart.Time => dateTime.TimeOfDay,
            _ => throw new NotSupportedException($"Date part {_part} is not supported")
        };

        return Task.FromResult<object?>(result);
    }
}

/// <summary>
/// Date/time parts that can be extracted.
/// </summary>
public enum DateTimePart
{
    /// <summary>
    /// Year component.
    /// </summary>
    Year,

    /// <summary>
    /// Month component.
    /// </summary>
    Month,

    /// <summary>
    /// Day component.
    /// </summary>
    Day,

    /// <summary>
    /// Hour component.
    /// </summary>
    Hour,

    /// <summary>
    /// Minute component.
    /// </summary>
    Minute,

    /// <summary>
    /// Second component.
    /// </summary>
    Second,

    /// <summary>
    /// Day of week.
    /// </summary>
    DayOfWeek,

    /// <summary>
    /// Day of year.
    /// </summary>
    DayOfYear,

    /// <summary>
    /// Date part only.
    /// </summary>
    Date,

    /// <summary>
    /// Time part only.
    /// </summary>
    Time
}
