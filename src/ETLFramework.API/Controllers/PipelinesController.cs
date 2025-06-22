using Microsoft.AspNetCore.Mvc;
using ETLFramework.API.Models;
using ETLFramework.API.Services;
using System.ComponentModel.DataAnnotations;

namespace ETLFramework.API.Controllers;

/// <summary>
/// Controller for pipeline management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PipelinesController : ControllerBase
{
    private readonly IPipelineService _pipelineService;
    private readonly ILogger<PipelinesController> _logger;

    /// <summary>
    /// Initializes a new instance of the PipelinesController class.
    /// </summary>
    /// <param name="pipelineService">The pipeline service</param>
    /// <param name="logger">The logger instance</param>
    public PipelinesController(IPipelineService pipelineService, ILogger<PipelinesController> logger)
    {
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all pipelines.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="search">Search term</param>
    /// <param name="isEnabled">Filter by enabled status</param>
    /// <returns>List of pipelines</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PipelineResponse>), 200)]
    public async Task<ActionResult<PagedResult<PipelineResponse>>> GetPipelines(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isEnabled = null)
    {
        try
        {
            var result = await _pipelineService.GetPipelinesAsync(page, pageSize, search, isEnabled);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipelines");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a specific pipeline by ID.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <returns>Pipeline details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PipelineResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<PipelineResponse>> GetPipeline([Required] string id)
    {
        try
        {
            var pipeline = await _pipelineService.GetPipelineAsync(id);
            if (pipeline == null)
                return NotFound(new { message = $"Pipeline with ID '{id}' not found" });

            return Ok(pipeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipeline {PipelineId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates a new pipeline.
    /// </summary>
    /// <param name="request">Pipeline creation request</param>
    /// <returns>Created pipeline</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PipelineResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<PipelineResponse>> CreatePipeline([FromBody] CreatePipelineRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var pipeline = await _pipelineService.CreatePipelineAsync(request);
            return CreatedAtAction(nameof(GetPipeline), new { id = pipeline.Id }, pipeline);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid pipeline creation request");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pipeline");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates an existing pipeline.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <param name="request">Pipeline update request</param>
    /// <returns>Updated pipeline</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PipelineResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<PipelineResponse>> UpdatePipeline([Required] string id, [FromBody] UpdatePipelineRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var pipeline = await _pipelineService.UpdatePipelineAsync(id, request);
            if (pipeline == null)
                return NotFound(new { message = $"Pipeline with ID '{id}' not found" });

            return Ok(pipeline);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid pipeline update request for {PipelineId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pipeline {PipelineId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a pipeline.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeletePipeline([Required] string id)
    {
        try
        {
            var deleted = await _pipelineService.DeletePipelineAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Pipeline with ID '{id}' not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pipeline {PipelineId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Executes a pipeline.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <param name="request">Execution request</param>
    /// <returns>Execution result</returns>
    [HttpPost("{id}/execute")]
    [ProducesResponseType(typeof(ExecutePipelineResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ExecutePipelineResponse>> ExecutePipeline([Required] string id, [FromBody] ExecutePipelineRequest? request = null)
    {
        try
        {
            request ??= new ExecutePipelineRequest();

            var result = await _pipelineService.ExecutePipelineAsync(id, request);
            if (result == null)
                return NotFound(new { message = $"Pipeline with ID '{id}' not found" });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid pipeline execution request for {PipelineId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing pipeline {PipelineId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets pipeline execution history.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Execution history</returns>
    [HttpGet("{id}/executions")]
    [ProducesResponseType(typeof(PagedResult<ExecutePipelineResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<PagedResult<ExecutePipelineResponse>>> GetPipelineExecutions(
        [Required] string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _pipelineService.GetPipelineExecutionsAsync(id, page, pageSize);
            if (result == null)
                return NotFound(new { message = $"Pipeline with ID '{id}' not found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipeline executions for {PipelineId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a specific pipeline execution.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <param name="executionId">Execution ID</param>
    /// <returns>Execution details</returns>
    [HttpGet("{id}/executions/{executionId}")]
    [ProducesResponseType(typeof(ExecutePipelineResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ExecutePipelineResponse>> GetPipelineExecution([Required] string id, [Required] string executionId)
    {
        try
        {
            var execution = await _pipelineService.GetPipelineExecutionAsync(id, executionId);
            if (execution == null)
                return NotFound(new { message = $"Execution with ID '{executionId}' not found for pipeline '{id}'" });

            return Ok(execution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipeline execution {ExecutionId} for {PipelineId}", executionId, id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancels a running pipeline execution.
    /// </summary>
    /// <param name="id">Pipeline ID</param>
    /// <param name="executionId">Execution ID</param>
    /// <returns>No content</returns>
    [HttpPost("{id}/executions/{executionId}/cancel")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CancelPipelineExecution([Required] string id, [Required] string executionId)
    {
        try
        {
            var cancelled = await _pipelineService.CancelPipelineExecutionAsync(id, executionId);
            if (!cancelled)
                return NotFound(new { message = $"Execution with ID '{executionId}' not found for pipeline '{id}'" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling pipeline execution {ExecutionId} for {PipelineId}", executionId, id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
