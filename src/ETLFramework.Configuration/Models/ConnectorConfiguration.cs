using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;

namespace ETLFramework.Configuration.Models;

/// <summary>
/// Concrete implementation of connector configuration.
/// </summary>
public class ConnectorConfiguration : IConnectorConfiguration
{
    /// <summary>
    /// Initializes a new instance of the ConnectorConfiguration class.
    /// </summary>
    public ConnectorConfiguration()
    {
        Id = Guid.NewGuid();
        Name = string.Empty;
        ConnectorType = string.Empty;
        Description = string.Empty;
        ConnectionString = string.Empty;
        ConnectionProperties = new Dictionary<string, object>();
        Settings = new Dictionary<string, object>();
        Tags = new List<string>();
        CreatedAt = DateTimeOffset.UtcNow;
        ModifiedAt = DateTimeOffset.UtcNow;
        MaxRetryAttempts = 3;
        RetryDelay = TimeSpan.FromSeconds(5);
        UseConnectionPooling = true;
        MaxPoolSize = 100;
        MinPoolSize = 5;
        BatchSize = 1000;
        EnableDetailedLogging = false;
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string ConnectorType { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public string ConnectionString { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object> ConnectionProperties { get; set; }

    /// <inheritdoc />
    public IAuthenticationConfiguration? Authentication { get; set; }

    /// <inheritdoc />
    public TimeSpan? ConnectionTimeout { get; set; }

    /// <inheritdoc />
    public TimeSpan? CommandTimeout { get; set; }

    /// <inheritdoc />
    public int MaxRetryAttempts { get; set; }

    /// <inheritdoc />
    public TimeSpan RetryDelay { get; set; }

    /// <inheritdoc />
    public bool UseConnectionPooling { get; set; }

    /// <inheritdoc />
    public int MaxPoolSize { get; set; }

    /// <inheritdoc />
    public int MinPoolSize { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object> Settings { get; set; }

    /// <inheritdoc />
    public ISchemaMapping? SchemaMapping { get; set; }

    /// <inheritdoc />
    public int BatchSize { get; set; }

    /// <inheritdoc />
    public bool EnableDetailedLogging { get; set; }

    /// <inheritdoc />
    public IList<string> Tags { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset ModifiedAt { get; set; }

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        // Validate basic properties
        if (string.IsNullOrWhiteSpace(Name))
        {
            result.AddError("Connector name is required", nameof(Name));
        }

        if (string.IsNullOrWhiteSpace(ConnectorType))
        {
            result.AddError("Connector type is required", nameof(ConnectorType));
        }

        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            result.AddError("Connection string is required", nameof(ConnectionString));
        }

        // Validate timeouts
        if (ConnectionTimeout.HasValue && ConnectionTimeout.Value <= TimeSpan.Zero)
        {
            result.AddError("Connection timeout must be greater than zero", nameof(ConnectionTimeout));
        }

        if (CommandTimeout.HasValue && CommandTimeout.Value <= TimeSpan.Zero)
        {
            result.AddError("Command timeout must be greater than zero", nameof(CommandTimeout));
        }

        // Validate retry configuration
        if (MaxRetryAttempts < 0)
        {
            result.AddError("Max retry attempts must be non-negative", nameof(MaxRetryAttempts));
        }

        if (RetryDelay <= TimeSpan.Zero)
        {
            result.AddError("Retry delay must be greater than zero", nameof(RetryDelay));
        }

        // Validate connection pooling settings
        if (UseConnectionPooling)
        {
            if (MaxPoolSize <= 0)
            {
                result.AddError("Max pool size must be greater than zero when connection pooling is enabled", nameof(MaxPoolSize));
            }

            if (MinPoolSize < 0)
            {
                result.AddError("Min pool size must be non-negative", nameof(MinPoolSize));
            }

            if (MinPoolSize > MaxPoolSize)
            {
                result.AddError("Min pool size cannot be greater than max pool size", nameof(MinPoolSize));
            }
        }

        // Validate batch size
        if (BatchSize <= 0)
        {
            result.AddError("Batch size must be greater than zero", nameof(BatchSize));
        }

        return result;
    }

    /// <inheritdoc />
    public IConnectorConfiguration Clone()
    {
        var clone = new ConnectorConfiguration
        {
            Id = Id,
            Name = Name,
            ConnectorType = ConnectorType,
            Description = Description,
            ConnectionString = ConnectionString,
            ConnectionTimeout = ConnectionTimeout,
            CommandTimeout = CommandTimeout,
            MaxRetryAttempts = MaxRetryAttempts,
            RetryDelay = RetryDelay,
            UseConnectionPooling = UseConnectionPooling,
            MaxPoolSize = MaxPoolSize,
            MinPoolSize = MinPoolSize,
            BatchSize = BatchSize,
            EnableDetailedLogging = EnableDetailedLogging,
            CreatedAt = CreatedAt,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConnectionProperties = new Dictionary<string, object>(ConnectionProperties),
            Settings = new Dictionary<string, object>(Settings),
            Tags = new List<string>(Tags)
        };

        // Clone authentication configuration if present
        if (Authentication != null)
        {
            clone.Authentication = ((AuthenticationConfiguration)Authentication).Clone();
        }

        // Clone schema mapping if present
        if (SchemaMapping != null)
        {
            clone.SchemaMapping = ((SchemaMapping)SchemaMapping).Clone();
        }

        return clone;
    }

    /// <inheritdoc />
    public T? GetConnectionProperty<T>(string key)
    {
        if (ConnectionProperties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <inheritdoc />
    public void SetConnectionProperty<T>(string key, T value)
    {
        ConnectionProperties[key] = value!;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public T? GetSetting<T>(string key)
    {
        if (Settings.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <inheritdoc />
    public void SetSetting<T>(string key, T value)
    {
        Settings[key] = value!;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
