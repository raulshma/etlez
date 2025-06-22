using ETLFramework.API.Models;

namespace ETLFramework.API.Services;

/// <summary>
/// Interface for pipeline service operations.
/// </summary>
public interface IPipelineService
{
    /// <summary>
    /// Gets all pipelines with pagination and filtering.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="search">Search term</param>
    /// <param name="isEnabled">Filter by enabled status</param>
    /// <returns>Paged result of pipelines</returns>
    Task<PagedResult<PipelineResponse>> GetPipelinesAsync(int page, int pageSize, string? search, bool? isEnabled);

    /// <summary>
    /// Gets a specific pipeline by ID.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <returns>Pipeline details or null if not found</returns>
    Task<PipelineResponse?> GetPipelineAsync(string id);

    /// <summary>
    /// Creates a new pipeline.
    /// </summary>
    /// <param name="request">Pipeline creation request</param>
    /// <returns>Created pipeline</returns>
    Task<PipelineResponse> CreatePipelineAsync(CreatePipelineRequest request);

    /// <summary>
    /// Updates an existing pipeline.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <param name="request">Pipeline update request</param>
    /// <returns>Updated pipeline or null if not found</returns>
    Task<PipelineResponse?> UpdatePipelineAsync(string id, UpdatePipelineRequest request);

    /// <summary>
    /// Deletes a pipeline.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeletePipelineAsync(string id);

    /// <summary>
    /// Executes a pipeline.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <param name="request">Execution request</param>
    /// <returns>Execution result or null if pipeline not found</returns>
    Task<ExecutePipelineResponse?> ExecutePipelineAsync(string id, ExecutePipelineRequest request);

    /// <summary>
    /// Gets pipeline execution history.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paged result of executions or null if pipeline not found</returns>
    Task<PagedResult<ExecutePipelineResponse>?> GetPipelineExecutionsAsync(string id, int page, int pageSize);

    /// <summary>
    /// Gets a specific pipeline execution.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <param name="executionId">Execution ID</param>
    /// <returns>Execution details or null if not found</returns>
    Task<ExecutePipelineResponse?> GetPipelineExecutionAsync(string id, string executionId);

    /// <summary>
    /// Cancels a running pipeline execution.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <param name="executionId">Execution ID</param>
    /// <returns>True if cancelled, false if not found</returns>
    Task<bool> CancelPipelineExecutionAsync(string id, string executionId);
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
