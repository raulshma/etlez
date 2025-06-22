using System.Diagnostics;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Connectors.Factory;

/// <summary>
/// Provides health checking and monitoring capabilities for connectors.
/// </summary>
public class ConnectorHealthChecker
{
    private readonly ILogger<ConnectorHealthChecker> _logger;

    /// <summary>
    /// Initializes a new instance of the ConnectorHealthChecker class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public ConnectorHealthChecker(ILogger<ConnectorHealthChecker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Performs a comprehensive health check on a connector.
    /// </summary>
    /// <param name="connector">The connector to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A health check result</returns>
    public async Task<ConnectorHealthResult> CheckHealthAsync(IConnector connector, CancellationToken cancellationToken = default)
    {
        var result = new ConnectorHealthResult
        {
            ConnectorId = connector.Id,
            ConnectorName = connector.Name,
            ConnectorType = connector.ConnectorType,
            CheckTime = DateTimeOffset.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Starting health check for connector: {ConnectorName}", connector.Name);

            // Test basic connectivity
            await CheckConnectivityAsync(connector, result, cancellationToken);

            // Test configuration validation
            CheckConfiguration(connector, result);

            // Test metadata retrieval
            await CheckMetadataAsync(connector, result, cancellationToken);

            // Performance test (if connector supports it)
            if (connector is ISourceConnector<DataRecord> sourceConnector)
            {
                await CheckReadPerformanceAsync(sourceConnector, result, cancellationToken);
            }

            result.OverallStatus = DetermineOverallStatus(result);
            result.Duration = stopwatch.Elapsed;

            _logger.LogDebug("Health check completed for connector: {ConnectorName}, Status: {Status}, Duration: {Duration}ms",
                connector.Name, result.OverallStatus, result.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            result.OverallStatus = HealthStatus.Critical;
            result.Errors.Add($"Health check failed: {ex.Message}");
            _logger.LogError(ex, "Health check failed for connector: {ConnectorName}", connector.Name);
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// Performs a quick health check on multiple connectors.
    /// </summary>
    /// <param name="connectors">The connectors to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A summary of health check results</returns>
    public async Task<ConnectorHealthSummary> CheckMultipleAsync(IEnumerable<IConnector> connectors, CancellationToken cancellationToken = default)
    {
        var summary = new ConnectorHealthSummary
        {
            CheckTime = DateTimeOffset.UtcNow
        };

        var tasks = connectors.Select(async connector =>
        {
            try
            {
                return await CheckHealthAsync(connector, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check health for connector: {ConnectorName}", connector.Name);
                return new ConnectorHealthResult
                {
                    ConnectorId = connector.Id,
                    ConnectorName = connector.Name,
                    ConnectorType = connector.ConnectorType,
                    OverallStatus = HealthStatus.Critical,
                    CheckTime = DateTimeOffset.UtcNow,
                    Errors = { $"Health check failed: {ex.Message}" }
                };
            }
        });

        var results = await Task.WhenAll(tasks);
        summary.Results.AddRange(results);

        // Calculate summary statistics
        summary.TotalConnectors = results.Length;
        summary.HealthyConnectors = results.Count(r => r.OverallStatus == HealthStatus.Healthy);
        summary.WarningConnectors = results.Count(r => r.OverallStatus == HealthStatus.Warning);
        summary.CriticalConnectors = results.Count(r => r.OverallStatus == HealthStatus.Critical);
        summary.AverageResponseTime = results.Average(r => r.Duration.TotalMilliseconds);

        return summary;
    }

    /// <summary>
    /// Monitors connector performance over time.
    /// </summary>
    /// <param name="connector">The connector to monitor</param>
    /// <param name="duration">The monitoring duration</param>
    /// <param name="interval">The check interval</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance monitoring results</returns>
    public async Task<ConnectorPerformanceMonitor> MonitorPerformanceAsync(
        IConnector connector, 
        TimeSpan duration, 
        TimeSpan interval, 
        CancellationToken cancellationToken = default)
    {
        var monitor = new ConnectorPerformanceMonitor
        {
            ConnectorId = connector.Id,
            ConnectorName = connector.Name,
            StartTime = DateTimeOffset.UtcNow,
            MonitoringDuration = duration,
            CheckInterval = interval
        };

        var endTime = DateTimeOffset.UtcNow.Add(duration);

        _logger.LogInformation("Starting performance monitoring for connector: {ConnectorName}, Duration: {Duration}",
            connector.Name, duration);

        while (DateTimeOffset.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var healthResult = await CheckHealthAsync(connector, cancellationToken);
                monitor.HealthChecks.Add(healthResult);

                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during performance monitoring for connector: {ConnectorName}", connector.Name);
            }
        }

        monitor.EndTime = DateTimeOffset.UtcNow;
        monitor.CalculateStatistics();

        _logger.LogInformation("Performance monitoring completed for connector: {ConnectorName}, Checks: {CheckCount}",
            connector.Name, monitor.HealthChecks.Count);

        return monitor;
    }

    /// <summary>
    /// Checks connectivity of a connector.
    /// </summary>
    /// <param name="connector">The connector</param>
    /// <param name="result">The health result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task CheckConnectivityAsync(IConnector connector, ConnectorHealthResult result, CancellationToken cancellationToken)
    {
        try
        {
            var connectivityStopwatch = Stopwatch.StartNew();
            var testResult = await connector.TestConnectionAsync(cancellationToken);
            connectivityStopwatch.Stop();

            result.ConnectivityCheck = new HealthCheck
            {
                Name = "Connectivity",
                Status = testResult.IsSuccessful ? HealthStatus.Healthy : HealthStatus.Critical,
                Message = testResult.Message ?? "No message provided",
                Duration = connectivityStopwatch.Elapsed
            };

            if (!testResult.IsSuccessful)
            {
                result.Errors.Add($"Connectivity test failed: {testResult.Message}");
            }
        }
        catch (Exception ex)
        {
            result.ConnectivityCheck = new HealthCheck
            {
                Name = "Connectivity",
                Status = HealthStatus.Critical,
                Message = $"Connectivity test error: {ex.Message}",
                Duration = TimeSpan.Zero
            };
            result.Errors.Add($"Connectivity test error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks configuration validation.
    /// </summary>
    /// <param name="connector">The connector</param>
    /// <param name="result">The health result</param>
    private void CheckConfiguration(IConnector connector, ConnectorHealthResult result)
    {
        try
        {
            var configStopwatch = Stopwatch.StartNew();

            // Check if connector has configuration
            var hasValidConfig = !string.IsNullOrEmpty(connector.Name) &&
                                !string.IsNullOrEmpty(connector.ConnectorType);

            configStopwatch.Stop();

            result.ConfigurationCheck = new HealthCheck
            {
                Name = "Configuration",
                Status = hasValidConfig ? HealthStatus.Healthy : HealthStatus.Warning,
                Message = hasValidConfig ? "Configuration appears valid" : "Configuration may have issues",
                Duration = configStopwatch.Elapsed
            };

            if (!hasValidConfig)
            {
                result.Warnings.Add("Configuration validation: Missing required properties");
            }
        }
        catch (Exception ex)
        {
            result.ConfigurationCheck = new HealthCheck
            {
                Name = "Configuration",
                Status = HealthStatus.Warning,
                Message = $"Configuration validation error: {ex.Message}",
                Duration = TimeSpan.Zero
            };
            result.Warnings.Add($"Configuration validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks metadata retrieval.
    /// </summary>
    /// <param name="connector">The connector</param>
    /// <param name="result">The health result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task CheckMetadataAsync(IConnector connector, ConnectorHealthResult result, CancellationToken cancellationToken)
    {
        try
        {
            var metadataStopwatch = Stopwatch.StartNew();
            var metadata = await connector.GetMetadataAsync(cancellationToken);
            metadataStopwatch.Stop();

            result.MetadataCheck = new HealthCheck
            {
                Name = "Metadata",
                Status = HealthStatus.Healthy,
                Message = $"Metadata retrieved successfully (Version: {metadata.Version})",
                Duration = metadataStopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            result.MetadataCheck = new HealthCheck
            {
                Name = "Metadata",
                Status = HealthStatus.Warning,
                Message = $"Metadata retrieval error: {ex.Message}",
                Duration = TimeSpan.Zero
            };
            result.Warnings.Add($"Metadata retrieval error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks read performance for source connectors.
    /// </summary>
    /// <param name="sourceConnector">The source connector</param>
    /// <param name="result">The health result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task CheckReadPerformanceAsync(ISourceConnector<DataRecord> sourceConnector, ConnectorHealthResult result, CancellationToken cancellationToken)
    {
        try
        {
            var performanceStopwatch = Stopwatch.StartNew();
            
            // Try to get estimated record count
            var estimatedCount = await sourceConnector.GetEstimatedRecordCountAsync(cancellationToken);
            
            performanceStopwatch.Stop();

            var message = estimatedCount.HasValue 
                ? $"Performance check completed, estimated records: {estimatedCount.Value}"
                : "Performance check completed, record count unavailable";

            result.PerformanceCheck = new HealthCheck
            {
                Name = "Performance",
                Status = HealthStatus.Healthy,
                Message = message,
                Duration = performanceStopwatch.Elapsed
            };

            if (estimatedCount.HasValue)
            {
                result.EstimatedRecordCount = estimatedCount.Value;
            }
        }
        catch (Exception ex)
        {
            result.PerformanceCheck = new HealthCheck
            {
                Name = "Performance",
                Status = HealthStatus.Warning,
                Message = $"Performance check error: {ex.Message}",
                Duration = TimeSpan.Zero
            };
            result.Warnings.Add($"Performance check error: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines the overall health status based on individual checks.
    /// </summary>
    /// <param name="result">The health result</param>
    /// <returns>The overall health status</returns>
    private static HealthStatus DetermineOverallStatus(ConnectorHealthResult result)
    {
        var checks = new[] { result.ConnectivityCheck, result.ConfigurationCheck, result.MetadataCheck, result.PerformanceCheck }
            .Where(c => c != null);

        if (checks.Any(c => c!.Status == HealthStatus.Critical))
            return HealthStatus.Critical;

        if (checks.Any(c => c!.Status == HealthStatus.Warning))
            return HealthStatus.Warning;

        return HealthStatus.Healthy;
    }
}

/// <summary>
/// Represents the health status of a component.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// The component is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// The component has warnings but is functional.
    /// </summary>
    Warning,

    /// <summary>
    /// The component is in a critical state.
    /// </summary>
    Critical
}

/// <summary>
/// Represents the result of a health check.
/// </summary>
public class HealthCheck
{
    /// <summary>
    /// Gets or sets the check name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the check message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the check duration.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Represents the result of a connector health check.
/// </summary>
public class ConnectorHealthResult
{
    /// <summary>
    /// Initializes a new instance of the ConnectorHealthResult class.
    /// </summary>
    public ConnectorHealthResult()
    {
        Errors = new List<string>();
        Warnings = new List<string>();
    }

    /// <summary>
    /// Gets or sets the connector ID.
    /// </summary>
    public Guid ConnectorId { get; set; }

    /// <summary>
    /// Gets or sets the connector name.
    /// </summary>
    public string ConnectorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connector type.
    /// </summary>
    public string ConnectorType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overall health status.
    /// </summary>
    public HealthStatus OverallStatus { get; set; }

    /// <summary>
    /// Gets or sets the check time.
    /// </summary>
    public DateTimeOffset CheckTime { get; set; }

    /// <summary>
    /// Gets or sets the total check duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the connectivity check result.
    /// </summary>
    public HealthCheck? ConnectivityCheck { get; set; }

    /// <summary>
    /// Gets or sets the configuration check result.
    /// </summary>
    public HealthCheck? ConfigurationCheck { get; set; }

    /// <summary>
    /// Gets or sets the metadata check result.
    /// </summary>
    public HealthCheck? MetadataCheck { get; set; }

    /// <summary>
    /// Gets or sets the performance check result.
    /// </summary>
    public HealthCheck? PerformanceCheck { get; set; }

    /// <summary>
    /// Gets the list of errors.
    /// </summary>
    public List<string> Errors { get; }

    /// <summary>
    /// Gets the list of warnings.
    /// </summary>
    public List<string> Warnings { get; }

    /// <summary>
    /// Gets or sets the estimated record count (for source connectors).
    /// </summary>
    public long? EstimatedRecordCount { get; set; }
}

/// <summary>
/// Represents a summary of multiple connector health checks.
/// </summary>
public class ConnectorHealthSummary
{
    /// <summary>
    /// Initializes a new instance of the ConnectorHealthSummary class.
    /// </summary>
    public ConnectorHealthSummary()
    {
        Results = new List<ConnectorHealthResult>();
    }

    /// <summary>
    /// Gets or sets the check time.
    /// </summary>
    public DateTimeOffset CheckTime { get; set; }

    /// <summary>
    /// Gets the list of individual health check results.
    /// </summary>
    public List<ConnectorHealthResult> Results { get; }

    /// <summary>
    /// Gets or sets the total number of connectors checked.
    /// </summary>
    public int TotalConnectors { get; set; }

    /// <summary>
    /// Gets or sets the number of healthy connectors.
    /// </summary>
    public int HealthyConnectors { get; set; }

    /// <summary>
    /// Gets or sets the number of connectors with warnings.
    /// </summary>
    public int WarningConnectors { get; set; }

    /// <summary>
    /// Gets or sets the number of critical connectors.
    /// </summary>
    public int CriticalConnectors { get; set; }

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTime { get; set; }
}

/// <summary>
/// Represents performance monitoring results for a connector.
/// </summary>
public class ConnectorPerformanceMonitor
{
    /// <summary>
    /// Initializes a new instance of the ConnectorPerformanceMonitor class.
    /// </summary>
    public ConnectorPerformanceMonitor()
    {
        HealthChecks = new List<ConnectorHealthResult>();
    }

    /// <summary>
    /// Gets or sets the connector ID.
    /// </summary>
    public Guid ConnectorId { get; set; }

    /// <summary>
    /// Gets or sets the connector name.
    /// </summary>
    public string ConnectorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the monitoring start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the monitoring end time.
    /// </summary>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Gets or sets the monitoring duration.
    /// </summary>
    public TimeSpan MonitoringDuration { get; set; }

    /// <summary>
    /// Gets or sets the check interval.
    /// </summary>
    public TimeSpan CheckInterval { get; set; }

    /// <summary>
    /// Gets the list of health check results.
    /// </summary>
    public List<ConnectorHealthResult> HealthChecks { get; }

    /// <summary>
    /// Gets or sets the average response time.
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the minimum response time.
    /// </summary>
    public double MinResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the maximum response time.
    /// </summary>
    public double MaxResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the success rate percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Calculates performance statistics from the health checks.
    /// </summary>
    public void CalculateStatistics()
    {
        if (HealthChecks.Count == 0)
        {
            return;
        }

        var responseTimes = HealthChecks.Select(h => h.Duration.TotalMilliseconds).ToList();
        AverageResponseTime = responseTimes.Average();
        MinResponseTime = responseTimes.Min();
        MaxResponseTime = responseTimes.Max();

        var successfulChecks = HealthChecks.Count(h => h.OverallStatus == HealthStatus.Healthy);
        SuccessRate = (double)successfulChecks / HealthChecks.Count * 100;
    }
}
