using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Transformation.Performance;

/// <summary>
/// Interface for performance optimization utilities.
/// </summary>
public interface IPerformanceOptimizer
{
    /// <summary>
    /// Analyzes transformation performance and suggests optimizations.
    /// </summary>
    /// <param name="transformationId">The transformation ID</param>
    /// <returns>Optimization analysis</returns>
    OptimizationAnalysis AnalyzePerformance(string transformationId);

    /// <summary>
    /// Gets optimal batch size for a transformation.
    /// </summary>
    /// <param name="transformationId">The transformation ID</param>
    /// <param name="targetThroughput">Target throughput in records per second</param>
    /// <returns>Recommended batch size</returns>
    int GetOptimalBatchSize(string transformationId, double targetThroughput = 1000);

    /// <summary>
    /// Determines if parallel execution would benefit the transformation.
    /// </summary>
    /// <param name="transformationId">The transformation ID</param>
    /// <returns>Parallel execution recommendation</returns>
    ParallelExecutionRecommendation GetParallelExecutionRecommendation(string transformationId);

    /// <summary>
    /// Gets memory optimization recommendations.
    /// </summary>
    /// <param name="transformationId">The transformation ID</param>
    /// <returns>Memory optimization recommendations</returns>
    IEnumerable<MemoryOptimizationRecommendation> GetMemoryOptimizations(string transformationId);
}

/// <summary>
/// Default implementation of performance optimizer.
/// </summary>
public class PerformanceOptimizer : IPerformanceOptimizer
{
    private readonly ITransformationPerformanceMonitor _performanceMonitor;
    private readonly ILogger<PerformanceOptimizer> _logger;
    private readonly ConcurrentDictionary<string, OptimizationCache> _optimizationCache;

    /// <summary>
    /// Initializes a new instance of the PerformanceOptimizer class.
    /// </summary>
    /// <param name="performanceMonitor">The performance monitor</param>
    /// <param name="logger">The logger instance</param>
    public PerformanceOptimizer(ITransformationPerformanceMonitor performanceMonitor, ILogger<PerformanceOptimizer> logger)
    {
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optimizationCache = new ConcurrentDictionary<string, OptimizationCache>();
    }

    /// <inheritdoc />
    public OptimizationAnalysis AnalyzePerformance(string transformationId)
    {
        var stats = _performanceMonitor.GetStatistics(transformationId);
        if (stats == null)
        {
            return new OptimizationAnalysis
            {
                TransformationId = transformationId,
                AnalysisDate = DateTimeOffset.UtcNow,
                OverallScore = 0,
                Issues = new List<PerformanceIssue>
                {
                    new PerformanceIssue
                    {
                        Severity = IssueSeverity.Low,
                        Category = "Data",
                        Description = "No performance data available for analysis",
                        Recommendation = "Execute the transformation to collect performance metrics"
                    }
                }
            };
        }

        var analysis = new OptimizationAnalysis
        {
            TransformationId = transformationId,
            AnalysisDate = DateTimeOffset.UtcNow,
            Statistics = stats
        };

        var issues = new List<PerformanceIssue>();

        // Analyze throughput
        if (stats.ThroughputRecordsPerSecond < 50)
        {
            issues.Add(new PerformanceIssue
            {
                Severity = IssueSeverity.High,
                Category = "Throughput",
                Description = $"Low throughput: {stats.ThroughputRecordsPerSecond:F1} records/sec",
                Recommendation = "Consider batch processing, parallel execution, or algorithm optimization"
            });
        }
        else if (stats.ThroughputRecordsPerSecond < 200)
        {
            issues.Add(new PerformanceIssue
            {
                Severity = IssueSeverity.Medium,
                Category = "Throughput",
                Description = $"Moderate throughput: {stats.ThroughputRecordsPerSecond:F1} records/sec",
                Recommendation = "Monitor performance and consider optimization if processing large datasets"
            });
        }

        // Analyze error rate
        if (stats.ErrorRate > 5)
        {
            issues.Add(new PerformanceIssue
            {
                Severity = stats.ErrorRate > 15 ? IssueSeverity.Critical : IssueSeverity.High,
                Category = "Reliability",
                Description = $"High error rate: {stats.ErrorRate:F1}%",
                Recommendation = "Improve data validation and error handling logic"
            });
        }

        // Analyze memory usage
        var memoryMB = stats.PeakMemoryUsageBytes / (1024.0 * 1024.0);
        if (memoryMB > 500)
        {
            issues.Add(new PerformanceIssue
            {
                Severity = memoryMB > 1000 ? IssueSeverity.High : IssueSeverity.Medium,
                Category = "Memory",
                Description = $"High memory usage: {memoryMB:F1} MB",
                Recommendation = "Implement streaming processing or reduce batch sizes"
            });
        }

        // Analyze processing time variance
        var timeVarianceMs = (stats.MaxProcessingTime - stats.MinProcessingTime).TotalMilliseconds;
        if (timeVarianceMs > 5000)
        {
            issues.Add(new PerformanceIssue
            {
                Severity = IssueSeverity.Medium,
                Category = "Consistency",
                Description = $"High processing time variance: {timeVarianceMs:F0}ms",
                Recommendation = "Investigate data complexity variations or optimize for worst-case scenarios"
            });
        }

        analysis.Issues = issues;
        analysis.OverallScore = CalculateOverallScore(stats, issues);

        _logger.LogDebug("Performance analysis completed for transformation {TransformationId}. Score: {Score}, Issues: {IssueCount}",
            transformationId, analysis.OverallScore, issues.Count);

        return analysis;
    }

