namespace ETLFramework.Messaging.Models;

/// <summary>
/// Context information for message processing.
/// </summary>
public abstract class MessageContext
{
    /// <summary>
    /// Gets the unique message identifier.
    /// </summary>
    public string? MessageId { get; protected set; }

    /// <summary>
    /// Gets the correlation identifier for message tracking.
    /// </summary>
    public string? CorrelationId { get; protected set; }

    /// <summary>
    /// Gets the timestamp when the message was received.
    /// </summary>
    public DateTimeOffset Timestamp { get; protected set; }

    /// <summary>
    /// Gets the message headers.
    /// </summary>
    public Dictionary<string, object> Headers { get; protected set; } = new();

    /// <summary>
    /// Gets the topic or queue name the message was received from.
    /// </summary>
    public string? Topic { get; protected set; }

    /// <summary>
    /// Gets the delivery attempt count.
    /// </summary>
    public int DeliveryCount { get; protected set; } = 1;

    /// <summary>
    /// Acknowledges the message as successfully processed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    public abstract Task AckAsync();

    /// <summary>
    /// Negatively acknowledges the message, indicating processing failure.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    public abstract Task NackAsync();

    /// <summary>
    /// Rejects the message and optionally requeues it.
    /// </summary>
    /// <param name="requeue">Whether to requeue the message</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public virtual Task RejectAsync(bool requeue = false)
    {
        return requeue ? NackAsync() : AckAsync();
    }
}
