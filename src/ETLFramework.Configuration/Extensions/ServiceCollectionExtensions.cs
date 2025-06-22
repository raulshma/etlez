using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ETLFramework.Core.Interfaces;
using ETLFramework.Configuration.Providers;

namespace ETLFramework.Configuration.Extensions;

/// <summary>
/// Extension methods for configuring ETL Framework configuration services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ETL Framework configuration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddETLConfiguration(this IServiceCollection services)
    {
        // Register configuration providers
        services.TryAddSingleton<JsonConfigurationProvider>();
        services.TryAddSingleton<YamlConfigurationProvider>();

        // Register configuration providers as IConfigurationProvider
        services.TryAddSingleton<IConfigurationProvider>(provider => 
            provider.GetRequiredService<JsonConfigurationProvider>());
        services.TryAddSingleton<IConfigurationProvider>(provider => 
            provider.GetRequiredService<YamlConfigurationProvider>());

        // Register configuration manager
        services.TryAddSingleton<ConfigurationManager>(provider =>
        {
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ConfigurationManager>>();
            var configProviders = provider.GetServices<IConfigurationProvider>();
            return new ConfigurationManager(logger, configProviders);
        });

        return services;
    }

    /// <summary>
    /// Adds ETL Framework configuration services with custom configuration providers.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureProviders">Action to configure custom providers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddETLConfiguration(this IServiceCollection services, Action<IServiceCollection> configureProviders)
    {
        // Add base configuration services
        services.AddETLConfiguration();

        // Allow custom provider configuration
        configureProviders(services);

        return services;
    }

    /// <summary>
    /// Adds a custom configuration provider to the service collection.
    /// </summary>
    /// <typeparam name="TProvider">The type of configuration provider</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConfigurationProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IConfigurationProvider
    {
        services.TryAddSingleton<TProvider>();
        services.TryAddSingleton<IConfigurationProvider>(provider => 
            provider.GetRequiredService<TProvider>());

        return services;
    }

    /// <summary>
    /// Adds a custom configuration provider instance to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="provider">The configuration provider instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConfigurationProvider(this IServiceCollection services, IConfigurationProvider provider)
    {
        services.TryAddSingleton(provider);
        return services;
    }

    /// <summary>
    /// Adds only JSON configuration support.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJsonConfiguration(this IServiceCollection services)
    {
        services.TryAddSingleton<JsonConfigurationProvider>();
        services.TryAddSingleton<IConfigurationProvider>(provider => 
            provider.GetRequiredService<JsonConfigurationProvider>());

        services.TryAddSingleton<ConfigurationManager>(provider =>
        {
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ConfigurationManager>>();
            var configProviders = provider.GetServices<IConfigurationProvider>();
            return new ConfigurationManager(logger, configProviders);
        });

        return services;
    }

    /// <summary>
    /// Adds only YAML configuration support.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddYamlConfiguration(this IServiceCollection services)
    {
        services.TryAddSingleton<YamlConfigurationProvider>();
        services.TryAddSingleton<IConfigurationProvider>(provider => 
            provider.GetRequiredService<YamlConfigurationProvider>());

        services.TryAddSingleton<ConfigurationManager>(provider =>
        {
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ConfigurationManager>>();
            var configProviders = provider.GetServices<IConfigurationProvider>();
            return new ConfigurationManager(logger, configProviders);
        });

        return services;
    }
}
