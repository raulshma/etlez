namespace ETLFramework.Core.Models;

/// <summary>
/// Result of plugin validation.
/// </summary>
public class PluginValidationResult
{
    /// <summary>
    /// Gets or sets whether the plugin is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the plugin metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the plugin dependencies.
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// Gets or sets whether all dependencies are satisfied.
    /// </summary>
    public bool DependenciesSatisfied { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum framework version required.
    /// </summary>
    public Version? MinimumFrameworkVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum framework version supported.
    /// </summary>
    public Version? MaximumFrameworkVersion { get; set; }

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    /// <param name="error">The error message</param>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// Adds a warning to the validation result.
    /// </summary>
    /// <param name="warning">The warning message</param>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}
