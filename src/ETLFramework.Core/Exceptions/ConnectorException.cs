namespace ETLFramework.Core.Exceptions;

/// <summary>
/// Exception thrown when an error occurs in a data connector.
/// </summary>
public class ConnectorException : ETLFrameworkException
{
    /// <summary>
    /// Initializes a new instance of the ConnectorException class.
    /// </summary>
    public ConnectorException()
    {
        Component = "Connector";
    }

    /// <summary>
    /// Initializes a new instance of the ConnectorException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public ConnectorException(string message) : base(message)
    {
        Component = "Connector";
    }

    /// <summary>
    /// Initializes a new instance of the ConnectorException class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public ConnectorException(string message, Exception innerException) : base(message, innerException)
    {
        Component = "Connector";
    }

    /// <summary>
    /// Gets or sets the connector identifier associated with this exception.
    /// </summary>
    public Guid? ConnectorId { get; set; }

    /// <summary>
    /// Gets or sets the connector type associated with this exception.
    /// </summary>
    public string? ConnectorType { get; set; }

    /// <summary>
    /// Gets or sets the connection string or source information.
    /// </summary>
    public string? ConnectionInfo { get; set; }

    /// <summary>
    /// Gets or sets the operation that was being performed when the exception occurred.
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// Creates a connector exception for connection failures.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="connectorType">The connector type</param>
    /// <param name="connectionInfo">The connection information</param>
    /// <returns>A new ConnectorException instance</returns>
    public static ConnectorException CreateConnectionFailure(string message, string connectorType, string connectionInfo)
    {
        return new ConnectorException(message)
        {
            ConnectorType = connectorType,
            ConnectionInfo = connectionInfo,
            Operation = "Connect",
            ErrorCode = "CONN_FAILURE"
        };
    }

    /// <summary>
    /// Creates a connector exception for read operations.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="connectorId">The connector identifier</param>
    /// <param name="connectorType">The connector type</param>
    /// <returns>A new ConnectorException instance</returns>
    public static ConnectorException CreateReadFailure(string message, Guid connectorId, string connectorType)
    {
        return new ConnectorException(message)
        {
            ConnectorId = connectorId,
            ConnectorType = connectorType,
            Operation = "Read",
            ErrorCode = "READ_FAILURE"
        };
    }

    /// <summary>
    /// Creates a connector exception for write operations.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="connectorId">The connector identifier</param>
    /// <param name="connectorType">The connector type</param>
    /// <returns>A new ConnectorException instance</returns>
    public static ConnectorException CreateWriteFailure(string message, Guid connectorId, string connectorType)
    {
        return new ConnectorException(message)
        {
            ConnectorId = connectorId,
            ConnectorType = connectorType,
            Operation = "Write",
            ErrorCode = "WRITE_FAILURE"
        };
    }

    /// <summary>
    /// Creates a connector exception for configuration errors.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="connectorType">The connector type</param>
    /// <returns>A new ConnectorException instance</returns>
    public static ConnectorException CreateConfigurationError(string message, string connectorType)
    {
        return new ConnectorException(message)
        {
            ConnectorType = connectorType,
            Operation = "Configure",
            ErrorCode = "CONFIG_ERROR"
        };
    }
}
