using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ETLFramework.Data.Models;
using ETLFramework.Data.Context;
using ETLFramework.Data.Entities;
using ETLFramework.Data.Repositories.Interfaces;
using ETLFramework.Data.Configuration;

namespace ETLFramework.Data.Repositories.Implementations;

/// <summary>
/// Repository implementation for pipeline operations.
/// </summary>
public class PipelineRepository : IPipelineRepository
{
    private readonly ETLDbContext _context;
    private readonly ILogger<PipelineRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the PipelineRepository class.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger</param>
    public PipelineRepository(ETLDbContext context, ILogger<PipelineRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Pipeline?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var pipeline = await _context.Pipelines
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (pipeline != null)
            {
                JsonConverters.PopulatePipelineFromJson(pipeline);
            }

            return pipeline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipeline by ID {PipelineId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<Pipeline>> GetPagedAsync(
        int page, 
        int pageSize, 
        string? search = null, 
        bool? isEnabled = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Pipelines.AsNoTracking();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || 
                                       (p.Description != null && p.Description.Contains(search)));
            }

            // Apply enabled filter
            if (isEnabled.HasValue)
            {
                query = query.Where(p => p.IsEnabled == isEnabled.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Populate JSON properties
            foreach (var pipeline in items)
            {
                JsonConverters.PopulatePipelineFromJson(pipeline);
            }

            return new PagedResult<Pipeline>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged pipelines. Page: {Page}, PageSize: {PageSize}, Search: {Search}, IsEnabled: {IsEnabled}", 
                page, pageSize, search, isEnabled);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Pipeline> CreateAsync(Pipeline pipeline, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));

            // Populate JSON columns from object properties
            JsonConverters.PopulatePipelineToJson(pipeline);

            // Set timestamps
            var now = DateTimeOffset.UtcNow;
            pipeline.CreatedAt = now;
            pipeline.ModifiedAt = now;

            _context.Pipelines.Add(pipeline);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created pipeline {PipelineId} with name '{PipelineName}'", 
                pipeline.Id, pipeline.Name);

            // Populate object properties for return
            JsonConverters.PopulatePipelineFromJson(pipeline);

            return pipeline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pipeline {PipelineId}", pipeline?.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Pipeline?> UpdateAsync(Pipeline pipeline, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));

            var existingPipeline = await _context.Pipelines
                .FirstOrDefaultAsync(p => p.Id == pipeline.Id, cancellationToken);

            if (existingPipeline == null)
            {
                return null;
            }

            // Update properties
            existingPipeline.Name = pipeline.Name;
            existingPipeline.Description = pipeline.Description;
            existingPipeline.IsEnabled = pipeline.IsEnabled;
            existingPipeline.ModifiedAt = DateTimeOffset.UtcNow;

            // Update JSON properties
            if (pipeline.SourceConnector != null)
                existingPipeline.SourceConnectorJson = JsonConverters.SerializeConnector(pipeline.SourceConnector);
            if (pipeline.TargetConnector != null)
                existingPipeline.TargetConnectorJson = JsonConverters.SerializeConnector(pipeline.TargetConnector);
            if (pipeline.Transformations != null)
                existingPipeline.TransformationsJson = JsonConverters.SerializeTransformations(pipeline.Transformations);
            if (pipeline.Configuration != null)
                existingPipeline.ConfigurationJson = JsonConverters.SerializeDictionary(pipeline.Configuration);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated pipeline {PipelineId}", pipeline.Id);

            // Populate object properties for return
            JsonConverters.PopulatePipelineFromJson(existingPipeline);

            return existingPipeline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pipeline {PipelineId}", pipeline?.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var pipeline = await _context.Pipelines
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (pipeline == null)
            {
                return false;
            }

            _context.Pipelines.Remove(pipeline);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted pipeline {PipelineId}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pipeline {PipelineId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Pipelines
                .AsNoTracking()
                .AnyAsync(p => p.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if pipeline exists {PipelineId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateLastExecutedAsync(string id, DateTimeOffset lastExecutedAt, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowsAffected = await _context.Pipelines
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(x => x.LastExecutedAt, lastExecutedAt)
                    .SetProperty(x => x.ModifiedAt, DateTimeOffset.UtcNow), 
                    cancellationToken);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last executed timestamp for pipeline {PipelineId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<Pipeline>> GetEnabledPipelinesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pipelines = await _context.Pipelines
                .AsNoTracking()
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            // Populate JSON properties
            foreach (var pipeline in pipelines)
            {
                JsonConverters.PopulatePipelineFromJson(pipeline);
            }

            return pipelines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enabled pipelines");
            throw;
        }
    }
}
