# ETL Framework - Message-Based Communication Guide

This guide covers how to utilize message-based communication patterns within the ETL Framework for event-driven architectures, asynchronous processing, and integration with message brokers.

## Table of Contents

1. [Message-Based Architecture](#message-based-architecture)
2. [Event-Driven Pipelines](#event-driven-pipelines)
3. [Message Brokers](#message-brokers)
4. [Pipeline Events](#pipeline-events)
5. [Message Connectors](#message-connectors)
6. [Async Processing Patterns](#async-processing-patterns)
7. [Error Handling & Dead Letter Queues](#error-handling--dead-letter-queues)
8. [Configuration](#configuration)
9. [Best Practices](#best-practices)

## Message-Based Architecture

The ETL Framework supports message-based communication through several patterns:

- **Event-Driven Pipelines**: Pipelines triggered by events
- **Message Queue Integration**: Direct integration with message brokers
- **Pipeline Events**: Built-in events for pipeline lifecycle
- **Asynchronous Processing**: Non-blocking pipeline execution
- **Pub/Sub Patterns**: Publisher-subscriber messaging

### Core Messaging Interfaces

```csharp
// Message publisher interface
public interface IMessagePublisher
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
    Task PublishAsync<T>(string topic, T message, MessageProperties properties, CancellationToken cancellationToken = default);
    Task PublishBatchAsync<T>(string topic, IEnumerable<T> messages, CancellationToken cancellationToken = default);
}

// Message subscriber interface
public interface IMessageSubscriber
{
    Task SubscribeAsync<T>(string topic, Func<T, MessageContext, Task> handler, CancellationToken cancellationToken = default);
    Task SubscribeAsync<T>(string topic, Func<T, MessageContext, Task> handler, SubscriptionOptions options, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default);
}

// Message broker abstraction
public interface IMessageBroker : IMessagePublisher, IMessageSubscriber, IDisposable
{
    string BrokerType { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);
}

// Pipeline event publisher
public interface IPipelineEventPublisher
{
    Task PublishPipelineStartedAsync(PipelineStartedEvent evt, CancellationToken cancellationToken = default);
    Task PublishPipelineCompletedAsync(PipelineCompletedEvent evt, CancellationToken cancellationToken = default);
    Task PublishPipelineFailedAsync(PipelineFailedEvent evt, CancellationToken cancellationToken = default);
    Task PublishStageCompletedAsync(StageCompletedEvent evt, CancellationToken cancellationToken = default);
    Task PublishDataProcessedAsync(DataProcessedEvent evt, CancellationToken cancellationToken = default);
}
```

## Event-Driven Pipelines

### Pipeline Event Models

```csharp
// Base pipeline event
public abstract class PipelineEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid PipelineId { get; set; }
    public Guid ExecutionId { get; set; }
    public string PipelineName { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Pipeline lifecycle events
public class PipelineStartedEvent : PipelineEvent
{
    public PipelineStartedEvent() { EventType = "PipelineStarted"; }
    public PipelineConfiguration Configuration { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class PipelineCompletedEvent : PipelineEvent
{
    public PipelineCompletedEvent() { EventType = "PipelineCompleted"; }
    public TimeSpan Duration { get; set; }
    public long RecordsProcessed { get; set; }
    public long RecordsSuccessful { get; set; }
    public long RecordsFailed { get; set; }
    public PipelineExecutionResult Result { get; set; } = new();
}

public class PipelineFailedEvent : PipelineEvent
{
    public PipelineFailedEvent() { EventType = "PipelineFailed"; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public List<ExecutionError> Errors { get; set; } = new();
    public string FailedStage { get; set; } = string.Empty;
}

public class StageCompletedEvent : PipelineEvent
{
    public StageCompletedEvent() { EventType = "StageCompleted"; }
    public string StageName { get; set; } = string.Empty;
    public StageType StageType { get; set; }
    public TimeSpan Duration { get; set; }
    public long RecordsProcessed { get; set; }
    public StageExecutionResult Result { get; set; } = new();
}

public class DataProcessedEvent : PipelineEvent
{
    public DataProcessedEvent() { EventType = "DataProcessed"; }
    public string StageName { get; set; } = string.Empty;
    public long BatchSize { get; set; }
    public long TotalRecordsProcessed { get; set; }
    public double RecordsPerSecond { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}
```

### Event-Driven Pipeline Implementation

```csharp
public class EventDrivenPipelineOrchestrator : IPipelineOrchestrator
{
    private readonly IPipelineEventPublisher _eventPublisher;
    private readonly IMessageSubscriber _messageSubscriber;
    private readonly ILogger<EventDrivenPipelineOrchestrator> _logger;
    private readonly IPipelineFactory _pipelineFactory;

    public EventDrivenPipelineOrchestrator(
        IPipelineEventPublisher eventPublisher,
        IMessageSubscriber messageSubscriber,
        ILogger<EventDrivenPipelineOrchestrator> logger,
        IPipelineFactory pipelineFactory)
    {
        _eventPublisher = eventPublisher;
        _messageSubscriber = messageSubscriber;
        _logger = logger;
        _pipelineFactory = pipelineFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // Subscribe to pipeline trigger events
        await _messageSubscriber.SubscribeAsync<PipelineTriggerEvent>(
            "pipeline.triggers", 
            HandlePipelineTriggerAsync, 
            cancellationToken);

        // Subscribe to data arrival events
        await _messageSubscriber.SubscribeAsync<DataArrivedEvent>(
            "data.arrived", 
            HandleDataArrivedAsync, 
            cancellationToken);

        // Subscribe to schedule events
        await _messageSubscriber.SubscribeAsync<ScheduleEvent>(
            "schedule.triggers", 
            HandleScheduleEventAsync, 
            cancellationToken);

        _logger.LogInformation("Event-driven pipeline orchestrator started");
    }

    private async Task HandlePipelineTriggerAsync(PipelineTriggerEvent triggerEvent, MessageContext context)
    {
        try
        {
            _logger.LogInformation("Received pipeline trigger for pipeline: {PipelineId}", triggerEvent.PipelineId);

            var pipeline = await _pipelineFactory.CreatePipelineAsync(triggerEvent.PipelineId);
            if (pipeline == null)
            {
                _logger.LogWarning("Pipeline not found: {PipelineId}", triggerEvent.PipelineId);
                return;
            }

            // Create execution context with trigger parameters
            var executionContext = new PipelineContext(Guid.NewGuid(), triggerEvent.PipelineId)
            {
                TriggerSource = "Event",
                TriggerData = triggerEvent.TriggerData
            };

            // Add trigger parameters to context
            foreach (var param in triggerEvent.Parameters)
            {
                executionContext.Variables[param.Key] = param.Value;
            }

            // Execute pipeline asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecutePipelineWithEventsAsync(pipeline, executionContext);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing triggered pipeline: {PipelineId}", triggerEvent.PipelineId);
                }
            });

            // Acknowledge message
            await context.AckAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling pipeline trigger event");
            await context.NackAsync();
        }
    }

    private async Task HandleDataArrivedAsync(DataArrivedEvent dataEvent, MessageContext context)
    {
        try
        {
            _logger.LogInformation("Data arrived event received for source: {Source}", dataEvent.Source);

            // Find pipelines configured to process this data source
            var pipelines = await FindPipelinesForDataSourceAsync(dataEvent.Source);

            foreach (var pipelineId in pipelines)
            {
                var triggerEvent = new PipelineTriggerEvent
                {
                    PipelineId = pipelineId,
                    TriggerSource = "DataArrival",
                    TriggerData = dataEvent.Metadata,
                    Parameters = new Dictionary<string, object>
                    {
                        ["DataSource"] = dataEvent.Source,
                        ["DataPath"] = dataEvent.Path,
                        ["RecordCount"] = dataEvent.RecordCount,
                        ["FileSize"] = dataEvent.FileSize
                    }
                };

                await _eventPublisher.PublishAsync("pipeline.triggers", triggerEvent);
            }

            await context.AckAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling data arrived event");
            await context.NackAsync();
        }
    }

    private async Task ExecutePipelineWithEventsAsync(IPipeline pipeline, IPipelineContext context)
    {
        var startedEvent = new PipelineStartedEvent
        {
            PipelineId = pipeline.Id,
            ExecutionId = context.ExecutionId,
            PipelineName = pipeline.Name,
            Parameters = context.Variables.ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        await _eventPublisher.PublishPipelineStartedAsync(startedEvent);

        try
        {
            var result = await pipeline.ExecuteAsync(context);

            if (result.IsSuccess)
            {
                var completedEvent = new PipelineCompletedEvent
                {
                    PipelineId = pipeline.Id,
                    ExecutionId = context.ExecutionId,
                    PipelineName = pipeline.Name,
                    Duration = result.EndTime - result.StartTime,
                    RecordsProcessed = result.RecordsProcessed,
                    RecordsSuccessful = result.RecordsSuccessful,
                    RecordsFailed = result.RecordsFailed,
                    Result = result
                };

                await _eventPublisher.PublishPipelineCompletedAsync(completedEvent);
            }
            else
            {
                var failedEvent = new PipelineFailedEvent
                {
                    PipelineId = pipeline.Id,
                    ExecutionId = context.ExecutionId,
                    PipelineName = pipeline.Name,
                    ErrorMessage = string.Join("; ", result.Errors.Select(e => e.Message)),
                    Errors = result.Errors.ToList()
                };

                await _eventPublisher.PublishPipelineFailedAsync(failedEvent);
            }
        }
        catch (Exception ex)
        {
            var failedEvent = new PipelineFailedEvent
            {
                PipelineId = pipeline.Id,
                ExecutionId = context.ExecutionId,
                PipelineName = pipeline.Name,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };

            await _eventPublisher.PublishPipelineFailedAsync(failedEvent);
            throw;
        }
    }

    private async Task<IEnumerable<Guid>> FindPipelinesForDataSourceAsync(string dataSource)
    {
        // Implementation to find pipelines configured for specific data sources
        // This could query a configuration store or registry
        return new List<Guid>(); // Placeholder
    }
}

// Supporting event models
public class PipelineTriggerEvent
{
    public Guid PipelineId { get; set; }
    public string TriggerSource { get; set; } = string.Empty;
    public Dictionary<string, object> TriggerData { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class DataArrivedEvent
{
    public string Source { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long RecordCount { get; set; }
    public long FileSize { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class ScheduleEvent
{
    public Guid PipelineId { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public DateTimeOffset ScheduledTime { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}
```

## Message Brokers

### RabbitMQ Implementation

```csharp
public class RabbitMQBroker : IMessageBroker
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<RabbitMQBroker> _logger;
    private readonly RabbitMQConfiguration _config;
    private readonly ConcurrentDictionary<string, IBasicConsumer> _consumers = new();

    public RabbitMQBroker(RabbitMQConfiguration config, ILogger<RabbitMQBroker> logger)
    {
        _config = config;
        _logger = logger;
        BrokerType = "RabbitMQ";
    }

    public string BrokerType { get; }

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

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        await PublishAsync(topic, message, new MessageProperties(), cancellationToken);
    }

    public async Task PublishAsync<T>(string topic, T message, MessageProperties properties, CancellationToken cancellationToken = default)
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
            basicProperties.ContentType = "application/json";
            basicProperties.ContentEncoding = "utf-8";

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
    }

    public async Task SubscribeAsync<T>(string topic, Func<T, MessageContext, Task> handler, CancellationToken cancellationToken = default)
    {
        await SubscribeAsync(topic, handler, new SubscriptionOptions(), cancellationToken);
    }

    public async Task SubscribeAsync<T>(string topic, Func<T, MessageContext, Task> handler, SubscriptionOptions options, CancellationToken cancellationToken = default)
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
                exclusive: false,
                autoDelete: options.AutoDelete,
                arguments: null);

            // Bind queue to exchange
            _channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey);

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
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from topic: {Topic}", topic);
                    await messageContext.NackAsync();
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
    }

    private async Task DeclareTopologyAsync()
    {
        if (_channel == null) return;

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

    // ... other interface implementations
}

// RabbitMQ-specific message context
public class RabbitMQMessageContext : MessageContext
{
    private readonly BasicDeliverEventArgs _eventArgs;
    private readonly IModel _channel;

    public RabbitMQMessageContext(BasicDeliverEventArgs eventArgs, IModel channel)
    {
        _eventArgs = eventArgs;
        _channel = channel;
        
        MessageId = eventArgs.BasicProperties.MessageId;
        Timestamp = DateTimeOffset.FromUnixTimeSeconds(eventArgs.BasicProperties.Timestamp.UnixTime);
        Headers = eventArgs.BasicProperties.Headers?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new Dictionary<string, object>();
    }

    public override async Task AckAsync()
    {
        _channel.BasicAck(_eventArgs.DeliveryTag, false);
    }

    public override async Task NackAsync()
    {
        _channel.BasicNack(_eventArgs.DeliveryTag, false, true);
    }
}

### Azure Service Bus Implementation

```csharp
public class AzureServiceBusBroker : IMessageBroker
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<AzureServiceBusBroker> _logger;
    private readonly AzureServiceBusConfiguration _config;
    private readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors = new();

    public AzureServiceBusBroker(AzureServiceBusConfiguration config, ILogger<AzureServiceBusBroker> logger)
    {
        _config = config;
        _logger = logger;
        _client = new ServiceBusClient(_config.ConnectionString);
        BrokerType = "AzureServiceBus";
    }

    public string BrokerType { get; }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test connection by creating a sender
            await using var sender = _client.CreateSender("test");
            _logger.LogInformation("Connected to Azure Service Bus");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Azure Service Bus");
            throw;
        }
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        await PublishAsync(topic, message, new MessageProperties(), cancellationToken);
    }

    public async Task PublishAsync<T>(string topic, T message, MessageProperties properties, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var sender = _client.CreateSender(topic);

            var json = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(json)
            {
                MessageId = properties.MessageId ?? Guid.NewGuid().ToString(),
                ContentType = "application/json",
                Subject = typeof(T).Name
            };

            if (properties.Headers?.Any() == true)
            {
                foreach (var header in properties.Headers)
                {
                    serviceBusMessage.ApplicationProperties[header.Key] = header.Value;
                }
            }

            if (properties.Expiration.HasValue)
            {
                serviceBusMessage.TimeToLive = properties.Expiration.Value;
            }

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            _logger.LogDebug("Published message to topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to topic: {Topic}", topic);
            throw;
        }
    }

    public async Task SubscribeAsync<T>(string topic, Func<T, MessageContext, Task> handler, CancellationToken cancellationToken = default)
    {
        await SubscribeAsync(topic, handler, new SubscriptionOptions(), cancellationToken);
    }

    public async Task SubscribeAsync<T>(string topic, Func<T, MessageContext, Task> handler, SubscriptionOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var processor = _client.CreateProcessor(topic, options.ConsumerGroup ?? "default");

            processor.ProcessMessageAsync += async args =>
            {
                var messageContext = new AzureServiceBusMessageContext(args);

                try
                {
                    var json = args.Message.Body.ToString();
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                    {
                        await handler(message, messageContext);
                        await args.CompleteMessageAsync(args.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from topic: {Topic}", topic);
                    await args.AbandonMessageAsync(args.Message);
                }
            };

            processor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "Error in message processor for topic: {Topic}", topic);
                return Task.CompletedTask;
            };

            await processor.StartProcessingAsync(cancellationToken);
            _processors[topic] = processor;

            _logger.LogInformation("Subscribed to topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to topic: {Topic}", topic);
            throw;
        }
    }

    public async Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
    {
        if (_processors.TryRemove(topic, out var processor))
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
            _logger.LogInformation("Unsubscribed from topic: {Topic}", topic);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        foreach (var processor in _processors.Values)
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
        }
        _processors.Clear();

        await _client.DisposeAsync();
        _logger.LogInformation("Disconnected from Azure Service Bus");
    }

    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var sender = _client.CreateSender("test");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }
}

// Azure Service Bus message context
public class AzureServiceBusMessageContext : MessageContext
{
    private readonly ProcessMessageEventArgs _args;

    public AzureServiceBusMessageContext(ProcessMessageEventArgs args)
    {
        _args = args;
        MessageId = args.Message.MessageId;
        Timestamp = args.Message.EnqueuedTime;
        Headers = args.Message.ApplicationProperties.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public override async Task AckAsync()
    {
        await _args.CompleteMessageAsync(_args.Message);
    }

    public override async Task NackAsync()
    {
        await _args.AbandonMessageAsync(_args.Message);
    }
}
```

## Message Connectors

### Message Queue Source Connector

```csharp
public class MessageQueueSourceConnector : ISourceConnector<MessageRecord>
{
    private readonly IMessageSubscriber _messageSubscriber;
    private readonly IConnectorConfiguration _configuration;
    private readonly ILogger<MessageQueueSourceConnector> _logger;
    private readonly Channel<MessageRecord> _channel;
    private readonly ChannelWriter<MessageRecord> _writer;
    private readonly ChannelReader<MessageRecord> _reader;

    public MessageQueueSourceConnector(
        IMessageSubscriber messageSubscriber,
        IConnectorConfiguration configuration,
        ILogger<MessageQueueSourceConnector> logger)
    {
        _messageSubscriber = messageSubscriber;
        _configuration = configuration;
        _logger = logger;

        var options = new BoundedChannelOptions(configuration.BatchSize * 10)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<MessageRecord>(options);
        _writer = _channel.Writer;
        _reader = _channel.Reader;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "Message Queue Source Connector";
    public string ConnectorType => "MessageQueue";
    public IConnectorConfiguration Configuration => _configuration;

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        var topics = _configuration.ConnectionProperties.GetValueOrDefault("topics") as string[]
                    ?? throw new ArgumentException("Topics are required");

        var subscriptionOptions = new SubscriptionOptions
        {
            ConsumerGroup = _configuration.ConnectionProperties.GetValueOrDefault("consumerGroup")?.ToString(),
            AutoAck = bool.Parse(_configuration.ConnectionProperties.GetValueOrDefault("autoAck")?.ToString() ?? "false"),
            Durable = bool.Parse(_configuration.ConnectionProperties.GetValueOrDefault("durable")?.ToString() ?? "true")
        };

        foreach (var topic in topics)
        {
            await _messageSubscriber.SubscribeAsync<object>(
                topic,
                async (message, context) => await HandleMessageAsync(message, context, topic),
                subscriptionOptions,
                cancellationToken);
        }

        _logger.LogInformation("Opened message queue source connector for topics: {Topics}", string.Join(", ", topics));
    }

    private async Task HandleMessageAsync(object message, MessageContext context, string topic)
    {
        try
        {
            var messageRecord = new MessageRecord
            {
                Id = context.MessageId ?? Guid.NewGuid().ToString(),
                Topic = topic,
                Payload = message,
                Headers = context.Headers,
                Timestamp = context.Timestamp,
                MessageType = message.GetType().Name
            };

            await _writer.WriteAsync(messageRecord);
            await context.AckAsync();

            _logger.LogDebug("Received message from topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from topic: {Topic}", topic);
            await context.NackAsync();
        }
    }

    public async IAsyncEnumerable<MessageRecord> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in _reader.ReadAllAsync(cancellationToken))
        {
            yield return message;
        }
    }

    public async IAsyncEnumerable<IEnumerable<MessageRecord>> ReadBatchAsync(
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var batch = new List<MessageRecord>(batchSize);

        await foreach (var message in ReadAsync(cancellationToken))
        {
            batch.Add(message);

            if (batch.Count >= batchSize)
            {
                yield return batch.ToList();
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    public async Task<DataSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        return new DataSchema
        {
            Fields = new List<DataField>
            {
                new() { Name = "Id", DataType = typeof(string), IsRequired = true },
                new() { Name = "Topic", DataType = typeof(string), IsRequired = true },
                new() { Name = "Payload", DataType = typeof(object), IsRequired = true },
                new() { Name = "Headers", DataType = typeof(Dictionary<string, object>), IsRequired = false },
                new() { Name = "Timestamp", DataType = typeof(DateTimeOffset), IsRequired = true },
                new() { Name = "MessageType", DataType = typeof(string), IsRequired = true }
            }
        };
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _writer.Complete();
        _logger.LogInformation("Closed message queue source connector");
    }

    // ... other interface implementations
}

// Message record model
public class MessageRecord
{
    public string Id { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public object? Payload { get; set; }
    public Dictionary<string, object> Headers { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; }
    public string MessageType { get; set; } = string.Empty;
}
```

### Message Queue Destination Connector

```csharp
public class MessageQueueDestinationConnector : IDestinationConnector<MessageRecord>
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly IConnectorConfiguration _configuration;
    private readonly ILogger<MessageQueueDestinationConnector> _logger;

    public MessageQueueDestinationConnector(
        IMessagePublisher messagePublisher,
        IConnectorConfiguration configuration,
        ILogger<MessageQueueDestinationConnector> logger)
    {
        _messagePublisher = messagePublisher;
        _configuration = configuration;
        _logger = logger;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "Message Queue Destination Connector";
    public string ConnectorType => "MessageQueue";
    public IConnectorConfiguration Configuration => _configuration;

    public async Task WriteAsync(IAsyncEnumerable<MessageRecord> records, CancellationToken cancellationToken = default)
    {
        var defaultTopic = _configuration.ConnectionProperties.GetValueOrDefault("defaultTopic")?.ToString()
                          ?? throw new ArgumentException("Default topic is required");

        var batchSize = _configuration.BatchSize;
        var batch = new List<MessageRecord>(batchSize);

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            batch.Add(record);

            if (batch.Count >= batchSize)
            {
                await PublishBatchAsync(batch, defaultTopic, cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await PublishBatchAsync(batch, defaultTopic, cancellationToken);
        }
    }

    private async Task PublishBatchAsync(IEnumerable<MessageRecord> records, string defaultTopic, CancellationToken cancellationToken)
    {
        var tasks = records.Select(async record =>
        {
            try
            {
                var topic = !string.IsNullOrEmpty(record.Topic) ? record.Topic : defaultTopic;

                var properties = new MessageProperties
                {
                    MessageId = record.Id,
                    Headers = record.Headers,
                    Persistent = true
                };

                await _messagePublisher.PublishAsync(topic, record.Payload, properties, cancellationToken);

                _logger.LogDebug("Published message to topic: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to topic: {Topic}", record.Topic);
                throw;
            }
        });

        await Task.WhenAll(tasks);
    }

    // ... other interface implementations
}
```

## Configuration

### Message Broker Configuration

```json
{
  "MessageBrokers": {
    "RabbitMQ": {
      "HostName": "${RABBITMQ_HOST:localhost}",
      "Port": "${RABBITMQ_PORT:5672}",
      "UserName": "${RABBITMQ_USER:guest}",
      "Password": "${RABBITMQ_PASS:guest}",
      "VirtualHost": "${RABBITMQ_VHOST:/}",
      "ConnectionTimeout": "00:00:30",
      "HeartbeatInterval": "00:01:00"
    },
    "AzureServiceBus": {
      "ConnectionString": "${AZURE_SERVICEBUS_CONNECTION_STRING}",
      "RetryOptions": {
        "MaxRetries": 3,
        "Delay": "00:00:01",
        "MaxDelay": "00:01:00"
      }
    },
    "AmazonSQS": {
      "AccessKey": "${AWS_ACCESS_KEY}",
      "SecretKey": "${AWS_SECRET_KEY}",
      "Region": "${AWS_REGION:us-east-1}",
      "QueueUrlPrefix": "${SQS_QUEUE_PREFIX}"
    }
  },
  "EventDrivenPipelines": {
    "EnableEventPublishing": true,
    "DefaultEventTopic": "pipeline.events",
    "EventRetention": "7.00:00:00",
    "BatchEventPublishing": true,
    "BatchSize": 100,
    "BatchTimeout": "00:00:05"
  }
}
```

### Pipeline Event Configuration

```json
{
  "stages": [
    {
      "name": "Message Queue Extract",
      "stageType": "Extract",
      "order": 1,
      "connectorConfiguration": {
        "connectorType": "MessageQueue",
        "connectionProperties": {
          "brokerType": "RabbitMQ",
          "topics": ["data.incoming", "files.uploaded"],
          "consumerGroup": "etl-pipeline-1",
          "autoAck": false,
          "durable": true,
          "prefetchCount": 100
        }
      }
    },
    {
      "name": "Process and Publish",
      "stageType": "Load",
      "order": 3,
      "connectorConfiguration": {
        "connectorType": "MessageQueue",
        "connectionProperties": {
          "brokerType": "RabbitMQ",
          "defaultTopic": "data.processed",
          "publishConfirms": true,
          "persistent": true
        }
      }
    }
  ],
  "eventConfiguration": {
    "publishPipelineEvents": true,
    "eventTopics": {
      "pipelineStarted": "pipeline.started",
      "pipelineCompleted": "pipeline.completed",
      "pipelineFailed": "pipeline.failed",
      "stageCompleted": "pipeline.stage.completed",
      "dataProcessed": "pipeline.data.processed"
    },
    "eventFilters": {
      "includeSuccessEvents": true,
      "includeErrorEvents": true,
      "includeProgressEvents": false,
      "minimumSeverity": "Information"
    }
  }
}
```

## Best Practices

### 1. Message Design

- **Use strongly-typed messages** for better maintainability
- **Include correlation IDs** for tracing across services
- **Keep messages small** to improve performance
- **Use message versioning** for backward compatibility

### 2. Error Handling

- **Implement retry logic** with exponential backoff
- **Use dead letter queues** for failed messages
- **Log message processing errors** with sufficient context
- **Monitor message queue depths** and processing rates

### 3. Performance Optimization

- **Use batching** for high-throughput scenarios
- **Configure appropriate prefetch counts** for consumers
- **Monitor memory usage** in message processing
- **Use connection pooling** where applicable

### 4. Security

- **Use secure connections** (TLS/SSL) for message brokers
- **Implement authentication** and authorization
- **Encrypt sensitive message content** when necessary
- **Validate message schemas** to prevent injection attacks

### 5. Monitoring and Observability

- **Track message processing metrics** (throughput, latency, errors)
- **Implement distributed tracing** across message flows
- **Set up alerts** for queue depth and error rates
- **Use structured logging** for better searchability

This comprehensive guide covers the essential aspects of extending the ETL Framework and implementing message-based communication patterns. The examples provide practical implementations that can be adapted to specific requirements and environments.
```
