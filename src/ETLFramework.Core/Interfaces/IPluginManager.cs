using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Interface for managing ETL Framework plugins.
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Discovers and loads plugins from the specified directory.
    /// </summary>
    /// <param name="pluginDirectory">The directory to search for plugins</param>
    /// <returns>The number of plugins loaded</returns>
    Task<int> DiscoverAndLoadPluginsAsync(string pluginDirectory);

    /// <summary>
    /// Loads a specific plugin from an assembly file.
    /// </summary>
    /// <param name="assemblyPath">The path to the plugin assembly</param>
    /// <returns>The loaded plugin</returns>
    Task<IETLPlugin> LoadPluginAsync(string assemblyPath);

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    /// <returns>Collection of loaded plugins</returns>
    IEnumerable<IETLPlugin> GetLoadedPlugins();

    /// <summary>
    /// Gets a plugin by name.
    /// </summary>
    /// <param name="name">The plugin name</param>
    /// <returns>The plugin if found, null otherwise</returns>
    IETLPlugin? GetPlugin(string name);

    /// <summary>
    /// Unloads a plugin.
    /// </summary>
    /// <param name="name">The plugin name</param>
    /// <returns>True if unloaded successfully, false otherwise</returns>
    Task<bool> UnloadPluginAsync(string name);

    /// <summary>
    /// Validates a plugin before loading.
    /// </summary>
    /// <param name="assemblyPath">The path to the plugin assembly</param>
    /// <returns>Validation result</returns>
    Task<PluginValidationResult> ValidatePluginAsync(string assemblyPath);
}
