using ETLFramework.Playground.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ETLFramework.Playground.Playgrounds;

/// <summary>
/// Playground module for performance testing.
/// </summary>
public class PerformancePlayground : IPerformancePlayground
{
    private readonly ILogger<PerformancePlayground> _logger;
    private readonly IPlaygroundUtilities _utilities;
    private readonly ISampleDataService _sampleDataService;

    public PerformancePlayground(
        ILogger<PerformancePlayground> logger,
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
        _utilities.DisplayHeader("Performance Playground", 
            "Benchmark performance with different data sizes");

        AnsiConsole.MarkupLine("[yellow]Performance playground will be implemented here.[/]");
        AnsiConsole.MarkupLine("[dim]This will include: Throughput testing, Memory usage, Scalability tests, etc.[/]");
        
        await Task.CompletedTask;
    }
}
