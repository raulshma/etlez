using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using ETLFramework.Connectors.FileSystem;
using ETLFramework.Connectors.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ETLFramework.Connectors;

/// <summary>
/// Factory for creating connector instances based on configuration.
/// </summary>
public class ConnectorFactory : IConnectorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConnectorFactory> _logger;
    private readonly Dictionary<string, Func<IConnectorConfiguration, IConnector>> _connectorCreators;

    /// <summary>
    /// Initializes a new instance of the ConnectorFactory class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    /// <param name="logger">The logger instance</param>
    public ConnectorFactory(IServiceProvider serviceProvider, ILogger<ConnectorFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectorCreators = new Dictionary<string, Func<IConnectorConfiguration, IConnector>>(StringComparer.OrdinalIgnoreCase);

        RegisterBuiltInConnectors();
    }

    /// <summary>
    /// Creates a connector instance based on configuration.
    /// </summary>
    /// <param name="configuration">The connector configuration</param>
    /// <returns>A configured connector instance</returns>
    public IConnector CreateConnector(IConnectorConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(configuration.ConnectorType))
        {
            throw new ArgumentException("Connector type must be specified", nameof(configuration));
        }

        _logger.LogDebug("Creating connector: {ConnectorType} - {ConnectorName}",
            configuration.ConnectorType, configuration.Name);

        if (_connectorCreators.TryGetValue(configuration.ConnectorType, out var creator))
        {
            try
            {
                var connector = creator(configuration);
                _logger.LogInformation("Created connector: {ConnectorType} - {ConnectorName} (ID: {ConnectorId})",
                    configuration.ConnectorType, configuration.Name, connector.Id);
                return connector;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create connector: {ConnectorType} - {ConnectorName}",
                    configuration.ConnectorType, configuration.Name);

                throw ConnectorException.CreateFactoryFailure(
                    $"Failed to create {configuration.ConnectorType} connector: {ex.Message}",
                    configuration.ConnectorType);
            }
        }

        throw ConnectorException.CreateFactoryFailure(
            $"Unknown connector type: {configuration.ConnectorType}",
            configuration.ConnectorType);
    }

    /// <inheritdoc />
    public ISourceConnector<T> CreateSourceConnector<T>(IConnectorConfiguration configuration)
    {
        var connector = CreateConnector(configuration);
        
        if (connector is ISourceConnector<T> sourceConnector)
        {
            return sourceConnector;
        }

        throw ConnectorException.CreateFactoryFailure(
            $"Connector {configuration.ConnectorType} does not implement ISourceConnector<{typeof(T).Name}>",
            configuration.ConnectorType);
    }

    /// <inheritdoc />
    public IDestinationConnector<T> CreateDestinationConnector<T>(IConnectorConfiguration configuration)
    {
        var connector = CreateConnector(configuration);
        
        if (connector is IDestinationConnector<T> destinationConnector)
        {
            return destinationConnector;
        }

        throw ConnectorException.CreateFactoryFailure(
            $"Connector {configuration.ConnectorType} does not implement IDestinationConnector<{typeof(T).Name}>",
            configuration.ConnectorType);
    }

    /// <inheritdoc />
    public IBidirectionalConnector<TSource, TDestination> CreateBidirectionalConnector<TSource, TDestination>(IConnectorConfiguration configuration)
    {
        var connector = CreateConnector(configuration);

        if (connector is IBidirectionalConnector<TSource, TDestination> bidirectionalConnector)
        {
            return bidirectionalConnector;
        }

        throw ConnectorException.CreateFactoryFailure(
            $"Connector {configuration.ConnectorType} does not implement IBidirectionalConnector<{typeof(TSource).Name}, {typeof(TDestination).Name}>",
            configuration.ConnectorType);
    }

    /// <inheritdoc />
    public ConnectorConfigurationSchema? GetConfigurationSchema(string connectorType)
    {
        // TODO: Implement configuration schema retrieval
        // This would return schema information for the specified connector type
        return null;
    }

    /// <inheritdoc />
    public void RegisterConnectorType(
        string connectorType,
        Func<IConnectorConfiguration, ISourceConnector<object>>? sourceFactory = null,
        Func<IConnectorConfiguration, IDestinationConnector<object>>? destinationFactory = null)
    {
        if (string.IsNullOrWhiteSpace(connectorType))
            throw new ArgumentException("Connector type cannot be null or empty", nameof(connectorType));

        // For now, use the first available factory
        if (sourceFactory != null)
        {
            _connectorCreators[connectorType] = config => sourceFactory(config);
        }
        else if (destinationFactory != null)
        {
            _connectorCreators[connectorType] = config => destinationFactory(config);
        }

        _logger.LogInformation("Registered connector type: {ConnectorType}", connectorType);
    }

    /// <inheritdoc />
    public bool UnregisterConnectorType(string connectorType)
    {
        if (string.IsNullOrWhiteSpace(connectorType))
            return false;

        var removed = _connectorCreators.Remove(connectorType);

        if (removed)
        {
            _logger.LogInformation("Unregistered connector type: {ConnectorType}", connectorType);
        }

        return removed;
    }

    /// <summary>
    /// Registers a connector with a factory function.
    /// </summary>
    /// <typeparam name="T">The connector type</typeparam>
    /// <param name="connectorType">The connector type name</param>
    /// <param name="factory">The factory function</param>
    public void RegisterConnector<T>(string connectorType, Func<IConnectorConfiguration, T> factory) where T : class, IConnector
    {
        if (string.IsNullOrWhiteSpace(connectorType))
            throw new ArgumentException("Connector type cannot be null or empty", nameof(connectorType));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _connectorCreators[connectorType] = config => factory(config);

        _logger.LogInformation("Registered connector type: {ConnectorType} -> {ConnectorClass}",
            connectorType, typeof(T).Name);
    }

    /// <summary>
    /// Registers a connector with a factory function.
    /// </summary>
    /// <param name="connectorType">The connector type name</param>
    /// <param name="factory">The factory function</param>
    public void RegisterConnector(string connectorType, Func<IConnectorConfiguration, IConnector> factory)
    {
        if (string.IsNullOrWhiteSpace(connectorType))
            throw new ArgumentException("Connector type cannot be null or empty", nameof(connectorType));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _connectorCreators[connectorType] = factory;

        _logger.LogInformation("Registered connector type: {ConnectorType}", connectorType);
    }

    /// <summary>
    /// Checks if a connector type is supported.
    /// </summary>
    /// <param name="connectorType">The connector type name</param>
    /// <returns>True if supported, false otherwise</returns>
    public bool IsConnectorTypeSupported(string connectorType)
    {
        return !string.IsNullOrWhiteSpace(connectorType) &&
               _connectorCreators.ContainsKey(connectorType);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSupportedConnectorTypes()
    {
        return _connectorCreators.Keys.ToList();
    }



    /// <summary>
    /// Registers built-in connector types.
    /// </summary>
    private void RegisterBuiltInConnectors()
    {
        // File System Connectors
        RegisterConnector("CSV", config => new CsvConnector(config, 
            _serviceProvider.GetRequiredService<ILogger<CsvConnector>>()));
        
        RegisterConnector("JSON", config => new JsonConnector(config, 
            _serviceProvider.GetRequiredService<ILogger<JsonConnector>>()));
        
        RegisterConnector("XML", config => new XmlConnector(config,
            _serviceProvider.GetRequiredService<ILogger<XmlConnector>>()));

        // Database Connectors
        RegisterConnector("SQLite", config => new SqliteConnector(config,
            _serviceProvider.GetRequiredService<ILogger<SqliteConnector>>()));

        RegisterConnector("SqlServer", config => new SqlServerConnector(config,
            _serviceProvider.GetRequiredService<ILogger<SqlServerConnector>>()));

        RegisterConnector("MySQL", config => new MySqlDatabaseConnector(config,
            _serviceProvider.GetRequiredService<ILogger<MySqlDatabaseConnector>>()));

        _logger.LogInformation("Registered {ConnectorCount} built-in connector types", _connectorCreators.Count);
    }

    /// <summary>
    /// Creates a connector configuration for testing purposes.
    /// </summary>
    /// <param name="connectorType">The connector type</param>
    /// <param name="name">The connector name</param>
    /// <param name="connectionString">The connection string</param>
    /// <param name="properties">Additional properties</param>
    /// <returns>A connector configuration</returns>
    public static IConnectorConfiguration CreateTestConfiguration(
        string connectorType, 
        string name, 
        string connectionString,
        Dictionary<string, object>? properties = null)
    {
        return new TestConnectorConfiguration
        {
            Id = Guid.NewGuid(),
            Name = name,
            ConnectorType = connectorType,
            ConnectionString = connectionString,
            ConnectionProperties = properties ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Test implementation of connector configuration.
    /// </summary>
    private class TestConnectorConfiguration : IConnectorConfiguration
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ConnectorType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public IDictionary<string, object> ConnectionProperties { get; set; } = new Dictionary<string, object>();
        public IAuthenticationConfiguration? Authentication { get; set; }
        public TimeSpan? ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan? CommandTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public bool UseConnectionPooling { get; set; } = true;
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; } = 5;
        public IDictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
        public ISchemaMapping? SchemaMapping { get; set; }
        public int BatchSize { get; set; } = 1000;
        public bool EnableDetailedLogging { get; set; } = false;
        public IList<string> Tags { get; set; } = new List<string>();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

        public ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(Name))
                result.AddError("Name is required", nameof(Name));

            if (string.IsNullOrWhiteSpace(ConnectorType))
                result.AddError("ConnectorType is required", nameof(ConnectorType));

            if (string.IsNullOrWhiteSpace(ConnectionString))
                result.AddError("ConnectionString is required", nameof(ConnectionString));

            return result;
        }

        public T? GetConnectionProperty<T>(string key)
        {
            if (ConnectionProperties.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }

        public void SetConnectionProperty<T>(string key, T value)
        {
            ConnectionProperties[key] = value!;
        }

        public T? GetSetting<T>(string key)
        {
            if (Settings.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }

        public void SetSetting<T>(string key, T value)
        {
            Settings[key] = value!;
        }

        public IConnectorConfiguration Clone()
        {
            return new TestConnectorConfiguration
            {
                Id = Id,
                Name = Name,
                ConnectorType = ConnectorType,
                Description = Description,
                ConnectionString = ConnectionString,
                ConnectionProperties = new Dictionary<string, object>(ConnectionProperties),
                Authentication = Authentication,
                ConnectionTimeout = ConnectionTimeout,
                CommandTimeout = CommandTimeout,
                MaxRetryAttempts = MaxRetryAttempts,
                RetryDelay = RetryDelay,
                UseConnectionPooling = UseConnectionPooling,
                MaxPoolSize = MaxPoolSize,
                MinPoolSize = MinPoolSize,
                Settings = new Dictionary<string, object>(Settings),
                SchemaMapping = SchemaMapping,
                BatchSize = BatchSize,
                EnableDetailedLogging = EnableDetailedLogging,
                Tags = new List<string>(Tags),
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt
            };
        }
    }
}
