using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using ETLFramework.Configuration.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Configuration.Providers;

/// <summary>
/// YAML-based configuration provider for loading and saving ETL configurations.
/// </summary>
public class YamlConfigurationProvider : IConfigurationProvider
{
    private readonly ILogger<YamlConfigurationProvider> _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the YamlConfigurationProvider class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public YamlConfigurationProvider(ILogger<YamlConfigurationProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <inheritdoc />
    public string Name => "YAML Configuration Provider";

    /// <inheritdoc />
    public IEnumerable<string> SupportedFormats => new[] { "yaml", "yml" };

    /// <inheritdoc />
    public async Task<IPipelineConfiguration> LoadPipelineConfigurationAsync(string source, string format, CancellationToken cancellationToken = default)
    {
        if (!SupportsFormat(format))
        {
            throw new ConfigurationException($"Unsupported format: {format}. Supported formats: {string.Join(", ", SupportedFormats)}");
        }

        try
        {
            _logger.LogInformation("Loading pipeline configuration from {Source}", source);

            string yamlContent;
            if (File.Exists(source))
            {
                yamlContent = await File.ReadAllTextAsync(source, cancellationToken);
            }
            else
            {
                // Assume source is the YAML content itself
                yamlContent = source;
            }

            // Resolve environment variables
            yamlContent = ResolveConfigurationValue(yamlContent);

            var configuration = _deserializer.Deserialize<PipelineConfiguration>(yamlContent);
            if (configuration == null)
            {
                throw new ConfigurationException("Failed to deserialize pipeline configuration - result was null");
            }

            _logger.LogInformation("Successfully loaded pipeline configuration: {Name} (ID: {Id})", 
                configuration.Name, configuration.Id);

            return configuration;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw ConfigurationException.CreateParseFailure(
                $"Failed to parse YAML configuration: {ex.Message}",
                source,
                format,
                (int)ex.Start.Line,
                (int)ex.Start.Column);
        }
        catch (FileNotFoundException)
        {
            throw ConfigurationException.CreateFileLoadFailure(
                $"Configuration file not found: {source}", 
                source, 
                format);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw ConfigurationException.CreateFileLoadFailure(
                $"Access denied to configuration file: {ex.Message}", 
                source, 
                format);
        }
        catch (Exception ex) when (!(ex is ConfigurationException))
        {
            throw new ConfigurationException($"Unexpected error loading configuration: {ex.Message}", ex)
            {
                ConfigurationSource = source,
                ConfigurationFormat = format
            };
        }
    }

    /// <inheritdoc />
    public async Task SavePipelineConfigurationAsync(IPipelineConfiguration configuration, string destination, string format, CancellationToken cancellationToken = default)
    {
        if (!SupportsFormat(format))
        {
            throw new ConfigurationException($"Unsupported format: {format}. Supported formats: {string.Join(", ", SupportedFormats)}");
        }

        try
        {
            _logger.LogInformation("Saving pipeline configuration {Name} to {Destination}", 
                configuration.Name, destination);

            var yamlContent = _serializer.Serialize(configuration);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(destination, yamlContent, cancellationToken);

            _logger.LogInformation("Successfully saved pipeline configuration to {Destination}", destination);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw ConfigurationException.CreateFileLoadFailure(
                $"Access denied to destination file: {ex.Message}", 
                destination, 
                format);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw ConfigurationException.CreateFileLoadFailure(
                $"Directory not found: {ex.Message}", 
                destination, 
                format);
        }
        catch (Exception ex) when (!(ex is ConfigurationException))
        {
            throw new ConfigurationException($"Unexpected error saving configuration: {ex.Message}", ex)
            {
                ConfigurationSource = destination,
                ConfigurationFormat = format
            };
        }
    }

    /// <inheritdoc />
    public async Task<IConnectorConfiguration> LoadConnectorConfigurationAsync(string source, string format, CancellationToken cancellationToken = default)
    {
        if (!SupportsFormat(format))
        {
            throw new ConfigurationException($"Unsupported format: {format}. Supported formats: {string.Join(", ", SupportedFormats)}");
        }

        try
        {
            _logger.LogInformation("Loading connector configuration from {Source}", source);

            string yamlContent;
            if (File.Exists(source))
            {
                yamlContent = await File.ReadAllTextAsync(source, cancellationToken);
            }
            else
            {
                yamlContent = source;
            }

            // Resolve environment variables
            yamlContent = ResolveConfigurationValue(yamlContent);

            var configuration = _deserializer.Deserialize<ConnectorConfiguration>(yamlContent);
            if (configuration == null)
            {
                throw new ConfigurationException("Failed to deserialize connector configuration - result was null");
            }

            _logger.LogInformation("Successfully loaded connector configuration: {Name} (Type: {Type})", 
                configuration.Name, configuration.ConnectorType);

            return configuration;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw ConfigurationException.CreateParseFailure(
                $"Failed to parse YAML configuration: {ex.Message}",
                source,
                format,
                (int)ex.Start.Line,
                (int)ex.Start.Column);
        }
        catch (Exception ex) when (!(ex is ConfigurationException))
        {
            throw new ConfigurationException($"Unexpected error loading connector configuration: {ex.Message}", ex)
            {
                ConfigurationSource = source,
                ConfigurationFormat = format
            };
        }
    }

    /// <inheritdoc />
    public async Task SaveConnectorConfigurationAsync(IConnectorConfiguration configuration, string destination, string format, CancellationToken cancellationToken = default)
    {
        if (!SupportsFormat(format))
        {
            throw new ConfigurationException($"Unsupported format: {format}. Supported formats: {string.Join(", ", SupportedFormats)}");
        }

        try
        {
            _logger.LogInformation("Saving connector configuration {Name} to {Destination}", 
                configuration.Name, destination);

            var yamlContent = _serializer.Serialize(configuration);
            
            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(destination, yamlContent, cancellationToken);

            _logger.LogInformation("Successfully saved connector configuration to {Destination}", destination);
        }
        catch (Exception ex) when (!(ex is ConfigurationException))
        {
            throw new ConfigurationException($"Unexpected error saving connector configuration: {ex.Message}", ex)
            {
                ConfigurationSource = destination,
                ConfigurationFormat = format
            };
        }
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateConfigurationAsync(string configurationContent, string format, ConfigurationSchemaType schemaType)
    {
        var result = new ValidationResult { IsValid = true };

        if (!SupportsFormat(format))
        {
            result.AddError($"Unsupported format: {format}");
            return Task.FromResult(result);
        }

        try
        {
            // Basic YAML syntax validation
            var yamlObject = _deserializer.Deserialize(configurationContent);

            if (yamlObject == null)
            {
                result.AddError("YAML configuration is empty or invalid");
                return Task.FromResult(result);
            }

            // Additional validation based on schema type
            switch (schemaType)
            {
                case ConfigurationSchemaType.Pipeline:
                    ValidatePipelineYamlSchema(yamlObject, result);
                    break;
                case ConfigurationSchemaType.Connector:
                    ValidateConnectorYamlSchema(yamlObject, result);
                    break;
            }
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            result.AddError($"Invalid YAML syntax: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<string> GetConfigurationSchemaAsync(ConfigurationSchemaType schemaType, string format)
    {
        if (!SupportsFormat(format))
        {
            throw new ConfigurationException($"Unsupported format: {format}");
        }

        // Return a basic YAML example based on the type
        var schema = schemaType switch
        {
            ConfigurationSchemaType.Pipeline => GetPipelineYamlSchema(),
            ConfigurationSchemaType.Connector => GetConnectorYamlSchema(),
            _ => ""
        };

        return Task.FromResult(schema);
    }

    /// <inheritdoc />
    public bool SupportsFormat(string format)
    {
        return SupportedFormats.Contains(format.ToLowerInvariant());
    }

    /// <inheritdoc />
    public string ResolveConfigurationValue(string configurationValue)
    {
        if (string.IsNullOrEmpty(configurationValue))
            return configurationValue;

        // Replace environment variables in the format ${ENV_VAR_NAME} or ${ENV_VAR_NAME:default_value}
        var envVarPattern = @"\$\{([^}:]+)(?::([^}]*))?\}";
        
        return Regex.Replace(configurationValue, envVarPattern, match =>
        {
            var envVarName = match.Groups[1].Value;
            var defaultValue = match.Groups[2].Value;
            
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            return envValue ?? defaultValue ?? match.Value;
        });
    }

    private string GetPipelineYamlSchema()
    {
        return """
        # ETL Pipeline Configuration Example
        name: "Sample Pipeline"
        description: "A sample ETL pipeline configuration"
        version: "1.0.0"
        author: "ETL Framework"
        isEnabled: true
        
        stages:
          - name: "Extract Data"
            description: "Extract data from source"
            stageType: "Extract"
            order: 1
            isEnabled: true
            
          - name: "Transform Data"
            description: "Transform extracted data"
            stageType: "Transform"
            order: 2
            isEnabled: true
            
          - name: "Load Data"
            description: "Load transformed data to destination"
            stageType: "Load"
            order: 3
            isEnabled: true
        
        errorHandling:
          stopOnError: false
          maxErrors: 100
        
        retry:
          maxAttempts: 3
          delay: "00:00:05"
        """;
    }

    private string GetConnectorYamlSchema()
    {
        return """
        # ETL Connector Configuration Example
        name: "Sample Connector"
        description: "A sample connector configuration"
        connectorType: "SqlServer"
        connectionString: "Server=${DB_SERVER:localhost};Database=${DB_NAME:SampleDB};Integrated Security=true"
        
        connectionTimeout: "00:00:30"
        commandTimeout: "00:05:00"
        maxRetryAttempts: 3
        retryDelay: "00:00:05"
        
        useConnectionPooling: true
        maxPoolSize: 100
        minPoolSize: 5
        batchSize: 1000
        
        enableDetailedLogging: false
        
        tags:
          - "production"
          - "sql-server"
        """;
    }

    /// <summary>
    /// Validates YAML pipeline schema.
    /// </summary>
    /// <param name="yamlObject">The parsed YAML object</param>
    /// <param name="result">The validation result to update</param>
    private void ValidatePipelineYamlSchema(object yamlObject, ValidationResult result)
    {
        if (yamlObject is not Dictionary<object, object> pipeline)
        {
            result.AddError("Pipeline configuration must be a YAML object");
            return;
        }

        // Validate required pipeline properties
        if (!pipeline.ContainsKey("name"))
        {
            result.AddError("Pipeline configuration must have a 'name' property");
        }

        if (pipeline.ContainsKey("stages"))
        {
            if (pipeline["stages"] is List<object> stages && stages.Count == 0)
            {
                result.AddWarning("Pipeline has no stages defined");
            }
        }
        else
        {
            result.AddWarning("Pipeline configuration should have a 'stages' property");
        }

        // Validate error handling if present
        if (pipeline.ContainsKey("errorHandling") && pipeline["errorHandling"] is Dictionary<object, object> errorHandling)
        {
            if (errorHandling.ContainsKey("maxErrors") && errorHandling["maxErrors"] is int maxErrors && maxErrors < 0)
            {
                result.AddError("maxErrors must be non-negative");
            }
        }

        // Validate schedule if present
        if (pipeline.ContainsKey("schedule") && pipeline["schedule"] is Dictionary<object, object> schedule)
        {
            if (schedule.ContainsKey("isEnabled") && schedule["isEnabled"] is bool isEnabled && isEnabled)
            {
                if (!schedule.ContainsKey("cronExpression") || schedule["cronExpression"] is not string cronExpr || string.IsNullOrWhiteSpace(cronExpr))
                {
                    result.AddError("cronExpression is required when schedule is enabled");
                }
            }
        }
    }

    /// <summary>
    /// Validates YAML connector schema.
    /// </summary>
    /// <param name="yamlObject">The parsed YAML object</param>
    /// <param name="result">The validation result to update</param>
    private void ValidateConnectorYamlSchema(object yamlObject, ValidationResult result)
    {
        if (yamlObject is not Dictionary<object, object> connector)
        {
            result.AddError("Connector configuration must be a YAML object");
            return;
        }

        // Validate required connector properties
        if (!connector.ContainsKey("name"))
        {
            result.AddError("Connector configuration must have a 'name' property");
        }

        if (!connector.ContainsKey("connectorType"))
        {
            result.AddError("Connector configuration must have a 'connectorType' property");
        }

        if (!connector.ContainsKey("connectionString"))
        {
            result.AddError("Connector configuration must have a 'connectionString' property");
        }

        // Validate timeout values if present
        if (connector.ContainsKey("connectionTimeout") && connector["connectionTimeout"] is string connTimeout)
        {
            if (!TimeSpan.TryParse(connTimeout, out var timeout) || timeout <= TimeSpan.Zero)
            {
                result.AddError("connectionTimeout must be a valid positive TimeSpan");
            }
        }

        if (connector.ContainsKey("commandTimeout") && connector["commandTimeout"] is string cmdTimeout)
        {
            if (!TimeSpan.TryParse(cmdTimeout, out var timeout) || timeout <= TimeSpan.Zero)
            {
                result.AddError("commandTimeout must be a valid positive TimeSpan");
            }
        }
    }
}
