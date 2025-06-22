using ETLFramework.Messaging.Events;

namespace ETLFramework.Messaging.Interfaces;

/// <summary>
/// Interface for publishing pipeline lifecycle events.
/// </summary>
public interface IPipelineEventPublisher
{
    /// <summary>
    /// Publishes a pipeline started event.
    /// </summary>
    /// <param name="evt">The pipeline started event</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishPipelineStartedAsync(PipelineStartedEvent evt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a pipeline completed event.
    /// </summary>
    /// <param name="evt">The pipeline completed event</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishPipelineCompletedAsync(PipelineCompletedEvent evt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a pipeline failed event.
    /// </summary>
    /// <param name="evt">The pipeline failed event</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishPipelineFailedAsync(PipelineFailedEvent evt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a stage completed event.
    /// </summary>
    /// <param name="evt">The stage completed event</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishStageCompletedAsync(StageCompletedEvent evt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a data processed event.
    /// </summary>
    /// <param name="evt">The data processed event</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishDataProcessedAsync(DataProcessedEvent evt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a generic pipeline event.
    /// </summary>
    /// <param name="evt">The pipeline event</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishEventAsync(PipelineEvent evt, CancellationToken cancellationToken = default);
}
