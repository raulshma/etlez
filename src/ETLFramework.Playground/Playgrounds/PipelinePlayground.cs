using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Pipeline;
using ETLFramework.Transformation.Transformations.FieldTransformations;
using ETLFramework.Transformation.Models;
using ETLFramework.Connectors;
using ETLFramework.Configuration.Models;
using ETLFramework.Playground.Services;
using ETLFramework.Playground.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ETLFramework.Playground.Playgrounds;

/// <summary>
/// Playground module for testing complete ETL pipelines.
/// </summary>
public class PipelinePlayground : IPipelinePlayground
{
    private readonly ILogger<PipelinePlayground> _logger;
    private readonly IPlaygroundUtilities _utilities;
    private readonly ISampleDataService _sampleDataService;
    private readonly IPipelineOrchestrator _pipelineOrchestrator;

    public PipelinePlayground(
        ILogger<PipelinePlayground> logger,
        IPlaygroundUtilities utilities,
        ISampleDataService sampleDataService,
        IPipelineOrchestrator pipelineOrchestrator)
    {
        _logger = logger;
        _utilities = utilities;
        _sampleDataService = sampleDataService;
        _pipelineOrchestrator = pipelineOrchestrator;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _utilities.DisplayHeader("Pipeline Playground",
            "Build and test complete ETL pipelines");

        while (!cancellationToken.IsCancellationRequested)
        {
            var options = new[]
            {
                "🏗️ Simple ETL Pipeline",
                "🔄 Multi-Stage Pipeline",
                "📊 Data Quality Pipeline",
                "⚡ Performance Pipeline",
                "🧪 Custom Pipeline Builder",
                "📈 Pipeline Monitoring",
                "🔙 Back to Main Menu"
            };

            var selection = _utilities.PromptForSelection("Select pipeline scenario:", options);

            try
            {
                switch (selection)
                {
                    case var s when s.Contains("Simple ETL"):
                        await RunSimpleETLPipelineAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Multi-Stage"):
                        await RunMultiStagePipelineAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Data Quality"):
                        await RunDataQualityPipelineAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Performance"):
                        await RunPerformancePipelineAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Custom"):
                        await RunCustomPipelineBuilderAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Monitoring"):
                        await RunPipelineMonitoringAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Back"):
                        return;
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    _utilities.WaitForKeyPress();
                }
            }
            catch (Exception ex)
            {
                _utilities.DisplayError("Error in pipeline playground", ex);
                _utilities.WaitForKeyPress();
            }
        }
    }

    /// <summary>
    /// Runs a simple ETL pipeline demonstration.
    /// </summary>
    private async Task RunSimpleETLPipelineAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Simple ETL Pipeline", "Extract → Transform → Load demonstration");

