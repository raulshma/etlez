using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Factory interface for creating connector instances based on configuration.
/// </summary>
public interface IConnectorFactory
{
    /// <summary>
    /// Creates a source connector instance.
    /// </summary>
    /// <typeparam name="T">The type of data records the connector will produce</typeparam>
    /// <param name="configuration">The connector configuration</param>
    /// <returns>A configured source connector instance</returns>
    ISourceConnector<T> CreateSourceConnector<T>(IConnectorConfiguration configuration);

    /// <summary>
    /// Creates a destination connector instance.
    /// </summary>
    /// <typeparam name="T">The type of data records the connector will accept</typeparam>
    /// <param name="configuration">The connector configuration</param>
    /// <returns>A configured destination connector instance</returns>
    IDestinationConnector<T> CreateDestinationConnector<T>(IConnectorConfiguration configuration);

    /// <summary>
    /// Creates a connector instance that can act as both source and destination.
    /// </summary>
    /// <typeparam name="TSource">The type of data records when used as a source</typeparam>
    /// <typeparam name="TDestination">The type of data records when used as a destination</typeparam>
    /// <param name="configuration">The connector configuration</param>
    /// <returns>A configured connector instance</returns>
    IBidirectionalConnector<TSource, TDestination> CreateBidirectionalConnector<TSource, TDestination>(IConnectorConfiguration configuration);

    /// <summary>
    /// Gets all supported connector types.
    /// </summary>
    /// <returns>Collection of supported connector type names</returns>
    IEnumerable<string> GetSupportedConnectorTypes();

    /// <summary>
    /// Gets the configuration schema for a specific connector type.
    /// </summary>
    /// <param name="connectorType">The connector type name</param>
    /// <returns>Configuration schema, or null if connector type is not supported</returns>
    ConnectorConfigurationSchema? GetConfigurationSchema(string connectorType);

    /// <summary>
    /// Registers a connector type with the factory.
    /// </summary>
    /// <param name="connectorType">The connector type name</param>
    /// <param name="sourceFactory">Factory function for creating source connectors</param>
    /// <param name="destinationFactory">Factory function for creating destination connectors</param>
    void RegisterConnectorType(
        string connectorType,
        Func<IConnectorConfiguration, ISourceConnector<object>>? sourceFactory = null,
        Func<IConnectorConfiguration, IDestinationConnector<object>>? destinationFactory = null);

    /// <summary>
    /// Unregisters a connector type from the factory.
    /// </summary>
    /// <param name="connectorType">The connector type name to unregister</param>
    /// <returns>True if the connector type was unregistered, false if it wasn't registered</returns>
    bool UnregisterConnectorType(string connectorType);
}

/// <summary>
/// Interface for connectors that can act as both source and destination.
/// </summary>
/// <typeparam name="TSource">The type of data records when used as a source</typeparam>
/// <typeparam name="TDestination">The type of data records when used as a destination</typeparam>
public interface IBidirectionalConnector<TSource, TDestination> : ISourceConnector<TSource>, IDestinationConnector<TDestination>
{
    /// <summary>
    /// Gets a value indicating whether the connector is currently configured as a source.
    /// </summary>
    bool IsSourceMode { get; }

    /// <summary>
    /// Gets a value indicating whether the connector is currently configured as a destination.
    /// </summary>
    bool IsDestinationMode { get; }

    /// <summary>
    /// Switches the connector to source mode.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task SwitchToSourceModeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches the connector to destination mode.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task SwitchToDestinationModeAsync(CancellationToken cancellationToken = default);
}