    /// <inheritdoc />
    public int GetOptimalBatchSize(string transformationId, double targetThroughput = 1000)
    {
        var stats = _performanceMonitor.GetStatistics(transformationId);
        if (stats == null) return 100; // Default batch size

        // Use cached result if available and recent
        if (_optimizationCache.TryGetValue(transformationId, out var cache) && 
            cache.LastUpdated > DateTimeOffset.UtcNow.AddMinutes(-30))
        {
            return cache.OptimalBatchSize;
        }

        // Calculate optimal batch size based on current performance
        var currentThroughput = stats.ThroughputRecordsPerSecond;
        var avgProcessingTimeMs = stats.AverageProcessingTime.TotalMilliseconds;

        int optimalBatchSize;
        if (currentThroughput < targetThroughput && avgProcessingTimeMs < 100)
        {
            // Increase batch size for better throughput
            optimalBatchSize = Math.Min(1000, (int)(targetThroughput / currentThroughput * 100));
        }
        else if (avgProcessingTimeMs > 1000)
        {
            // Decrease batch size for better responsiveness
            optimalBatchSize = Math.Max(10, (int)(100 * 1000 / avgProcessingTimeMs));
        }
        else
        {
            // Current performance is acceptable
            optimalBatchSize = 100;
        }

        // Cache the result
        _optimizationCache.AddOrUpdate(transformationId,
            new OptimizationCache { OptimalBatchSize = optimalBatchSize, LastUpdated = DateTimeOffset.UtcNow },
            (key, existing) => 
            {
                existing.OptimalBatchSize = optimalBatchSize;
                existing.LastUpdated = DateTimeOffset.UtcNow;
                return existing;
            });

        _logger.LogDebug("Calculated optimal batch size for transformation {TransformationId}: {BatchSize}",
            transformationId, optimalBatchSize);

        return optimalBatchSize;
    }

