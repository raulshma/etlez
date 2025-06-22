namespace ETLFramework.Transformation.Performance;

/// <summary>
/// Interface for monitoring transformation performance.
/// </summary>
public interface ITransformationPerformanceMonitor
{
    /// <summary>
    /// Starts monitoring a transformation.
    /// </summary>
    /// <param name="transformationId">The transformation ID</param>
    /// <param name="transformationName">The transformation name</param>
    /// <returns>A performance session</returns>
    IPerformanceSession StartSession(string transformationId, string transformationName);

    /// <summary>
    /// Gets performance statistics for a transformation.
    /// </summary>
    /// <param name="transformationId">The transformation ID</param>
    /// <returns>Performance statistics</returns>
    TransformationPerformanceStats? GetStatistics(string transformationId);

    /// <summary>
    /// Gets performance statistics for all transformations.
    /// </summary>
    /// <returns>All performance statistics</returns>
    IEnumerable<TransformationPerformanceStats> GetAllStatistics();

    /// <summary>
    /// Resets performance statistics for a transformation.
    /// </summary>
    /// <param name="transformationId">The transformation ID</param>
    void ResetStatistics(string transformationId);

    /// <summary>
    /// Resets all performance statistics.
    /// </summary>
    void ResetAllStatistics();

    /// <summary>
    /// Gets performance recommendations for optimization.
    /// </summary>
    /// <param name="transformationId">The transformation ID</param>
    /// <returns>Performance recommendations</returns>
    IEnumerable<PerformanceRecommendation> GetRecommendations(string transformationId);
}

/// <summary>
/// Interface for a performance monitoring session.
/// </summary>
public interface IPerformanceSession : IDisposable
{
    /// <summary>
    /// Gets the session ID.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the transformation ID.
    /// </summary>
    string TransformationId { get; }

    /// <summary>
    /// Gets the session start time.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Records that a record was processed.
    /// </summary>
    /// <param name="processingTime">The time taken to process the record</param>
    /// <param name="success">Whether the processing was successful</param>
    void RecordProcessing(TimeSpan processingTime, bool success = true);

    /// <summary>
    /// Records memory usage.
    /// </summary>
    /// <param name="memoryUsageBytes">Memory usage in bytes</param>
    void RecordMemoryUsage(long memoryUsageBytes);

    /// <summary>
    /// Records an error.
    /// </summary>
    /// <param name="error">The error that occurred</param>
    void RecordError(Exception error);

    /// <summary>
    /// Records a warning.
    /// </summary>
    /// <param name="warning">The warning message</param>
    void RecordWarning(string warning);

    /// <summary>
    /// Gets the current session statistics.
    /// </summary>
    /// <returns>Session statistics</returns>
    SessionStatistics GetStatistics();
}

/// <summary>
/// Performance statistics for a transformation.
/// </summary>
public class TransformationPerformanceStats
{
    /// <summary>
    /// Gets or sets the transformation ID.
    /// </summary>
    public string TransformationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation name.
    /// </summary>
    public string TransformationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of records processed.
    /// </summary>
    public long TotalRecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of successful records.
    /// </summary>
    public long SuccessfulRecords { get; set; }

    /// <summary>
    /// Gets or sets the total number of failed records.
    /// </summary>
    public long FailedRecords { get; set; }

    /// <summary>
    /// Gets or sets the total processing time.
    /// </summary>
    public TimeSpan TotalProcessingTime { get; set; }

    /// <summary>
    /// Gets or sets the average processing time per record.
    /// </summary>
    public TimeSpan AverageProcessingTime { get; set; }

    /// <summary>
    /// Gets or sets the minimum processing time.
    /// </summary>
    public TimeSpan MinProcessingTime { get; set; }

    /// <summary>
    /// Gets or sets the maximum processing time.
    /// </summary>
    public TimeSpan MaxProcessingTime { get; set; }

    /// <summary>
    /// Gets or sets the throughput in records per second.
    /// </summary>
    public double ThroughputRecordsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the peak memory usage in bytes.
    /// </summary>
    public long PeakMemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the average memory usage in bytes.
    /// </summary>
    public long AverageMemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the total number of errors.
    /// </summary>
    public int TotalErrors { get; set; }

    /// <summary>
    /// Gets or sets the total number of warnings.
    /// </summary>
    public int TotalWarnings { get; set; }

    /// <summary>
    /// Gets or sets the first execution time.
    /// </summary>
    public DateTimeOffset? FirstExecution { get; set; }

    /// <summary>
    /// Gets or sets the last execution time.
    /// </summary>
    public DateTimeOffset? LastExecution { get; set; }

    /// <summary>
    /// Gets or sets the total number of sessions.
    /// </summary>
    public int TotalSessions { get; set; }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalRecordsProcessed > 0 
        ? (double)SuccessfulRecords / TotalRecordsProcessed * 100 
        : 0;

    /// <summary>
    /// Gets the error rate as a percentage.
    /// </summary>
    public double ErrorRate => TotalRecordsProcessed > 0 
        ? (double)FailedRecords / TotalRecordsProcessed * 100 
        : 0;
}

/// <summary>
/// Statistics for a performance session.
/// </summary>
public class SessionStatistics
{
    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the session end time.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the number of records processed in this session.
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of successful records in this session.
    /// </summary>
    public long SuccessfulRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of failed records in this session.
    /// </summary>
    public long FailedRecords { get; set; }

    /// <summary>
    /// Gets or sets the total processing time for this session.
    /// </summary>
    public TimeSpan TotalProcessingTime { get; set; }

    /// <summary>
    /// Gets or sets the peak memory usage in this session.
    /// </summary>
    public long PeakMemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of errors in this session.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the number of warnings in this session.
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Gets the session duration.
    /// </summary>
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? DateTimeOffset.UtcNow.Subtract(StartTime);

    /// <summary>
    /// Gets the throughput for this session.
    /// </summary>
    public double ThroughputRecordsPerSecond => Duration.TotalSeconds > 0 
        ? RecordsProcessed / Duration.TotalSeconds 
        : 0;
}

/// <summary>
/// Performance recommendation for optimization.
/// </summary>
public class PerformanceRecommendation
{
    /// <summary>
    /// Gets or sets the recommendation type.
    /// </summary>
    public RecommendationType Type { get; set; }

    /// <summary>
    /// Gets or sets the recommendation title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recommendation description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority level.
    /// </summary>
    public RecommendationPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets the estimated impact.
    /// </summary>
    public string EstimatedImpact { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the implementation effort.
    /// </summary>
    public string ImplementationEffort { get; set; } = string.Empty;
}

/// <summary>
/// Types of performance recommendations.
/// </summary>
public enum RecommendationType
{
    /// <summary>
    /// Memory optimization recommendation.
    /// </summary>
    Memory,

    /// <summary>
    /// CPU optimization recommendation.
    /// </summary>
    Cpu,

    /// <summary>
    /// Throughput optimization recommendation.
    /// </summary>
    Throughput,

    /// <summary>
    /// Error handling optimization recommendation.
    /// </summary>
    ErrorHandling,

    /// <summary>
    /// Configuration optimization recommendation.
    /// </summary>
    Configuration,

    /// <summary>
    /// Architecture optimization recommendation.
    /// </summary>
    Architecture
}

/// <summary>
/// Priority levels for recommendations.
/// </summary>
public enum RecommendationPriority
{
    /// <summary>
    /// Low priority recommendation.
    /// </summary>
    Low,

    /// <summary>
    /// Medium priority recommendation.
    /// </summary>
    Medium,

    /// <summary>
    /// High priority recommendation.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority recommendation.
    /// </summary>
    Critical
}
