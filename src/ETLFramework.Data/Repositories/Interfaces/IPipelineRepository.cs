using ETLFramework.Data.Models;
using ETLFramework.Data.Entities;

namespace ETLFramework.Data.Repositories.Interfaces;

/// <summary>
/// Repository interface for pipeline operations.
/// </summary>
public interface IPipelineRepository
{
    /// <summary>
    /// Gets a pipeline by ID.
    /// </summary>
    /// <param name="id">The pipeline ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The pipeline or null if not found</returns>
    Task<Pipeline?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pipelines with pagination and filtering.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="search">Search term for name or description</param>
    /// <param name="isEnabled">Filter by enabled status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of pipelines</returns>
    Task<PagedResult<Pipeline>> GetPagedAsync(
        int page, 
        int pageSize, 
        string? search = null, 
        bool? isEnabled = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new pipeline.
    /// </summary>
    /// <param name="pipeline">The pipeline to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created pipeline</returns>
    Task<Pipeline> CreateAsync(Pipeline pipeline, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing pipeline.
    /// </summary>
    /// <param name="pipeline">The pipeline to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated pipeline or null if not found</returns>
    Task<Pipeline?> UpdateAsync(Pipeline pipeline, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a pipeline by ID.
    /// </summary>
    /// <param name="id">The pipeline ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a pipeline exists.
    /// </summary>
    /// <param name="id">The pipeline ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last executed timestamp for a pipeline.
    /// </summary>
    /// <param name="id">The pipeline ID</param>
    /// <param name="lastExecutedAt">The last execution timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated, false if not found</returns>
    Task<bool> UpdateLastExecutedAsync(string id, DateTimeOffset lastExecutedAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pipelines that are enabled and ready for execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of enabled pipelines</returns>
    Task<List<Pipeline>> GetEnabledPipelinesAsync(CancellationToken cancellationToken = default);
}
