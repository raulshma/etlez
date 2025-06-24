using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ETLFramework.Data.Models;
using ETLFramework.Data.Context;
using ETLFramework.Data.Entities;
using ETLFramework.Data.Repositories.Interfaces;
using ETLFramework.Data.Configuration;

namespace ETLFramework.Data.Repositories.Implementations;

/// <summary>
/// Repository implementation for execution operations.
/// </summary>
public class ExecutionRepository : IExecutionRepository
{
    private readonly ETLDbContext _context;
    private readonly ILogger<ExecutionRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the ExecutionRepository class.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger</param>
    public ExecutionRepository(ETLDbContext context, ILogger<ExecutionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Execution?> GetByExecutionIdAsync(string executionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var execution = await _context.Executions
                .AsNoTracking()
                .Include(e => e.Pipeline)
                .FirstOrDefaultAsync(e => e.ExecutionId == executionId, cancellationToken);

            if (execution != null)
            {
                JsonConverters.PopulateExecutionFromJson(execution);
            }

            return execution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution by ID {ExecutionId}", executionId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Execution?> GetByPipelineAndExecutionIdAsync(string pipelineId, string executionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var execution = await _context.Executions
                .AsNoTracking()
                .Include(e => e.Pipeline)
                .FirstOrDefaultAsync(e => e.PipelineId == pipelineId && e.ExecutionId == executionId, cancellationToken);

            if (execution != null)
            {
                JsonConverters.PopulateExecutionFromJson(execution);
            }

            return execution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution {ExecutionId} for pipeline {PipelineId}", executionId, pipelineId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<Execution>> GetByPipelineIdAsync(
        string pipelineId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Executions
                .AsNoTracking()
                .Where(e => e.PipelineId == pipelineId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(e => e.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Populate JSON properties
            foreach (var execution in items)
            {
                JsonConverters.PopulateExecutionFromJson(execution);
            }

            return new PagedResult<Execution>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged executions for pipeline {PipelineId}. Page: {Page}, PageSize: {PageSize}", 
                pipelineId, page, pageSize);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Execution> CreateAsync(Execution execution, CancellationToken cancellationToken = default)
    {
        try
        {
            if (execution == null)
                throw new ArgumentNullException(nameof(execution));

            // Populate JSON columns from object properties
            JsonConverters.PopulateExecutionToJson(execution);

            _context.Executions.Add(execution);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created execution {ExecutionId} for pipeline {PipelineId}", 
                execution.ExecutionId, execution.PipelineId);

            // Populate object properties for return
            JsonConverters.PopulateExecutionFromJson(execution);

            return execution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating execution {ExecutionId} for pipeline {PipelineId}", 
                execution?.ExecutionId, execution?.PipelineId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Execution?> UpdateAsync(Execution execution, CancellationToken cancellationToken = default)
    {
        try
        {
            if (execution == null)
                throw new ArgumentNullException(nameof(execution));

            var existingExecution = await _context.Executions
                .FirstOrDefaultAsync(e => e.ExecutionId == execution.ExecutionId, cancellationToken);

            if (existingExecution == null)
            {
                return null;
            }

            // Update properties
            existingExecution.Status = execution.Status;
            existingExecution.EndTime = execution.EndTime;
            existingExecution.RecordsProcessed = execution.RecordsProcessed;
            existingExecution.SuccessfulRecords = execution.SuccessfulRecords;
            existingExecution.FailedRecords = execution.FailedRecords;
            existingExecution.ErrorMessage = execution.ErrorMessage;

            // Update JSON properties
            if (execution.Parameters != null)
                existingExecution.ParametersJson = JsonConverters.SerializeDictionary(execution.Parameters);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated execution {ExecutionId}", execution.ExecutionId);

            // Populate object properties for return
            JsonConverters.PopulateExecutionFromJson(existingExecution);

            return existingExecution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating execution {ExecutionId}", execution?.ExecutionId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Execution?> GetLatestByPipelineIdAsync(string pipelineId, CancellationToken cancellationToken = default)
    {
        try
        {
            var execution = await _context.Executions
                .AsNoTracking()
                .Where(e => e.PipelineId == pipelineId)
                .OrderByDescending(e => e.StartTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (execution != null)
            {
                JsonConverters.PopulateExecutionFromJson(execution);
            }

            return execution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest execution for pipeline {PipelineId}", pipelineId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<Execution>> GetRunningExecutionsAsync(string pipelineId, CancellationToken cancellationToken = default)
    {
        try
        {
            var executions = await _context.Executions
                .AsNoTracking()
                .Where(e => e.PipelineId == pipelineId && e.Status == "Running")
                .OrderBy(e => e.StartTime)
                .ToListAsync(cancellationToken);

            // Populate JSON properties
            foreach (var execution in executions)
            {
                JsonConverters.PopulateExecutionFromJson(execution);
            }

            return executions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting running executions for pipeline {PipelineId}", pipelineId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ExecutionStatistics> GetStatisticsAsync(string pipelineId, CancellationToken cancellationToken = default)
    {
        try
        {
            var executions = await _context.Executions
                .AsNoTracking()
                .Where(e => e.PipelineId == pipelineId)
                .ToListAsync(cancellationToken);

            var totalExecutions = executions.Count;
            var successfulExecutions = executions.Count(e => e.Status == "Completed");
            var failedExecutions = executions.Count(e => e.Status == "Failed");
            var runningExecutions = executions.Count(e => e.Status == "Running");

            var completedExecutions = executions.Where(e => e.EndTime.HasValue).ToList();
            var averageExecutionTime = completedExecutions.Any() 
                ? TimeSpan.FromTicks((long)completedExecutions.Average(e => e.EndTime!.Value.Subtract(e.StartTime).Ticks))
                : TimeSpan.Zero;

            var totalRecordsProcessed = executions.Sum(e => e.RecordsProcessed);
            var averageThroughput = completedExecutions.Any() && averageExecutionTime.TotalSeconds > 0
                ? totalRecordsProcessed / completedExecutions.Count / averageExecutionTime.TotalSeconds
                : 0;

            var latestExecution = executions.OrderByDescending(e => e.StartTime).FirstOrDefault();

            return new ExecutionStatistics
            {
                TotalExecutions = totalExecutions,
                SuccessfulExecutions = successfulExecutions,
                FailedExecutions = failedExecutions,
                RunningExecutions = runningExecutions,
                AverageExecutionTime = averageExecutionTime,
                TotalRecordsProcessed = totalRecordsProcessed,
                AverageThroughput = averageThroughput,
                LastExecutionStatus = latestExecution?.Status,
                LastExecutionDate = latestExecution?.StartTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for pipeline {PipelineId}", pipelineId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldExecutionsAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedCount = await _context.Executions
                .Where(e => e.StartTime < olderThan)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted {DeletedCount} executions older than {OlderThan}", deletedCount, olderThan);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old executions older than {OlderThan}", olderThan);
            throw;
        }
    }
}
