namespace ETLFramework.Messaging.Events;

/// <summary>
/// Base class for all pipeline events.
/// </summary>
public abstract class PipelineEvent
{
    /// <summary>
    /// Gets or sets the unique event identifier.
    /// </summary>
    public Guid EventId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the pipeline identifier.
    /// </summary>
    public Guid PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the execution identifier.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata for the event.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the correlation identifier for tracking related events.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the source of the event.
    /// </summary>
    public string Source { get; set; } = "ETLFramework";

    /// <summary>
    /// Gets or sets the version of the event schema.
    /// </summary>
    public string Version { get; set; } = "1.0";
}
