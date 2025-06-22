using ETLFramework.Core.Interfaces;

namespace ETLFramework.Messaging.Events;

/// <summary>
/// Event published when a pipeline starts execution.
/// </summary>
public class PipelineStartedEvent : PipelineEvent
{
    /// <summary>
    /// Initializes a new instance of the PipelineStartedEvent class.
    /// </summary>
    public PipelineStartedEvent()
    {
        EventType = "PipelineStarted";
    }

    /// <summary>
    /// Gets or sets the pipeline configuration.
    /// </summary>
    public IPipelineConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the execution parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the trigger source that initiated the pipeline.
    /// </summary>
    public string? TriggerSource { get; set; }

    /// <summary>
    /// Gets or sets the user or system that initiated the pipeline.
    /// </summary>
    public string? InitiatedBy { get; set; }

    /// <summary>
    /// Gets or sets the expected duration of the pipeline execution.
    /// </summary>
    public TimeSpan? ExpectedDuration { get; set; }

    /// <summary>
    /// Gets or sets the priority of the pipeline execution.
    /// </summary>
    public string Priority { get; set; } = "Normal";
}
