using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Interface for connector configuration that defines how to connect to and interact with data sources/destinations.
/// </summary>
public interface IConnectorConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this connector configuration.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the connector.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of connector (e.g., "SqlServer", "MySQL", "CSV", "JSON").
    /// </summary>
    string ConnectorType { get; set; }

    /// <summary>
    /// Gets or sets the description of this connector configuration.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Gets or sets the connection string or primary connection information.
    /// </summary>
    string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets additional connection properties specific to the connector type.
    /// </summary>
    IDictionary<string, object> ConnectionProperties { get; set; }

    /// <summary>
    /// Gets or sets authentication configuration for the connector.
    /// </summary>
    IAuthenticationConfiguration? Authentication { get; set; }

    /// <summary>
    /// Gets or sets the timeout for connection operations.
    /// </summary>
    TimeSpan? ConnectionTimeout { get; set; }

    /// <summary>
    /// Gets or sets the timeout for command/query operations.
    /// </summary>
    TimeSpan? CommandTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed operations.
    /// </summary>
    int MaxRetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the delay between retry attempts.
    /// </summary>
    TimeSpan RetryDelay { get; set; }

    /// <summary>
    /// Gets or sets whether to use connection pooling.
    /// </summary>
    bool UseConnectionPooling { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of connections in the pool.
    /// </summary>
    int MaxPoolSize { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of connections in the pool.
    /// </summary>
    int MinPoolSize { get; set; }

    /// <summary>
    /// Gets or sets connector-specific settings.
    /// </summary>
    IDictionary<string, object> Settings { get; set; }

    /// <summary>
    /// Gets or sets the schema mapping configuration for data type conversions.
    /// </summary>
    ISchemaMapping? SchemaMapping { get; set; }

    /// <summary>
    /// Gets or sets the batch size for bulk operations.
    /// </summary>
    int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets whether to enable detailed logging for this connector.
    /// </summary>
    bool EnableDetailedLogging { get; set; }

    /// <summary>
    /// Gets or sets custom tags for categorizing and filtering connectors.
    /// </summary>
    IList<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets when this configuration was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when this configuration was last modified.
    /// </summary>
    DateTimeOffset ModifiedAt { get; set; }

    /// <summary>
    /// Validates the connector configuration.
    /// </summary>
    /// <returns>Validation result</returns>
    ValidationResult Validate();

    /// <summary>
    /// Creates a deep copy of this configuration.
    /// </summary>
    /// <returns>A new instance with the same configuration values</returns>
    IConnectorConfiguration Clone();

    /// <summary>
    /// Gets a connection property value by key.
    /// </summary>
    /// <typeparam name="T">The type of the property value</typeparam>
    /// <param name="key">The property key</param>
    /// <returns>The property value, or default if not found</returns>
    T? GetConnectionProperty<T>(string key);

    /// <summary>
    /// Sets a connection property value.
    /// </summary>
    /// <typeparam name="T">The type of the property value</typeparam>
    /// <param name="key">The property key</param>
    /// <param name="value">The property value</param>
    void SetConnectionProperty<T>(string key, T value);

    /// <summary>
    /// Gets a setting value by key.
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">The setting key</param>
    /// <returns>The setting value, or default if not found</returns>
    T? GetSetting<T>(string key);

    /// <summary>
    /// Sets a setting value.
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">The setting key</param>
    /// <param name="value">The setting value</param>
    void SetSetting<T>(string key, T value);
}
