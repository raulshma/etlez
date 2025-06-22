using ETLFramework.Core.Models;

namespace ETLFramework.Connectors.Factory;

/// <summary>
/// Describes the capabilities, requirements, and metadata of a connector.
/// </summary>
public class ConnectorDescriptor
{
    /// <summary>
    /// Initializes a new instance of the ConnectorDescriptor class.
    /// </summary>
    public ConnectorDescriptor()
    {
        SupportedOperations = new List<ConnectorOperation>();
        RequiredProperties = new List<string>();
        OptionalProperties = new List<string>();
        SupportedFormats = new List<string>();
        Tags = new List<string>();
        ConfigurationSchema = new Dictionary<string, PropertyDescriptor>();
        Examples = new List<ConnectorExample>();
    }

    /// <summary>
    /// Gets or sets the connector type identifier.
    /// </summary>
    public string ConnectorType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the connector.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the connector.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the connector.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the author or vendor of the connector.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category of the connector (e.g., "File System", "Database", "Cloud Storage").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon or logo URL for the connector.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Gets or sets the documentation URL for the connector.
    /// </summary>
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// Gets or sets whether the connector is currently available.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the connector is deprecated.
    /// </summary>
    public bool IsDeprecated { get; set; } = false;

    /// <summary>
    /// Gets or sets the deprecation message if the connector is deprecated.
    /// </summary>
    public string? DeprecationMessage { get; set; }

    /// <summary>
    /// Gets the list of operations supported by the connector.
    /// </summary>
    public List<ConnectorOperation> SupportedOperations { get; }

    /// <summary>
    /// Gets the list of required configuration properties.
    /// </summary>
    public List<string> RequiredProperties { get; }

    /// <summary>
    /// Gets the list of optional configuration properties.
    /// </summary>
    public List<string> OptionalProperties { get; }

    /// <summary>
    /// Gets the list of supported data formats.
    /// </summary>
    public List<string> SupportedFormats { get; }

    /// <summary>
    /// Gets the list of tags associated with the connector.
    /// </summary>
    public List<string> Tags { get; }

    /// <summary>
    /// Gets the configuration schema for the connector.
    /// </summary>
    public Dictionary<string, PropertyDescriptor> ConfigurationSchema { get; }

    /// <summary>
    /// Gets the list of configuration examples.
    /// </summary>
    public List<ConnectorExample> Examples { get; }

    /// <summary>
    /// Gets or sets the minimum framework version required.
    /// </summary>
    public string? MinimumFrameworkVersion { get; set; }

    /// <summary>
    /// Gets or sets the .NET implementation type for the connector.
    /// </summary>
    public Type? ImplementationType { get; set; }

    /// <summary>
    /// Gets or sets the assembly name containing the connector.
    /// </summary>
    public string? AssemblyName { get; set; }

    /// <summary>
    /// Gets or sets performance characteristics of the connector.
    /// </summary>
    public ConnectorPerformance? Performance { get; set; }

    /// <summary>
    /// Gets or sets security information for the connector.
    /// </summary>
    public ConnectorSecurity? Security { get; set; }

    /// <summary>
    /// Checks if the connector supports a specific operation.
    /// </summary>
    /// <param name="operation">The operation to check</param>
    /// <returns>True if the operation is supported</returns>
    public bool SupportsOperation(ConnectorOperation operation)
    {
        return SupportedOperations.Contains(operation);
    }

