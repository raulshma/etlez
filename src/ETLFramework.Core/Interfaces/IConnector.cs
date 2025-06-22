using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Base interface for all data connectors in the ETL framework.
/// Provides common functionality for connecting to and interacting with data sources and destinations.
/// </summary>
public interface IConnector : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this connector.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of the connector.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type of connector (e.g., "SqlServer", "MySQL", "CSV", "JSON").
    /// </summary>
    string ConnectorType { get; }

    /// <summary>
    /// Gets the configuration for this connector.
    /// </summary>
    IConnectorConfiguration Configuration { get; }

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    ConnectionStatus Status { get; }

    /// <summary>
    /// Tests the connection to the data source/destination.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result of the connection test</returns>
    Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the connection to the data source/destination.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the connection to the data source/destination.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the connector configuration.
    /// </summary>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateConfigurationAsync();

    /// <summary>
    /// Gets metadata about the data source/destination.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Metadata information</returns>
    Task<ConnectorMetadata> GetMetadataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for connectors that can read data from a source.
/// </summary>
/// <typeparam name="T">The type of data records this connector produces</typeparam>
public interface ISourceConnector<T> : IConnector
{
    /// <summary>
    /// Reads data from the source asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Async enumerable of data records</returns>
    IAsyncEnumerable<T> ReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads data from the source in batches.
    /// </summary>
    /// <param name="batchSize">The size of each batch</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Async enumerable of data record batches</returns>
    IAsyncEnumerable<IEnumerable<T>> ReadBatchAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the estimated count of records that will be read.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Estimated record count, or null if unknown</returns>
    Task<long?> GetEstimatedRecordCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the schema information for the data that will be read.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Schema information</returns>
    Task<DataSchema> GetSchemaAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for connectors that can write data to a destination.
/// </summary>
/// <typeparam name="T">The type of data records this connector accepts</typeparam>
public interface IDestinationConnector<T> : IConnector
{
    /// <summary>
    /// Writes data to the destination asynchronously.
    /// </summary>
    /// <param name="data">The data records to write</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result of the write operation</returns>
    Task<WriteResult> WriteAsync(IAsyncEnumerable<T> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a batch of data to the destination.
    /// </summary>
    /// <param name="batch">The batch of data records to write</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result of the write operation</returns>
    Task<WriteResult> WriteBatchAsync(IEnumerable<T> batch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prepares the destination for writing (e.g., creating tables, truncating data).
    /// </summary>
    /// <param name="schema">The schema of the data to be written</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task PrepareAsync(DataSchema schema, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes the write operation (e.g., committing transactions, closing files).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task FinalizeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported write modes for this connector.
    /// </summary>
    WriteMode[] SupportedWriteModes { get; }
}
