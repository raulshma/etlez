namespace ETLFramework.Core.Attributes;

/// <summary>
/// Attribute to mark an assembly as an ETL Framework plugin.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class ETLPluginAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the minimum framework version required by this plugin.
    /// </summary>
    public string? MinimumFrameworkVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum framework version supported by this plugin.
    /// </summary>
    public string? MaximumFrameworkVersion { get; set; }

    /// <summary>
    /// Gets or sets the dependencies required by this plugin.
    /// </summary>
    public string[]? Dependencies { get; set; }

    /// <summary>
    /// Gets or sets the tags for categorizing this plugin.
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Gets or sets the plugin category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the plugin license.
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// Gets or sets the plugin website URL.
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Gets or sets whether this plugin is experimental.
    /// </summary>
    public bool IsExperimental { get; set; } = false;
}
