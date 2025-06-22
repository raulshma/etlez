namespace ETLFramework.Core.Exceptions;

/// <summary>
/// Exception thrown when an error occurs during data transformation.
/// </summary>
public class TransformationException : ETLFrameworkException
{
    /// <summary>
    /// Initializes a new instance of the TransformationException class.
    /// </summary>
    public TransformationException()
    {
        Component = "Transformation";
    }

    /// <summary>
    /// Initializes a new instance of the TransformationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public TransformationException(string message) : base(message)
    {
        Component = "Transformation";
    }

    /// <summary>
    /// Initializes a new instance of the TransformationException class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public TransformationException(string message, Exception innerException) : base(message, innerException)
    {
        Component = "Transformation";
    }

    /// <summary>
    /// Gets or sets the transformation rule identifier associated with this exception.
    /// </summary>
    public Guid? RuleId { get; set; }

    /// <summary>
    /// Gets or sets the transformation rule name associated with this exception.
    /// </summary>
    public string? RuleName { get; set; }

    /// <summary>
    /// Gets or sets the transformation rule type associated with this exception.
    /// </summary>
    public string? RuleType { get; set; }

    /// <summary>
    /// Gets or sets the field name that caused the transformation error.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Gets or sets the field value that caused the transformation error.
    /// </summary>
    public object? FieldValue { get; set; }

    /// <summary>
    /// Gets or sets the record identifier being transformed when the exception occurred.
    /// </summary>
    public Guid? RecordId { get; set; }

    /// <summary>
    /// Creates a transformation exception for rule execution failures.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="ruleId">The transformation rule identifier</param>
    /// <param name="ruleName">The transformation rule name</param>
    /// <param name="ruleType">The transformation rule type</param>
    /// <returns>A new TransformationException instance</returns>
    public static TransformationException CreateRuleFailure(string message, Guid ruleId, string ruleName, string ruleType)
    {
        return new TransformationException(message)
        {
            RuleId = ruleId,
            RuleName = ruleName,
            RuleType = ruleType,
            ErrorCode = "RULE_FAILURE"
        };
    }

    /// <summary>
    /// Creates a transformation exception for field transformation failures.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="fieldName">The field name</param>
    /// <param name="fieldValue">The field value</param>
    /// <param name="ruleType">The transformation rule type</param>
    /// <returns>A new TransformationException instance</returns>
    public static TransformationException CreateFieldFailure(string message, string fieldName, object? fieldValue, string ruleType)
    {
        return new TransformationException(message)
        {
            FieldName = fieldName,
            FieldValue = fieldValue,
            RuleType = ruleType,
            ErrorCode = "FIELD_FAILURE"
        };
    }

    /// <summary>
    /// Creates a transformation exception for data type conversion failures.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="fieldName">The field name</param>
    /// <param name="sourceType">The source data type</param>
    /// <param name="targetType">The target data type</param>
    /// <returns>A new TransformationException instance</returns>
    public static TransformationException CreateTypeConversionFailure(string message, string fieldName, Type sourceType, Type targetType)
    {
        var exception = new TransformationException(message)
        {
            FieldName = fieldName,
            ErrorCode = "TYPE_CONVERSION_FAILURE"
        };
        
        exception.AddContext("SourceType", sourceType.Name);
        exception.AddContext("TargetType", targetType.Name);
        
        return exception;
    }

    /// <summary>
    /// Creates a transformation exception for validation failures.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="fieldName">The field name</param>
    /// <param name="validationRule">The validation rule that failed</param>
    /// <returns>A new TransformationException instance</returns>
    public static TransformationException CreateValidationFailure(string message, string fieldName, string validationRule)
    {
        var exception = new TransformationException(message)
        {
            FieldName = fieldName,
            ErrorCode = "VALIDATION_FAILURE"
        };
        
        exception.AddContext("ValidationRule", validationRule);
        
        return exception;
    }
}
