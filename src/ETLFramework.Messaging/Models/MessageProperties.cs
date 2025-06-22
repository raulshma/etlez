namespace ETLFramework.Messaging.Models;

/// <summary>
/// Properties for message publishing and handling.
/// </summary>
public class MessageProperties
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier for message tracking.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the reply-to address for response messages.
    /// </summary>
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the message expiration time.
    /// </summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>
    /// Gets or sets whether the message should be persisted.
    /// </summary>
    public bool Persistent { get; set; } = true;

    /// <summary>
    /// Gets or sets the message priority (0-255).
    /// </summary>
    public byte Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets custom headers for the message.
    /// </summary>
    public Dictionary<string, object> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the content type of the message.
    /// </summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// Gets or sets the content encoding of the message.
    /// </summary>
    public string ContentEncoding { get; set; } = "utf-8";

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
