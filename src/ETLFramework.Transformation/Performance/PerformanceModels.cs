namespace ETLFramework.Transformation.Performance;

/// <summary>
/// Represents a comprehensive performance analysis.
/// </summary>
public class OptimizationAnalysis
{
    /// <summary>
    /// Gets or sets the transformation ID.
    /// </summary>
    public string TransformationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the analysis date.
    /// </summary>
    public DateTimeOffset AnalysisDate { get; set; }

    /// <summary>
    /// Gets or sets the overall performance score (0-100).
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// Gets or sets the performance statistics.
    /// </summary>
    public TransformationPerformanceStats? Statistics { get; set; }

    /// <summary>
    /// Gets or sets the identified performance issues.
    /// </summary>
    public List<PerformanceIssue> Issues { get; set; } = new List<PerformanceIssue>();

    /// <summary>
    /// Gets or sets the optimization recommendations.
    /// </summary>
    public List<OptimizationRecommendation> Recommendations { get; set; } = new List<OptimizationRecommendation>();

    /// <summary>
    /// Gets the performance grade based on the overall score.
    /// </summary>
    public string PerformanceGrade => OverallScore switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };

    /// <summary>
    /// Gets whether the performance is considered acceptable.
    /// </summary>
    public bool IsPerformanceAcceptable => OverallScore >= 70;
}

/// <summary>
/// Represents a performance issue.
/// </summary>
public class PerformanceIssue
{
    /// <summary>
    /// Gets or sets the issue severity.
    /// </summary>
    public IssueSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the issue category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issue description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recommendation to fix the issue.
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated impact of fixing the issue.
    /// </summary>
    public string EstimatedImpact { get; set; } = string.Empty;
}

/// <summary>
/// Severity levels for performance issues.
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// Low severity issue.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity issue.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity issue.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity issue.
    /// </summary>
    Critical
}

/// <summary>
/// Represents an optimization recommendation.
/// </summary>
public class OptimizationRecommendation
{
    /// <summary>
    /// Gets or sets the recommendation type.
    /// </summary>
    public OptimizationType Type { get; set; }

    /// <summary>
    /// Gets or sets the recommendation title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recommendation description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the implementation steps.
    /// </summary>
    public List<string> ImplementationSteps { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the estimated performance improvement.
    /// </summary>
    public string EstimatedImprovement { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the implementation complexity.
    /// </summary>
    public string ImplementationComplexity { get; set; } = string.Empty;
}

/// <summary>
/// Types of optimizations.
/// </summary>
public enum OptimizationType
{
    /// <summary>
    /// Batch size optimization.
    /// </summary>
    BatchSize,

    /// <summary>
    /// Parallel processing optimization.
    /// </summary>
    ParallelProcessing,

    /// <summary>
    /// Memory usage optimization.
    /// </summary>
    Memory,

    /// <summary>
    /// Algorithm optimization.
    /// </summary>
    Algorithm,

    /// <summary>
    /// Configuration optimization.
    /// </summary>
    Configuration,

    /// <summary>
    /// Infrastructure optimization.
    /// </summary>
    Infrastructure
}

/// <summary>
/// Recommendation for parallel execution.
/// </summary>
public class ParallelExecutionRecommendation
{
    /// <summary>
    /// Gets or sets whether parallel execution is recommended.
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// Gets or sets the recommended degree of parallelism.
    /// </summary>
    public int RecommendedDegreeOfParallelism { get; set; }

    /// <summary>
    /// Gets or sets the reason for the recommendation.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated speedup factor.
    /// </summary>
    public double EstimatedSpeedup { get; set; }

    /// <summary>
    /// Gets or sets any considerations or warnings.
    /// </summary>
    public List<string> Considerations { get; set; } = new List<string>();
}

/// <summary>
/// Memory optimization recommendation.
/// </summary>
public class MemoryOptimizationRecommendation
{
    /// <summary>
    /// Gets or sets the optimization type.
    /// </summary>
    public MemoryOptimizationType Type { get; set; }

    /// <summary>
    /// Gets or sets the recommendation description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated memory savings.
    /// </summary>
    public string EstimatedSavings { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the implementation complexity.
    /// </summary>
    public string ImplementationComplexity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the implementation steps.
    /// </summary>
    public List<string> ImplementationSteps { get; set; } = new List<string>();
}

/// <summary>
/// Types of memory optimizations.
/// </summary>
public enum MemoryOptimizationType
{
    /// <summary>
    /// Reduce batch size to lower memory usage.
    /// </summary>
    BatchSizeReduction,

    /// <summary>
    /// Implement streaming processing.
    /// </summary>
    StreamingProcessing,

    /// <summary>
    /// Use object pooling to reduce allocations.
    /// </summary>
    MemoryPooling,

    /// <summary>
    /// Optimize data structures.
    /// </summary>
    DataStructureOptimization,

    /// <summary>
    /// Implement garbage collection optimization.
    /// </summary>
    GarbageCollectionOptimization,

    /// <summary>
    /// Use memory-mapped files for large datasets.
    /// </summary>
    MemoryMappedFiles
}

/// <summary>
/// Performance benchmark result.
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// Gets or sets the benchmark name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation ID.
    /// </summary>
    public string TransformationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the benchmark date.
    /// </summary>
    public DateTimeOffset BenchmarkDate { get; set; }

    /// <summary>
    /// Gets or sets the number of records processed.
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total execution time.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the throughput in records per second.
    /// </summary>
    public double ThroughputRecordsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the peak memory usage in bytes.
    /// </summary>
    public long PeakMemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the CPU usage percentage.
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets the configuration used for the benchmark.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets any notes about the benchmark.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Performance trend analysis.
/// </summary>
public class PerformanceTrend
{
    /// <summary>
    /// Gets or sets the transformation ID.
    /// </summary>
    public string TransformationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the analysis period.
    /// </summary>
    public TimeSpan AnalysisPeriod { get; set; }

    /// <summary>
    /// Gets or sets the trend direction.
    /// </summary>
    public TrendDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the trend strength (0-1).
    /// </summary>
    public double Strength { get; set; }

    /// <summary>
    /// Gets or sets the throughput trend.
    /// </summary>
    public MetricTrend ThroughputTrend { get; set; } = new MetricTrend();

    /// <summary>
    /// Gets or sets the error rate trend.
    /// </summary>
    public MetricTrend ErrorRateTrend { get; set; } = new MetricTrend();

    /// <summary>
    /// Gets or sets the memory usage trend.
    /// </summary>
    public MetricTrend MemoryUsageTrend { get; set; } = new MetricTrend();
}

/// <summary>
/// Trend direction.
/// </summary>
public enum TrendDirection
{
    /// <summary>
    /// Performance is improving.
    /// </summary>
    Improving,

    /// <summary>
    /// Performance is stable.
    /// </summary>
    Stable,

    /// <summary>
    /// Performance is degrading.
    /// </summary>
    Degrading
}

/// <summary>
/// Trend for a specific metric.
/// </summary>
public class MetricTrend
{
    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public double CurrentValue { get; set; }

    /// <summary>
    /// Gets or sets the previous value.
    /// </summary>
    public double PreviousValue { get; set; }

    /// <summary>
    /// Gets or sets the percentage change.
    /// </summary>
    public double PercentageChange { get; set; }

    /// <summary>
    /// Gets or sets the trend direction.
    /// </summary>
    public TrendDirection Direction { get; set; }
}
