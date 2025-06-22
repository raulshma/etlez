using ETLFramework.Messaging.Models;

namespace ETLFramework.Messaging.Interfaces;

/// <summary>
/// Interface for subscribing to messages from message brokers.
/// </summary>
public interface IMessageSubscriber
{
    /// <summary>
    /// Subscribes to messages from the specified topic.
    /// </summary>
    /// <typeparam name="T">The type of message to receive</typeparam>
    /// <param name="topic">The topic to subscribe to</param>
    /// <param name="handler">The handler function to process received messages</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SubscribeAsync<T>(string topic, Func<T, MessageContext, Task> handler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to messages from the specified topic with custom options.
    /// </summary>
    /// <typeparam name="T">The type of message to receive</typeparam>
    /// <param name="topic">The topic to subscribe to</param>
    /// <param name="handler">The handler function to process received messages</param>
    /// <param name="options">Subscription options</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SubscribeAsync<T>(string topic, Func<T, MessageContext, Task> handler, SubscriptionOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from the specified topic.
    /// </summary>
    /// <param name="topic">The topic to unsubscribe from</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default);
}
