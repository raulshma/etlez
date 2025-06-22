using Microsoft.Extensions.Logging;
using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Provides execution context and shared state for pipeline execution.
/// Contains configuration, data, and services needed during pipeline execution.
/// </summary>
public interface IPipelineContext
{
    /// <summary>
    /// Gets the unique identifier for this execution context.
    /// </summary>
    Guid ExecutionId { get; }

    /// <summary>
    /// Gets the pipeline configuration.
    /// </summary>
    IPipelineConfiguration Configuration { get; }

    /// <summary>
    /// Gets the logger for this execution context.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the cancellation token for this execution.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the start time of the pipeline execution.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets or sets custom properties for this execution context.
    /// </summary>
    IDictionary<string, object> Properties { get; }

    /// <summary>
    /// Gets the execution statistics for this pipeline run.
    /// </summary>
    IExecutionStatistics Statistics { get; }

    /// <summary>
    /// Gets or sets the current data being processed in the pipeline.
    /// </summary>
    object? CurrentData { get; set; }

    /// <summary>
    /// Gets the collection of errors that occurred during execution.
    /// </summary>
    IList<ExecutionError> Errors { get; }

    /// <summary>
    /// Gets the collection of warnings that occurred during execution.
    /// </summary>
    IList<ExecutionWarning> Warnings { get; }

    /// <summary>
    /// Adds an error to the execution context.
    /// </summary>
    /// <param name="error">The error to add</param>
    void AddError(ExecutionError error);

    /// <summary>
    /// Adds a warning to the execution context.
    /// </summary>
    /// <param name="warning">The warning to add</param>
    void AddWarning(ExecutionWarning warning);

    /// <summary>
    /// Gets a property value by key.
    /// </summary>
    /// <typeparam name="T">The type of the property value</typeparam>
    /// <param name="key">The property key</param>
    /// <returns>The property value, or default if not found</returns>
    T? GetProperty<T>(string key);

    /// <summary>
    /// Sets a property value.
    /// </summary>
    /// <typeparam name="T">The type of the property value</typeparam>
    /// <param name="key">The property key</param>
    /// <param name="value">The property value</param>
    void SetProperty<T>(string key, T value);

    /// <summary>
    /// Creates a child context for a specific stage execution.
    /// </summary>
    /// <param name="stage">The stage for which to create the context</param>
    /// <returns>A new context for the stage</returns>
    IPipelineContext CreateStageContext(IPipelineStage stage);

    /// <summary>
    /// Reports progress for the current operation.
    /// </summary>
    /// <param name="progress">Progress information</param>
    void ReportProgress(ProgressInfo progress);
}
