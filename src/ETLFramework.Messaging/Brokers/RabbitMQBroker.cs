using ETLFramework.Messaging.Interfaces;
using ETLFramework.Messaging.Models;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace ETLFramework.Messaging.Brokers;

/// <summary>
/// RabbitMQ message broker implementation.
/// </summary>
public class RabbitMQBroker : IMessageBroker, IDisposable
{
    private readonly ILogger<RabbitMQBroker> _logger;
    private readonly RabbitMQConfiguration _config;
    private readonly ConcurrentDictionary<string, IBasicConsumer> _consumers = new();
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the RabbitMQBroker class.
    /// </summary>
    /// <param name="config">RabbitMQ configuration</param>
    /// <param name="logger">Logger instance</param>
    public RabbitMQBroker(RabbitMQConfiguration config, ILogger<RabbitMQBroker> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        BrokerType = "RabbitMQ";
    }

    /// <inheritdoc />
    public string BrokerType { get; }

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchanges and queues
            await DeclareTopologyAsync();

            _logger.LogInformation("Connected to RabbitMQ: {HostName}:{Port}", _config.HostName, _config.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var consumer in _consumers.Values)
            {
                // Stop consumers
            }
            _consumers.Clear();

            _channel?.Close();
            _channel?.Dispose();
            _channel = null;

            _connection?.Close();
            _connection?.Dispose();
            _connection = null;

            _logger.LogInformation("Disconnected from RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from RabbitMQ");
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_connection?.IsOpen == true && _channel?.IsOpen == true);
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        await PublishAsync(topic, message, new MessageProperties(), cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(string topic, T message, MessageProperties properties, CancellationToken cancellationToken = default)
    {
        if (_channel == null)
            throw new InvalidOperationException("Not connected to RabbitMQ");

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var basicProperties = _channel.CreateBasicProperties();
            basicProperties.Persistent = properties.Persistent;
            basicProperties.MessageId = properties.MessageId ?? Guid.NewGuid().ToString();
            basicProperties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            basicProperties.ContentType = properties.ContentType;
            basicProperties.ContentEncoding = properties.ContentEncoding;

            if (properties.Headers?.Any() == true)
            {
                basicProperties.Headers = new Dictionary<string, object>(properties.Headers);
            }

            if (properties.Expiration.HasValue)
            {
                basicProperties.Expiration = properties.Expiration.Value.TotalMilliseconds.ToString();
            }

            var exchangeName = GetExchangeName(topic);
            var routingKey = GetRoutingKey(topic);

            _channel.BasicPublish(
                exchange: exchangeName,
                routingKey: routingKey,
                basicProperties: basicProperties,
                body: body);

            _logger.LogDebug("Published message to topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to topic: {Topic}", topic);
            throw;
        }
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
        if (_channel == null)
            throw new InvalidOperationException("Not connected to RabbitMQ");

        try
        {
            var queueName = GetQueueName(topic, options.ConsumerGroup);
            var exchangeName = GetExchangeName(topic);
            var routingKey = GetRoutingKey(topic);

            // Declare queue
            _channel.QueueDeclare(
                queue: queueName,
                durable: options.Durable,
                exclusive: options.Exclusive,
                autoDelete: options.AutoDelete,
                arguments: null);

            // Bind queue to exchange
            _channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey);

            // Set prefetch count
            _channel.BasicQos(0, (ushort)options.PrefetchCount, false);

            // Create consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var messageContext = new RabbitMQMessageContext(ea, _channel);
                
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                    {
                        await handler(message, messageContext);
                        
                        if (!options.AutoAck)
                        {
                            await messageContext.AckAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from topic: {Topic}", topic);
                    
                    if (!options.AutoAck)
                    {
                        await messageContext.NackAsync();
                    }
                }
            };

            // Start consuming
            var consumerTag = _channel.BasicConsume(
                queue: queueName,
                autoAck: options.AutoAck,
                consumer: consumer);

            _consumers[topic] = consumer;

            _logger.LogInformation("Subscribed to topic: {Topic} with queue: {QueueName}", topic, queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to topic: {Topic}", topic);
            throw;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
    {
        if (_consumers.TryRemove(topic, out _))
        {
            // Consumer cleanup would go here
            _logger.LogInformation("Unsubscribed from topic: {Topic}", topic);
        }
        return Task.CompletedTask;
    }

    private Task DeclareTopologyAsync()
    {
        if (_channel == null) return Task.CompletedTask;

        // Declare exchanges for different event types
        var exchanges = new[]
        {
            "pipeline.events",
            "data.events",
            "schedule.events",
            "system.events"
        };

        foreach (var exchange in exchanges)
        {
            _channel.ExchangeDeclare(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);
        }

        _logger.LogDebug("Declared RabbitMQ topology");
        return Task.CompletedTask;
    }

    private string GetExchangeName(string topic)
    {
        var parts = topic.Split('.');
        return parts.Length > 1 ? $"{parts[0]}.events" : "default.events";
    }

    private string GetRoutingKey(string topic)
    {
        return topic;
    }

    private string GetQueueName(string topic, string? consumerGroup)
    {
        return string.IsNullOrEmpty(consumerGroup) 
            ? $"queue.{topic}" 
            : $"queue.{topic}.{consumerGroup}";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        DisconnectAsync().GetAwaiter().GetResult();
        _disposed = true;
    }
}

/// <summary>
/// RabbitMQ configuration settings.
/// </summary>
public class RabbitMQConfiguration
{
    /// <summary>
    /// Gets or sets the RabbitMQ host name.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the RabbitMQ port.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Gets or sets the user name for authentication.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Gets or sets the connection timeout.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the heartbeat interval.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// RabbitMQ-specific message context.
/// </summary>
public class RabbitMQMessageContext : MessageContext
{
    private readonly BasicDeliverEventArgs _eventArgs;
    private readonly IModel _channel;

    /// <summary>
    /// Initializes a new instance of the RabbitMQMessageContext class.
    /// </summary>
    /// <param name="eventArgs">RabbitMQ delivery event args</param>
    /// <param name="channel">RabbitMQ channel</param>
    public RabbitMQMessageContext(BasicDeliverEventArgs eventArgs, IModel channel)
    {
        _eventArgs = eventArgs;
        _channel = channel;
        
        MessageId = eventArgs.BasicProperties.MessageId;
        Timestamp = DateTimeOffset.FromUnixTimeSeconds(eventArgs.BasicProperties.Timestamp.UnixTime);
        Headers = eventArgs.BasicProperties.Headers?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new Dictionary<string, object>();
        Topic = eventArgs.RoutingKey;
        DeliveryCount = 1; // RabbitMQ doesn't provide this directly
    }

    /// <inheritdoc />
    public override Task AckAsync()
    {
        _channel.BasicAck(_eventArgs.DeliveryTag, false);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task NackAsync()
    {
        _channel.BasicNack(_eventArgs.DeliveryTag, false, true);
        return Task.CompletedTask;
    }
}
