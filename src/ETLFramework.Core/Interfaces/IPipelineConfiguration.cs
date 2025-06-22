using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Interface for pipeline configuration that defines how a pipeline should be constructed and executed.
/// </summary>
public interface IPipelineConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this pipeline configuration.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the pipeline.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the pipeline.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Gets or sets the version of this configuration.
    /// </summary>
    string Version { get; set; }

    /// <summary>
    /// Gets or sets the author/creator of this pipeline configuration.
    /// </summary>
    string? Author { get; set; }

    /// <summary>
    /// Gets or sets when this configuration was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when this configuration was last modified.
    /// </summary>
    DateTimeOffset ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the collection of stage configurations that make up this pipeline.
    /// </summary>
    IList<IStageConfiguration> Stages { get; set; }

    /// <summary>
    /// Gets or sets the global configuration settings for this pipeline.
    /// </summary>
    IDictionary<string, object> GlobalSettings { get; set; }

    /// <summary>
    /// Gets or sets the error handling configuration for this pipeline.
    /// </summary>
    IErrorHandlingConfiguration ErrorHandling { get; set; }

    /// <summary>
    /// Gets or sets the retry configuration for this pipeline.
    /// </summary>
    IRetryConfiguration Retry { get; set; }

    /// <summary>
    /// Gets or sets the timeout configuration for this pipeline.
    /// </summary>
    TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for this pipeline.
    /// </summary>
    int? MaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// Gets or sets the scheduling configuration for this pipeline.
    /// </summary>
    IScheduleConfiguration? Schedule { get; set; }

    /// <summary>
    /// Gets or sets the notification configuration for this pipeline.
    /// </summary>
    INotificationConfiguration? Notifications { get; set; }

    /// <summary>
    /// Gets or sets custom tags for categorizing and filtering pipelines.
    /// </summary>
    IList<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets whether this pipeline is enabled for execution.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Validates the pipeline configuration.
    /// </summary>
    /// <returns>Validation result</returns>
    ValidationResult Validate();

    /// <summary>
    /// Creates a deep copy of this configuration.
    /// </summary>
    /// <returns>A new instance with the same configuration values</returns>
    IPipelineConfiguration Clone();

    /// <summary>
    /// Merges another configuration into this one.
    /// </summary>
    /// <param name="other">The configuration to merge</param>
    /// <param name="overwriteExisting">Whether to overwrite existing values</param>
    void Merge(IPipelineConfiguration other, bool overwriteExisting = false);
}

/// <summary>
/// Interface for stage configuration within a pipeline.
/// </summary>
public interface IStageConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this stage.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the stage.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the stage.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Gets or sets the type of stage.
    /// </summary>
    StageType StageType { get; set; }

    /// <summary>
    /// Gets or sets the order/position of this stage in the pipeline.
    /// </summary>
    int Order { get; set; }

    /// <summary>
    /// Gets or sets whether this stage is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the connector configuration for this stage.
    /// </summary>
    IConnectorConfiguration? ConnectorConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the transformation configuration for this stage.
    /// </summary>
    ITransformationConfiguration? TransformationConfiguration { get; set; }

    /// <summary>
    /// Gets or sets stage-specific settings.
    /// </summary>
    IDictionary<string, object> Settings { get; set; }

    /// <summary>
    /// Gets or sets the conditions under which this stage should be executed.
    /// </summary>
    IList<IExecutionCondition> ExecutionConditions { get; set; }

    /// <summary>
    /// Gets or sets the timeout for this stage.
    /// </summary>
    TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the retry configuration for this stage.
    /// </summary>
    IRetryConfiguration? Retry { get; set; }

    /// <summary>
    /// Validates the stage configuration.
    /// </summary>
    /// <returns>Validation result</returns>
    ValidationResult Validate();
}
