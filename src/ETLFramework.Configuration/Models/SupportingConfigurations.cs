using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using System.Text.RegularExpressions;

namespace ETLFramework.Configuration.Models;

/// <summary>
/// Concrete implementation of error handling configuration.
/// </summary>
public class ErrorHandlingConfiguration : IErrorHandlingConfiguration, IValidatable
{
    /// <summary>
    /// Initializes a new instance of the ErrorHandlingConfiguration class.
    /// </summary>
    public ErrorHandlingConfiguration()
    {
        StopOnError = false;
        MaxErrors = 100;
    }

    /// <inheritdoc />
    public bool StopOnError { get; set; }

    /// <inheritdoc />
    public int MaxErrors { get; set; }

    /// <summary>
    /// Validates the error handling configuration.
    /// </summary>
    /// <returns>Validation result</returns>
    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        if (MaxErrors < 0)
        {
            result.AddError("MaxErrors must be non-negative", nameof(MaxErrors));
        }

        if (MaxErrors == 0 && !StopOnError)
        {
            result.AddWarning("MaxErrors is 0 but StopOnError is false - this may cause infinite error accumulation", nameof(MaxErrors));
        }

        return result;
    }

    /// <summary>
    /// Creates a deep copy of this error handling configuration.
    /// </summary>
    /// <returns>A new ErrorHandlingConfiguration instance</returns>
    public ErrorHandlingConfiguration Clone()
    {
        return new ErrorHandlingConfiguration
        {
            StopOnError = StopOnError,
            MaxErrors = MaxErrors
        };
    }
}

/// <summary>
/// Concrete implementation of retry configuration.
/// </summary>
public class RetryConfiguration : IRetryConfiguration
{
    /// <summary>
    /// Initializes a new instance of the RetryConfiguration class.
    /// </summary>
    public RetryConfiguration()
    {
        MaxAttempts = 3;
        Delay = TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc />
    public int MaxAttempts { get; set; }

    /// <inheritdoc />
    public TimeSpan Delay { get; set; }

    /// <summary>
    /// Validates the retry configuration.
    /// </summary>
    /// <returns>Validation result</returns>
    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        if (MaxAttempts < 0)
        {
            result.AddError("Max attempts must be non-negative", nameof(MaxAttempts));
        }

        if (Delay <= TimeSpan.Zero)
        {
            result.AddError("Delay must be greater than zero", nameof(Delay));
        }

        return result;
    }

    /// <summary>
    /// Creates a deep copy of this retry configuration.
    /// </summary>
    /// <returns>A new RetryConfiguration instance</returns>
    public RetryConfiguration Clone()
    {
        return new RetryConfiguration
        {
            MaxAttempts = MaxAttempts,
            Delay = Delay
        };
    }
}

/// <summary>
/// Concrete implementation of schedule configuration.
/// </summary>
public class ScheduleConfiguration : IScheduleConfiguration, IValidatable
{
    /// <summary>
    /// Initializes a new instance of the ScheduleConfiguration class.
    /// </summary>
    public ScheduleConfiguration()
    {
        CronExpression = string.Empty;
        IsEnabled = false;
    }

    /// <inheritdoc />
    public string CronExpression { get; set; }

    /// <inheritdoc />
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Validates the schedule configuration.
    /// </summary>
    /// <returns>Validation result</returns>
    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        if (IsEnabled)
        {
            if (string.IsNullOrWhiteSpace(CronExpression))
            {
                result.AddError("CronExpression is required when schedule is enabled", nameof(CronExpression));
            }
            else if (!IsValidCronExpression(CronExpression))
            {
                result.AddError($"Invalid cron expression: {CronExpression}", nameof(CronExpression));
            }
        }

        return result;
    }

    /// <summary>
    /// Validates if a cron expression is in the correct format.
    /// </summary>
    /// <param name="cronExpression">The cron expression to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    private static bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return false;

        // Basic cron expression validation (5 or 6 fields)
        var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5 && parts.Length != 6)
            return false;

        // Validate each field using regex patterns
        var patterns = new[]
        {
            @"^(\*|[0-5]?\d)$", // Minutes (0-59)
            @"^(\*|[01]?\d|2[0-3])$", // Hours (0-23)
            @"^(\*|[01]?\d|2\d|3[01])$", // Day of month (1-31)
            @"^(\*|[01]?\d)$", // Month (1-12)
            @"^(\*|[0-6])$" // Day of week (0-6)
        };

        // If 6 fields, first is seconds
        var startIndex = parts.Length == 6 ? 1 : 0;
        if (parts.Length == 6 && !Regex.IsMatch(parts[0], @"^(\*|[0-5]?\d)$"))
            return false;

        for (int i = 0; i < 5; i++)
        {
            var part = parts[startIndex + i];
            // Allow more complex expressions with ranges, lists, and steps
            if (!IsValidCronField(part, i))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a single cron field.
    /// </summary>
    /// <param name="field">The field value</param>
    /// <param name="fieldIndex">The field index (0=minute, 1=hour, etc.)</param>
    /// <returns>True if valid</returns>
    private static bool IsValidCronField(string field, int fieldIndex)
    {
        if (field == "*")
            return true;

        // Handle ranges (e.g., 1-5), lists (e.g., 1,3,5), and steps (e.g., */5, 1-10/2)
        var pattern = fieldIndex switch
        {
            0 => @"^(\*|[0-5]?\d)([,-/](\*|[0-5]?\d))*$", // Minutes
            1 => @"^(\*|[01]?\d|2[0-3])([,-/](\*|[01]?\d|2[0-3]))*$", // Hours
            2 => @"^(\*|[01]?\d|2\d|3[01])([,-/](\*|[01]?\d|2\d|3[01]))*$", // Day of month
            3 => @"^(\*|[01]?\d)([,-/](\*|[01]?\d))*$", // Month
            4 => @"^(\*|[0-6])([,-/](\*|[0-6]))*$", // Day of week
            _ => @"^.*$"
        };

        return Regex.IsMatch(field, pattern);
    }

    /// <summary>
    /// Creates a deep copy of this schedule configuration.
    /// </summary>
    /// <returns>A new ScheduleConfiguration instance</returns>
    public ScheduleConfiguration Clone()
    {
        return new ScheduleConfiguration
        {
            CronExpression = CronExpression,
            IsEnabled = IsEnabled
        };
    }
}

