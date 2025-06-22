using ETLFramework.Core.Implementations;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ETLFramework.Core.Extensions;

/// <summary>
/// Extension methods for configuring ETL Framework services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core ETL Framework services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddETLFramework(this IServiceCollection services)
    {
        return services.AddETLFramework(options => { });
    }

    /// <summary>
    /// Adds the core ETL Framework services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure framework options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddETLFramework(this IServiceCollection services, Action<FrameworkOptions> configureOptions)
    {
        // Configure framework options
        services.Configure(configureOptions);

        // Register core framework services
        services.TryAddSingleton<IETLFrameworkBuilder, ETLFrameworkBuilder>();
        services.TryAddSingleton<IPluginManager, PluginManager>();

        // Register type registries
        services.TryAddSingleton<ConnectorTypeRegistry>();
        services.TryAddSingleton<TransformationTypeRegistry>();
        services.TryAddSingleton<MessageBrokerTypeRegistry>();

        return services;
    }

    /// <summary>
    /// Adds ETL Framework connector services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddETLConnectors(this IServiceCollection services)
    {
        // Register built-in connectors would go here
        // services.AddTransient<CsvConnector>();
        // services.AddTransient<JsonConnector>();
        // etc.

        return services;
    }

    /// <summary>
    /// Adds ETL Framework transformation services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddETLTransformations(this IServiceCollection services)
    {
        // Register built-in transformations would go here
        // services.AddTransient<FieldMappingTransformation>();
        // services.AddTransient<DataValidationTransformation>();
        // etc.

        return services;
    }

    /// <summary>
    /// Adds ETL Framework messaging services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddETLMessaging(this IServiceCollection services)
    {
        // Register messaging services - implementations will be provided by the Messaging project
        // These are just the interface registrations
        return services;
    }

    /// <summary>
    /// Adds ETL Framework pipeline services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddETLPipeline(this IServiceCollection services)
    {
        // Register pipeline services would go here
        // services.TryAddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();
        // services.TryAddTransient<IPipelineFactory, PipelineFactory>();
        // etc.

        return services;
    }

    /// <summary>
    /// Adds ETL Framework configuration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddETLConfiguration(this IServiceCollection services)
    {
        // Register configuration services would go here
        // services.TryAddSingleton<IConfigurationManager, ConfigurationManager>();
        // services.AddTransient<IConfigurationProvider, JsonConfigurationProvider>();
        // services.AddTransient<IConfigurationProvider, YamlConfigurationProvider>();
        // etc.

        return services;
    }

    /// <summary>
    /// Configures the ETL Framework with a builder action.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureBuilder">Action to configure the framework builder</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection ConfigureETLFramework(this IServiceCollection services, Action<IETLFrameworkBuilder> configureBuilder)
    {
        services.AddSingleton<IConfigureOptions<ETLFrameworkBuilderOptions>>(provider =>
            new ConfigureOptions<ETLFrameworkBuilderOptions>(options =>
            {
                var builder = provider.GetRequiredService<IETLFrameworkBuilder>();
                configureBuilder(builder);
            }));

        return services;
    }
}

/// <summary>
/// Options for configuring the ETL Framework builder.
/// </summary>
public class ETLFrameworkBuilderOptions
{
    /// <summary>
    /// Gets or sets whether to auto-discover plugins.
    /// </summary>
    public bool AutoDiscoverPlugins { get; set; } = true;

    /// <summary>
    /// Gets or sets the plugin discovery paths.
    /// </summary>
    public List<string> PluginPaths { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to validate plugins before loading.
    /// </summary>
    public bool ValidatePlugins { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to load plugins in isolated contexts.
    /// </summary>
    public bool IsolatePlugins { get; set; } = true;
}
