using ETLFramework.Messaging.Events;
using ETLFramework.Messaging.Interfaces;
using ETLFramework.Messaging.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ETLFramework.Messaging.Implementations;

/// <summary>
/// Default implementation of pipeline event publisher.
/// </summary>
public class PipelineEventPublisher : IPipelineEventPublisher
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<PipelineEventPublisher> _logger;
    private readonly PipelineEventPublisherOptions _options;

    /// <summary>
    /// Initializes a new instance of the PipelineEventPublisher class.
    /// </summary>
    /// <param name="messagePublisher">The message publisher</param>
    /// <param name="logger">The logger</param>
    /// <param name="options">The publisher options</param>
    public PipelineEventPublisher(
        IMessagePublisher messagePublisher,
        ILogger<PipelineEventPublisher> logger,
        PipelineEventPublisherOptions? options = null)
    {
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new PipelineEventPublisherOptions();
    }

    /// <inheritdoc />
    public async Task PublishPipelineStartedAsync(PipelineStartedEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync(evt, _options.PipelineStartedTopic, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishPipelineCompletedAsync(PipelineCompletedEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync(evt, _options.PipelineCompletedTopic, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishPipelineFailedAsync(PipelineFailedEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync(evt, _options.PipelineFailedTopic, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishStageCompletedAsync(StageCompletedEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync(evt, _options.StageCompletedTopic, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishDataProcessedAsync(DataProcessedEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync(evt, _options.DataProcessedTopic, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishEventAsync(PipelineEvent evt, CancellationToken cancellationToken = default)
    {
        var topic = GetTopicForEventType(evt.EventType);
        await PublishEventAsync(evt, topic, cancellationToken);
    }

    private async Task PublishEventAsync(PipelineEvent evt, string topic, CancellationToken cancellationToken)
    {
        try
        {
            var properties = new MessageProperties
            {
                MessageId = evt.EventId.ToString(),
                CorrelationId = evt.CorrelationId,
                Timestamp = evt.Timestamp,
                Headers = new Dictionary<string, object>
                {
                    ["EventType"] = evt.EventType,
                    ["PipelineId"] = evt.PipelineId.ToString(),
                    ["ExecutionId"] = evt.ExecutionId.ToString(),
                    ["Source"] = evt.Source,
                    ["Version"] = evt.Version
                }
            };

            await _messagePublisher.PublishAsync(topic, evt, properties, cancellationToken);

            _logger.LogDebug("Published {EventType} event for pipeline {PipelineId} to topic {Topic}",
                evt.EventType, evt.PipelineId, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} event for pipeline {PipelineId}",
                evt.EventType, evt.PipelineId);
            throw;
        }
    }

    private string GetTopicForEventType(string eventType)
    {
        return eventType switch
        {
            "PipelineStarted" => _options.PipelineStartedTopic,
            "PipelineCompleted" => _options.PipelineCompletedTopic,
            "PipelineFailed" => _options.PipelineFailedTopic,
            "StageCompleted" => _options.StageCompletedTopic,
            "DataProcessed" => _options.DataProcessedTopic,
            _ => _options.DefaultTopic
        };
    }
}

/// <summary>
/// Configuration options for the pipeline event publisher.
/// </summary>
public class PipelineEventPublisherOptions
{
    /// <summary>
    /// Gets or sets the default topic for pipeline events.
    /// </summary>
    public string DefaultTopic { get; set; } = "pipeline.events";

    /// <summary>
    /// Gets or sets the topic for pipeline started events.
    /// </summary>
    public string PipelineStartedTopic { get; set; } = "pipeline.started";

    /// <summary>
    /// Gets or sets the topic for pipeline completed events.
    /// </summary>
    public string PipelineCompletedTopic { get; set; } = "pipeline.completed";

    /// <summary>
    /// Gets or sets the topic for pipeline failed events.
    /// </summary>
    public string PipelineFailedTopic { get; set; } = "pipeline.failed";

    /// <summary>
    /// Gets or sets the topic for stage completed events.
    /// </summary>
    public string StageCompletedTopic { get; set; } = "pipeline.stage.completed";

    /// <summary>
    /// Gets or sets the topic for data processed events.
    /// </summary>
    public string DataProcessedTopic { get; set; } = "pipeline.data.processed";

    /// <summary>
    /// Gets or sets whether to include sensitive data in events.
    /// </summary>
    public bool IncludeSensitiveData { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum event size in bytes.
    /// </summary>
    public int MaxEventSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Gets or sets whether to batch events for better performance.
    /// </summary>
    public bool EnableBatching { get; set; } = false;

    /// <summary>
    /// Gets or sets the batch size for event publishing.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the batch timeout for event publishing.
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
