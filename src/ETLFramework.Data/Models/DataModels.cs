using System.ComponentModel.DataAnnotations;

namespace ETLFramework.Data.Models;

/// <summary>
/// DTO for connector configuration.
/// </summary>
public class ConnectorConfigurationDto
{
    /// <summary>
    /// Gets or sets the connector type.
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connector configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// DTO for transformation configuration.
/// </summary>
public class TransformationConfigurationDto
{
    /// <summary>
    /// Gets or sets the transformation ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation type.
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the execution order.
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Paged result wrapper.
/// </summary>
/// <typeparam name="T">The item type</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}