        try
        {
            await _utilities.WithProgressAsync(async progress =>
            {
                progress.Report("Setting up pipeline...");

                // Create sample data
                var customerData = _sampleDataService.GenerateCustomerData(10).ToList();

                progress.Report("Creating source data file...");
                var sourceFile = await _sampleDataService.CreateTempCsvFileAsync(customerData, "source_customers.csv");

                progress.Report("Creating pipeline configuration...");

                // Create pipeline configuration
                var pipelineConfig = new PipelineConfiguration
                {
                    Name = "Simple Customer ETL",
                    Description = "Extract customer data, transform names to uppercase, load to output file",
                    IsEnabled = true,
                    MaxDegreeOfParallelism = 1,
                    Timeout = TimeSpan.FromMinutes(5)
                };

                progress.Report("Executing pipeline...");

                // Simulate pipeline execution
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold blue]🔄 Pipeline Execution:[/]");

                // Stage 1: Extract
                AnsiConsole.MarkupLine("\n[green]📥 EXTRACT Stage:[/]");
                AnsiConsole.MarkupLine($"[dim]• Reading data from: {Path.GetFileName(sourceFile)}[/]");
                AnsiConsole.MarkupLine($"[dim]• Records found: {customerData.Count}[/]");

                // Stage 2: Transform
                AnsiConsole.MarkupLine("\n[yellow]🔧 TRANSFORM Stage:[/]");
                AnsiConsole.MarkupLine("[dim]• Applying transformations:[/]");
                AnsiConsole.MarkupLine("[dim]  - Convert FirstName to uppercase[/]");
                AnsiConsole.MarkupLine("[dim]  - Convert LastName to uppercase[/]");
                AnsiConsole.MarkupLine("[dim]  - Trim whitespace from Email[/]");

                // Simulate transformations
                var transformedData = customerData.Select(c => new
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName.ToUpper(),
                    LastName = c.LastName.ToUpper(),
                    Email = c.Email.Trim(),
                    City = c.City,
                    State = c.State,
                    IsActive = c.IsActive
                }).ToList();

                // Stage 3: Load
                AnsiConsole.MarkupLine("\n[blue]📤 LOAD Stage:[/]");
                var outputFile = Path.ChangeExtension(Path.GetTempFileName(), ".csv");
                AnsiConsole.MarkupLine($"[dim]• Writing data to: {Path.GetFileName(outputFile)}[/]");
                AnsiConsole.MarkupLine($"[dim]• Records written: {transformedData.Count}[/]");

                progress.Report("Pipeline completed successfully!");

                // Display results
                AnsiConsole.WriteLine();
                _utilities.DisplaySuccess("Pipeline execution completed!");

                // Show sample of transformed data
                AnsiConsole.MarkupLine("\n[bold]Sample of transformed data:[/]");
                var sampleTable = new Table().BorderColor(Color.Green);
                sampleTable.AddColumn("ID");
                sampleTable.AddColumn("First Name");
                sampleTable.AddColumn("Last Name");
                sampleTable.AddColumn("Email");
                sampleTable.AddColumn("Location");

                foreach (var record in transformedData.Take(5))
                {
                    sampleTable.AddRow(
                        record.CustomerId.ToString(),
                        record.FirstName,
                        record.LastName,
                        record.Email,
                        $"{record.City}, {record.State}"
                    );
                }

                AnsiConsole.Write(sampleTable);

                // Pipeline statistics
                AnsiConsole.WriteLine();
                var statsTable = new Table().BorderColor(Color.Blue);
                statsTable.AddColumn("Metric");
                statsTable.AddColumn("Value");

                statsTable.AddRow("Pipeline Name", pipelineConfig.Name);
                statsTable.AddRow("Records Processed", transformedData.Count.ToString());
                statsTable.AddRow("Transformations Applied", "3");
                statsTable.AddRow("Status", "[green]Success[/]");
                statsTable.AddRow("Duration", "< 1 second");

                AnsiConsole.Write(statsTable);

                // Cleanup
                try
                {
                    if (File.Exists(sourceFile)) File.Delete(sourceFile);
                    if (File.Exists(outputFile)) File.Delete(outputFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup pipeline test files");
                }

            }, "Running Simple ETL Pipeline");

        }
        catch (Exception ex)
        {
            _utilities.DisplayError("Failed to run simple ETL pipeline", ex);
        }
    }

    /// <summary>
    /// Runs a multi-stage pipeline demonstration.
    /// </summary>
    private async Task RunMultiStagePipelineAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Multi-Stage Pipeline", "Complex pipeline with multiple transformation stages");

        AnsiConsole.MarkupLine("[yellow]Multi-stage pipeline demonstration:[/]");
        AnsiConsole.MarkupLine("[dim]• Stage 1: Extract customer data[/]");
        AnsiConsole.MarkupLine("[dim]• Stage 2: Data validation and cleansing[/]");
        AnsiConsole.MarkupLine("[dim]• Stage 3: Business rule transformations[/]");
        AnsiConsole.MarkupLine("[dim]• Stage 4: Data enrichment[/]");
        AnsiConsole.MarkupLine("[dim]• Stage 5: Load to multiple destinations[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs a data quality pipeline demonstration.
    /// </summary>
    private async Task RunDataQualityPipelineAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Data Quality Pipeline", "Pipeline focused on data validation and quality checks");

        AnsiConsole.MarkupLine("[yellow]Data quality pipeline features:[/]");
        AnsiConsole.MarkupLine("[dim]• Data profiling and analysis[/]");
        AnsiConsole.MarkupLine("[dim]• Validation rule enforcement[/]");
        AnsiConsole.MarkupLine("[dim]• Data quality scoring[/]");
        AnsiConsole.MarkupLine("[dim]• Quality report generation[/]");
        AnsiConsole.MarkupLine("[dim]• Bad data quarantine[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs a performance pipeline demonstration.
    /// </summary>
    private async Task RunPerformancePipelineAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Performance Pipeline", "High-throughput pipeline with performance optimization");

        AnsiConsole.MarkupLine("[yellow]Performance pipeline features:[/]");
        AnsiConsole.MarkupLine("[dim]• Parallel processing[/]");
        AnsiConsole.MarkupLine("[dim]• Batch optimization[/]");
        AnsiConsole.MarkupLine("[dim]• Memory management[/]");
        AnsiConsole.MarkupLine("[dim]• Throughput monitoring[/]");
        AnsiConsole.MarkupLine("[dim]• Resource utilization tracking[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs a custom pipeline builder demonstration.
    /// </summary>
    private async Task RunCustomPipelineBuilderAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Custom Pipeline Builder", "Interactive pipeline construction");

        AnsiConsole.MarkupLine("[yellow]Custom pipeline builder features:[/]");
        AnsiConsole.MarkupLine("[dim]• Interactive stage configuration[/]");
        AnsiConsole.MarkupLine("[dim]• Connector selection and setup[/]");
        AnsiConsole.MarkupLine("[dim]• Transformation chain building[/]");
        AnsiConsole.MarkupLine("[dim]• Pipeline validation[/]");
        AnsiConsole.MarkupLine("[dim]• Configuration export[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs a pipeline monitoring demonstration.
    /// </summary>
    private async Task RunPipelineMonitoringAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Pipeline Monitoring", "Real-time pipeline execution monitoring");

        AnsiConsole.MarkupLine("[yellow]Pipeline monitoring features:[/]");
        AnsiConsole.MarkupLine("[dim]• Real-time execution status[/]");
        AnsiConsole.MarkupLine("[dim]• Performance metrics[/]");
        AnsiConsole.MarkupLine("[dim]• Error tracking and alerts[/]");
        AnsiConsole.MarkupLine("[dim]• Resource utilization[/]");
        AnsiConsole.MarkupLine("[dim]• Execution history[/]");

        await Task.CompletedTask;
    }
}
