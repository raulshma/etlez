using Microsoft.Extensions.Logging;
using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Represents an ETL pipeline that can be executed to process data from source to destination.
/// A pipeline consists of multiple stages that are executed in sequence.
/// </summary>
public interface IPipeline
{
    /// <summary>
    /// Gets the unique identifier for this pipeline.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of the pipeline.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this pipeline does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the collection of stages that make up this pipeline.
    /// </summary>
    IReadOnlyList<IPipelineStage> Stages { get; }

    /// <summary>
    /// Gets the current status of the pipeline.
    /// </summary>
    PipelineStatus Status { get; }

    /// <summary>
    /// Executes the pipeline asynchronously with the provided context.
    /// </summary>
    /// <param name="context">The execution context for the pipeline</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The result of the pipeline execution</returns>
    Task<PipelineExecutionResult> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the pipeline configuration and stages.
    /// </summary>
    /// <returns>Validation result indicating if the pipeline is valid</returns>
    Task<ValidationResult> ValidateAsync();

    /// <summary>
    /// Adds a stage to the pipeline.
    /// </summary>
    /// <param name="stage">The stage to add</param>
    void AddStage(IPipelineStage stage);

    /// <summary>
    /// Removes a stage from the pipeline.
    /// </summary>
    /// <param name="stageId">The ID of the stage to remove</param>
    /// <returns>True if the stage was removed, false if not found</returns>
    bool RemoveStage(Guid stageId);
}
