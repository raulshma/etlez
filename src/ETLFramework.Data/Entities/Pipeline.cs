using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ETLFramework.Data.Models;

namespace ETLFramework.Data.Entities;

/// <summary>
/// Database entity representing a pipeline configuration.
/// </summary>
[Table("Pipelines")]
public class Pipeline
{
    /// <summary>
    /// Gets or sets the pipeline ID.
    /// </summary>
    [Key]
    [StringLength(50)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline description.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the source connector configuration as JSON.
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")]
    public string SourceConnectorJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target connector configuration as JSON.
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")]
    public string TargetConnectorJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation configurations as JSON.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string TransformationsJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the pipeline configuration as JSON.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string ConfigurationJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets whether the pipeline is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets when the pipeline was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the pipeline was last modified.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets when the pipeline was last executed.
    /// </summary>
    public DateTimeOffset? LastExecutedAt { get; set; }

    /// <summary>
    /// Navigation property for pipeline executions.
    /// </summary>
    public virtual ICollection<Execution> Executions { get; set; } = new List<Execution>();

    // Non-mapped properties for working with deserialized objects
    /// <summary>
    /// Gets or sets the source connector configuration (not mapped to database).
    /// </summary>
    [NotMapped]
    public ConnectorConfigurationDto? SourceConnector { get; set; }

    /// <summary>
    /// Gets or sets the target connector configuration (not mapped to database).
    /// </summary>
    [NotMapped]
    public ConnectorConfigurationDto? TargetConnector { get; set; }

    /// <summary>
    /// Gets or sets the transformation configurations (not mapped to database).
    /// </summary>
    [NotMapped]
    public List<TransformationConfigurationDto>? Transformations { get; set; }

    /// <summary>
    /// Gets or sets the pipeline configuration (not mapped to database).
    /// </summary>
    [NotMapped]
    public Dictionary<string, object>? Configuration { get; set; }
}
