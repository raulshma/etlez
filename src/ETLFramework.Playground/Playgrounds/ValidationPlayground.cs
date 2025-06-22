using ETLFramework.Playground.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ETLFramework.Playground.Playgrounds;

/// <summary>
/// Playground module for testing data validation.
/// </summary>
public class ValidationPlayground : IValidationPlayground
{
    private readonly ILogger<ValidationPlayground> _logger;
    private readonly IPlaygroundUtilities _utilities;
    private readonly ISampleDataService _sampleDataService;

    public ValidationPlayground(
        ILogger<ValidationPlayground> logger,
        IPlaygroundUtilities utilities,
        ISampleDataService sampleDataService)
    {
        _logger = logger;
        _utilities = utilities;
        _sampleDataService = sampleDataService;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _utilities.DisplayHeader("Validation Playground", 
            "Test data validation rules and quality checks");

        AnsiConsole.MarkupLine("[yellow]Validation playground will be implemented here.[/]");
        AnsiConsole.MarkupLine("[dim]This will include: Required fields, Regex patterns, Range validation, etc.[/]");
        
        await Task.CompletedTask;
    }
}
