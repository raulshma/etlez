using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Transformation.Performance;

/// <summary>
/// Default implementation of transformation performance monitor.
/// </summary>
public class TransformationPerformanceMonitor : ITransformationPerformanceMonitor, IDisposable
{
    private readonly ILogger<TransformationPerformanceMonitor> _logger;
    private readonly ConcurrentDictionary<string, TransformationPerformanceStats> _statistics;
    private readonly ConcurrentDictionary<string, List<SessionStatistics>> _sessionHistory;

    /// <summary>
    /// Initializes a new instance of the TransformationPerformanceMonitor class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public TransformationPerformanceMonitor(ILogger<TransformationPerformanceMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statistics = new ConcurrentDictionary<string, TransformationPerformanceStats>();
        _sessionHistory = new ConcurrentDictionary<string, List<SessionStatistics>>();
    }

    /// <inheritdoc />
    public IPerformanceSession StartSession(string transformationId, string transformationName)
    {
        if (string.IsNullOrWhiteSpace(transformationId))
            throw new ArgumentException("Transformation ID cannot be empty", nameof(transformationId));

        var session = new PerformanceSession(transformationId, transformationName, this, _logger);
        
        _logger.LogDebug("Started performance monitoring session {SessionId} for transformation {TransformationId}", 
            session.SessionId, transformationId);

        return session;
    }

    /// <inheritdoc />
    public TransformationPerformanceStats? GetStatistics(string transformationId)
    {
        return _statistics.TryGetValue(transformationId, out var stats) ? stats : null;
    }

    /// <inheritdoc />
    public IEnumerable<TransformationPerformanceStats> GetAllStatistics()
    {
        return _statistics.Values.ToList();
    }

    /// <inheritdoc />
    public void ResetStatistics(string transformationId)
    {
        _statistics.TryRemove(transformationId, out _);
        _sessionHistory.TryRemove(transformationId, out _);
        _logger.LogInformation("Reset performance statistics for transformation {TransformationId}", transformationId);
    }

    /// <inheritdoc />
    public void ResetAllStatistics()
    {
        var count = _statistics.Count;
        _statistics.Clear();
        _sessionHistory.Clear();
        _logger.LogInformation("Reset all performance statistics ({Count} transformations)", count);
    }

    /// <inheritdoc />
    public IEnumerable<PerformanceRecommendation> GetRecommendations(string transformationId)
    {
        var stats = GetStatistics(transformationId);
        if (stats == null) return Enumerable.Empty<PerformanceRecommendation>();

        var recommendations = new List<PerformanceRecommendation>();

        // Analyze error rate
        if (stats.ErrorRate > 10)
        {
            recommendations.Add(new PerformanceRecommendation
            {
                Type = RecommendationType.ErrorHandling,
                Title = "High Error Rate Detected",
                Description = $"Error rate is {stats.ErrorRate:F1}%. Consider improving error handling and data validation.",
                Priority = stats.ErrorRate > 25 ? RecommendationPriority.Critical : RecommendationPriority.High,
                EstimatedImpact = "Reduce processing failures and improve data quality",
                ImplementationEffort = "Medium"
            });
        }

        // Analyze throughput
        if (stats.ThroughputRecordsPerSecond < 100)
        {
            recommendations.Add(new PerformanceRecommendation
            {
                Type = RecommendationType.Throughput,
                Title = "Low Throughput Performance",
                Description = $"Current throughput is {stats.ThroughputRecordsPerSecond:F1} records/sec. Consider optimization.",
                Priority = RecommendationPriority.Medium,
                EstimatedImpact = "Improve processing speed and reduce execution time",
                ImplementationEffort = "Medium to High"
            });
        }

        // Analyze memory usage
        if (stats.PeakMemoryUsageBytes > 1024 * 1024 * 1024) // > 1GB
        {
            recommendations.Add(new PerformanceRecommendation
            {
                Type = RecommendationType.Memory,
                Title = "High Memory Usage",
                Description = $"Peak memory usage is {stats.PeakMemoryUsageBytes / (1024 * 1024):F1} MB. Consider memory optimization.",
                Priority = RecommendationPriority.Medium,
                EstimatedImpact = "Reduce memory footprint and improve scalability",
                ImplementationEffort = "Medium"
            });
        }

        // Analyze processing time variance
        var timeVariance = stats.MaxProcessingTime.TotalMilliseconds - stats.MinProcessingTime.TotalMilliseconds;
        if (timeVariance > 1000) // > 1 second variance
        {
            recommendations.Add(new PerformanceRecommendation
            {
                Type = RecommendationType.Configuration,
                Title = "High Processing Time Variance",
                Description = "Processing times vary significantly. Consider batch size optimization or parallel processing.",
                Priority = RecommendationPriority.Low,
                EstimatedImpact = "More consistent performance",
                ImplementationEffort = "Low to Medium"
            });
        }

        return recommendations;
    }

    /// <summary>
    /// Updates statistics when a session completes.
    /// </summary>
    /// <param name="sessionStats">The session statistics</param>
    internal void UpdateStatistics(SessionStatistics sessionStats)
    {
        var transformationId = sessionStats.SessionId.Split('_')[0]; // Extract transformation ID from session ID
        
        _statistics.AddOrUpdate(transformationId, 
            key => CreateInitialStats(sessionStats, transformationId),
            (key, existing) => UpdateExistingStats(existing, sessionStats));

        // Store session history
        _sessionHistory.AddOrUpdate(transformationId,
            new List<SessionStatistics> { sessionStats },
            (key, existing) => 
            {
                existing.Add(sessionStats);
                // Keep only last 100 sessions
                if (existing.Count > 100)
                {
                    existing.RemoveAt(0);
                }
                return existing;
            });
    }

    /// <summary>
    /// Creates initial statistics from the first session.
    /// </summary>
    /// <param name="sessionStats">The session statistics</param>
    /// <param name="transformationId">The transformation ID</param>
    /// <returns>Initial performance statistics</returns>
    private static TransformationPerformanceStats CreateInitialStats(SessionStatistics sessionStats, string transformationId)
    {
        return new TransformationPerformanceStats
        {
            TransformationId = transformationId,
            TotalRecordsProcessed = sessionStats.RecordsProcessed,
            SuccessfulRecords = sessionStats.SuccessfulRecords,
            FailedRecords = sessionStats.FailedRecords,
            TotalProcessingTime = sessionStats.TotalProcessingTime,
            AverageProcessingTime = sessionStats.RecordsProcessed > 0 
                ? TimeSpan.FromTicks(sessionStats.TotalProcessingTime.Ticks / sessionStats.RecordsProcessed) 
                : TimeSpan.Zero,
            MinProcessingTime = sessionStats.TotalProcessingTime,
            MaxProcessingTime = sessionStats.TotalProcessingTime,
            ThroughputRecordsPerSecond = sessionStats.ThroughputRecordsPerSecond,
            PeakMemoryUsageBytes = sessionStats.PeakMemoryUsageBytes,
            AverageMemoryUsageBytes = sessionStats.PeakMemoryUsageBytes,
            TotalErrors = sessionStats.ErrorCount,
            TotalWarnings = sessionStats.WarningCount,
            FirstExecution = sessionStats.StartTime,
            LastExecution = sessionStats.EndTime ?? DateTimeOffset.UtcNow,
            TotalSessions = 1
        };
    }

    /// <summary>
    /// Updates existing statistics with new session data.
    /// </summary>
    /// <param name="existing">The existing statistics</param>
    /// <param name="sessionStats">The new session statistics</param>
    /// <returns>Updated performance statistics</returns>
    private static TransformationPerformanceStats UpdateExistingStats(TransformationPerformanceStats existing, SessionStatistics sessionStats)
    {
        var totalRecords = existing.TotalRecordsProcessed + sessionStats.RecordsProcessed;
        var totalTime = existing.TotalProcessingTime + sessionStats.TotalProcessingTime;

        existing.TotalRecordsProcessed = totalRecords;
        existing.SuccessfulRecords += sessionStats.SuccessfulRecords;
        existing.FailedRecords += sessionStats.FailedRecords;
        existing.TotalProcessingTime = totalTime;
        existing.AverageProcessingTime = totalRecords > 0 
            ? TimeSpan.FromTicks(totalTime.Ticks / totalRecords) 
            : TimeSpan.Zero;
        existing.MinProcessingTime = sessionStats.TotalProcessingTime < existing.MinProcessingTime 
            ? sessionStats.TotalProcessingTime 
            : existing.MinProcessingTime;
        existing.MaxProcessingTime = sessionStats.TotalProcessingTime > existing.MaxProcessingTime 
            ? sessionStats.TotalProcessingTime 
            : existing.MaxProcessingTime;
        existing.ThroughputRecordsPerSecond = totalTime.TotalSeconds > 0 
            ? totalRecords / totalTime.TotalSeconds 
            : 0;
        existing.PeakMemoryUsageBytes = Math.Max(existing.PeakMemoryUsageBytes, sessionStats.PeakMemoryUsageBytes);
        existing.AverageMemoryUsageBytes = (existing.AverageMemoryUsageBytes * existing.TotalSessions + sessionStats.PeakMemoryUsageBytes) / (existing.TotalSessions + 1);
        existing.TotalErrors += sessionStats.ErrorCount;
        existing.TotalWarnings += sessionStats.WarningCount;
        existing.LastExecution = sessionStats.EndTime ?? DateTimeOffset.UtcNow;
        existing.TotalSessions++;

        return existing;
    }

    /// <summary>
    /// Gets session history for a transformation.
    /// </summary>
    /// <param name="transformationId">The transformation ID</param>
    /// <returns>Session history</returns>
    public IEnumerable<SessionStatistics> GetSessionHistory(string transformationId)
    {
        return _sessionHistory.TryGetValue(transformationId, out var history)
            ? history.ToList()
            : Enumerable.Empty<SessionStatistics>();
    }

    /// <summary>
    /// Disposes the performance monitor and clears all statistics.
    /// </summary>
    public void Dispose()
    {
        _statistics.Clear();
        _sessionHistory.Clear();
        _logger.LogDebug("TransformationPerformanceMonitor disposed and statistics cleared");
    }
}

