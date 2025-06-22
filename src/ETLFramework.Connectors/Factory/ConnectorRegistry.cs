using System.Collections.Concurrent;
using System.Reflection;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Connectors.Factory;

/// <summary>
/// Registry for managing connector descriptors and metadata.
/// </summary>
public class ConnectorRegistry
{
    private readonly ConcurrentDictionary<string, ConnectorDescriptor> _descriptors;
    private readonly ILogger<ConnectorRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the ConnectorRegistry class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public ConnectorRegistry(ILogger<ConnectorRegistry> logger)
    {
        _descriptors = new ConcurrentDictionary<string, ConnectorDescriptor>(StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    /// <summary>
    /// Gets all registered connector descriptors.
    /// </summary>
    public IEnumerable<ConnectorDescriptor> AllDescriptors => _descriptors.Values;

    /// <summary>
    /// Gets the count of registered connectors.
    /// </summary>
    public int Count => _descriptors.Count;

    /// <summary>
    /// Registers a connector descriptor.
    /// </summary>
    /// <param name="descriptor">The connector descriptor</param>
    /// <returns>True if registered successfully, false if already exists</returns>
    public bool Register(ConnectorDescriptor descriptor)
    {
        if (string.IsNullOrEmpty(descriptor.ConnectorType))
        {
            throw new ArgumentException("Connector type cannot be null or empty", nameof(descriptor));
        }

        var added = _descriptors.TryAdd(descriptor.ConnectorType, descriptor);
        if (added)
        {
            _logger.LogDebug("Registered connector descriptor: {ConnectorType} - {DisplayName}", 
                descriptor.ConnectorType, descriptor.DisplayName);
        }
        else
        {
            _logger.LogWarning("Connector descriptor already exists: {ConnectorType}", descriptor.ConnectorType);
        }

        return added;
    }

    /// <summary>
    /// Unregisters a connector descriptor.
    /// </summary>
    /// <param name="connectorType">The connector type</param>
    /// <returns>True if unregistered successfully</returns>
    public bool Unregister(string connectorType)
    {
        var removed = _descriptors.TryRemove(connectorType, out var descriptor);
        if (removed)
        {
            _logger.LogDebug("Unregistered connector descriptor: {ConnectorType}", connectorType);
        }

        return removed;
    }

    /// <summary>
    /// Gets a connector descriptor by type.
    /// </summary>
    /// <param name="connectorType">The connector type</param>
    /// <returns>The connector descriptor or null if not found</returns>
    public ConnectorDescriptor? GetDescriptor(string connectorType)
    {
        _descriptors.TryGetValue(connectorType, out var descriptor);
        return descriptor;
    }

    /// <summary>
    /// Checks if a connector type is registered.
    /// </summary>
    /// <param name="connectorType">The connector type</param>
    /// <returns>True if registered</returns>
    public bool IsRegistered(string connectorType)
    {
        return _descriptors.ContainsKey(connectorType);
    }

    /// <summary>
    /// Gets connectors by category.
    /// </summary>
    /// <param name="category">The category</param>
    /// <returns>Connectors in the specified category</returns>
    public IEnumerable<ConnectorDescriptor> GetByCategory(string category)
    {
        return _descriptors.Values.Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets connectors that support a specific operation.
    /// </summary>
    /// <param name="operation">The operation</param>
    /// <returns>Connectors that support the operation</returns>
    public IEnumerable<ConnectorDescriptor> GetByOperation(ConnectorOperation operation)
    {
        return _descriptors.Values.Where(d => d.SupportsOperation(operation));
    }

    /// <summary>
    /// Gets connectors that support a specific format.
    /// </summary>
    /// <param name="format">The format</param>
    /// <returns>Connectors that support the format</returns>
    public IEnumerable<ConnectorDescriptor> GetByFormat(string format)
    {
        return _descriptors.Values.Where(d => d.SupportsFormat(format));
    }

    /// <summary>
    /// Gets connectors by tag.
    /// </summary>
    /// <param name="tag">The tag</param>
    /// <returns>Connectors with the specified tag</returns>
    public IEnumerable<ConnectorDescriptor> GetByTag(string tag)
    {
        return _descriptors.Values.Where(d => d.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Searches connectors by name or description.
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <returns>Matching connectors</returns>
    public IEnumerable<ConnectorDescriptor> Search(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return AllDescriptors;
        }

        var term = searchTerm.ToLowerInvariant();
        return _descriptors.Values.Where(d =>
            d.ConnectorType.ToLowerInvariant().Contains(term) ||
            d.DisplayName.ToLowerInvariant().Contains(term) ||
            d.Description.ToLowerInvariant().Contains(term) ||
            d.Tags.Any(t => t.ToLowerInvariant().Contains(term)));
    }

    /// <summary>
    /// Gets available (non-deprecated) connectors.
    /// </summary>
    /// <returns>Available connectors</returns>
    public IEnumerable<ConnectorDescriptor> GetAvailable()
    {
        return _descriptors.Values.Where(d => d.IsAvailable && !d.IsDeprecated);
    }

    /// <summary>
    /// Gets deprecated connectors.
    /// </summary>
    /// <returns>Deprecated connectors</returns>
    public IEnumerable<ConnectorDescriptor> GetDeprecated()
    {
        return _descriptors.Values.Where(d => d.IsDeprecated);
    }

    /// <summary>
    /// Validates a configuration against a connector descriptor.
    /// </summary>
    /// <param name="connectorType">The connector type</param>
    /// <param name="configuration">The configuration to validate</param>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateConfiguration(string connectorType, Dictionary<string, object> configuration)
    {
        var descriptor = GetDescriptor(connectorType);
        if (descriptor == null)
        {
            var result = new ValidationResult();
            result.AddError($"Connector type '{connectorType}' is not registered", "connectorType");
            return result;
        }

        return descriptor.ValidateConfiguration(configuration);
    }

    /// <summary>
    /// Creates a configuration template for a connector.
    /// </summary>
    /// <param name="connectorType">The connector type</param>
    /// <param name="exampleName">Optional example name</param>
    /// <returns>A configuration template or null if connector not found</returns>
    public Dictionary<string, object>? CreateTemplate(string connectorType, string? exampleName = null)
    {
        var descriptor = GetDescriptor(connectorType);
        if (descriptor == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(exampleName))
        {
            var example = descriptor.GetExample(exampleName);
            if (example != null)
            {
                return new Dictionary<string, object>(example.Configuration);
            }
        }

        return descriptor.CreateBasicTemplate();
    }

    /// <summary>
    /// Discovers and registers connectors from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan</param>
    /// <returns>The number of connectors discovered</returns>
    public int DiscoverFromAssembly(Assembly assembly)
    {
        var discovered = 0;

        try
        {
            var connectorTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && IsConnectorType(t));

            foreach (var type in connectorTypes)
            {
                try
                {
                    var descriptor = CreateDescriptorFromType(type);
                    if (descriptor != null && Register(descriptor))
                    {
                        discovered++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create descriptor for type: {TypeName}", type.FullName);
                }
            }

            _logger.LogInformation("Discovered {Count} connectors from assembly: {AssemblyName}", 
                discovered, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering connectors from assembly: {AssemblyName}", 
                assembly.GetName().Name);
        }

        return discovered;
    }

    /// <summary>
    /// Clears all registered descriptors.
    /// </summary>
    public void Clear()
    {
        var count = _descriptors.Count;
        _descriptors.Clear();
        _logger.LogDebug("Cleared {Count} connector descriptors", count);
    }

    /// <summary>
    /// Gets registry statistics.
    /// </summary>
    /// <returns>Registry statistics</returns>
    public ConnectorRegistryStats GetStats()
    {
        var descriptors = _descriptors.Values.ToList();
        
        return new ConnectorRegistryStats
        {
            TotalConnectors = descriptors.Count,
            AvailableConnectors = descriptors.Count(d => d.IsAvailable && !d.IsDeprecated),
            DeprecatedConnectors = descriptors.Count(d => d.IsDeprecated),
            CategoriesCount = descriptors.Select(d => d.Category).Distinct().Count(),
            Categories = descriptors.GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// Checks if a type is a connector type.
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if it's a connector type</returns>
    private static bool IsConnectorType(Type type)
    {
        return type.GetInterfaces().Any(i => 
            i.IsGenericType && 
            (i.GetGenericTypeDefinition() == typeof(Core.Interfaces.ISourceConnector<>) ||
             i.GetGenericTypeDefinition() == typeof(Core.Interfaces.IDestinationConnector<>)));
    }

    /// <summary>
    /// Creates a descriptor from a connector type.
    /// </summary>
    /// <param name="type">The connector type</param>
    /// <returns>A connector descriptor</returns>
    private ConnectorDescriptor? CreateDescriptorFromType(Type type)
    {
        // This is a basic implementation - in a real system, you might use attributes
        // or other metadata to populate the descriptor
        var descriptor = new ConnectorDescriptor
        {
            ConnectorType = type.Name.Replace("Connector", ""),
            DisplayName = type.Name,
            Description = $"Auto-discovered connector: {type.Name}",
            ImplementationType = type,
            AssemblyName = type.Assembly.GetName().Name,
            Category = DetermineCategory(type)
        };

        // Determine supported operations based on interfaces
        if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Core.Interfaces.ISourceConnector<>)))
        {
            descriptor.SupportedOperations.Add(ConnectorOperation.Read);
            descriptor.SupportedOperations.Add(ConnectorOperation.TestConnection);
            descriptor.SupportedOperations.Add(ConnectorOperation.DiscoverSchema);
        }

        if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Core.Interfaces.IDestinationConnector<>)))
        {
            descriptor.SupportedOperations.Add(ConnectorOperation.Write);
        }

        return descriptor;
    }

