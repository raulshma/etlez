using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Interface for building and configuring the ETL Framework.
/// </summary>
public interface IETLFrameworkBuilder
{
    /// <summary>
    /// Adds a custom connector to the framework.
    /// </summary>
    /// <typeparam name="T">The connector type</typeparam>
    /// <param name="connectorType">The connector type identifier</param>
    /// <returns>The framework builder for method chaining</returns>
    IETLFrameworkBuilder AddConnector<T>(string connectorType) where T : class, IConnector;

    /// <summary>
    /// Adds a custom transformation to the framework.
    /// </summary>
    /// <typeparam name="T">The transformation type</typeparam>
    /// <returns>The framework builder for method chaining</returns>
    IETLFrameworkBuilder AddTransformation<T>() where T : class, ITransformationRule;

    /// <summary>
    /// Adds a custom configuration provider to the framework.
    /// </summary>
    /// <typeparam name="T">The configuration provider type</typeparam>
    /// <returns>The framework builder for method chaining</returns>
    IETLFrameworkBuilder AddConfigurationProvider<T>() where T : class, IConfigurationProvider;

    /// <summary>
    /// Adds a custom pipeline stage to the framework.
    /// </summary>
    /// <typeparam name="T">The pipeline stage type</typeparam>
    /// <returns>The framework builder for method chaining</returns>
    IETLFrameworkBuilder AddPipelineStage<T>() where T : class, IPipelineStage;

    /// <summary>
    /// Adds a custom message broker to the framework.
    /// </summary>
    /// <typeparam name="T">The message broker type</typeparam>
    /// <param name="brokerType">The broker type identifier</param>
    /// <returns>The framework builder for method chaining</returns>
    IETLFrameworkBuilder AddMessageBroker<T>(string brokerType) where T : class;

    /// <summary>
    /// Configures framework settings.
    /// </summary>
    /// <param name="configureAction">Action to configure framework settings</param>
    /// <returns>The framework builder for method chaining</returns>
    IETLFrameworkBuilder Configure(Action<FrameworkOptions> configureAction);
}
