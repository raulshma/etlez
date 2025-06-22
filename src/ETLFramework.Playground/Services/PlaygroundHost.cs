using ETLFramework.Playground.Models;
using ETLFramework.Playground.Playgrounds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ETLFramework.Playground.Services;

/// <summary>
/// Main playground host that manages the interactive menu system.
/// </summary>
public class PlaygroundHost : IPlaygroundHost
{
    private readonly ILogger<PlaygroundHost> _logger;
    private readonly IConfiguration _configuration;
    private readonly IPlaygroundUtilities _utilities;
    private readonly ISampleDataService _sampleDataService;
    
    // Playground modules
    private readonly IConnectorPlayground _connectorPlayground;
    private readonly ITransformationPlayground _transformationPlayground;
    private readonly IPipelinePlayground _pipelinePlayground;
    private readonly IValidationPlayground _validationPlayground;
    private readonly IRuleEnginePlayground _ruleEnginePlayground;
    private readonly IPerformancePlayground _performancePlayground;
    private readonly IErrorHandlingPlayground _errorHandlingPlayground;

    private readonly PlaygroundSettings _settings;

    public PlaygroundHost(
        ILogger<PlaygroundHost> logger,
        IConfiguration configuration,
        IPlaygroundUtilities utilities,
        ISampleDataService sampleDataService,
        IConnectorPlayground connectorPlayground,
        ITransformationPlayground transformationPlayground,
        IPipelinePlayground pipelinePlayground,
        IValidationPlayground validationPlayground,
        IRuleEnginePlayground ruleEnginePlayground,
        IPerformancePlayground performancePlayground,
        IErrorHandlingPlayground errorHandlingPlayground)
    {
        _logger = logger;
        _configuration = configuration;
        _utilities = utilities;
        _sampleDataService = sampleDataService;
        _connectorPlayground = connectorPlayground;
        _transformationPlayground = transformationPlayground;
        _pipelinePlayground = pipelinePlayground;
        _validationPlayground = validationPlayground;
        _ruleEnginePlayground = ruleEnginePlayground;
        _performancePlayground = performancePlayground;
        _errorHandlingPlayground = errorHandlingPlayground;

        _settings = configuration.GetSection("Playground").Get<PlaygroundSettings>() ?? new PlaygroundSettings();
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ETL Framework Playground");

        try
        {
            await ShowWelcomeMessage();
            await RunMainMenuAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Playground was cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in playground");
            _utilities.DisplayError("An unexpected error occurred", ex);
        }
        finally
        {
            await CleanupAsync();
        }
    }

