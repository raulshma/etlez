using ETLFramework.Core.Models;
using ETLFramework.Playground.Services;
using ETLFramework.Playground.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;

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

        while (!cancellationToken.IsCancellationRequested)
        {
            var options = new[]
            {
                "⚡ Throughput Benchmarks",
                "🧠 Memory Usage Analysis",
                "⏱️ Latency Testing",
                "📊 Batch Size Optimization",
                "🔄 Parallel Processing Tests",
                "📈 Performance Profiling",
                "🔙 Back to Main Menu"
            };

            var selection = _utilities.PromptForSelection("Select performance test:", options);

            try
            {
                switch (selection)
                {
                    case var s when s.Contains("Throughput"):
                        await RunThroughputBenchmarksAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Memory"):
                        await RunMemoryUsageAnalysisAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Latency"):
                        await RunLatencyTestingAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Batch Size"):
                        await RunBatchSizeOptimizationAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Parallel"):
                        await RunParallelProcessingTestsAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Profiling"):
                        await RunPerformanceProfilingAsync(cancellationToken);
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
                _utilities.DisplayError("Error in performance playground", ex);
                _utilities.WaitForKeyPress();
            }
        }
    }

    /// <summary>
    /// Runs throughput benchmarks.
    /// </summary>
    private async Task RunThroughputBenchmarksAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Throughput Benchmarks", "Test data processing throughput");

        var dataSizes = new[] { 1000, 5000, 10000, 50000 };

        AnsiConsole.MarkupLine("[blue]Running throughput benchmarks with different data sizes...[/]");

        var resultsTable = new Table().BorderColor(Color.Green);
        resultsTable.AddColumn("Data Size");
        resultsTable.AddColumn("Processing Time");
        resultsTable.AddColumn("Records/Second");
        resultsTable.AddColumn("Memory Used");

        foreach (var size in dataSizes)
        {
            AnsiConsole.MarkupLine($"\n[yellow]Testing with {size:N0} records...[/]");

            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(false);

            // Generate test data
            var testData = _sampleDataService.GenerateCustomerData(size).ToList();

            // Simulate processing
            var processedCount = 0;
            foreach (var customer in testData)
            {
                // Simulate some processing work
                var processed = customer.FirstName.ToUpper() + customer.LastName.ToUpper();
                processedCount++;

                if (processedCount % 1000 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            var recordsPerSecond = size / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);

            resultsTable.AddRow(
                size.ToString("N0"),
                stopwatch.Elapsed.ToString(@"mm\:ss\.fff"),
                recordsPerSecond.ToString("N0"),
                (memoryUsed / 1024 / 1024).ToString("N1") + " MB"
            );
        }

        AnsiConsole.Write(resultsTable);

        // Performance summary
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✅ Throughput benchmark completed[/]");
        AnsiConsole.MarkupLine("[blue]💡 Tip: Higher records/second indicates better throughput performance[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs memory usage analysis.
    /// </summary>
    private async Task RunMemoryUsageAnalysisAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Memory Usage Analysis", "Analyze memory consumption patterns");

        AnsiConsole.MarkupLine("[blue]Analyzing memory usage patterns...[/]");

        var memoryTable = new Table().BorderColor(Color.Blue);
        memoryTable.AddColumn("Operation");
        memoryTable.AddColumn("Before (MB)");
        memoryTable.AddColumn("After (MB)");
        memoryTable.AddColumn("Difference (MB)");

        // Baseline memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(false);

        // Test 1: Small dataset
        var beforeSmall = GC.GetTotalMemory(false);
        var smallData = _sampleDataService.GenerateCustomerData(1000).ToList();
        var afterSmall = GC.GetTotalMemory(false);

        memoryTable.AddRow(
            "Small Dataset (1K records)",
            (beforeSmall / 1024 / 1024).ToString("N1"),
            (afterSmall / 1024 / 1024).ToString("N1"),
            ((afterSmall - beforeSmall) / 1024 / 1024).ToString("N1")
        );

        // Test 2: Large dataset
        var beforeLarge = GC.GetTotalMemory(false);
        var largeData = _sampleDataService.GenerateCustomerData(10000).ToList();
        var afterLarge = GC.GetTotalMemory(false);

        memoryTable.AddRow(
            "Large Dataset (10K records)",
            (beforeLarge / 1024 / 1024).ToString("N1"),
            (afterLarge / 1024 / 1024).ToString("N1"),
            ((afterLarge - beforeLarge) / 1024 / 1024).ToString("N1")
        );

        // Test 3: After cleanup
        smallData.Clear();
        largeData.Clear();
        GC.Collect();
        var afterCleanup = GC.GetTotalMemory(false);

        memoryTable.AddRow(
            "After Cleanup",
            (afterLarge / 1024 / 1024).ToString("N1"),
            (afterCleanup / 1024 / 1024).ToString("N1"),
            ((afterCleanup - afterLarge) / 1024 / 1024).ToString("N1")
        );

        AnsiConsole.Write(memoryTable);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✅ Memory analysis completed[/]");
        AnsiConsole.MarkupLine("[blue]💡 Tip: Monitor memory growth patterns to identify potential leaks[/]");

        await Task.CompletedTask;
    }

    // Placeholder methods for other performance tests
    private async Task RunLatencyTestingAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Latency Testing", "Measure response time characteristics");
        AnsiConsole.MarkupLine("[yellow]Latency testing features:[/]");
        AnsiConsole.MarkupLine("[dim]• Single operation latency measurement[/]");
        AnsiConsole.MarkupLine("[dim]• Percentile analysis (P50, P95, P99)[/]");
        AnsiConsole.MarkupLine("[dim]• Latency distribution visualization[/]");
        await Task.CompletedTask;
    }

    private async Task RunBatchSizeOptimizationAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Batch Size Optimization", "Find optimal batch sizes for processing");
        AnsiConsole.MarkupLine("[yellow]Batch size optimization features:[/]");
        AnsiConsole.MarkupLine("[dim]• Test different batch sizes[/]");
        AnsiConsole.MarkupLine("[dim]• Throughput vs memory trade-offs[/]");
        AnsiConsole.MarkupLine("[dim]• Optimal batch size recommendations[/]");
        await Task.CompletedTask;
    }

    private async Task RunParallelProcessingTestsAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Parallel Processing Tests", "Test parallel execution performance");
        AnsiConsole.MarkupLine("[yellow]Parallel processing features:[/]");
        AnsiConsole.MarkupLine("[dim]• Multi-threaded processing tests[/]");
        AnsiConsole.MarkupLine("[dim]• Scalability analysis[/]");
        AnsiConsole.MarkupLine("[dim]• Thread contention detection[/]");
        await Task.CompletedTask;
    }

    private async Task RunPerformanceProfilingAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Performance Profiling", "Detailed performance analysis");
        AnsiConsole.MarkupLine("[yellow]Performance profiling features:[/]");
        AnsiConsole.MarkupLine("[dim]• CPU usage analysis[/]");
        AnsiConsole.MarkupLine("[dim]• Memory allocation tracking[/]");
        AnsiConsole.MarkupLine("[dim]• Bottleneck identification[/]");
        await Task.CompletedTask;
    }
}
