using ETLFramework.Core.Attributes;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Loader;

namespace ETLFramework.Core.Implementations;

/// <summary>
/// Implementation of the plugin manager for loading and managing ETL Framework plugins.
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceCollection _services;
    private readonly List<IETLPlugin> _loadedPlugins = new();
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
    private readonly Dictionary<string, PluginLoadContext> _loadContexts = new();

    /// <summary>
    /// Initializes a new instance of the PluginManager class.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="services">The service collection</param>
    public PluginManager(ILogger<PluginManager> logger, IServiceCollection services)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc />
    public async Task<int> DiscoverAndLoadPluginsAsync(string pluginDirectory)
    {
        var pluginCount = 0;

        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogWarning("Plugin directory does not exist: {PluginDirectory}", pluginDirectory);
            return 0;
        }

        var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);

        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                var plugin = await LoadPluginAsync(pluginFile);
                if (plugin != null)
                {
                    pluginCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from file: {PluginFile}", pluginFile);
            }
        }

        _logger.LogInformation("Loaded {PluginCount} plugins from {PluginDirectory}", pluginCount, pluginDirectory);
        return pluginCount;
    }

    /// <inheritdoc />
    public async Task<IETLPlugin> LoadPluginAsync(string assemblyPath)
    {
        try
        {
            _logger.LogInformation("Loading plugin from: {AssemblyPath}", assemblyPath);

            // Validate plugin first
            var validationResult = await ValidatePluginAsync(assemblyPath);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Plugin validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            // Load assembly in isolated context
            var loadContext = new PluginLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            _loadedAssemblies[assemblyPath] = assembly;
            _loadContexts[assemblyPath] = loadContext;

            // Discover plugins in assembly
            var plugins = await DiscoverPluginsInAssemblyAsync(assembly);
            var plugin = plugins.FirstOrDefault();

            if (plugin != null)
            {
                await LoadPluginAsync(plugin);
                return plugin;
            }

            throw new InvalidOperationException($"No plugins found in assembly: {assemblyPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from: {AssemblyPath}", assemblyPath);
            throw;
        }
    }

    /// <inheritdoc />
    public IEnumerable<IETLPlugin> GetLoadedPlugins()
    {
        return _loadedPlugins.AsReadOnly();
    }

    /// <inheritdoc />
    public IETLPlugin? GetPlugin(string name)
    {
        return _loadedPlugins.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public Task<bool> UnloadPluginAsync(string name)
    {
        try
        {
            var plugin = GetPlugin(name);
            if (plugin == null)
            {
                return Task.FromResult(false);
            }

            _loadedPlugins.Remove(plugin);

            // Find and unload the assembly context
            var assemblyPath = _loadedAssemblies.FirstOrDefault(kv =>
                kv.Value.GetTypes().Any(t => typeof(IETLPlugin).IsAssignableFrom(t))).Key;

            if (!string.IsNullOrEmpty(assemblyPath) && _loadContexts.TryGetValue(assemblyPath, out var context))
            {
                context.Unload();
                _loadContexts.Remove(assemblyPath);
                _loadedAssemblies.Remove(assemblyPath);
            }

            _logger.LogInformation("Unloaded plugin: {PluginName}", name);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin: {PluginName}", name);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public async Task<PluginValidationResult> ValidatePluginAsync(string assemblyPath)
    {
        var result = new PluginValidationResult();

        try
        {
            if (!File.Exists(assemblyPath))
            {
                result.AddError($"Assembly file not found: {assemblyPath}");
                return result;
            }

            // Load assembly for validation (without executing)
            var assemblyBytes = await File.ReadAllBytesAsync(assemblyPath);
            var assembly = Assembly.Load(assemblyBytes);

            // Check for ETLPlugin attribute
            var pluginAttribute = assembly.GetCustomAttribute<ETLPluginAttribute>();
            if (pluginAttribute == null)
            {
                result.AddError("Assembly does not have ETLPlugin attribute");
                return result;
            }

            // Validate framework version compatibility
            if (!string.IsNullOrEmpty(pluginAttribute.MinimumFrameworkVersion))
            {
                if (Version.TryParse(pluginAttribute.MinimumFrameworkVersion, out var minVersion))
                {
                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    if (currentVersion < minVersion)
                    {
                        result.AddError($"Plugin requires minimum framework version {minVersion}, current version is {currentVersion}");
                    }
                    result.MinimumFrameworkVersion = minVersion;
                }
                else
                {
                    result.AddWarning($"Invalid minimum framework version format: {pluginAttribute.MinimumFrameworkVersion}");
                }
            }

            // Check for plugin implementations
            var pluginTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IETLPlugin).IsAssignableFrom(t))
                .ToList();

            if (!pluginTypes.Any())
            {
                result.AddError("No plugin implementations found in assembly");
                return result;
            }

            // Validate plugin dependencies
            if (pluginAttribute.Dependencies?.Any() == true)
            {
                result.Dependencies.AddRange(pluginAttribute.Dependencies);
                // TODO: Check if dependencies are satisfied
            }

            // Store metadata
            result.Metadata["PluginTypes"] = pluginTypes.Select(t => t.FullName).ToList();
            result.Metadata["Tags"] = pluginAttribute.Tags ?? Array.Empty<string>();
            result.Metadata["Category"] = pluginAttribute.Category ?? "General";

            result.IsValid = true;
        }
        catch (Exception ex)
        {
            result.AddError($"Error validating plugin: {ex.Message}");
        }

        return result;
    }

    private Task<IEnumerable<IETLPlugin>> DiscoverPluginsInAssemblyAsync(Assembly assembly)
    {
        var plugins = new List<IETLPlugin>();

        try
        {
            var pluginTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IETLPlugin).IsAssignableFrom(t));

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = Activator.CreateInstance(pluginType) as IETLPlugin;
                    if (plugin != null)
                    {
                        plugins.Add(plugin);
                        _logger.LogDebug("Discovered plugin: {PluginName} v{Version}", plugin.Name, plugin.Version);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create instance of plugin type: {PluginType}", pluginType.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering plugins in assembly: {AssemblyName}", assembly.FullName);
        }

        return Task.FromResult<IEnumerable<IETLPlugin>>(plugins);
    }

    private Task LoadPluginAsync(IETLPlugin plugin)
    {
        try
        {
            _logger.LogInformation("Loading plugin: {PluginName} v{Version}", plugin.Name, plugin.Version);

            // Configure services
            plugin.ConfigureServices(_services);

            _loadedPlugins.Add(plugin);

            _logger.LogInformation("Successfully loaded plugin: {PluginName}", plugin.Name);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin: {PluginName}", plugin.Name);
            throw;
        }
    }
}

/// <summary>
/// Plugin load context for assembly isolation.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the PluginLoadContext class.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin assembly</param>
    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    /// <inheritdoc />
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}
