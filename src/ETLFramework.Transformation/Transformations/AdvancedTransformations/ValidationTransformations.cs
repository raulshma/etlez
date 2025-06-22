using System.Text.RegularExpressions;
using ETLFramework.Core.Models;
using ETLFramework.Transformation.Interfaces;
using ETLFramework.Transformation.Helpers;

namespace ETLFramework.Transformation.Transformations.AdvancedTransformations;

/// <summary>
/// Base class for validation transformations.
/// </summary>
public abstract class BaseValidationTransformation : ITransformation
{
    /// <summary>
    /// Initializes a new instance of the BaseValidationTransformation class.
    /// </summary>
    /// <param name="id">The transformation ID</param>
    /// <param name="name">The transformation name</param>
    /// <param name="fieldName">The field name to validate</param>
    /// <param name="validationAction">Action to take when validation fails</param>
    protected BaseValidationTransformation(string id, string name, string fieldName, ValidationAction validationAction = ValidationAction.AddError)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        ValidationAction = validationAction;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description => $"Validation transformation for field {FieldName}";

    /// <inheritdoc />
    public TransformationType Type => TransformationType.Record;

    /// <inheritdoc />
    public bool SupportsParallelExecution => true;

    /// <summary>
    /// Gets the field name to validate.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Gets the action to take when validation fails.
    /// </summary>
    public ValidationAction ValidationAction { get; }

    /// <summary>
    /// Gets or sets the error message template.
    /// </summary>
    public string ErrorMessage { get; set; } = "Validation failed for field '{FieldName}': {ValidationError}";

    /// <inheritdoc />
    public virtual ValidationResult Validate(ETLFramework.Transformation.Interfaces.ITransformationContext context)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(FieldName))
        {
            result.AddError("Field name cannot be empty");
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
            RequiredInputFields = new List<string> { FieldName },
            OutputFields = new List<string> { FieldName }
        };
    }

    /// <inheritdoc />
    public async Task<Core.Models.TransformationResult> TransformAsync(DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var fieldValue = record.GetField<object>(FieldName);
            var validationResult = await ValidateValueAsync(fieldValue, record, context, cancellationToken);

            if (validationResult.IsValid)
            {
                context.UpdateStatistics(1, 1, DateTimeOffset.UtcNow - startTime);
                return TransformationResultHelper.Success(record);
            }

            // Handle validation failure
            var errorMessage = FormatErrorMessage(validationResult.ErrorMessage);
            
            switch (ValidationAction)
            {
                case ValidationAction.AddError:
                    context.AddError(errorMessage, null, fieldName: FieldName);
                    return TransformationResultHelper.Failure(errorMessage);

                case ValidationAction.AddWarning:
                    context.AddWarning(errorMessage, fieldName: FieldName);
                    return TransformationResultHelper.Success(record);

                case ValidationAction.SkipRecord:
                    context.SkipRecord();
                    return TransformationResultHelper.Skipped(record, errorMessage);

                case ValidationAction.SetDefault:
                    var outputRecord = record.Clone();
                    outputRecord.SetField(FieldName, GetDefaultValue());
                    context.UpdateStatistics(1, 1, DateTimeOffset.UtcNow - startTime);
                    return TransformationResultHelper.Success(outputRecord);

                case ValidationAction.RemoveField:
                    var cleanedRecord = record.Clone();
                    cleanedRecord.RemoveField(FieldName);
                    context.UpdateStatistics(1, 1, DateTimeOffset.UtcNow - startTime);
                    return TransformationResultHelper.Success(cleanedRecord);

                default:
                    return TransformationResultHelper.Failure($"Unknown validation action: {ValidationAction}");
            }
        }
        catch (Exception ex)
        {
            context.AddError($"Validation transformation failed for field '{FieldName}': {ex.Message}", ex, fieldName: FieldName);
            return TransformationResultHelper.Failure($"Validation transformation failed: {ex.Message}", ex);
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
    /// Validates a field value.
    /// </summary>
    /// <param name="value">The value to validate</param>
    /// <param name="record">The source record for context</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The validation result</returns>
    protected abstract Task<FieldValidationResult> ValidateValueAsync(object? value, DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the default value to use when validation fails and action is SetDefault.
    /// </summary>
    /// <returns>The default value</returns>
    protected virtual object? GetDefaultValue() => null;

    /// <summary>
    /// Formats the error message.
    /// </summary>
    /// <param name="validationError">The validation error</param>
    /// <returns>The formatted error message</returns>
    private string FormatErrorMessage(string validationError)
    {
        return ErrorMessage
            .Replace("{FieldName}", FieldName)
            .Replace("{ValidationError}", validationError);
    }
}

/// <summary>
/// Validation transformation for required fields.
/// </summary>
public class RequiredFieldValidation : BaseValidationTransformation
{
    /// <summary>
    /// Initializes a new instance of the RequiredFieldValidation class.
    /// </summary>
    /// <param name="fieldName">The field name to validate</param>
    /// <param name="validationAction">Action to take when validation fails</param>
    public RequiredFieldValidation(string fieldName, ValidationAction validationAction = ValidationAction.AddError)
        : base($"required_{fieldName}", $"Required Field {fieldName}", fieldName, validationAction)
    {
    }

    /// <inheritdoc />
    protected override Task<FieldValidationResult> ValidateValueAsync(object? value, DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken)
    {
        var isValid = value != null && !string.IsNullOrWhiteSpace(value.ToString());
        var errorMessage = isValid ? string.Empty : "Field is required but is null or empty";
        
        return Task.FromResult(new FieldValidationResult
        {
            IsValid = isValid,
            ErrorMessage = errorMessage
        });
    }
}

/// <summary>
/// Validation transformation for regex patterns.
/// </summary>
public class RegexValidation : BaseValidationTransformation
{
    private readonly Regex _regex;
    private readonly string _pattern;

    /// <summary>
    /// Initializes a new instance of the RegexValidation class.
    /// </summary>
    /// <param name="fieldName">The field name to validate</param>
    /// <param name="pattern">The regex pattern</param>
    /// <param name="validationAction">Action to take when validation fails</param>
    public RegexValidation(string fieldName, string pattern, ValidationAction validationAction = ValidationAction.AddError)
        : base($"regex_{fieldName}", $"Regex Validation {fieldName}", fieldName, validationAction)
    {
        _pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        _regex = new Regex(pattern);
    }

    /// <inheritdoc />
    protected override Task<FieldValidationResult> ValidateValueAsync(object? value, DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken)
    {
        var stringValue = value?.ToString() ?? string.Empty;
        var isValid = _regex.IsMatch(stringValue);
        var errorMessage = isValid ? string.Empty : $"Value '{stringValue}' does not match pattern '{_pattern}'";
        
        return Task.FromResult(new FieldValidationResult
        {
            IsValid = isValid,
            ErrorMessage = errorMessage
        });
    }

    /// <summary>
    /// Creates an email validation transformation.
    /// </summary>
    /// <param name="fieldName">The field name to validate</param>
    /// <param name="validationAction">Action to take when validation fails</param>
    /// <returns>Email validation transformation</returns>
    public static RegexValidation Email(string fieldName, ValidationAction validationAction = ValidationAction.AddError)
    {
        const string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return new RegexValidation(fieldName, emailPattern, validationAction);
    }

    /// <summary>
    /// Creates a phone number validation transformation.
    /// </summary>
    /// <param name="fieldName">The field name to validate</param>
    /// <param name="validationAction">Action to take when validation fails</param>
    /// <returns>Phone validation transformation</returns>
    public static RegexValidation Phone(string fieldName, ValidationAction validationAction = ValidationAction.AddError)
    {
        const string phonePattern = @"^\+?[\d\s\-\(\)]{10,}$";
        return new RegexValidation(fieldName, phonePattern, validationAction);
    }
}

/// <summary>
/// Validation transformation for numeric ranges.
/// </summary>
public class RangeValidation : BaseValidationTransformation
{
    private readonly decimal _minValue;
    private readonly decimal _maxValue;

    /// <summary>
    /// Initializes a new instance of the RangeValidation class.
    /// </summary>
    /// <param name="fieldName">The field name to validate</param>
    /// <param name="minValue">The minimum value</param>
    /// <param name="maxValue">The maximum value</param>
    /// <param name="validationAction">Action to take when validation fails</param>
    public RangeValidation(string fieldName, decimal minValue, decimal maxValue, ValidationAction validationAction = ValidationAction.AddError)
        : base($"range_{fieldName}", $"Range Validation {fieldName}", fieldName, validationAction)
    {
        _minValue = minValue;
        _maxValue = maxValue;
    }

    /// <inheritdoc />
    protected override Task<FieldValidationResult> ValidateValueAsync(object? value, DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            return Task.FromResult(new FieldValidationResult
            {
                IsValid = false,
                ErrorMessage = "Value is null"
            });
        }

        if (!decimal.TryParse(value.ToString(), out var numericValue))
        {
            return Task.FromResult(new FieldValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Value '{value}' is not a valid number"
            });
        }

        var isValid = numericValue >= _minValue && numericValue <= _maxValue;
        var errorMessage = isValid ? string.Empty : $"Value {numericValue} is not between {_minValue} and {_maxValue}";
        
        return Task.FromResult(new FieldValidationResult
        {
            IsValid = isValid,
            ErrorMessage = errorMessage
        });
    }
}

