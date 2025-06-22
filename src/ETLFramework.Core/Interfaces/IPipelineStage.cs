using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Represents a single stage within an ETL pipeline.
/// Each stage performs a specific operation such as extraction, transformation, or loading.
/// </summary>
public interface IPipelineStage
{
    /// <summary>
    /// Gets the unique identifier for this stage.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of the stage.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this stage does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the type of stage (Extract, Transform, Load, etc.).
    /// </summary>
    StageType StageType { get; }

    /// <summary>
    /// Gets the current status of the stage.
    /// </summary>
    StageStatus Status { get; }

    /// <summary>
    /// Gets the order/position of this stage in the pipeline.
    /// </summary>
    int Order { get; set; }

    /// <summary>
    /// Executes the stage asynchronously with the provided context.
    /// </summary>
    /// <param name="context">The execution context for the stage</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The result of the stage execution</returns>
    Task<StageExecutionResult> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the stage configuration.
    /// </summary>
    /// <returns>Validation result indicating if the stage is valid</returns>
    Task<ValidationResult> ValidateAsync();

    /// <summary>
    /// Prepares the stage for execution (initialization, resource allocation, etc.).
    /// </summary>
    /// <param name="context">The execution context</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task PrepareAsync(IPipelineContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up resources after stage execution.
    /// </summary>
    /// <param name="context">The execution context</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task CleanupAsync(IPipelineContext context, CancellationToken cancellationToken = default);
}
