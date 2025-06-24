using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ETLFramework.Data.Entities;

/// <summary>
/// Database entity representing a pipeline execution.
/// </summary>
[Table("Executions")]
public class Execution
{
    /// <summary>
    /// Gets or sets the database ID (primary key).
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the execution ID (unique identifier for the execution).
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline ID (foreign key).
    /// </summary>
    [Required]
    [StringLength(50)]
    public string PipelineId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the execution started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets when the execution ended.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the number of records processed.
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of successful records.
    /// </summary>
    public long SuccessfulRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of failed records.
    /// </summary>
    public long FailedRecords { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the execution parameters as JSON.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string ParametersJson { get; set; } = "{}";

    /// <summary>
    /// Navigation property to the pipeline.
    /// </summary>
    public virtual Pipeline Pipeline { get; set; } = null!;

    /// <summary>
    /// Gets or sets the execution parameters (not mapped to database).
    /// </summary>
    [NotMapped]
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    [NotMapped]
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
}
