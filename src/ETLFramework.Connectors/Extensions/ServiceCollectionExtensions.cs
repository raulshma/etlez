using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ETLFramework.Core.Interfaces;
using ETLFramework.Connectors.FileSystem;

namespace ETLFramework.Connectors.Extensions;

/// <summary>
/// Extension methods for configuring ETL Framework connector services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ETL Framework connector services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddETLConnectors(this IServiceCollection services)
    {
        // Register connector factory
        services.TryAddSingleton<IConnectorFactory, ConnectorFactory>();

        // Register individual connector types as transient
        services.TryAddTransient<CsvConnector>();
        services.TryAddTransient<JsonConnector>();
        services.TryAddTransient<XmlConnector>();

        return services;
    }

    /// <summary>
    /// Adds file system connectors to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFileSystemConnectors(this IServiceCollection services)
    {
        services.TryAddTransient<CsvConnector>();
        services.TryAddTransient<JsonConnector>();
        services.TryAddTransient<XmlConnector>();

        return services;
    }

    /// <summary>
    /// Adds a custom connector factory implementation.
    /// </summary>
    /// <typeparam name="TFactory">The type of connector factory</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConnectorFactory<TFactory>(this IServiceCollection services)
        where TFactory : class, IConnectorFactory
    {
        services.TryAddSingleton<IConnectorFactory, TFactory>();
        return services;
    }

    /// <summary>
    /// Adds a custom connector factory instance.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="factory">The connector factory instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConnectorFactory(this IServiceCollection services, IConnectorFactory factory)
    {
        services.TryAddSingleton(factory);
        return services;
    }

    /// <summary>
    /// Configures connector services with custom options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureConnectors">Action to configure connector services</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddETLConnectors(this IServiceCollection services, Action<IServiceCollection> configureConnectors)
    {
        // Add base connector services
        services.AddETLConnectors();

        // Allow custom connector configuration
        configureConnectors(services);

        return services;
    }
}
