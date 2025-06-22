using Spectre.Console;

namespace ETLFramework.Playground.Services;

/// <summary>
/// Service for providing help and documentation within the playground.
/// </summary>
public interface IHelpService
{
    /// <summary>
    /// Displays the main help menu.
    /// </summary>
    void ShowMainHelp();

    /// <summary>
    /// Displays help for a specific playground module.
    /// </summary>
    /// <param name="moduleName">The name of the module</param>
    void ShowModuleHelp(string moduleName);

    /// <summary>
    /// Displays getting started guide.
    /// </summary>
    void ShowGettingStarted();

    /// <summary>
    /// Displays keyboard shortcuts and navigation help.
    /// </summary>
    void ShowKeyboardShortcuts();
}

/// <summary>
/// Implementation of the help service.
/// </summary>
public class HelpService : IHelpService
{
    /// <inheritdoc />
    public void ShowMainHelp()
    {
        var panel = new Panel(
            new Markup("""
            [bold blue]ETL Framework Playground Help[/]
            
            [yellow]Available Modules:[/]
            • [green]🔌 Connector Playground[/] - Test data connectors (CSV, JSON, XML, databases)
            • [green]🔧 Transformation Playground[/] - Test data transformations and field mappings
            • [green]⚙️ Pipeline Playground[/] - Build and test complete ETL pipelines
            • [green]✅ Validation Playground[/] - Test data validation rules and quality checks
            • [green]🎯 Rule Engine Playground[/] - Test rule-based processing and business logic
            • [green]⚡ Performance Playground[/] - Benchmark performance and analyze bottlenecks
            
            [yellow]Navigation:[/]
            • Use [cyan]arrow keys[/] to navigate menus
            • Press [cyan]Enter[/] to select an option
            • Press [cyan]Escape[/] or select "Back" to return to previous menu
            • Press [cyan]Ctrl+C[/] to exit the application
            
            [yellow]Getting Help:[/]
            • Select [cyan]"📚 Help & Documentation"[/] from the main menu
            • Each module has built-in help and examples
            • Check the README.md file for detailed documentation
            """))
        {
            Header = new PanelHeader(" 📚 ETL Framework Playground Help "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue)
        };

        AnsiConsole.Write(panel);
    }

