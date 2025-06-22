using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Core.Implementations;

/// <summary>
/// Implementation of the ETL Framework builder.
/// </summary>
public class ETLFrameworkBuilder : IETLFrameworkBuilder
{
    private readonly IServiceCollection _services;
    private readonly ILogger<ETLFrameworkBuilder> _logger;
    private readonly FrameworkOptions _options;

    /// <summary>
    /// Initializes a new instance of the ETLFrameworkBuilder class.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="logger">The logger</param>
    /// <param name="options">The framework options</param>
    public ETLFrameworkBuilder(
        IServiceCollection services,
        ILogger<ETLFrameworkBuilder> logger,
        FrameworkOptions? options = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FrameworkOptions();
    }

    /// <inheritdoc />
    public IETLFrameworkBuilder AddConnector<T>(string connectorType) where T : class, IConnector
    {
        try
        {
            // Register connector with DI container
            _services.AddTransient<T>();

            // Register connector factory delegate
            _services.AddSingleton<Func<IConnectorConfiguration, T>>(provider =>
                config => ActivatorUtilities.CreateInstance<T>(provider, config));

            // Register connector type mapping
            _services.Configure<ConnectorTypeRegistry>(registry =>
                registry.RegisterConnector(connectorType, typeof(T)));

            _logger.LogInformation("Registered connector: {ConnectorType} -> {ConnectorClass}", 
                connectorType, typeof(T).Name);

            return this;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register connector: {ConnectorType}", connectorType);
            throw;
        }
    }

    /// <inheritdoc />
    public IETLFrameworkBuilder AddTransformation<T>() where T : class, ITransformationRule
    {
        try
        {
            // Register transformation with DI container
            _services.AddTransient<T>();

            // Register transformation factory delegate
            _services.AddSingleton<Func<ITransformationRuleConfiguration, T>>(provider =>
                config => ActivatorUtilities.CreateInstance<T>(provider));

            // Register transformation type mapping
            _services.Configure<TransformationTypeRegistry>(registry =>
                registry.RegisterTransformation(typeof(T).Name, typeof(T)));

            _logger.LogInformation("Registered transformation: {TransformationType}", typeof(T).Name);

            return this;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register transformation: {TransformationType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public IETLFrameworkBuilder AddConfigurationProvider<T>() where T : class, IConfigurationProvider
    {
        try
        {
            _services.AddSingleton<T>();
            _services.AddSingleton<IConfigurationProvider, T>();

            _logger.LogInformation("Registered configuration provider: {ProviderType}", typeof(T).Name);

            return this;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register configuration provider: {ProviderType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public IETLFrameworkBuilder AddPipelineStage<T>() where T : class, IPipelineStage
    {
        try
        {
            _services.AddTransient<T>();

            // Register stage factory delegate
            _services.AddSingleton<Func<IStageConfiguration, T>>(provider =>
                config => ActivatorUtilities.CreateInstance<T>(provider, config));

            _logger.LogInformation("Registered pipeline stage: {StageType}", typeof(T).Name);

            return this;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register pipeline stage: {StageType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public IETLFrameworkBuilder AddMessageBroker<T>(string brokerType) where T : class
    {
        try
        {
            _services.AddSingleton<T>();

            // Register broker type mapping
            _services.Configure<MessageBrokerTypeRegistry>(registry =>
                registry.RegisterBroker(brokerType, typeof(T)));

            _logger.LogInformation("Registered message broker: {BrokerType} -> {BrokerClass}", 
                brokerType, typeof(T).Name);

            return this;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register message broker: {BrokerType}", brokerType);
            throw;
        }
    }

    /// <inheritdoc />
    public IETLFrameworkBuilder Configure(Action<FrameworkOptions> configureAction)
    {
        try
        {
            configureAction(_options);
            _services.Configure<FrameworkOptions>(options => configureAction(options));

            _logger.LogInformation("Configured framework options");

            return this;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure framework options");
            throw;
        }
    }
}

/// <summary>
/// Registry for connector type mappings.
/// </summary>
public class ConnectorTypeRegistry
{
    private readonly Dictionary<string, Type> _connectorTypes = new();

    /// <summary>
    /// Registers a connector type.
    /// </summary>
    /// <param name="connectorType">The connector type identifier</param>
    /// <param name="implementationType">The implementation type</param>
    public void RegisterConnector(string connectorType, Type implementationType)
    {
        _connectorTypes[connectorType] = implementationType;
    }

    /// <summary>
    /// Gets the implementation type for a connector type.
    /// </summary>
    /// <param name="connectorType">The connector type identifier</param>
    /// <returns>The implementation type</returns>
    public Type? GetConnectorType(string connectorType)
    {
        return _connectorTypes.TryGetValue(connectorType, out var type) ? type : null;
    }

    /// <summary>
    /// Gets all registered connector types.
    /// </summary>
    /// <returns>Dictionary of connector type mappings</returns>
    public IReadOnlyDictionary<string, Type> GetAllConnectorTypes()
    {
        return _connectorTypes.AsReadOnly();
    }
}

/// <summary>
/// Registry for transformation type mappings.
/// </summary>
public class TransformationTypeRegistry
{
    private readonly Dictionary<string, Type> _transformationTypes = new();

    /// <summary>
    /// Registers a transformation type.
    /// </summary>
    /// <param name="transformationType">The transformation type identifier</param>
    /// <param name="implementationType">The implementation type</param>
    public void RegisterTransformation(string transformationType, Type implementationType)
    {
        _transformationTypes[transformationType] = implementationType;
    }

    /// <summary>
    /// Gets the implementation type for a transformation type.
    /// </summary>
    /// <param name="transformationType">The transformation type identifier</param>
    /// <returns>The implementation type</returns>
    public Type? GetTransformationType(string transformationType)
    {
        return _transformationTypes.TryGetValue(transformationType, out var type) ? type : null;
    }

    /// <summary>
    /// Gets all registered transformation types.
    /// </summary>
    /// <returns>Dictionary of transformation type mappings</returns>
    public IReadOnlyDictionary<string, Type> GetAllTransformationTypes()
    {
        return _transformationTypes.AsReadOnly();
    }
}

/// <summary>
/// Registry for message broker type mappings.
/// </summary>
public class MessageBrokerTypeRegistry
{
    private readonly Dictionary<string, Type> _brokerTypes = new();

    /// <summary>
    /// Registers a message broker type.
    /// </summary>
    /// <param name="brokerType">The broker type identifier</param>
    /// <param name="implementationType">The implementation type</param>
    public void RegisterBroker(string brokerType, Type implementationType)
    {
        _brokerTypes[brokerType] = implementationType;
    }

    /// <summary>
    /// Gets the implementation type for a broker type.
    /// </summary>
    /// <param name="brokerType">The broker type identifier</param>
    /// <returns>The implementation type</returns>
    public Type? GetBrokerType(string brokerType)
    {
        return _brokerTypes.TryGetValue(brokerType, out var type) ? type : null;
    }

    /// <summary>
    /// Gets all registered broker types.
    /// </summary>
    /// <returns>Dictionary of broker type mappings</returns>
    public IReadOnlyDictionary<string, Type> GetAllBrokerTypes()
    {
        return _brokerTypes.AsReadOnly();
    }
}
