using ETLFramework.Messaging.Models;

namespace ETLFramework.Messaging.Interfaces;

/// <summary>
/// Interface for publishing messages to message brokers.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to the specified topic.
    /// </summary>
    /// <typeparam name="T">The type of message to publish</typeparam>
    /// <param name="topic">The topic to publish to</param>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message to the specified topic with custom properties.
    /// </summary>
    /// <typeparam name="T">The type of message to publish</typeparam>
    /// <param name="topic">The topic to publish to</param>
    /// <param name="message">The message to publish</param>
    /// <param name="properties">Message properties</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<T>(string topic, T message, MessageProperties properties, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple messages to the specified topic in a batch.
    /// </summary>
    /// <typeparam name="T">The type of messages to publish</typeparam>
    /// <param name="topic">The topic to publish to</param>
    /// <param name="messages">The messages to publish</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishBatchAsync<T>(string topic, IEnumerable<T> messages, CancellationToken cancellationToken = default);
}