    /// <inheritdoc />
    public void ShowModuleHelp(string moduleName)
    {
        var helpContent = moduleName.ToLower() switch
        {
            "connector" => GetConnectorHelp(),
            "transformation" => GetTransformationHelp(),
            "pipeline" => GetPipelineHelp(),
            "validation" => GetValidationHelp(),
            "ruleengine" => GetRuleEngineHelp(),
            "performance" => GetPerformanceHelp(),
            _ => "[red]Help not available for this module.[/]"
        };

        var panel = new Panel(new Markup(helpContent))
        {
            Header = new PanelHeader($" 📖 {moduleName} Module Help "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };

        AnsiConsole.Write(panel);
    }

    /// <inheritdoc />
    public void ShowGettingStarted()
    {
        var panel = new Panel(
            new Markup("""
            [bold green]Getting Started with ETL Framework Playground[/]
            
            [yellow]1. Choose a Module[/]
            Start by selecting one of the playground modules from the main menu.
            Each module focuses on a specific aspect of ETL processing.
            
            [yellow]2. Explore Sample Scenarios[/]
            Each module provides pre-built scenarios with sample data.
            These demonstrate common use cases and best practices.
            
            [yellow]3. Experiment with Settings[/]
            Try different configurations, data sizes, and parameters
            to see how they affect performance and results.
            
            [yellow]4. Learn from Examples[/]
            Pay attention to the code examples and explanations
            provided throughout the playground.
            
            [yellow]5. Apply to Your Projects[/]
            Use the knowledge gained to implement ETL solutions
            in your own applications.
            
            [cyan]💡 Tip:[/] Start with the Connector Playground to understand
            how data flows through the ETL Framework.
            """))
        {
            Header = new PanelHeader(" 🚀 Getting Started "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };

        AnsiConsole.Write(panel);
    }

    /// <inheritdoc />
    public void ShowKeyboardShortcuts()
    {
        var table = new Table()
            .Title("[bold blue]Keyboard Shortcuts[/]")
            .BorderColor(Color.Blue)
            .AddColumn("Key")
            .AddColumn("Action")
            .AddRow("[cyan]↑/↓[/]", "Navigate menu items")
            .AddRow("[cyan]Enter[/]", "Select menu item")
            .AddRow("[cyan]Escape[/]", "Go back to previous menu")
            .AddRow("[cyan]Ctrl+C[/]", "Exit application")
            .AddRow("[cyan]Space[/]", "Continue after viewing results")
            .AddRow("[cyan]Tab[/]", "Navigate between UI elements")
            .AddRow("[cyan]F1[/]", "Show help (when available)")
            .AddRow("[cyan]F5[/]", "Refresh/reload current view");

        AnsiConsole.Write(table);
    }

    private static string GetConnectorHelp()
    {
        return """
        [bold blue]Connector Playground Help[/]
        
        [yellow]Purpose:[/] Test and explore different data connectors
        
        [yellow]Available Connectors:[/]
        • [green]CSV[/] - Comma-separated values files
        • [green]JSON[/] - JavaScript Object Notation files
        • [green]XML[/] - Extensible Markup Language files
        • [green]SQLite[/] - Lightweight database files
        • [green]SQL Server[/] - Microsoft SQL Server databases
        • [green]MySQL[/] - MySQL databases
        • [green]Azure Blob[/] - Azure Blob Storage
        • [green]AWS S3[/] - Amazon S3 Storage
        
        [yellow]Features:[/]
        • Test read/write operations
        • Performance benchmarking
        • Configuration validation
        • Health checks
        • Custom connector setup
        
        [cyan]💡 Tip:[/] Start with file-based connectors (CSV, JSON)
        before moving to database connectors.
        """;
    }

    private static string GetTransformationHelp()
    {
        return """
        [bold blue]Transformation Playground Help[/]
        
        [yellow]Purpose:[/] Test data transformation operations
        
        [yellow]Transformation Types:[/]
        • [green]String[/] - Text manipulation (uppercase, lowercase, trim, regex)
        • [green]Numeric[/] - Number operations (round, add, multiply, format)
        • [green]Date/Time[/] - Date manipulation and formatting
        • [green]Field Mapping[/] - Rename and restructure fields
        • [green]Complex[/] - Multi-step transformations
        • [green]Custom[/] - Build your own transformation logic
        
        [yellow]Features:[/]
        • Interactive transformation testing
        • Before/after data comparison
        • Performance measurement
        • Transformation chaining
        • Error handling
        
        [cyan]💡 Tip:[/] Use the "Test All" options to see
        multiple transformations applied to the same data.
        """;
    }

    private static string GetPipelineHelp()
    {
        return """
        [bold blue]Pipeline Playground Help[/]
        
        [yellow]Purpose:[/] Build and test complete ETL pipelines
        
        [yellow]Pipeline Types:[/]
        • [green]Simple ETL[/] - Basic Extract → Transform → Load
        • [green]Multi-Stage[/] - Complex pipelines with multiple stages
        • [green]Data Quality[/] - Focus on validation and quality checks
        • [green]Performance[/] - High-throughput optimized pipelines
        • [green]Custom[/] - Interactive pipeline builder
        • [green]Monitoring[/] - Real-time execution monitoring
        
        [yellow]Features:[/]
        • Visual pipeline execution
        • Stage-by-stage monitoring
        • Error handling and recovery
        • Performance metrics
        • Configuration export
        
        [cyan]💡 Tip:[/] Start with Simple ETL to understand
        the basic pipeline flow.
        """;
    }

    private static string GetValidationHelp()
    {
        return """
        [bold blue]Validation Playground Help[/]
        
        [yellow]Purpose:[/] Test data validation rules and quality checks
        
        [yellow]Validation Types:[/]
        • [green]Required Fields[/] - Check for missing data
        • [green]Regex Patterns[/] - Pattern matching validation
        • [green]Range Validation[/] - Numeric and date ranges
        • [green]Email Validation[/] - Email format checking
        • [green]Phone Validation[/] - Phone number formats
        • [green]Data Type[/] - Type checking and conversion
        • [green]Custom Rules[/] - Business logic validation
        • [green]Quality Reports[/] - Comprehensive analysis
        
        [yellow]Features:[/]
        • Interactive rule testing
        • Validation result visualization
        • Error reporting
        • Quality scoring
        • Rule configuration
        
        [cyan]💡 Tip:[/] Start with Required Field validation
        to understand basic validation concepts.
        """;
    }

    private static string GetRuleEngineHelp()
    {
        return """
        [bold blue]Rule Engine Playground Help[/]
        
        [yellow]Purpose:[/] Test rule-based processing and business logic
        
        [yellow]Rule Types:[/]
        • [green]Conditional[/] - Simple if-then logic
        • [green]Priority-Based[/] - Rules with execution order
        • [green]Business Logic[/] - Complex business rules
        • [green]Chained[/] - Sequential rule processing
        • [green]Matching[/] - Pattern-based rule selection
        • [green]Configuration[/] - Interactive rule builder
        
        [yellow]Features:[/]
        • Rule condition testing
        • Priority ordering
        • Rule conflict resolution
        • Performance optimization
        • Visual rule builder
        
        [cyan]💡 Tip:[/] Start with Simple Conditional rules
        to understand basic rule concepts.
        """;
    }

    private static string GetPerformanceHelp()
    {
        return """
        [bold blue]Performance Playground Help[/]
        
        [yellow]Purpose:[/] Benchmark performance and analyze bottlenecks
        
        [yellow]Test Types:[/]
        • [green]Throughput[/] - Records processed per second
        • [green]Memory Usage[/] - Memory consumption analysis
        • [green]Latency[/] - Response time measurement
        • [green]Batch Size[/] - Optimal batch size finding
        • [green]Parallel Processing[/] - Multi-threading tests
        • [green]Profiling[/] - Detailed performance analysis
        
        [yellow]Features:[/]
        • Real-time performance metrics
        • Memory usage tracking
        • Bottleneck identification
        • Optimization recommendations
        • Comparative analysis
        
        [cyan]💡 Tip:[/] Start with Throughput benchmarks
        to establish baseline performance.
        """;
    }
}