    /// <summary>
    /// Determines the category of a connector type.
    /// </summary>
    /// <param name="type">The connector type</param>
    /// <returns>The category</returns>
    private static string DetermineCategory(Type type)
    {
        var namespaceParts = type.Namespace?.Split('.') ?? Array.Empty<string>();
        
        if (namespaceParts.Contains("FileSystem"))
            return "File System";
        if (namespaceParts.Contains("Database"))
            return "Database";
        if (namespaceParts.Contains("CloudStorage"))
            return "Cloud Storage";
        
        return "Other";
    }
}

/// <summary>
/// Represents connector registry statistics.
/// </summary>
public class ConnectorRegistryStats
{
    /// <summary>
    /// Gets or sets the total number of connectors.
    /// </summary>
    public int TotalConnectors { get; set; }

    /// <summary>
    /// Gets or sets the number of available connectors.
    /// </summary>
    public int AvailableConnectors { get; set; }

    /// <summary>
    /// Gets or sets the number of deprecated connectors.
    /// </summary>
    public int DeprecatedConnectors { get; set; }

    /// <summary>
    /// Gets or sets the number of categories.
    /// </summary>
    public int CategoriesCount { get; set; }

    /// <summary>
    /// Gets or sets the categories and their connector counts.
    /// </summary>
    public Dictionary<string, int> Categories { get; set; } = new Dictionary<string, int>();
}
