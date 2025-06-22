namespace ETLFramework.Messaging.Interfaces;

/// <summary>
/// Interface for message brokers that combine publishing and subscribing capabilities.
/// </summary>
public interface IMessageBroker : IMessagePublisher, IMessageSubscriber, IDisposable
{
    /// <summary>
    /// Gets the type of message broker (e.g., "RabbitMQ", "AzureServiceBus", "AmazonSQS").
    /// </summary>
    string BrokerType { get; }

    /// <summary>
    /// Connects to the message broker.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the message broker.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the broker is currently connected.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if connected, false otherwise</returns>
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);
}