/// <summary>
/// Concrete implementation of notification configuration.
/// </summary>
public class NotificationConfiguration : INotificationConfiguration
{
    /// <summary>
    /// Initializes a new instance of the NotificationConfiguration class.
    /// </summary>
    public NotificationConfiguration()
    {
        EnableEmailNotifications = false;
        EmailRecipients = new List<string>();
    }

    /// <inheritdoc />
    public bool EnableEmailNotifications { get; set; }

    /// <inheritdoc />
    public IList<string> EmailRecipients { get; set; }

    /// <summary>
    /// Creates a deep copy of this notification configuration.
    /// </summary>
    /// <returns>A new NotificationConfiguration instance</returns>
    public NotificationConfiguration Clone()
    {
        return new NotificationConfiguration
        {
            EnableEmailNotifications = EnableEmailNotifications,
            EmailRecipients = new List<string>(EmailRecipients)
        };
    }
}

/// <summary>
/// Concrete implementation of authentication configuration.
/// </summary>
public class AuthenticationConfiguration : IAuthenticationConfiguration
{
    /// <summary>
    /// Initializes a new instance of the AuthenticationConfiguration class.
    /// </summary>
    public AuthenticationConfiguration()
    {
        AuthenticationType = string.Empty;
        Credentials = new Dictionary<string, object>();
    }

    /// <inheritdoc />
    public string AuthenticationType { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object> Credentials { get; set; }

    /// <summary>
    /// Creates a deep copy of this authentication configuration.
    /// </summary>
    /// <returns>A new AuthenticationConfiguration instance</returns>
    public AuthenticationConfiguration Clone()
    {
        return new AuthenticationConfiguration
        {
            AuthenticationType = AuthenticationType,
            Credentials = new Dictionary<string, object>(Credentials)
        };
    }
}

/// <summary>
/// Concrete implementation of schema mapping.
/// </summary>
public class SchemaMapping : ISchemaMapping
{
    /// <summary>
    /// Initializes a new instance of the SchemaMapping class.
    /// </summary>
    public SchemaMapping()
    {
        TypeMappings = new Dictionary<string, string>();
    }

    /// <inheritdoc />
    public IDictionary<string, string> TypeMappings { get; set; }

    /// <summary>
    /// Creates a deep copy of this schema mapping.
    /// </summary>
    /// <returns>A new SchemaMapping instance</returns>
    public SchemaMapping Clone()
    {
        return new SchemaMapping
        {
            TypeMappings = new Dictionary<string, string>(TypeMappings)
        };
    }
}

/// <summary>
/// Concrete implementation of transformation configuration.
/// </summary>
public class TransformationConfiguration : ITransformationConfiguration
{
    /// <summary>
    /// Initializes a new instance of the TransformationConfiguration class.
    /// </summary>
    public TransformationConfiguration()
    {
        Rules = new List<ITransformationRuleConfiguration>();
    }

    /// <inheritdoc />
    public IList<ITransformationRuleConfiguration> Rules { get; set; }

    /// <summary>
    /// Creates a deep copy of this transformation configuration.
    /// </summary>
    /// <returns>A new TransformationConfiguration instance</returns>
    public TransformationConfiguration Clone()
    {
        var clone = new TransformationConfiguration();
        
        foreach (var rule in Rules)
        {
            clone.Rules.Add(((TransformationRuleConfiguration)rule).Clone());
        }

        return clone;
    }
}

/// <summary>
/// Concrete implementation of transformation rule configuration.
/// </summary>
public class TransformationRuleConfiguration : ITransformationRuleConfiguration
{
    /// <summary>
    /// Initializes a new instance of the TransformationRuleConfiguration class.
    /// </summary>
    public TransformationRuleConfiguration()
    {
        RuleType = string.Empty;
        Settings = new Dictionary<string, object>();
    }

    /// <inheritdoc />
    public string RuleType { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object> Settings { get; set; }

    /// <summary>
    /// Creates a deep copy of this transformation rule configuration.
    /// </summary>
    /// <returns>A new TransformationRuleConfiguration instance</returns>
    public TransformationRuleConfiguration Clone()
    {
        return new TransformationRuleConfiguration
        {
            RuleType = RuleType,
            Settings = new Dictionary<string, object>(Settings)
        };
    }
}
