using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ETLFramework.Data.Entities;
using ETLFramework.Data.Models;

namespace ETLFramework.Data.Context;

/// <summary>
/// Entity Framework DbContext for the ETL Framework.
/// </summary>
public class ETLDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the ETLDbContext class.
    /// </summary>
    /// <param name="options">The DbContext options</param>
    public ETLDbContext(DbContextOptions<ETLDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Pipelines DbSet.
    /// </summary>
    public DbSet<Pipeline> Pipelines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Executions DbSet.
    /// </summary>
    public DbSet<Execution> Executions { get; set; } = null!;

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePipeline(modelBuilder);
        ConfigureExecution(modelBuilder);
    }

    /// <summary>
    /// Configures the Pipeline entity.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigurePipeline(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Pipeline>();

        // Primary key
        entity.HasKey(p => p.Id);

        // Indexes
        entity.HasIndex(p => p.Name);
        entity.HasIndex(p => p.IsEnabled);
        entity.HasIndex(p => p.CreatedAt);
        entity.HasIndex(p => p.LastExecutedAt);

        // Properties
        entity.Property(p => p.Id)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(p => p.Description)
            .HasMaxLength(500);

        entity.Property(p => p.SourceConnectorJson)
            .HasColumnType("jsonb")
            .IsRequired();

        entity.Property(p => p.TargetConnectorJson)
            .HasColumnType("jsonb")
            .IsRequired();

        entity.Property(p => p.TransformationsJson)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        entity.Property(p => p.ConfigurationJson)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        entity.Property(p => p.IsEnabled)
            .HasDefaultValue(true);

        entity.Property(p => p.CreatedAt)
            .IsRequired();

        entity.Property(p => p.ModifiedAt)
            .IsRequired();

        // Relationships
        entity.HasMany(p => p.Executions)
            .WithOne(e => e.Pipeline)
            .HasForeignKey(e => e.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Configures the Execution entity.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureExecution(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Execution>();

        // Primary key
        entity.HasKey(e => e.Id);

        // Indexes
        entity.HasIndex(e => e.ExecutionId)
            .IsUnique();
        entity.HasIndex(e => e.PipelineId);
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => e.StartTime);
        entity.HasIndex(e => new { e.PipelineId, e.StartTime });

        // Properties
        entity.Property(e => e.ExecutionId)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(e => e.PipelineId)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(e => e.Status)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(e => e.StartTime)
            .IsRequired();

        entity.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        entity.Property(e => e.ParametersJson)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        // Relationships
        entity.HasOne(e => e.Pipeline)
            .WithMany(p => p.Executions)
            .HasForeignKey(e => e.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update ModifiedAt for pipelines
        var modifiedPipelines = ChangeTracker.Entries<Pipeline>()
            .Where(e => e.State == EntityState.Modified)
            .Select(e => e.Entity);

        foreach (var pipeline in modifiedPipelines)
        {
            pipeline.ModifiedAt = DateTimeOffset.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