/// <summary>
/// Validation transformation for string length.
/// </summary>
public class LengthValidation : BaseValidationTransformation
{
    private readonly int _minLength;
    private readonly int _maxLength;

    /// <summary>
    /// Initializes a new instance of the LengthValidation class.
    /// </summary>
    /// <param name="fieldName">The field name to validate</param>
    /// <param name="minLength">The minimum length</param>
    /// <param name="maxLength">The maximum length</param>
    /// <param name="validationAction">Action to take when validation fails</param>
    public LengthValidation(string fieldName, int minLength, int maxLength, ValidationAction validationAction = ValidationAction.AddError)
        : base($"length_{fieldName}", $"Length Validation {fieldName}", fieldName, validationAction)
    {
        _minLength = minLength;
        _maxLength = maxLength;
    }

    /// <inheritdoc />
    protected override Task<FieldValidationResult> ValidateValueAsync(object? value, DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken)
    {
        var stringValue = value?.ToString() ?? string.Empty;
        var length = stringValue.Length;
        var isValid = length >= _minLength && length <= _maxLength;
        var errorMessage = isValid ? string.Empty : $"Length {length} is not between {_minLength} and {_maxLength}";
        
        return Task.FromResult(new FieldValidationResult
        {
            IsValid = isValid,
            ErrorMessage = errorMessage
        });
    }
}

/// <summary>
/// Result of field validation.
/// </summary>
public class FieldValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the error message if validation failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Actions to take when validation fails.
/// </summary>
public enum ValidationAction
{
    /// <summary>
    /// Add an error and fail the transformation.
    /// </summary>
    AddError,

    /// <summary>
    /// Add a warning but continue processing.
    /// </summary>
    AddWarning,

    /// <summary>
    /// Skip the record.
    /// </summary>
    SkipRecord,

    /// <summary>
    /// Set a default value.
    /// </summary>
    SetDefault,

    /// <summary>
    /// Remove the field.
    /// </summary>
    RemoveField
}
