namespace ETLFramework.Core.Exceptions;

/// <summary>
/// Base exception class for all ETL Framework exceptions.
/// </summary>
public class ETLFrameworkException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ETLFrameworkException class.
    /// </summary>
    public ETLFrameworkException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the ETLFrameworkException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public ETLFrameworkException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ETLFrameworkException class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public ETLFrameworkException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the error code associated with this exception.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets additional context information about the exception.
    /// </summary>
    public IDictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the component or source that caused the exception.
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// Gets or sets when the exception occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the severity level of the exception.
    /// </summary>
    public ExceptionSeverity Severity { get; set; } = ExceptionSeverity.Error;

    /// <summary>
    /// Adds context information to the exception.
    /// </summary>
    /// <param name="key">The context key</param>
    /// <param name="value">The context value</param>
    public void AddContext(string key, object value)
    {
        Context[key] = value;
    }

    /// <summary>
    /// Gets context information from the exception.
    /// </summary>
    /// <typeparam name="T">The type of the context value</typeparam>
    /// <param name="key">The context key</param>
    /// <returns>The context value, or default if not found</returns>
    public T? GetContext<T>(string key)
    {
        if (Context.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Returns a string representation of the exception with additional context.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        var baseString = base.ToString();
        var contextInfo = Context.Count > 0 ? $"\nContext: {string.Join(", ", Context.Select(kvp => $"{kvp.Key}={kvp.Value}"))}" : "";
        var componentInfo = !string.IsNullOrEmpty(Component) ? $"\nComponent: {Component}" : "";
        var errorCodeInfo = !string.IsNullOrEmpty(ErrorCode) ? $"\nError Code: {ErrorCode}" : "";
        var severityInfo = $"\nSeverity: {Severity}";
        var timestampInfo = $"\nTimestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}";

        return $"{baseString}{componentInfo}{errorCodeInfo}{severityInfo}{timestampInfo}{contextInfo}";
    }
}

/// <summary>
/// Represents the severity level of an exception.
/// </summary>
public enum ExceptionSeverity
{
    /// <summary>
    /// Low severity - minor issues that don't affect functionality.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity - issues that may affect some functionality.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity - issues that significantly affect functionality.
    /// </summary>
    High,

    /// <summary>
    /// Error severity - standard error level.
    /// </summary>
    Error,

    /// <summary>
    /// Critical severity - severe issues that may cause system failure.
    /// </summary>
    Critical
}
