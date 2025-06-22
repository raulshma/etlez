using System.ComponentModel.DataAnnotations;

namespace ETLFramework.API.Models;

/// <summary>
/// Request model for creating a pipeline.
/// </summary>
public class CreatePipelineRequest
{
    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline description.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the source connector configuration.
    /// </summary>
    [Required]
    public ConnectorConfigurationDto SourceConnector { get; set; } = new();

    /// <summary>
    /// Gets or sets the target connector configuration.
    /// </summary>
    [Required]
    public ConnectorConfigurationDto TargetConnector { get; set; } = new();

    /// <summary>
    /// Gets or sets the transformation configurations.
    /// </summary>
    public List<TransformationConfigurationDto> Transformations { get; set; } = new();

    /// <summary>
    /// Gets or sets the pipeline configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the pipeline is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Request model for updating a pipeline.
/// </summary>
public class UpdatePipelineRequest
{
    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the pipeline description.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the source connector configuration.
    /// </summary>
    public ConnectorConfigurationDto? SourceConnector { get; set; }

    /// <summary>
    /// Gets or sets the target connector configuration.
    /// </summary>
    public ConnectorConfigurationDto? TargetConnector { get; set; }

    /// <summary>
    /// Gets or sets the transformation configurations.
    /// </summary>
    public List<TransformationConfigurationDto>? Transformations { get; set; }

    /// <summary>
    /// Gets or sets the pipeline configuration.
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }

    /// <summary>
    /// Gets or sets whether the pipeline is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// Response model for pipeline information.
/// </summary>
public class PipelineResponse
{
    /// <summary>
    /// Gets or sets the pipeline ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the source connector configuration.
    /// </summary>
    public ConnectorConfigurationDto SourceConnector { get; set; } = new();

    /// <summary>
    /// Gets or sets the target connector configuration.
    /// </summary>
    public ConnectorConfigurationDto TargetConnector { get; set; } = new();

    /// <summary>
    /// Gets or sets the transformation configurations.
    /// </summary>
    public List<TransformationConfigurationDto> Transformations { get; set; } = new();

    /// <summary>
    /// Gets or sets the pipeline configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the pipeline is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified date.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the last execution date.
    /// </summary>
    public DateTimeOffset? LastExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution statistics.
    /// </summary>
    public PipelineStatisticsDto? Statistics { get; set; }
}

/// <summary>
/// Request model for executing a pipeline.
/// </summary>
public class ExecutePipelineRequest
{
    /// <summary>
    /// Gets or sets the execution parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to execute asynchronously.
    /// </summary>
    public bool Async { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Response model for pipeline execution.
/// </summary>
public class ExecutePipelineResponse
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline ID.
    /// </summary>
    public string PipelineId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the execution duration.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Gets or sets the number of records processed.
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of successful records.
    /// </summary>
    public long SuccessfulRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of failed records.
    /// </summary>
    public long FailedRecords { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets execution details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// DTO for connector configuration.
/// </summary>
public class ConnectorConfigurationDto
{
    /// <summary>
    /// Gets or sets the connector type.
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connector configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// DTO for transformation configuration.
/// </summary>
public class TransformationConfigurationDto
{
    /// <summary>
    /// Gets or sets the transformation ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation type.
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the execution order.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets whether the transformation is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// DTO for pipeline statistics.
/// </summary>
public class PipelineStatisticsDto
{
    /// <summary>
    /// Gets or sets the total number of executions.
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of successful executions.
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions.
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Gets or sets the average execution time.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the total records processed.
    /// </summary>
    public long TotalRecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the average throughput in records per second.
    /// </summary>
    public double AverageThroughput { get; set; }

    /// <summary>
    /// Gets or sets the last execution status.
    /// </summary>
    public string? LastExecutionStatus { get; set; }
}
