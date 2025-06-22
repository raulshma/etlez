using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ETLFramework.Configuration.Extensions;
using ETLFramework.Pipeline.Extensions;
using ETLFramework.Connectors.Extensions;
using ETLFramework.Core.Interfaces;
using ETLFramework.Pipeline;

namespace ETLFramework.Host;

/// <summary>
/// Main entry point for the ETL Framework Host application.
/// Provides a console host for running ETL pipelines with dependency injection and logging.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    public static async Task<int> Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Starting ETL Framework Host");

            // Create and configure the host
            var host = CreateHostBuilder(args).Build();

            // Run the host
            await host.RunAsync();

            Log.Information("ETL Framework Host stopped");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "ETL Framework Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Creates and configures the host builder with dependency injection and logging.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured host builder</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Register ETL Framework services
                ConfigureServices(services);

                // Register hosted service for demo
                services.AddHostedService<DemoPipelineService>();
            });

    /// <summary>
    /// Configures dependency injection services for the ETL Framework.
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Register ETL Framework configuration services
        services.AddETLConfiguration();

        // Register ETL Framework pipeline services
        services.AddETLPipeline();

        // Register ETL Framework connector services
        services.AddETLConnectors();

        // Demo service will be registered as hosted service

        // TODO: Add connector services
        // TODO: Add transformation services

        Log.Information("ETL Framework services configured");
    }
}
