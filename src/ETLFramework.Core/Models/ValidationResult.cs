namespace ETLFramework.Core.Models;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the ValidationResult class.
    /// </summary>
    public ValidationResult()
    {
        Errors = new List<ValidationError>();
        Warnings = new List<ValidationWarning>();
    }

    /// <summary>
    /// Gets or sets whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the collection of validation errors.
    /// </summary>
    public IList<ValidationError> Errors { get; set; }

    /// <summary>
    /// Gets or sets the collection of validation warnings.
    /// </summary>
    public IList<ValidationWarning> Warnings { get; set; }

    /// <summary>
    /// Gets or sets additional context information about the validation.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Gets or sets when the validation was performed.
    /// </summary>
    public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Adds a validation error.
    /// </summary>
    /// <param name="error">The validation error to add</param>
    public void AddError(ValidationError error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// Adds a validation error with the specified message and property.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="propertyName">The name of the property that failed validation</param>
    /// <param name="errorCode">Optional error code</param>
    public void AddError(string message, string? propertyName = null, string? errorCode = null)
    {
        AddError(new ValidationError
        {
            Message = message,
            PropertyName = propertyName,
            ErrorCode = errorCode
        });
    }

    /// <summary>
    /// Adds a validation warning.
    /// </summary>
    /// <param name="warning">The validation warning to add</param>
    public void AddWarning(ValidationWarning warning)
    {
        Warnings.Add(warning);
    }

    /// <summary>
    /// Adds a validation warning with the specified message and property.
    /// </summary>
    /// <param name="message">The warning message</param>
    /// <param name="propertyName">The name of the property that generated the warning</param>
    /// <param name="warningCode">Optional warning code</param>
    public void AddWarning(string message, string? propertyName = null, string? warningCode = null)
    {
        AddWarning(new ValidationWarning
        {
            Message = message,
            PropertyName = propertyName,
            WarningCode = warningCode
        });
    }

    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    /// <param name="other">The validation result to merge</param>
    public void Merge(ValidationResult other)
    {
        foreach (var error in other.Errors)
        {
            Errors.Add(error);
        }

        foreach (var warning in other.Warnings)
        {
            Warnings.Add(warning);
        }

        if (!other.IsValid)
        {
            IsValid = false;
        }
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result with the specified error.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="propertyName">The name of the property that failed validation</param>
    /// <param name="errorCode">Optional error code</param>
    /// <returns>A validation result indicating failure</returns>
    public static ValidationResult Failure(string errorMessage, string? propertyName = null, string? errorCode = null)
    {
        var result = new ValidationResult();
        result.AddError(errorMessage, propertyName, errorCode);
        return result;
    }

    /// <summary>
    /// Returns a string representation of this validation result.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        if (IsValid)
        {
            return $"ValidationResult[Valid, Warnings={Warnings.Count}]";
        }
        return $"ValidationResult[Invalid, Errors={Errors.Count}, Warnings={Warnings.Count}]";
    }
}

/// <summary>
/// Represents a validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the property that failed validation.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets additional context information about the error.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Gets or sets the severity of the error.
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;

    /// <summary>
    /// Returns a string representation of this validation error.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        var property = !string.IsNullOrEmpty(PropertyName) ? $" ({PropertyName})" : "";
        var code = !string.IsNullOrEmpty(ErrorCode) ? $" [{ErrorCode}]" : "";
        return $"{Severity}: {Message}{property}{code}";
    }
}

/// <summary>
/// Represents a validation warning.
/// </summary>
public class ValidationWarning
{
    /// <summary>
    /// Gets or sets the warning message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the property that generated the warning.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the warning code.
    /// </summary>
    public string? WarningCode { get; set; }

    /// <summary>
    /// Gets or sets additional context information about the warning.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Returns a string representation of this validation warning.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        var property = !string.IsNullOrEmpty(PropertyName) ? $" ({PropertyName})" : "";
        var code = !string.IsNullOrEmpty(WarningCode) ? $" [{WarningCode}]" : "";
        return $"Warning: {Message}{property}{code}";
    }
}

/// <summary>
/// Represents the severity of a validation issue.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Information level.
    /// </summary>
    Info,

    /// <summary>
    /// Warning level.
    /// </summary>
    Warning,

    /// <summary>
    /// Error level.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error level.
    /// </summary>
    Critical
}
