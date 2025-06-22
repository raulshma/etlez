using Microsoft.Extensions.DependencyInjection;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Interface for ETL Framework plugins that provide custom functionality.
/// </summary>
public interface IETLPlugin
{
    /// <summary>
    /// Gets the name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the description of the plugin.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the author of the plugin.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Configures the ETL framework with plugin components.
    /// </summary>
    /// <param name="builder">The framework builder to configure</param>
    void Configure(IETLFrameworkBuilder builder);
}