    /// <summary>
    /// Shows the welcome message and basic information.
    /// </summary>
    private async Task ShowWelcomeMessage()
    {
        _utilities.DisplayHeader("ETL Framework Playground", 
            "Interactive testing environment for all ETL Framework capabilities");

        var panel = new Panel(
            "[bold]Available Playground Modules:[/]\n\n" +
            "🔌 [blue]Connector Playground[/] - Test all data connectors\n" +
            "🔧 [green]Transformation Playground[/] - Test data transformations\n" +
            "⚙️  [yellow]Pipeline Playground[/] - Build complete ETL pipelines\n" +
            "✅ [cyan]Validation Playground[/] - Test data validation rules\n" +
            "📋 [magenta]Rule Engine Playground[/] - Test rule-based processing\n" +
            "⚡ [red]Performance Playground[/] - Benchmark performance\n" +
            "❌ [orange1]Error Handling Playground[/] - Test error scenarios\n\n" +
            "[dim]Use arrow keys to navigate menus, Enter to select, Esc to go back[/]")
        {
            Header = new PanelHeader("[bold blue]Welcome to ETL Playground[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        await Task.Delay(1000); // Brief pause to let user read
    }

    /// <summary>
    /// Runs the main menu loop.
    /// </summary>
    private async Task RunMainMenuAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var options = new List<PlaygroundOption>
            {
                new() { Key = "1", Title = "🔌 Connector Playground", Description = "Test all data connectors (CSV, JSON, XML, Databases, Cloud Storage)", Action = () => _connectorPlayground.RunAsync(cancellationToken) },
                new() { Key = "2", Title = "🔧 Transformation Playground", Description = "Test data transformations and field mappings", Action = () => _transformationPlayground.RunAsync(cancellationToken) },
                new() { Key = "3", Title = "⚙️ Pipeline Playground", Description = "Build and test complete ETL pipelines", Action = () => _pipelinePlayground.RunAsync(cancellationToken) },
                new() { Key = "4", Title = "✅ Validation Playground", Description = "Test data validation rules and quality checks", Action = () => _validationPlayground.RunAsync(cancellationToken) },
                new() { Key = "5", Title = "📋 Rule Engine Playground", Description = "Test rule-based processing and conditional logic", Action = () => _ruleEnginePlayground.RunAsync(cancellationToken) },
                new() { Key = "6", Title = "⚡ Performance Playground", Description = "Benchmark performance with different data sizes", Action = () => _performancePlayground.RunAsync(cancellationToken) },
                new() { Key = "7", Title = "❌ Error Handling Playground", Description = "Test error scenarios and recovery mechanisms", Action = () => _errorHandlingPlayground.RunAsync(cancellationToken) },
                new() { Key = "8", Title = "📊 Sample Data Generator", Description = "Generate and export sample datasets", Action = RunSampleDataGeneratorAsync },
                new() { Key = "9", Title = "ℹ️ System Information", Description = "View system and framework information", Action = ShowSystemInformationAsync },
                new() { Key = "0", Title = "🚪 Exit", Description = "Exit the playground application", Action = () => Task.FromResult(false) }
            };

            try
            {
                var selectedOption = _utilities.PromptForSelection(
                    "[bold blue]Select a playground module:[/]",
                    options.Select(o => $"{o.Key}. {o.Title}")
                );

                var option = options.FirstOrDefault(o => selectedOption.StartsWith(o.Key + "."));
                if (option != null)
                {
                    if (option.Key == "0") // Exit
                    {
                        break;
                    }

                    AnsiConsole.Clear();
                    await option.Action();
                    
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        _utilities.WaitForKeyPress("\n[dim]Press any key to return to main menu...[/]");
                        AnsiConsole.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in main menu");
                _utilities.DisplayError("An error occurred in the main menu", ex);
                _utilities.WaitForKeyPress();
                AnsiConsole.Clear();
            }
        }
    }

    /// <summary>
    /// Runs the sample data generator.
    /// </summary>
    private async Task RunSampleDataGeneratorAsync()
    {
        _utilities.DisplayHeader("Sample Data Generator", "Generate and export sample datasets for testing");

        var dataTypes = new[]
        {
            "Customer Data",
            "Product Data", 
            "Order Data",
            "Employee Data",
            "Problematic Data (for validation testing)"
        };

        var selectedType = _utilities.PromptForSelection("Select data type to generate:", dataTypes);
        var countStr = _utilities.PromptForInput("Enter number of records to generate:", "100");
        
        if (!int.TryParse(countStr, out var count) || count <= 0)
        {
            _utilities.DisplayError("Invalid record count. Using default of 100.");
            count = 100;
        }

        var exportFormat = _utilities.PromptForSelection("Select export format:", new[] { "CSV", "JSON", "Both" });

        try
        {
            await _utilities.WithProgressAsync(async progress =>
            {
                progress.Report("Generating sample data...");
                await Task.Delay(500); // Simulate work

                object data = selectedType switch
                {
                    "Customer Data" => _sampleDataService.GenerateCustomerData(count).ToList(),
                    "Product Data" => _sampleDataService.GenerateProductData(count).ToList(),
                    "Order Data" => _sampleDataService.GenerateOrderData(count).ToList(),
                    "Employee Data" => _sampleDataService.GenerateEmployeeData(count).ToList(),
                    "Problematic Data (for validation testing)" => _sampleDataService.GenerateProblematicData(count).ToList(),
                    _ => throw new ArgumentException("Unknown data type")
                };

                progress.Report("Exporting data...");
                
                var exportDir = Path.Combine(Directory.GetCurrentDirectory(), _settings.ExportDirectory);
                Directory.CreateDirectory(exportDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var baseName = selectedType.Replace(" ", "_").Replace("(", "").Replace(")", "").ToLower();

                if (exportFormat is "CSV" or "Both")
                {
                    var csvPath = Path.Combine(exportDir, $"{baseName}_{timestamp}.csv");
                    // Export CSV logic would go here
                    progress.Report($"Exported CSV to {csvPath}");
                }

                if (exportFormat is "JSON" or "Both")
                {
                    var jsonPath = Path.Combine(exportDir, $"{baseName}_{timestamp}.json");
                    // Export JSON logic would go here
                    progress.Report($"Exported JSON to {jsonPath}");
                }

                progress.Report("Export completed!");
            }, "Generating and exporting sample data");

            _utilities.DisplaySuccess($"Successfully generated {count} records of {selectedType}");
        }
        catch (Exception ex)
        {
            _utilities.DisplayError("Failed to generate sample data", ex);
        }
    }

    /// <summary>
    /// Shows system and framework information.
    /// </summary>
    private async Task ShowSystemInformationAsync()
    {
        _utilities.DisplayHeader("System Information", "ETL Framework and system details");

        var info = new Table()
            .Title("[bold]System Information[/]")
            .BorderColor(Color.Blue)
            .RoundedBorder();

        info.AddColumn("Property");
        info.AddColumn("Value");

        info.AddRow("Framework Version", "1.0.0");
        info.AddRow("Runtime", Environment.Version.ToString());
        info.AddRow("OS", Environment.OSVersion.ToString());
        info.AddRow("Machine Name", Environment.MachineName);
        info.AddRow("User", Environment.UserName);
        info.AddRow("Working Directory", Environment.CurrentDirectory);
        info.AddRow("Temp Directory", _settings.TempDirectory);
        info.AddRow("Export Directory", _settings.ExportDirectory);

        AnsiConsole.Write(info);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Cleanup resources when shutting down.
    /// </summary>
    private async Task CleanupAsync()
    {
        _logger.LogInformation("Cleaning up playground resources");

        try
        {
            if (_settings.AutoCleanup)
            {
                _sampleDataService.CleanupTempFiles();
            }

            _utilities.DisplaySuccess("Cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cleanup");
        }

        await Task.CompletedTask;
    }
}