    /// <inheritdoc />
    public ParallelExecutionRecommendation GetParallelExecutionRecommendation(string transformationId)
    {
        var stats = _performanceMonitor.GetStatistics(transformationId);
        if (stats == null)
        {
            return new ParallelExecutionRecommendation
            {
                IsRecommended = false,
                Reason = "Insufficient performance data",
                RecommendedDegreeOfParallelism = 1
            };
        }

        var recommendation = new ParallelExecutionRecommendation();

        // Recommend parallel execution if:
        // 1. Processing time per record is significant (> 10ms)
        // 2. Throughput is low (< 500 records/sec)
        // 3. Error rate is acceptable (< 10%)
        var avgProcessingTimeMs = stats.AverageProcessingTime.TotalMilliseconds;
        var shouldParallelize = avgProcessingTimeMs > 10 && 
                               stats.ThroughputRecordsPerSecond < 500 && 
                               stats.ErrorRate < 10;

        recommendation.IsRecommended = shouldParallelize;

        if (shouldParallelize)
        {
            // Calculate recommended degree of parallelism
            var cpuCores = Environment.ProcessorCount;
            var recommendedDop = Math.Min(cpuCores, Math.Max(2, (int)(avgProcessingTimeMs / 10)));
            
            recommendation.RecommendedDegreeOfParallelism = recommendedDop;
            recommendation.Reason = $"Processing time ({avgProcessingTimeMs:F1}ms) and low throughput ({stats.ThroughputRecordsPerSecond:F1} rec/sec) suggest parallel execution would help";
            recommendation.EstimatedSpeedup = Math.Min(recommendedDop * 0.8, cpuCores * 0.6); // Conservative estimate
        }
        else
        {
            recommendation.RecommendedDegreeOfParallelism = 1;
            recommendation.Reason = avgProcessingTimeMs <= 10 
                ? "Fast processing time doesn't justify parallel overhead"
                : stats.ErrorRate >= 10 
                    ? "High error rate suggests sequential processing for better error handling"
                    : "Current performance is acceptable for sequential processing";
        }

        return recommendation;
    }

    /// <inheritdoc />
    public IEnumerable<MemoryOptimizationRecommendation> GetMemoryOptimizations(string transformationId)
    {
        var stats = _performanceMonitor.GetStatistics(transformationId);
        if (stats == null) return Enumerable.Empty<MemoryOptimizationRecommendation>();

        var recommendations = new List<MemoryOptimizationRecommendation>();
        var memoryMB = stats.PeakMemoryUsageBytes / (1024.0 * 1024.0);

        if (memoryMB > 100)
        {
            recommendations.Add(new MemoryOptimizationRecommendation
            {
                Type = MemoryOptimizationType.BatchSizeReduction,
                Description = $"High memory usage ({memoryMB:F1} MB). Consider reducing batch size.",
                EstimatedSavings = "20-40% memory reduction",
                ImplementationComplexity = "Low"
            });
        }

        if (memoryMB > 500)
        {
            recommendations.Add(new MemoryOptimizationRecommendation
            {
                Type = MemoryOptimizationType.StreamingProcessing,
                Description = "Very high memory usage. Implement streaming processing.",
                EstimatedSavings = "60-80% memory reduction",
                ImplementationComplexity = "High"
            });
        }

        if (stats.AverageMemoryUsageBytes > 0 && 
            stats.PeakMemoryUsageBytes > stats.AverageMemoryUsageBytes * 3)
        {
            recommendations.Add(new MemoryOptimizationRecommendation
            {
                Type = MemoryOptimizationType.MemoryPooling,
                Description = "High memory variance suggests object pooling could help.",
                EstimatedSavings = "15-30% memory reduction",
                ImplementationComplexity = "Medium"
            });
        }

        return recommendations;
    }

    /// <summary>
    /// Calculates an overall performance score.
    /// </summary>
    /// <param name="stats">Performance statistics</param>
    /// <param name="issues">Performance issues</param>
    /// <returns>Score from 0-100</returns>
    private static int CalculateOverallScore(TransformationPerformanceStats stats, List<PerformanceIssue> issues)
    {
        var score = 100;

        // Deduct points for issues
        foreach (var issue in issues)
        {
            score -= issue.Severity switch
            {
                IssueSeverity.Critical => 30,
                IssueSeverity.High => 20,
                IssueSeverity.Medium => 10,
                IssueSeverity.Low => 5,
                _ => 0
            };
        }

        // Bonus points for good performance
        if (stats.ThroughputRecordsPerSecond > 1000) score += 10;
        if (stats.ErrorRate < 1) score += 10;
        if (stats.SuccessRate > 99) score += 5;

        return Math.Max(0, Math.Min(100, score));
    }
}

/// <summary>
/// Cache for optimization calculations.
/// </summary>
internal class OptimizationCache
{
    public int OptimalBatchSize { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
}
