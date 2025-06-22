using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;

namespace ETLFramework.Configuration.Models;

/// <summary>
/// Concrete implementation of stage configuration.
/// </summary>
public class StageConfiguration : IStageConfiguration
{
    /// <summary>
    /// Initializes a new instance of the StageConfiguration class.
    /// </summary>
    public StageConfiguration()
    {
        Id = Guid.NewGuid();
        Name = string.Empty;
        Description = string.Empty;
        StageType = StageType.Custom;
        Order = 0;
        IsEnabled = true;
        Settings = new Dictionary<string, object>();
        ExecutionConditions = new List<IExecutionCondition>();
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public StageType StageType { get; set; }

    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public bool IsEnabled { get; set; }

    /// <inheritdoc />
    public IConnectorConfiguration? ConnectorConfiguration { get; set; }

    /// <inheritdoc />
    public ITransformationConfiguration? TransformationConfiguration { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object> Settings { get; set; }

    /// <inheritdoc />
    public IList<IExecutionCondition> ExecutionConditions { get; set; }

    /// <inheritdoc />
    public TimeSpan? Timeout { get; set; }

    /// <inheritdoc />
    public IRetryConfiguration? Retry { get; set; }

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        // Validate basic properties
        if (string.IsNullOrWhiteSpace(Name))
        {
            result.AddError("Stage name is required", nameof(Name));
        }

        if (Order < 0)
        {
            result.AddError("Stage order must be non-negative", nameof(Order));
        }

        // Validate timeout
        if (Timeout.HasValue && Timeout.Value <= TimeSpan.Zero)
        {
            result.AddError("Timeout must be greater than zero", nameof(Timeout));
        }

        // Validate stage type specific requirements
        switch (StageType)
        {
            case StageType.Extract:
                if (ConnectorConfiguration == null)
                {
                    result.AddError("Extract stage requires a connector configuration", nameof(ConnectorConfiguration));
                }
                break;

            case StageType.Load:
                if (ConnectorConfiguration == null)
                {
                    result.AddError("Load stage requires a connector configuration", nameof(ConnectorConfiguration));
                }
                break;

            case StageType.Transform:
                if (TransformationConfiguration == null)
                {
                    result.AddError("Transform stage requires a transformation configuration", nameof(TransformationConfiguration));
                }
                break;
        }

        // Validate connector configuration if present
        if (ConnectorConfiguration != null)
        {
            var connectorValidation = ConnectorConfiguration.Validate();
            result.Merge(connectorValidation);
        }

        // Validate retry configuration if present
        if (Retry != null)
        {
            var retryValidation = ((RetryConfiguration)Retry).Validate();
            result.Merge(retryValidation);
        }

        return result;
    }

    /// <summary>
    /// Creates a deep copy of this stage configuration.
    /// </summary>
    /// <returns>A new StageConfiguration instance with the same values</returns>
    public StageConfiguration Clone()
    {
        var clone = new StageConfiguration
        {
            Id = Id,
            Name = Name,
            Description = Description,
            StageType = StageType,
            Order = Order,
            IsEnabled = IsEnabled,
            Timeout = Timeout,
            Settings = new Dictionary<string, object>(Settings),
            ExecutionConditions = new List<IExecutionCondition>(ExecutionConditions)
        };

        // Clone connector configuration if present
        if (ConnectorConfiguration != null)
        {
            clone.ConnectorConfiguration = ConnectorConfiguration.Clone();
        }

        // Clone transformation configuration if present
        if (TransformationConfiguration != null)
        {
            clone.TransformationConfiguration = ((TransformationConfiguration)TransformationConfiguration).Clone();
        }

        // Clone retry configuration if present
        if (Retry != null)
        {
            clone.Retry = ((RetryConfiguration)Retry).Clone();
        }

        return clone;
    }
}
