using ETLFramework.Playground.Services;
using ETLFramework.Playground.Playgrounds;
using ETLFramework.Core.Interfaces;
using ETLFramework.Connectors;
using ETLFramework.Transformation;
using ETLFramework.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;

namespace ETLFramework.Playground;

/// <summary>
/// Main program entry point for the ETL Framework Playground.
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
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/playground-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            AnsiConsole.Write(
                new FigletText("ETL Playground")
                    .LeftJustified()
                    .Color(Color.Blue));

            AnsiConsole.MarkupLine("[bold green]Welcome to the ETL Framework Playground![/]");
            AnsiConsole.MarkupLine("[dim]Interactive testing environment for all ETL Framework capabilities[/]");
            AnsiConsole.WriteLine();

            Log.Information("Starting ETL Framework Playground");

            // Create and configure the host
            var host = CreateHostBuilder(args).Build();

            // Run the playground
            await host.RunAsync();

            Log.Information("ETL Framework Playground completed");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "ETL Framework Playground terminated unexpectedly");
            AnsiConsole.WriteException(ex);
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
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Register ETL Framework services
                services.AddSingleton<IConnectorFactory, ConnectorFactory>();
                services.AddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();

                // Register transformation services
                services.AddSingleton<ETLFramework.Transformation.Interfaces.ITransformationProcessor,
                    ETLFramework.Transformation.Processors.TransformationProcessor>();

                // Register playground services
                services.AddSingleton<IPlaygroundHost, PlaygroundHost>();
                services.AddSingleton<ISampleDataService, SampleDataService>();
                services.AddSingleton<IPlaygroundUtilities, PlaygroundUtilities>();
                services.AddSingleton<IHelpService, HelpService>();

                // Register playground modules
                services.AddTransient<IConnectorPlayground, ConnectorPlayground>();
                services.AddTransient<ITransformationPlayground, TransformationPlayground>();
                services.AddTransient<IPipelinePlayground, PipelinePlayground>();
                services.AddTransient<IValidationPlayground, ValidationPlayground>();
                services.AddTransient<IRuleEnginePlayground, RuleEnginePlayground>();
                services.AddTransient<IPerformancePlayground, PerformancePlayground>();
                services.AddTransient<IErrorHandlingPlayground, ErrorHandlingPlayground>();

                // Register hosted service
                services.AddHostedService<PlaygroundHostedService>();
            });
}

/// <summary>
/// Hosted service that runs the playground application.
/// </summary>
public class PlaygroundHostedService : BackgroundService
{
    private readonly IPlaygroundHost _playgroundHost;
    private readonly ILogger<PlaygroundHostedService> _logger;

    public PlaygroundHostedService(IPlaygroundHost playgroundHost, ILogger<PlaygroundHostedService> logger)
    {
        _playgroundHost = playgroundHost;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _playgroundHost.RunAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Playground was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in playground hosted service");
            throw;
        }
    }
}
