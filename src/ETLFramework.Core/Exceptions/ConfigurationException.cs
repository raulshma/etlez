namespace ETLFramework.Core.Exceptions;

/// <summary>
/// Exception thrown when an error occurs in configuration loading, parsing, or validation.
/// </summary>
public class ConfigurationException : ETLFrameworkException
{
    /// <summary>
    /// Initializes a new instance of the ConfigurationException class.
    /// </summary>
    public ConfigurationException()
    {
        Component = "Configuration";
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public ConfigurationException(string message) : base(message)
    {
        Component = "Configuration";
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationException class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
        Component = "Configuration";
    }

    /// <summary>
    /// Gets or sets the configuration source (file path, connection string, etc.).
    /// </summary>
    public string? ConfigurationSource { get; set; }

    /// <summary>
    /// Gets or sets the configuration format (JSON, YAML, XML, etc.).
    /// </summary>
    public string? ConfigurationFormat { get; set; }

    /// <summary>
    /// Gets or sets the configuration section or property that caused the error.
    /// </summary>
    public string? ConfigurationSection { get; set; }

    /// <summary>
    /// Gets or sets the line number where the configuration error occurred (for file-based configurations).
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the column number where the configuration error occurred (for file-based configurations).
    /// </summary>
    public int? ColumnNumber { get; set; }

    /// <summary>
    /// Creates a configuration exception for file loading failures.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="filePath">The configuration file path</param>
    /// <param name="format">The configuration format</param>
    /// <returns>A new ConfigurationException instance</returns>
    public static ConfigurationException CreateFileLoadFailure(string message, string filePath, string format)
    {
        return new ConfigurationException(message)
        {
            ConfigurationSource = filePath,
            ConfigurationFormat = format,
            ErrorCode = "FILE_LOAD_FAILURE"
        };
    }

    /// <summary>
    /// Creates a configuration exception for parsing failures.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="source">The configuration source</param>
    /// <param name="format">The configuration format</param>
    /// <param name="lineNumber">The line number where the error occurred</param>
    /// <param name="columnNumber">The column number where the error occurred</param>
    /// <returns>A new ConfigurationException instance</returns>
    public static ConfigurationException CreateParseFailure(string message, string source, string format, int? lineNumber = null, int? columnNumber = null)
    {
        return new ConfigurationException(message)
        {
            ConfigurationSource = source,
            ConfigurationFormat = format,
            LineNumber = lineNumber,
            ColumnNumber = columnNumber,
            ErrorCode = "PARSE_FAILURE"
        };
    }

    /// <summary>
    /// Creates a configuration exception for validation failures.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="source">The configuration source</param>
    /// <param name="section">The configuration section that failed validation</param>
    /// <returns>A new ConfigurationException instance</returns>
    public static ConfigurationException CreateValidationFailure(string message, string source, string section)
    {
        return new ConfigurationException(message)
        {
            ConfigurationSource = source,
            ConfigurationSection = section,
            ErrorCode = "VALIDATION_FAILURE"
        };
    }

    /// <summary>
    /// Creates a configuration exception for missing required properties.
    /// </summary>
    /// <param name="propertyName">The missing property name</param>
    /// <param name="source">The configuration source</param>
    /// <param name="section">The configuration section</param>
    /// <returns>A new ConfigurationException instance</returns>
    public static ConfigurationException CreateMissingProperty(string propertyName, string source, string? section = null)
    {
        var message = $"Required configuration property '{propertyName}' is missing";
        if (!string.IsNullOrEmpty(section))
        {
            message += $" in section '{section}'";
        }

        return new ConfigurationException(message)
        {
            ConfigurationSource = source,
            ConfigurationSection = section,
            ErrorCode = "MISSING_PROPERTY"
        };
    }

    /// <summary>
    /// Creates a configuration exception for invalid property values.
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <param name="propertyValue">The invalid property value</param>
    /// <param name="expectedType">The expected property type or format</param>
    /// <param name="source">The configuration source</param>
    /// <returns>A new ConfigurationException instance</returns>
    public static ConfigurationException CreateInvalidProperty(string propertyName, object? propertyValue, string expectedType, string source)
    {
        var message = $"Configuration property '{propertyName}' has invalid value '{propertyValue}'. Expected: {expectedType}";

        var exception = new ConfigurationException(message)
        {
            ConfigurationSource = source,
            ErrorCode = "INVALID_PROPERTY"
        };

        exception.AddContext("PropertyName", propertyName);
        exception.AddContext("PropertyValue", propertyValue?.ToString() ?? "null");
        exception.AddContext("ExpectedType", expectedType);

        return exception;
    }
}
