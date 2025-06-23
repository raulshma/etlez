using ETLFramework.Core.Interfaces;

namespace ETLFramework.Core.Models;

// Placeholder models to resolve compilation errors
// These will be expanded in later tasks

/// <summary>
/// Placeholder for connection test result.
/// </summary>
public class ConnectionTestResult
{
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
}

/// <summary>
/// Placeholder for connector metadata.
/// </summary>
public class ConnectorMetadata
{
    public string? Version { get; set; }
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Placeholder for data schema.
/// </summary>
public class DataSchema
{
    public string Name { get; set; } = string.Empty;
    public IList<DataField> Fields { get; set; } = new List<DataField>();
}

/// <summary>
/// Placeholder for data field.
/// </summary>
public class DataField
{
    public string Name { get; set; } = string.Empty;
    public Type DataType { get; set; } = typeof(object);
    public bool IsRequired { get; set; }
}

/// <summary>
/// Placeholder for write result.
/// </summary>
public class WriteResult
{
    public bool IsSuccessful { get; set; }
    public long RecordsWritten { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Placeholder for transformation result.
/// </summary>
public class TransformationResult
{
    public bool IsSuccessful { get; set; }
    public DataRecord? OutputRecord { get; set; }
    public IList<ExecutionError> Errors { get; set; } = new List<ExecutionError>();
}

/// <summary>
/// Placeholder for transformation rule configuration.
/// </summary>
public interface ITransformationRuleConfiguration
{
    string RuleType { get; }
    IDictionary<string, object> Settings { get; }
}

/// <summary>
/// Placeholder for mapping configuration.
/// </summary>
public interface IMappingConfiguration
{
    IList<FieldMapping> FieldMappings { get; }
}

/// <summary>
/// Placeholder for field mapping.
/// </summary>
public class FieldMapping
{
    public string SourceField { get; set; } = string.Empty;
    public string DestinationField { get; set; } = string.Empty;
}

/// <summary>
/// Placeholder for connector configuration schema.
/// </summary>
public class ConnectorConfigurationSchema
{
    public string SchemaVersion { get; set; } = string.Empty;
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Placeholder for pipeline execution status.
/// </summary>
public class PipelineExecutionStatus
{
    public Guid ExecutionId { get; set; }
    public PipelineStatus Status { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public long RecordsProcessed { get; set; }
}

/// <summary>
/// Placeholder for pipeline execution history.
/// </summary>
public class PipelineExecutionHistory
{
    public Guid ExecutionId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public PipelineStatus Status { get; set; }
    public long RecordsProcessed { get; set; }
}

/// <summary>
/// Placeholder for pipeline execution event args.
/// </summary>
public class PipelineExecutionEventArgs : EventArgs
{
    public Guid ExecutionId { get; set; }
    public Guid PipelineId { get; set; }
    public PipelineExecutionResult? Result { get; set; }
}

/// <summary>
/// Placeholder for error handling configuration.
/// </summary>
public interface IErrorHandlingConfiguration
{
    bool StopOnError { get; }
    int MaxErrors { get; }
}

/// <summary>
/// Placeholder for retry configuration.
/// </summary>
public interface IRetryConfiguration
{
    int MaxAttempts { get; }
    TimeSpan Delay { get; }
}

/// <summary>
/// Placeholder for schedule configuration.
/// </summary>
public interface IScheduleConfiguration
{
    string CronExpression { get; }
    bool IsEnabled { get; }
}

/// <summary>
/// Placeholder for notification configuration.
/// </summary>
public interface INotificationConfiguration
{
    bool EnableEmailNotifications { get; }
    IList<string> EmailRecipients { get; }
}

/// <summary>
/// Placeholder for execution condition.
/// </summary>
public interface IExecutionCondition
{
    string ConditionType { get; }
    bool Evaluate(IPipelineContext context);
}

/// <summary>
/// Placeholder for authentication configuration.
/// </summary>
public interface IAuthenticationConfiguration
{
    string AuthenticationType { get; }
    IDictionary<string, object> Credentials { get; }
}

/// <summary>
/// Placeholder for schema mapping.
/// </summary>
public interface ISchemaMapping
{
    IDictionary<string, string> TypeMappings { get; }
}

/// <summary>
/// Placeholder for transformation configuration.
/// </summary>
public interface ITransformationConfiguration
{
    IList<ITransformationRuleConfiguration> Rules { get; }
}