/// <summary>
/// Implementation of performance session.
/// </summary>
internal class PerformanceSession : IPerformanceSession
{
    private readonly TransformationPerformanceMonitor _monitor;
    private readonly ILogger _logger;
    private readonly object _lock = new object();
    private readonly List<TimeSpan> _processingTimes = new List<TimeSpan>();
    private readonly List<long> _memoryUsages = new List<long>();
    private readonly List<Exception> _errors = new List<Exception>();
    private readonly List<string> _warnings = new List<string>();
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the PerformanceSession class.
    /// </summary>
    /// <param name="transformationId">The transformation ID</param>
    /// <param name="transformationName">The transformation name</param>
    /// <param name="monitor">The performance monitor</param>
    /// <param name="logger">The logger instance</param>
    public PerformanceSession(string transformationId, string transformationName, TransformationPerformanceMonitor monitor, ILogger logger)
    {
        TransformationId = transformationId;
        TransformationName = transformationName;
        _monitor = monitor;
        _logger = logger;
        SessionId = $"{transformationId}_{Guid.NewGuid():N}";
        StartTime = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public string SessionId { get; }

    /// <inheritdoc />
    public string TransformationId { get; }

    /// <summary>
    /// Gets the transformation name.
    /// </summary>
    public string TransformationName { get; }

    /// <inheritdoc />
    public DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets the session end time.
    /// </summary>
    public DateTimeOffset? EndTime { get; private set; }

    /// <summary>
    /// Gets the number of successful records processed.
    /// </summary>
    public long SuccessfulRecords { get; private set; }

    /// <summary>
    /// Gets the number of failed records processed.
    /// </summary>
    public long FailedRecords { get; private set; }

    /// <inheritdoc />
    public void RecordProcessing(TimeSpan processingTime, bool success = true)
    {
        lock (_lock)
        {
            _processingTimes.Add(processingTime);
            if (success)
                SuccessfulRecords++;
            else
                FailedRecords++;
        }
    }

    /// <inheritdoc />
    public void RecordMemoryUsage(long memoryUsageBytes)
    {
        lock (_lock)
        {
            _memoryUsages.Add(memoryUsageBytes);
        }
    }

    /// <inheritdoc />
    public void RecordError(Exception error)
    {
        lock (_lock)
        {
            _errors.Add(error);
        }
        _logger.LogError(error, "Error recorded in performance session {SessionId}", SessionId);
    }

    /// <inheritdoc />
    public void RecordWarning(string warning)
    {
        lock (_lock)
        {
            _warnings.Add(warning);
        }
        _logger.LogWarning("Warning recorded in performance session {SessionId}: {Warning}", SessionId, warning);
    }

    /// <inheritdoc />
    public SessionStatistics GetStatistics()
    {
        lock (_lock)
        {
            return new SessionStatistics
            {
                SessionId = SessionId,
                StartTime = StartTime,
                EndTime = EndTime,
                RecordsProcessed = SuccessfulRecords + FailedRecords,
                SuccessfulRecords = SuccessfulRecords,
                FailedRecords = FailedRecords,
                TotalProcessingTime = _processingTimes.Count > 0
                    ? TimeSpan.FromTicks(_processingTimes.Sum(t => t.Ticks))
                    : TimeSpan.Zero,
                PeakMemoryUsageBytes = _memoryUsages.Count > 0 ? _memoryUsages.Max() : 0,
                ErrorCount = _errors.Count,
                WarningCount = _warnings.Count
            };
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        EndTime = DateTimeOffset.UtcNow;
        var statistics = GetStatistics();
        _monitor.UpdateStatistics(statistics);

        _logger.LogDebug("Completed performance monitoring session {SessionId} for transformation {TransformationId}. " +
                        "Records: {RecordsProcessed}, Duration: {Duration}ms, Throughput: {Throughput:F1} records/sec",
            SessionId, TransformationId, statistics.RecordsProcessed,
            statistics.Duration.TotalMilliseconds, statistics.ThroughputRecordsPerSecond);

        _disposed = true;
    }
}
