using ETLFramework.Messaging.Interfaces;
using ETLFramework.Messaging.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ETLFramework.Messaging.Brokers;

/// <summary>
/// In-memory message broker implementation for testing and development.
/// </summary>
public class InMemoryMessageBroker : IMessageBroker, IDisposable
{
    private readonly ILogger<InMemoryMessageBroker> _logger;
    private readonly ConcurrentDictionary<string, List<Subscription>> _subscriptions = new();
    private readonly ConcurrentQueue<MessageEnvelope> _messageQueue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _processingTask;
    private bool _isConnected;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the InMemoryMessageBroker class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public InMemoryMessageBroker(ILogger<InMemoryMessageBroker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        BrokerType = "InMemory";
    }

    /// <inheritdoc />
    public string BrokerType { get; }

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
            return Task.CompletedTask;

        _processingTask = Task.Run(ProcessMessagesAsync, cancellationToken);
        _isConnected = true;

        _logger.LogInformation("Connected to in-memory message broker");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return;

        _cancellationTokenSource.Cancel();
        
        if (_processingTask != null)
        {
            await _processingTask;
        }

        _isConnected = false;
        _logger.LogInformation("Disconnected from in-memory message broker");
    }

    /// <inheritdoc />
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_isConnected);
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        await PublishAsync(topic, message, new MessageProperties(), cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(string topic, T message, MessageProperties properties, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Broker is not connected");

        var envelope = new MessageEnvelope
        {
            Topic = topic,
            MessageType = typeof(T).Name,
            Payload = JsonSerializer.Serialize(message),
            Properties = properties,
            Timestamp = DateTimeOffset.UtcNow
        };

        _messageQueue.Enqueue(envelope);

        _logger.LogDebug("Published message to topic: {Topic}", topic);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task PublishBatchAsync<T>(string topic, IEnumerable<T> messages, CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            await PublishAsync(topic, message, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(string topic, Func<T, MessageContext, Task> handler, CancellationToken cancellationToken = default)
    {
        await SubscribeAsync(topic, handler, new SubscriptionOptions(), cancellationToken);
    }

    /// <inheritdoc />
    public Task SubscribeAsync<T>(string topic, Func<T, MessageContext, Task> handler, SubscriptionOptions options, CancellationToken cancellationToken = default)
    {
        var subscription = new Subscription
        {
            Topic = topic,
            MessageType = typeof(T),
            Handler = async (envelope, context) =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<T>(envelope.Payload);
                    if (message != null)
                    {
                        await handler(message, context);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message in subscription for topic: {Topic}", topic);
                    throw;
                }
            },
            Options = options
        };

        _subscriptions.AddOrUpdate(topic,
            [subscription],
            (key, existing) =>
            {
                existing.Add(subscription);
                return existing;
            });

        _logger.LogInformation("Subscribed to topic: {Topic}", topic);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
    {
        _subscriptions.TryRemove(topic, out _);
        _logger.LogInformation("Unsubscribed from topic: {Topic}", topic);
        return Task.CompletedTask;
    }

    private async Task ProcessMessagesAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                if (_messageQueue.TryDequeue(out var envelope))
                {
                    await ProcessMessageAsync(envelope);
                }
                else
                {
                    await Task.Delay(10, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing messages");
            }
        }
    }

    private async Task ProcessMessageAsync(MessageEnvelope envelope)
    {
        if (_subscriptions.TryGetValue(envelope.Topic, out var subscriptions))
        {
            var context = new InMemoryMessageContext(envelope);

            foreach (var subscription in subscriptions)
            {
                try
                {
                    await subscription.Handler(envelope, context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in message handler for topic: {Topic}", envelope.Topic);
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        DisconnectAsync().GetAwaiter().GetResult();
        _cancellationTokenSource.Dispose();
        _disposed = true;
    }

    private class Subscription
    {
        public string Topic { get; set; } = string.Empty;
        public Type MessageType { get; set; } = typeof(object);
        public Func<MessageEnvelope, MessageContext, Task> Handler { get; set; } = null!;
        public SubscriptionOptions Options { get; set; } = new();
    }

    private class MessageEnvelope
    {
        public string Topic { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public MessageProperties Properties { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; }
    }

    private class InMemoryMessageContext : MessageContext
    {
        public InMemoryMessageContext(MessageEnvelope envelope)
        {
            MessageId = envelope.Properties.MessageId;
            CorrelationId = envelope.Properties.CorrelationId;
            Timestamp = envelope.Timestamp;
            Headers = envelope.Properties.Headers;
            Topic = envelope.Topic;
        }

        public override Task AckAsync()
        {
            // In-memory broker doesn't need explicit acknowledgment
            return Task.CompletedTask;
        }

        public override Task NackAsync()
        {
            // In-memory broker doesn't support negative acknowledgment
            return Task.CompletedTask;
        }
    }
}
