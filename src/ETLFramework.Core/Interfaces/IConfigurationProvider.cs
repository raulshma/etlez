using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Interface for configuration providers that can load and save ETL framework configurations.
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Gets the name of this configuration provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the supported configuration formats (e.g., "json", "yaml", "xml").
    /// </summary>
    IEnumerable<string> SupportedFormats { get; }

    /// <summary>
    /// Loads a pipeline configuration from the specified source.
    /// </summary>
    /// <param name="source">The configuration source (file path, connection string, etc.)</param>
    /// <param name="format">The format of the configuration</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The loaded pipeline configuration</returns>
    Task<IPipelineConfiguration> LoadPipelineConfigurationAsync(string source, string format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a pipeline configuration to the specified destination.
    /// </summary>
    /// <param name="configuration">The pipeline configuration to save</param>
    /// <param name="destination">The destination (file path, connection string, etc.)</param>
    /// <param name="format">The format to save in</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task SavePipelineConfigurationAsync(IPipelineConfiguration configuration, string destination, string format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a connector configuration from the specified source.
    /// </summary>
    /// <param name="source">The configuration source</param>
    /// <param name="format">The format of the configuration</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The loaded connector configuration</returns>
    Task<IConnectorConfiguration> LoadConnectorConfigurationAsync(string source, string format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a connector configuration to the specified destination.
    /// </summary>
    /// <param name="configuration">The connector configuration to save</param>
    /// <param name="destination">The destination</param>
    /// <param name="format">The format to save in</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task SaveConnectorConfigurationAsync(IConnectorConfiguration configuration, string destination, string format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a configuration against its schema.
    /// </summary>
    /// <param name="configurationContent">The configuration content to validate</param>
    /// <param name="format">The format of the configuration</param>
    /// <param name="schemaType">The type of configuration schema to validate against</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateConfigurationAsync(string configurationContent, string format, ConfigurationSchemaType schemaType);

    /// <summary>
    /// Gets the configuration schema for a specific type.
    /// </summary>
    /// <param name="schemaType">The type of schema to retrieve</param>
    /// <param name="format">The format of the schema</param>
    /// <returns>The configuration schema</returns>
    Task<string> GetConfigurationSchemaAsync(ConfigurationSchemaType schemaType, string format);

    /// <summary>
    /// Checks if the provider supports the specified format.
    /// </summary>
    /// <param name="format">The format to check</param>
    /// <returns>True if the format is supported, false otherwise</returns>
    bool SupportsFormat(string format);

    /// <summary>
    /// Resolves environment variables and other placeholders in configuration values.
    /// </summary>
    /// <param name="configurationValue">The configuration value that may contain placeholders</param>
    /// <returns>The resolved configuration value</returns>
    string ResolveConfigurationValue(string configurationValue);
}

/// <summary>
/// Represents the type of configuration schema.
/// </summary>
public enum ConfigurationSchemaType
{
    /// <summary>
    /// Schema for pipeline configurations.
    /// </summary>
    Pipeline,

    /// <summary>
    /// Schema for connector configurations.
    /// </summary>
    Connector,

    /// <summary>
    /// Schema for transformation configurations.
    /// </summary>
    Transformation,

    /// <summary>
    /// Schema for scheduling configurations.
    /// </summary>
    Schedule
}
