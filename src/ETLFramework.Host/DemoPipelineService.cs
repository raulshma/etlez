using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ETLFramework.Core.Interfaces;
using ETLFramework.Pipeline;
using ETLFramework.Configuration.Models;

namespace ETLFramework.Host;

/// <summary>
/// Demo hosted service that demonstrates the ETL Framework pipeline execution.
/// </summary>
public class DemoPipelineService : BackgroundService
{
    private readonly ILogger<DemoPipelineService> _logger;
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly PipelineBuilder _pipelineBuilder;

    /// <summary>
    /// Initializes a new instance of the DemoPipelineService class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="orchestrator">The pipeline orchestrator</param>
    /// <param name="pipelineBuilder">The pipeline builder</param>
    public DemoPipelineService(
        ILogger<DemoPipelineService> logger,
        IPipelineOrchestrator orchestrator,
        PipelineBuilder pipelineBuilder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _pipelineBuilder = pipelineBuilder ?? throw new ArgumentNullException(nameof(pipelineBuilder));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting ETL Framework Demo");

            // Wait a moment for the host to fully start
            await Task.Delay(1000, stoppingToken);

            // Create a demo pipeline
            var pipeline = await CreateDemoPipelineAsync();

            // Create a demo configuration
            var configuration = CreateDemoConfiguration();

            // Create pipeline logger
            var pipelineLogger = _logger;

            // Create pipeline context
            var context = new PipelineContext(Guid.NewGuid(), configuration, pipelineLogger, stoppingToken);

            // Execute the pipeline
            _logger.LogInformation("Executing demo pipeline: {PipelineName}", pipeline.Name);

            var result = await _orchestrator.ExecutePipelineAsync(pipeline, context, stoppingToken);

            // Report results
            if (result.IsSuccess)
            {
                _logger.LogInformation("Demo pipeline completed successfully!");
                _logger.LogInformation("Records processed: {RecordsProcessed}", result.RecordsProcessed);
                _logger.LogInformation("Execution time: {Duration}", result.Duration);
            }
            else
            {
                _logger.LogWarning("Demo pipeline completed with errors:");
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("  - {ErrorMessage}", error.Message);
                }
            }

            // Wait a bit before shutting down
            await Task.Delay(2000, stoppingToken);

            _logger.LogInformation("ETL Framework Demo completed");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Demo pipeline service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in demo pipeline service");
        }
    }

    /// <summary>
    /// Creates a demo pipeline for testing.
    /// </summary>
    /// <returns>A configured demo pipeline</returns>
    private Task<IPipeline> CreateDemoPipelineAsync()
    {
        var pipeline = _pipelineBuilder
            .WithName("Demo ETL Pipeline")
            .WithDescription("A demonstration pipeline showing ETL Framework capabilities")
            .AddExtractStage("Extract Customer Data", 1, 50)
            .AddTransformStage("Transform Customer Data", 2, 50)
            .AddLoadStage("Load Customer Data", 3, 50)
            .Build();

        _logger.LogInformation("Created demo pipeline with {StageCount} stages", pipeline.Stages.Count);

        return Task.FromResult(pipeline);
    }

    /// <summary>
    /// Creates a demo configuration for the pipeline.
    /// </summary>
    /// <returns>A demo pipeline configuration</returns>
    private IPipelineConfiguration CreateDemoConfiguration()
    {
        var config = new PipelineConfiguration
        {
            Name = "Demo Pipeline Configuration",
            Description = "Configuration for demo pipeline execution",
            Version = "1.0.0",
            Author = "ETL Framework Demo",
            IsEnabled = true,
            ErrorHandling = new ErrorHandlingConfiguration
            {
                StopOnError = false,
                MaxErrors = 10
            },
            Retry = new RetryConfiguration
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromSeconds(1)
            },
            Timeout = TimeSpan.FromMinutes(5)
        };

        // Add some global settings
        config.GlobalSettings["DemoMode"] = true;
        config.GlobalSettings["LogLevel"] = "Information";
        config.GlobalSettings["BatchSize"] = 10;

        // Add tags
        config.Tags.Add("demo");
        config.Tags.Add("test");
        config.Tags.Add("etl-framework");

        _logger.LogInformation("Created demo configuration: {ConfigName}", config.Name);

        return config;
    }

    /// <summary>
    /// Demonstrates configuration loading from file.
    /// </summary>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateConfigurationLoadingAsync()
    {
        try
        {
            // This would load a configuration from the sample files we created
            _logger.LogInformation("Configuration loading demonstration would go here");
            
            // Example of how to use the configuration manager:
            // var configManager = serviceProvider.GetRequiredService<ConfigurationManager>();
            // var config = await configManager.LoadPipelineConfigurationAsync("samples/configurations/sample-pipeline.json");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demonstrating configuration loading");
        }
    }
}
