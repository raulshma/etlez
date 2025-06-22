using ETLFramework.Core.Interfaces;
using ETLFramework.Playground.Services;
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

        AnsiConsole.MarkupLine("[yellow]Pipeline playground will be implemented here.[/]");
        AnsiConsole.MarkupLine("[dim]This will include: Pipeline builder, Stage management, Execution monitoring, etc.[/]");
        
        await Task.CompletedTask;
    }
}
