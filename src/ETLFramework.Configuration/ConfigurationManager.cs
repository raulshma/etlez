using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Configuration;

/// <summary>
/// Central configuration manager that coordinates multiple configuration providers.
/// </summary>
public class ConfigurationManager
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly Dictionary<string, IConfigurationProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the ConfigurationManager class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public ConfigurationManager(ILogger<ConfigurationManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = new Dictionary<string, IConfigurationProvider>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationManager class with providers.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="providers">The configuration providers to register</param>
    public ConfigurationManager(ILogger<ConfigurationManager> logger, IEnumerable<IConfigurationProvider> providers)
        : this(logger)
    {
        foreach (var provider in providers)
        {
            RegisterProvider(provider);
        }
    }

    /// <summary>
    /// Gets all registered configuration providers.
    /// </summary>
    public IEnumerable<IConfigurationProvider> Providers => _providers.Values;

    /// <summary>
    /// Gets all supported configuration formats across all providers.
    /// </summary>
    public IEnumerable<string> SupportedFormats => _providers.Values.SelectMany(p => p.SupportedFormats).Distinct();

    /// <summary>
    /// Registers a configuration provider.
    /// </summary>
    /// <param name="provider">The configuration provider to register</param>
    public void RegisterProvider(IConfigurationProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        foreach (var format in provider.SupportedFormats)
        {
            if (_providers.ContainsKey(format))
            {
                _logger.LogWarning("Overriding existing provider for format {Format} with {ProviderName}", 
                    format, provider.Name);
            }

            _providers[format] = provider;
            _logger.LogInformation("Registered configuration provider {ProviderName} for format {Format}", 
                provider.Name, format);
        }
    }

    /// <summary>
    /// Unregisters a configuration provider for specific formats.
    /// </summary>
    /// <param name="formats">The formats to unregister</param>
    public void UnregisterProvider(params string[] formats)
    {
        foreach (var format in formats)
        {
            if (_providers.Remove(format))
            {
                _logger.LogInformation("Unregistered configuration provider for format {Format}", format);
            }
        }
    }

    /// <summary>
    /// Gets the configuration provider for a specific format.
    /// </summary>
    /// <param name="format">The configuration format</param>
    /// <returns>The configuration provider</returns>
    /// <exception cref="ConfigurationException">Thrown when no provider is found for the format</exception>
    public IConfigurationProvider GetProvider(string format)
    {
        if (_providers.TryGetValue(format, out var provider))
        {
            return provider;
        }

        throw new ConfigurationException($"No configuration provider found for format: {format}. Supported formats: {string.Join(", ", SupportedFormats)}")
        {
            ErrorCode = "UNSUPPORTED_FORMAT"
        };
    }

    /// <summary>
    /// Loads a pipeline configuration from the specified source.
    /// </summary>
    /// <param name="source">The configuration source</param>
    /// <param name="format">The configuration format (optional, will be auto-detected if not provided)</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The loaded pipeline configuration</returns>
    public async Task<IPipelineConfiguration> LoadPipelineConfigurationAsync(string source, string? format = null, CancellationToken cancellationToken = default)
    {
        format ??= DetectFormat(source);
        
        _logger.LogInformation("Loading pipeline configuration from {Source} using format {Format}", source, format);

        var provider = GetProvider(format);
        var configuration = await provider.LoadPipelineConfigurationAsync(source, format, cancellationToken);

        // Validate the loaded configuration
        var validationResult = configuration.Validate();
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.Message));
            throw ConfigurationException.CreateValidationFailure(
                $"Pipeline configuration validation failed: {errorMessages}", 
                source, 
                "Pipeline");
        }

        if (validationResult.Warnings.Count > 0)
        {
            foreach (var warning in validationResult.Warnings)
            {
                _logger.LogWarning("Configuration warning: {Warning}", warning.Message);
            }
        }

        return configuration;
    }

    /// <summary>
    /// Saves a pipeline configuration to the specified destination.
    /// </summary>
    /// <param name="configuration">The pipeline configuration to save</param>
    /// <param name="destination">The destination path</param>
    /// <param name="format">The configuration format (optional, will be auto-detected if not provided)</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public async Task SavePipelineConfigurationAsync(IPipelineConfiguration configuration, string destination, string? format = null, CancellationToken cancellationToken = default)
    {
        format ??= DetectFormat(destination);

        _logger.LogInformation("Saving pipeline configuration {Name} to {Destination} using format {Format}", 
            configuration.Name, destination, format);

        // Validate before saving
        var validationResult = configuration.Validate();
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.Message));
            throw new ConfigurationException($"Cannot save invalid configuration: {errorMessages}")
            {
                ErrorCode = "INVALID_CONFIGURATION"
            };
        }

        var provider = GetProvider(format);
        await provider.SavePipelineConfigurationAsync(configuration, destination, format, cancellationToken);
    }

    /// <summary>
    /// Loads a connector configuration from the specified source.
    /// </summary>
    /// <param name="source">The configuration source</param>
    /// <param name="format">The configuration format (optional, will be auto-detected if not provided)</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The loaded connector configuration</returns>
    public async Task<IConnectorConfiguration> LoadConnectorConfigurationAsync(string source, string? format = null, CancellationToken cancellationToken = default)
    {
        format ??= DetectFormat(source);

        _logger.LogInformation("Loading connector configuration from {Source} using format {Format}", source, format);

        var provider = GetProvider(format);
        var configuration = await provider.LoadConnectorConfigurationAsync(source, format, cancellationToken);

        // Validate the loaded configuration
        var validationResult = configuration.Validate();
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.Message));
            throw ConfigurationException.CreateValidationFailure(
                $"Connector configuration validation failed: {errorMessages}", 
                source, 
                "Connector");
        }

        return configuration;
    }

    /// <summary>
    /// Saves a connector configuration to the specified destination.
    /// </summary>
    /// <param name="configuration">The connector configuration to save</param>
    /// <param name="destination">The destination path</param>
    /// <param name="format">The configuration format (optional, will be auto-detected if not provided)</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public async Task SaveConnectorConfigurationAsync(IConnectorConfiguration configuration, string destination, string? format = null, CancellationToken cancellationToken = default)
    {
        format ??= DetectFormat(destination);

        _logger.LogInformation("Saving connector configuration {Name} to {Destination} using format {Format}", 
            configuration.Name, destination, format);

        // Validate before saving
        var validationResult = configuration.Validate();
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.Message));
            throw new ConfigurationException($"Cannot save invalid configuration: {errorMessages}")
            {
                ErrorCode = "INVALID_CONFIGURATION"
            };
        }

        var provider = GetProvider(format);
        await provider.SaveConnectorConfigurationAsync(configuration, destination, format, cancellationToken);
    }

    /// <summary>
    /// Validates configuration content against a schema.
    /// </summary>
    /// <param name="configurationContent">The configuration content to validate</param>
    /// <param name="format">The configuration format</param>
    /// <param name="schemaType">The type of schema to validate against</param>
    /// <returns>Validation result</returns>
    public async Task<ValidationResult> ValidateConfigurationAsync(string configurationContent, string format, ConfigurationSchemaType schemaType)
    {
        var provider = GetProvider(format);
        return await provider.ValidateConfigurationAsync(configurationContent, format, schemaType);
    }

    /// <summary>
    /// Gets the configuration schema for a specific type and format.
    /// </summary>
    /// <param name="schemaType">The type of schema to retrieve</param>
    /// <param name="format">The format of the schema</param>
    /// <returns>The configuration schema</returns>
    public async Task<string> GetConfigurationSchemaAsync(ConfigurationSchemaType schemaType, string format)
    {
        var provider = GetProvider(format);
        return await provider.GetConfigurationSchemaAsync(schemaType, format);
    }

    /// <summary>
    /// Checks if a format is supported by any registered provider.
    /// </summary>
    /// <param name="format">The format to check</param>
    /// <returns>True if the format is supported, false otherwise</returns>
    public bool IsFormatSupported(string format)
    {
        return _providers.ContainsKey(format);
    }

    /// <summary>
    /// Detects the configuration format from a file path or content.
    /// </summary>
    /// <param name="source">The source path or content</param>
    /// <returns>The detected format</returns>
    private string DetectFormat(string source)
    {
        // Try to detect from file extension first
        if (File.Exists(source) || source.Contains('.'))
        {
            var extension = Path.GetExtension(source).TrimStart('.').ToLowerInvariant();
            if (!string.IsNullOrEmpty(extension) && IsFormatSupported(extension))
            {
                return extension;
            }
        }

        // Try to detect from content
        var trimmedSource = source.Trim();
        if (trimmedSource.StartsWith("{") || trimmedSource.StartsWith("["))
        {
            return "json";
        }

        if (trimmedSource.Contains(":") && !trimmedSource.StartsWith("<"))
        {
            return "yaml";
        }

        if (trimmedSource.StartsWith("<"))
        {
            return "xml";
        }

        // Default to JSON if unable to detect
        return "json";
    }
}
