using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Pipeline;

/// <summary>
/// Implementation of pipeline execution context that provides shared state and services during pipeline execution.
/// </summary>
public class PipelineContext : IPipelineContext
{
    private readonly Dictionary<string, object> _properties;
    private readonly List<ExecutionError> _errors;
    private readonly List<ExecutionWarning> _warnings;

    /// <summary>
    /// Initializes a new instance of the PipelineContext class.
    /// </summary>
    /// <param name="executionId">The unique execution identifier</param>
    /// <param name="configuration">The pipeline configuration</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public PipelineContext(
        Guid executionId,
        IPipelineConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ExecutionId = executionId;
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        CancellationToken = cancellationToken;
        StartTime = DateTimeOffset.UtcNow;
        
        _properties = new Dictionary<string, object>();
        _errors = new List<ExecutionError>();
        _warnings = new List<ExecutionWarning>();
        
        Statistics = new ExecutionStatistics();
    }

    /// <inheritdoc />
    public Guid ExecutionId { get; }

    /// <inheritdoc />
    public IPipelineConfiguration Configuration { get; }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc />
    public DateTimeOffset StartTime { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Properties => _properties;

    /// <inheritdoc />
    public IExecutionStatistics Statistics { get; }

    /// <inheritdoc />
    public object? CurrentData { get; set; }

    /// <inheritdoc />
    public IList<ExecutionError> Errors => _errors;

    /// <inheritdoc />
    public IList<ExecutionWarning> Warnings => _warnings;

    /// <inheritdoc />
    public void AddError(ExecutionError error)
    {
        if (error == null) throw new ArgumentNullException(nameof(error));
        
        _errors.Add(error);
        Logger.LogError("Execution error: {Message} (Source: {Source}, Code: {ErrorCode})", 
            error.Message, error.Source, error.ErrorCode);
    }

    /// <inheritdoc />
    public void AddWarning(ExecutionWarning warning)
    {
        if (warning == null) throw new ArgumentNullException(nameof(warning));
        
        _warnings.Add(warning);
        Logger.LogWarning("Execution warning: {Message} (Source: {Source}, Code: {WarningCode})", 
            warning.Message, warning.Source, warning.WarningCode);
    }

    /// <inheritdoc />
    public T? GetProperty<T>(string key)
    {
        if (string.IsNullOrEmpty(key)) return default;
        
        if (_properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <inheritdoc />
    public void SetProperty<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Property key cannot be null or empty", nameof(key));
        
        _properties[key] = value!;
        Logger.LogDebug("Set context property {Key} = {Value}", key, value);
    }

    /// <inheritdoc />
    public IPipelineContext CreateStageContext(IPipelineStage stage)
    {
        if (stage == null) throw new ArgumentNullException(nameof(stage));

        // Use the same logger for the stage (child logger creation would require Serilog-specific implementation)
        var stageLogger = Logger;

        // Create a new context for the stage with the same execution ID but stage-specific properties
        var stageContext = new PipelineContext(ExecutionId, Configuration, stageLogger, CancellationToken)
        {
            CurrentData = CurrentData
        };

        // Copy relevant properties to the stage context
        foreach (var property in _properties)
        {
            stageContext.SetProperty(property.Key, property.Value);
        }

        // Add stage-specific properties
        stageContext.SetProperty("StageId", stage.Id);
        stageContext.SetProperty("StageName", stage.Name);
        stageContext.SetProperty("StageType", stage.StageType);
        stageContext.SetProperty("StageOrder", stage.Order);

        Logger.LogDebug("Created stage context for stage {StageName} (ID: {StageId})", stage.Name, stage.Id);

        return stageContext;
    }

    /// <inheritdoc />
    public void ReportProgress(ProgressInfo progress)
    {
        if (progress == null) throw new ArgumentNullException(nameof(progress));

        SetProperty("LastProgress", progress);
        
        Logger.LogInformation("Progress: {PercentComplete:F1}% - {StatusMessage} ({ItemsProcessed}/{TotalItems})",
            progress.PercentComplete,
            progress.StatusMessage ?? "Processing",
            progress.ItemsProcessed,
            progress.TotalItems);

        // Raise progress event if needed (could be implemented later)
        ProgressReported?.Invoke(this, new ProgressEventArgs(progress));
    }

    /// <summary>
    /// Event raised when progress is reported.
    /// </summary>
    public event EventHandler<ProgressEventArgs>? ProgressReported;

    /// <summary>
    /// Gets a value indicating whether the context has any errors.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the context has any warnings.
    /// </summary>
    public bool HasWarnings => _warnings.Count > 0;

    /// <summary>
    /// Gets the total number of errors.
    /// </summary>
    public int ErrorCount => _errors.Count;

    /// <summary>
    /// Gets the total number of warnings.
    /// </summary>
    public int WarningCount => _warnings.Count;

    /// <summary>
    /// Clears all errors from the context.
    /// </summary>
    public void ClearErrors()
    {
        _errors.Clear();
        Logger.LogDebug("Cleared all errors from context");
    }

    /// <summary>
    /// Clears all warnings from the context.
    /// </summary>
    public void ClearWarnings()
    {
        _warnings.Clear();
        Logger.LogDebug("Cleared all warnings from context");
    }

    /// <summary>
    /// Gets the elapsed time since the context was created.
    /// </summary>
    public TimeSpan ElapsedTime => DateTimeOffset.UtcNow - StartTime;

    /// <summary>
    /// Creates a summary of the execution context.
    /// </summary>
    /// <returns>A summary string</returns>
    public string GetSummary()
    {
        return $"Execution {ExecutionId}: {ErrorCount} errors, {WarningCount} warnings, " +
               $"elapsed: {ElapsedTime:hh\\:mm\\:ss}, records: {Statistics.TotalRecords}";
    }
}

/// <summary>
/// Event arguments for progress reporting.
/// </summary>
public class ProgressEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the ProgressEventArgs class.
    /// </summary>
    /// <param name="progress">The progress information</param>
    public ProgressEventArgs(ProgressInfo progress)
    {
        Progress = progress ?? throw new ArgumentNullException(nameof(progress));
    }

    /// <summary>
    /// Gets the progress information.
    /// </summary>
    public ProgressInfo Progress { get; }
}
