using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Connectors;

/// <summary>
/// Base implementation for all connectors providing common functionality.
/// </summary>
public abstract class BaseConnector : IConnector
{
    private readonly ILogger _logger;
    private ConnectionStatus _status;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the BaseConnector class.
    /// </summary>
    /// <param name="id">The connector identifier</param>
    /// <param name="name">The connector name</param>
    /// <param name="connectorType">The connector type</param>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    protected BaseConnector(
        Guid id,
        string name,
        string connectorType,
        IConnectorConfiguration configuration,
        ILogger logger)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ConnectorType = connectorType ?? throw new ArgumentNullException(nameof(connectorType));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _status = ConnectionStatus.Closed;
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string ConnectorType { get; }

    /// <inheritdoc />
    public IConnectorConfiguration Configuration { get; }

    /// <inheritdoc />
    public ConnectionStatus Status => _status;

    /// <summary>
    /// Gets the logger instance for this connector.
    /// </summary>
    protected ILogger Logger => _logger;

    /// <inheritdoc />
    public async Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation("Testing connection for connector: {ConnectorName} ({ConnectorType})", Name, ConnectorType);

            var startTime = DateTimeOffset.UtcNow;
            var result = await TestConnectionInternalAsync(cancellationToken);
            var responseTime = DateTimeOffset.UtcNow - startTime;

            result.ResponseTime = responseTime;

            _logger.LogInformation("Connection test completed for {ConnectorName}: {IsSuccessful} (Response time: {ResponseTime}ms)",
                Name, result.IsSuccessful, responseTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for connector: {ConnectorName}", Name);
            
            return new ConnectionTestResult
            {
                IsSuccessful = false,
                Message = $"Connection test failed: {ex.Message}",
                ResponseTime = TimeSpan.Zero
            };
        }
    }

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_status == ConnectionStatus.Open)
        {
            _logger.LogDebug("Connection already open for connector: {ConnectorName}", Name);
            return;
        }

        if (_status == ConnectionStatus.Opening)
        {
            _logger.LogDebug("Connection already opening for connector: {ConnectorName}", Name);
            return;
        }

        try
        {
            _logger.LogInformation("Opening connection for connector: {ConnectorName} ({ConnectorType})", Name, ConnectorType);
            _status = ConnectionStatus.Opening;

            await OpenInternalAsync(cancellationToken);

            _status = ConnectionStatus.Open;
            _logger.LogInformation("Connection opened successfully for connector: {ConnectorName}", Name);
        }
        catch (Exception ex)
        {
            _status = ConnectionStatus.Failed;
            _logger.LogError(ex, "Failed to open connection for connector: {ConnectorName}", Name);
            
            throw ConnectorException.CreateConnectionFailure(
                $"Failed to open connection: {ex.Message}",
                ConnectorType,
                Configuration.ConnectionString);
        }
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || _status == ConnectionStatus.Closed)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Closing connection for connector: {ConnectorName}", Name);
            _status = ConnectionStatus.Closing;

            await CloseInternalAsync(cancellationToken);

            _status = ConnectionStatus.Closed;
            _logger.LogInformation("Connection closed successfully for connector: {ConnectorName}", Name);
        }
        catch (Exception ex)
        {
            _status = ConnectionStatus.Failed;
            _logger.LogError(ex, "Error closing connection for connector: {ConnectorName}", Name);
            
            // Don't throw on close - just log the error
        }
    }

    /// <inheritdoc />
    public virtual Task<ValidationResult> ValidateConfigurationAsync()
    {
        var result = new ValidationResult { IsValid = true };

        // Validate basic configuration
        if (string.IsNullOrWhiteSpace(Configuration.Name))
        {
            result.AddError("Connector name is required", nameof(Configuration.Name));
        }

        if (string.IsNullOrWhiteSpace(Configuration.ConnectorType))
        {
            result.AddError("Connector type is required", nameof(Configuration.ConnectorType));
        }

        if (string.IsNullOrWhiteSpace(Configuration.ConnectionString))
        {
            result.AddError("Connection string is required", nameof(Configuration.ConnectionString));
        }

        // Allow derived classes to add additional validation
        ValidateConfigurationInternal(result);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<ConnectorMetadata> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Getting metadata for connector: {ConnectorName}", Name);
            return await GetMetadataInternalAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for connector: {ConnectorName}", Name);
            throw;
        }
    }

    /// <summary>
    /// Tests the connection to the data source. Must be implemented by derived classes.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Connection test result</returns>
    protected abstract Task<ConnectionTestResult> TestConnectionInternalAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens the connection to the data source. Must be implemented by derived classes.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    protected abstract Task OpenInternalAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Closes the connection to the data source. Must be implemented by derived classes.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    protected abstract Task CloseInternalAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets metadata about the data source. Must be implemented by derived classes.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Connector metadata</returns>
    protected abstract Task<ConnectorMetadata> GetMetadataInternalAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Validates connector-specific configuration. Override in derived classes for additional validation.
    /// </summary>
    /// <param name="result">The validation result to add errors to</param>
    protected virtual void ValidateConfigurationInternal(ValidationResult result)
    {
        // Default implementation does nothing - override in derived classes
    }

    /// <summary>
    /// Ensures the connector is connected before performing operations.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    protected async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (_status != ConnectionStatus.Open)
        {
            await OpenAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Throws an exception if the connector has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <summary>
    /// Disposes the connector and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the connector and releases resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during connector disposal: {ConnectorName}", Name);
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Returns a string representation of the connector.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        return $"{ConnectorType}Connector[{Name}, Status={_status}]";
    }
}
