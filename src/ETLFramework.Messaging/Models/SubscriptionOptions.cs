namespace ETLFramework.Messaging.Models;

/// <summary>
/// Options for message subscription configuration.
/// </summary>
public class SubscriptionOptions
{
    /// <summary>
    /// Gets or sets the consumer group name for load balancing.
    /// </summary>
    public string? ConsumerGroup { get; set; }

    /// <summary>
    /// Gets or sets whether messages should be automatically acknowledged.
    /// </summary>
    public bool AutoAck { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the subscription should be durable.
    /// </summary>
    public bool Durable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the queue should be auto-deleted when not in use.
    /// </summary>
    public bool AutoDelete { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the subscription is exclusive to this consumer.
    /// </summary>
    public bool Exclusive { get; set; } = false;

    /// <summary>
    /// Gets or sets the prefetch count for message batching.
    /// </summary>
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed messages.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retry attempts.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets whether to use exponential backoff for retries.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Gets or sets the dead letter queue name for failed messages.
    /// </summary>
    public string? DeadLetterQueue { get; set; }

    /// <summary>
    /// Gets or sets custom subscription properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}
