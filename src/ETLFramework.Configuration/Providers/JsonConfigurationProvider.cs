using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using ETLFramework.Configuration.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Configuration.Providers;

/// <summary>
/// JSON-based configuration provider for loading and saving ETL configurations.
/// </summary>
public class JsonConfigurationProvider : IConfigurationProvider
{
    private readonly ILogger<JsonConfigurationProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the JsonConfigurationProvider class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public JsonConfigurationProvider(ILogger<JsonConfigurationProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(),
                new TimeSpanJsonConverter(),
                new DateTimeOffsetJsonConverter()
            }
        };
    }

    /// <inheritdoc />
    public string Name => "JSON Configuration Provider";

    /// <inheritdoc />
    public IEnumerable<string> SupportedFormats => new[] { "json" };

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

            string jsonContent;
            if (File.Exists(source))
            {
                jsonContent = await File.ReadAllTextAsync(source, cancellationToken);
            }
            else
            {
                // Assume source is the JSON content itself
                jsonContent = source;
            }

            // Resolve environment variables
            jsonContent = ResolveConfigurationValue(jsonContent);

            var configuration = JsonSerializer.Deserialize<PipelineConfiguration>(jsonContent, _jsonOptions);
            if (configuration == null)
            {
                throw new ConfigurationException("Failed to deserialize pipeline configuration - result was null");
            }

            _logger.LogInformation("Successfully loaded pipeline configuration: {Name} (ID: {Id})", 
                configuration.Name, configuration.Id);

            return configuration;
        }
        catch (JsonException ex)
        {
            throw ConfigurationException.CreateParseFailure(
                $"Failed to parse JSON configuration: {ex.Message}",
                source,
                format,
                (int?)ex.LineNumber,
                (int?)ex.BytePositionInLine);
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

            var jsonContent = JsonSerializer.Serialize(configuration, _jsonOptions);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(destination, jsonContent, cancellationToken);

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

            string jsonContent;
            if (File.Exists(source))
            {
                jsonContent = await File.ReadAllTextAsync(source, cancellationToken);
            }
            else
            {
                jsonContent = source;
            }

            // Resolve environment variables
            jsonContent = ResolveConfigurationValue(jsonContent);

            var configuration = JsonSerializer.Deserialize<ConnectorConfiguration>(jsonContent, _jsonOptions);
            if (configuration == null)
            {
                throw new ConfigurationException("Failed to deserialize connector configuration - result was null");
            }

            _logger.LogInformation("Successfully loaded connector configuration: {Name} (Type: {Type})", 
                configuration.Name, configuration.ConnectorType);

            return configuration;
        }
        catch (JsonException ex)
        {
            throw ConfigurationException.CreateParseFailure(
                $"Failed to parse JSON configuration: {ex.Message}",
                source,
                format,
                (int?)ex.LineNumber,
                (int?)ex.BytePositionInLine);
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

            var jsonContent = JsonSerializer.Serialize(configuration, _jsonOptions);
            
            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(destination, jsonContent, cancellationToken);

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
            // Basic JSON syntax validation
            using var document = JsonDocument.Parse(configurationContent);
            
            // Additional validation based on schema type
            switch (schemaType)
            {
                case ConfigurationSchemaType.Pipeline:
                    ValidatePipelineSchema(document.RootElement, result);
                    break;
                case ConfigurationSchemaType.Connector:
                    ValidateConnectorSchema(document.RootElement, result);
                    break;
            }
        }
        catch (JsonException ex)
        {
            result.AddError($"Invalid JSON syntax: {ex.Message}");
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

        // Return a basic JSON schema based on the type
        var schema = schemaType switch
        {
            ConfigurationSchemaType.Pipeline => GetPipelineJsonSchema(),
            ConfigurationSchemaType.Connector => GetConnectorJsonSchema(),
            _ => "{}"
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

    private void ValidatePipelineSchema(JsonElement root, ValidationResult result)
    {
        // Validate required pipeline properties
        if (!root.TryGetProperty("name", out _))
        {
            result.AddError("Pipeline configuration must have a 'name' property");
        }

        if (!root.TryGetProperty("stages", out var stagesElement))
        {
            result.AddWarning("Pipeline configuration should have a 'stages' property");
        }
        else if (stagesElement.ValueKind == JsonValueKind.Array && stagesElement.GetArrayLength() == 0)
        {
            result.AddWarning("Pipeline has no stages defined");
        }

        // Validate error handling if present
        if (root.TryGetProperty("errorHandling", out var errorHandlingElement))
        {
            if (errorHandlingElement.TryGetProperty("maxErrors", out var maxErrorsElement))
            {
                if (maxErrorsElement.ValueKind == JsonValueKind.Number && maxErrorsElement.GetInt32() < 0)
                {
                    result.AddError("maxErrors must be non-negative");
                }
            }
        }

        // Validate schedule if present
        if (root.TryGetProperty("schedule", out var scheduleElement))
        {
            if (scheduleElement.TryGetProperty("isEnabled", out var isEnabledElement) &&
                isEnabledElement.ValueKind == JsonValueKind.True)
            {
                if (!scheduleElement.TryGetProperty("cronExpression", out var cronElement) ||
                    cronElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(cronElement.GetString()))
                {
                    result.AddError("cronExpression is required when schedule is enabled");
                }
            }
        }

        // Validate timeout if present
        if (root.TryGetProperty("timeout", out var timeoutElement))
        {
            if (timeoutElement.ValueKind == JsonValueKind.String)
            {
                var timeoutStr = timeoutElement.GetString();
                if (!TimeSpan.TryParse(timeoutStr, out var timeout) || timeout <= TimeSpan.Zero)
                {
                    result.AddError("timeout must be a valid positive TimeSpan");
                }
            }
        }

        // Validate maxDegreeOfParallelism if present
        if (root.TryGetProperty("maxDegreeOfParallelism", out var parallelismElement))
        {
            if (parallelismElement.ValueKind == JsonValueKind.Number && parallelismElement.GetInt32() <= 0)
            {
                result.AddError("maxDegreeOfParallelism must be greater than zero");
            }
        }
    }

    private void ValidateConnectorSchema(JsonElement root, ValidationResult result)
    {
        // Validate required connector properties
        if (!root.TryGetProperty("name", out _))
        {
            result.AddError("Connector configuration must have a 'name' property");
        }

        if (!root.TryGetProperty("connectorType", out _))
        {
            result.AddError("Connector configuration must have a 'connectorType' property");
        }

        if (!root.TryGetProperty("connectionString", out _))
        {
            result.AddError("Connector configuration must have a 'connectionString' property");
        }

        // Validate timeout values if present
        if (root.TryGetProperty("connectionTimeout", out var connTimeoutElement))
        {
            if (connTimeoutElement.ValueKind == JsonValueKind.String)
            {
                var timeoutStr = connTimeoutElement.GetString();
                if (!TimeSpan.TryParse(timeoutStr, out var timeout) || timeout <= TimeSpan.Zero)
                {
                    result.AddError("connectionTimeout must be a valid positive TimeSpan");
                }
            }
        }

        if (root.TryGetProperty("commandTimeout", out var cmdTimeoutElement))
        {
            if (cmdTimeoutElement.ValueKind == JsonValueKind.String)
            {
                var timeoutStr = cmdTimeoutElement.GetString();
                if (!TimeSpan.TryParse(timeoutStr, out var timeout) || timeout <= TimeSpan.Zero)
                {
                    result.AddError("commandTimeout must be a valid positive TimeSpan");
                }
            }
        }

        // Validate numeric properties
        if (root.TryGetProperty("maxRetryAttempts", out var retryElement))
        {
            if (retryElement.ValueKind == JsonValueKind.Number && retryElement.GetInt32() < 0)
            {
                result.AddError("maxRetryAttempts must be non-negative");
            }
        }

        if (root.TryGetProperty("batchSize", out var batchElement))
        {
            if (batchElement.ValueKind == JsonValueKind.Number && batchElement.GetInt32() <= 0)
            {
                result.AddError("batchSize must be greater than zero");
            }
        }
    }

    private string GetPipelineJsonSchema()
    {
        return """
        {
          "$schema": "http://json-schema.org/draft-07/schema#",
          "type": "object",
          "required": ["name"],
          "properties": {
            "id": { "type": "string", "format": "uuid" },
            "name": { "type": "string" },
            "description": { "type": "string" },
            "version": { "type": "string" },
            "stages": {
              "type": "array",
              "items": { "$ref": "#/definitions/stage" }
            }
          }
        }
        """;
    }

    private string GetConnectorJsonSchema()
    {
        return """
        {
          "$schema": "http://json-schema.org/draft-07/schema#",
          "type": "object",
          "required": ["name", "connectorType", "connectionString"],
          "properties": {
            "id": { "type": "string", "format": "uuid" },
            "name": { "type": "string" },
            "connectorType": { "type": "string" },
            "connectionString": { "type": "string" }
          }
        }
        """;
    }
}

/// <summary>
/// Custom JSON converter for TimeSpan.
/// </summary>
public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return TimeSpan.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// Custom JSON converter for DateTimeOffset.
/// </summary>
public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTimeOffset.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("O"));
    }
}
