using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ETLFramework.Core.Interfaces;

namespace ETLFramework.Pipeline.Extensions;

/// <summary>
/// Extension methods for configuring ETL Framework pipeline services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ETL Framework pipeline services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddETLPipeline(this IServiceCollection services)
    {
        // Register pipeline orchestrator
        services.TryAddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();

        // Register pipeline builder as transient (new instance each time)
        services.TryAddTransient<PipelineBuilder>();

        return services;
    }

    /// <summary>
    /// Adds ETL Framework pipeline services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configurePipeline">Action to configure pipeline services</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddETLPipeline(this IServiceCollection services, Action<IServiceCollection> configurePipeline)
    {
        // Add base pipeline services
        services.AddETLPipeline();

        // Allow custom pipeline configuration
        configurePipeline(services);

        return services;
    }

    /// <summary>
    /// Adds a custom pipeline orchestrator implementation.
    /// </summary>
    /// <typeparam name="TOrchestrator">The type of pipeline orchestrator</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPipelineOrchestrator<TOrchestrator>(this IServiceCollection services)
        where TOrchestrator : class, IPipelineOrchestrator
    {
        services.TryAddSingleton<IPipelineOrchestrator, TOrchestrator>();
        return services;
    }

    /// <summary>
    /// Adds a custom pipeline orchestrator instance.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="orchestrator">The pipeline orchestrator instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPipelineOrchestrator(this IServiceCollection services, IPipelineOrchestrator orchestrator)
    {
        services.TryAddSingleton(orchestrator);
        return services;
    }
}