    /// <summary>
    /// Checks if the connector supports a specific data format.
    /// </summary>
    /// <param name="format">The format to check</param>
    /// <returns>True if the format is supported</returns>
    public bool SupportsFormat(string format)
    {
        return SupportedFormats.Contains(format, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a configuration example by name.
    /// </summary>
    /// <param name="name">The example name</param>
    /// <returns>The configuration example or null if not found</returns>
    public ConnectorExample? GetExample(string name)
    {
        return Examples.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates that all required properties are present in a configuration.
    /// </summary>
    /// <param name="configuration">The configuration to validate</param>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateConfiguration(Dictionary<string, object> configuration)
    {
        var result = new ValidationResult();

        // Check required properties
        foreach (var requiredProperty in RequiredProperties)
        {
            if (!configuration.ContainsKey(requiredProperty))
            {
                result.AddError($"Required property '{requiredProperty}' is missing", requiredProperty);
            }
        }

        // Validate property types and constraints
        foreach (var kvp in configuration)
        {
            if (ConfigurationSchema.TryGetValue(kvp.Key, out var propertyDescriptor))
            {
                var propertyResult = propertyDescriptor.Validate(kvp.Value);
                result.Merge(propertyResult);
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a basic configuration template for the connector.
    /// </summary>
    /// <returns>A basic configuration dictionary</returns>
    public Dictionary<string, object> CreateBasicTemplate()
    {
        var template = new Dictionary<string, object>();

        foreach (var property in RequiredProperties)
        {
            if (ConfigurationSchema.TryGetValue(property, out var descriptor))
            {
                template[property] = descriptor.DefaultValue ?? GetDefaultValueForType(descriptor.PropertyType);
            }
            else
            {
                template[property] = "";
            }
        }

        return template;
    }

    /// <summary>
    /// Gets a default value for a given type.
    /// </summary>
    /// <param name="type">The type</param>
    /// <returns>A default value</returns>
    private static object GetDefaultValueForType(Type type)
    {
        if (type == typeof(string))
            return "";
        if (type == typeof(int))
            return 0;
        if (type == typeof(bool))
            return false;
        if (type == typeof(TimeSpan))
            return TimeSpan.Zero;
        
        return type.IsValueType ? Activator.CreateInstance(type)! : "";
    }
}

/// <summary>
/// Represents an operation that a connector can perform.
/// </summary>
public enum ConnectorOperation
{
    /// <summary>
    /// Read data from a source.
    /// </summary>
    Read,

    /// <summary>
    /// Write data to a destination.
    /// </summary>
    Write,

    /// <summary>
    /// Test connectivity.
    /// </summary>
    TestConnection,

    /// <summary>
    /// Discover schema.
    /// </summary>
    DiscoverSchema,

    /// <summary>
    /// Batch operations.
    /// </summary>
    BatchOperations,

    /// <summary>
    /// Streaming operations.
    /// </summary>
    Streaming,

    /// <summary>
    /// Transaction support.
    /// </summary>
    Transactions,

    /// <summary>
    /// Bulk operations.
    /// </summary>
    BulkOperations
}

/// <summary>
/// Describes a configuration property.
/// </summary>
public class PropertyDescriptor
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    public Type PropertyType { get; set; } = typeof(string);

    /// <summary>
    /// Gets or sets the property description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the property is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum value (for numeric types).
    /// </summary>
    public object? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value (for numeric types).
    /// </summary>
    public object? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets allowed values (for enumeration types).
    /// </summary>
    public List<object>? AllowedValues { get; set; }

    /// <summary>
    /// Gets or sets a validation pattern (for string types).
    /// </summary>
    public string? ValidationPattern { get; set; }

    /// <summary>
    /// Validates a property value.
    /// </summary>
    /// <param name="value">The value to validate</param>
    /// <returns>A validation result</returns>
    public ValidationResult Validate(object? value)
    {
        var result = new ValidationResult();

        if (value == null)
        {
            if (IsRequired)
            {
                result.AddError($"Property '{Name}' is required", Name);
            }
            return result;
        }

        // Type validation
        if (!PropertyType.IsAssignableFrom(value.GetType()))
        {
            result.AddError($"Property '{Name}' must be of type {PropertyType.Name}", Name);
            return result;
        }

        // Range validation for numeric types
        if (MinValue != null && value is IComparable comparable1 && comparable1.CompareTo(MinValue) < 0)
        {
            result.AddError($"Property '{Name}' must be greater than or equal to {MinValue}", Name);
        }

        if (MaxValue != null && value is IComparable comparable2 && comparable2.CompareTo(MaxValue) > 0)
        {
            result.AddError($"Property '{Name}' must be less than or equal to {MaxValue}", Name);
        }

        // Allowed values validation
        if (AllowedValues != null && !AllowedValues.Contains(value))
        {
            result.AddError($"Property '{Name}' must be one of: {string.Join(", ", AllowedValues)}", Name);
        }

        // Pattern validation for strings
        if (!string.IsNullOrEmpty(ValidationPattern) && value is string stringValue)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(stringValue, ValidationPattern))
            {
                result.AddError($"Property '{Name}' does not match the required pattern", Name);
            }
        }

        return result;
    }
}

/// <summary>
/// Represents a configuration example.
/// </summary>
public class ConnectorExample
{
    /// <summary>
    /// Gets or sets the example name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the example description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the example configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the use case for this example.
    /// </summary>
    public string UseCase { get; set; } = string.Empty;
}

/// <summary>
/// Represents performance characteristics of a connector.
/// </summary>
public class ConnectorPerformance
{
    /// <summary>
    /// Gets or sets the typical throughput in records per second.
    /// </summary>
    public int? TypicalThroughput { get; set; }

    /// <summary>
    /// Gets or sets the maximum batch size.
    /// </summary>
    public int? MaxBatchSize { get; set; }

    /// <summary>
    /// Gets or sets whether the connector supports parallel processing.
    /// </summary>
    public bool SupportsParallelProcessing { get; set; }

    /// <summary>
    /// Gets or sets the recommended number of concurrent connections.
    /// </summary>
    public int? RecommendedConcurrency { get; set; }
}

/// <summary>
/// Represents security information for a connector.
/// </summary>
public class ConnectorSecurity
{
    /// <summary>
    /// Gets or sets whether the connector supports encryption in transit.
    /// </summary>
    public bool SupportsEncryptionInTransit { get; set; }

    /// <summary>
    /// Gets or sets whether the connector supports encryption at rest.
    /// </summary>
    public bool SupportsEncryptionAtRest { get; set; }

    /// <summary>
    /// Gets or sets the supported authentication methods.
    /// </summary>
    public List<string> SupportedAuthenticationMethods { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets whether the connector requires credentials.
    /// </summary>
    public bool RequiresCredentials { get; set; }

    /// <summary>
    /// Gets or sets security notes or warnings.
    /// </summary>
    public string? SecurityNotes { get; set; }
}
