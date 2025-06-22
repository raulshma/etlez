using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;

namespace ETLFramework.Configuration.Models;

/// <summary>
/// Concrete implementation of pipeline configuration.
/// </summary>
public class PipelineConfiguration : IPipelineConfiguration
{
    /// <summary>
    /// Initializes a new instance of the PipelineConfiguration class.
    /// </summary>
    public PipelineConfiguration()
    {
        Id = Guid.NewGuid();
        Name = string.Empty;
        Description = string.Empty;
        Version = "1.0.0";
        CreatedAt = DateTimeOffset.UtcNow;
        ModifiedAt = DateTimeOffset.UtcNow;
        Stages = new List<IStageConfiguration>();
        GlobalSettings = new Dictionary<string, object>();
        Tags = new List<string>();
        ErrorHandling = new ErrorHandlingConfiguration();
        Retry = new RetryConfiguration();
        IsEnabled = true;
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public string Version { get; set; }

    /// <inheritdoc />
    public string? Author { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset ModifiedAt { get; set; }

    /// <inheritdoc />
    public IList<IStageConfiguration> Stages { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object> GlobalSettings { get; set; }

    /// <inheritdoc />
    public IErrorHandlingConfiguration ErrorHandling { get; set; }

    /// <inheritdoc />
    public IRetryConfiguration Retry { get; set; }

    /// <inheritdoc />
    public TimeSpan? Timeout { get; set; }

    /// <inheritdoc />
    public int? MaxDegreeOfParallelism { get; set; }

    /// <inheritdoc />
    public IScheduleConfiguration? Schedule { get; set; }

    /// <inheritdoc />
    public INotificationConfiguration? Notifications { get; set; }

    /// <inheritdoc />
    public IList<string> Tags { get; set; }

    /// <inheritdoc />
    public bool IsEnabled { get; set; }

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        // Validate basic properties
        if (string.IsNullOrWhiteSpace(Name))
        {
            result.AddError("Pipeline name is required", nameof(Name));
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            result.AddError("Pipeline version is required", nameof(Version));
        }

        // Validate stages
        if (Stages.Count == 0)
        {
            result.AddWarning("Pipeline has no stages defined", nameof(Stages));
        }
        else
        {
            var stageOrders = new HashSet<int>();
            foreach (var stage in Stages)
            {
                var stageValidation = stage.Validate();
                result.Merge(stageValidation);

                // Check for duplicate stage orders
                if (stageOrders.Contains(stage.Order))
                {
                    result.AddError($"Duplicate stage order: {stage.Order}", nameof(Stages));
                }
                stageOrders.Add(stage.Order);
            }
        }

        // Validate timeout
        if (Timeout.HasValue && Timeout.Value <= TimeSpan.Zero)
        {
            result.AddError("Timeout must be greater than zero", nameof(Timeout));
        }

        // Validate max degree of parallelism
        if (MaxDegreeOfParallelism.HasValue && MaxDegreeOfParallelism.Value <= 0)
        {
            result.AddError("MaxDegreeOfParallelism must be greater than zero", nameof(MaxDegreeOfParallelism));
        }

        return result;
    }

    /// <inheritdoc />
    public IPipelineConfiguration Clone()
    {
        var clone = new PipelineConfiguration
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Version = Version,
            Author = Author,
            CreatedAt = CreatedAt,
            ModifiedAt = DateTimeOffset.UtcNow,
            Timeout = Timeout,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            IsEnabled = IsEnabled,
            GlobalSettings = new Dictionary<string, object>(GlobalSettings),
            Tags = new List<string>(Tags),
            ErrorHandling = ((ErrorHandlingConfiguration)ErrorHandling).Clone(),
            Retry = ((RetryConfiguration)Retry).Clone()
        };

        // Clone stages
        foreach (var stage in Stages)
        {
            clone.Stages.Add(((StageConfiguration)stage).Clone());
        }

        // Clone optional configurations
        if (Schedule != null)
        {
            clone.Schedule = ((ScheduleConfiguration)Schedule).Clone();
        }

        if (Notifications != null)
        {
            clone.Notifications = ((NotificationConfiguration)Notifications).Clone();
        }

        return clone;
    }

    /// <inheritdoc />
    public void Merge(IPipelineConfiguration other, bool overwriteExisting = false)
    {
        if (other == null) return;

        if (overwriteExisting || string.IsNullOrWhiteSpace(Name))
            Name = other.Name;

        if (overwriteExisting || string.IsNullOrWhiteSpace(Description))
            Description = other.Description;

        if (overwriteExisting || string.IsNullOrWhiteSpace(Version))
            Version = other.Version;

        if (overwriteExisting || string.IsNullOrWhiteSpace(Author))
            Author = other.Author;

        if (overwriteExisting || !Timeout.HasValue)
            Timeout = other.Timeout;

        if (overwriteExisting || !MaxDegreeOfParallelism.HasValue)
            MaxDegreeOfParallelism = other.MaxDegreeOfParallelism;

        // Merge global settings
        foreach (var setting in other.GlobalSettings)
        {
            if (overwriteExisting || !GlobalSettings.ContainsKey(setting.Key))
            {
                GlobalSettings[setting.Key] = setting.Value;
            }
        }

        // Merge tags
        foreach (var tag in other.Tags)
        {
            if (!Tags.Contains(tag))
            {
                Tags.Add(tag);
            }
        }

        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
