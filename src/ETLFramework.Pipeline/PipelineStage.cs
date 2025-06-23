using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Pipeline;

/// <summary>
/// Base implementation of a pipeline stage that provides common functionality.
/// </summary>
public abstract class PipelineStage : IPipelineStage
{
    private readonly ILogger _logger;
    private readonly object _statusLock = new object();
    private volatile StageStatus _status;

    /// <summary>
    /// Initializes a new instance of the PipelineStage class.
    /// </summary>
    /// <param name="id">The unique stage identifier</param>
    /// <param name="name">The stage name</param>
    /// <param name="description">The stage description</param>
    /// <param name="stageType">The stage type</param>
    /// <param name="order">The stage order</param>
    /// <param name="logger">The logger instance</param>
    protected PipelineStage(
        Guid id,
        string name,
        string description,
        StageType stageType,
        int order,
        ILogger logger)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        StageType = stageType;
        Order = order;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _status = StageStatus.Ready;
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public StageType StageType { get; }

    /// <inheritdoc />
    public StageStatus Status
    {
        get
        {
            lock (_statusLock)
            {
                return _status;
            }
        }
    }

    /// <inheritdoc />
    public int Order { get; set; }

    /// <summary>
    /// Gets the logger instance for this stage.
    /// </summary>
    protected ILogger Logger => _logger;

    /// <inheritdoc />
    public async Task<StageExecutionResult> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var result = new StageExecutionResult
        {
            StageId = Id,
            StartTime = DateTimeOffset.UtcNow,
            IsSuccess = false
        };

        try
        {
            _logger.LogInformation("Starting stage execution: {StageName} (Type: {StageType})", Name, StageType);
            SetStatus(StageStatus.Running);

            // Validate stage before execution
            var validationResult = await ValidateAsync();
            if (!validationResult.IsValid)
            {
                var errorMessage = $"Stage validation failed: {string.Join("; ", validationResult.Errors.Select(e => e.Message))}";
                throw new PipelineExecutionException(errorMessage);
            }

            // Execute the stage-specific logic
            result.RecordsProcessed = await ExecuteStageAsync(context, cancellationToken);
            result.IsSuccess = true;
            SetStatus(StageStatus.Completed);

            _logger.LogInformation("Stage execution completed: {StageName} - Records processed: {RecordsProcessed}",
                Name, result.RecordsProcessed);
        }
        catch (OperationCanceledException)
        {
            SetStatus(StageStatus.Cancelled);
            result.IsSuccess = false;
            _logger.LogWarning("Stage execution was cancelled: {StageName}", Name);
            throw;
        }
        catch (Exception ex)
        {
            SetStatus(StageStatus.Failed);
            result.IsSuccess = false;

            _logger.LogError(ex, "Stage execution failed: {StageName}", Name);

            var executionError = new ExecutionError
            {
                Message = ex.Message,
                Exception = ex,
                Source = $"Stage: {Name}",
                ErrorCode = "STAGE_EXECUTION_ERROR",
                Severity = ErrorSeverity.Error
            };

            result.Errors.Add(executionError);
            context.AddError(executionError);

            // Don't re-throw here, let the pipeline decide how to handle the error
        }
        finally
        {
            result.EndTime = DateTimeOffset.UtcNow;
        }

        return result;
    }

    /// <inheritdoc />
    public virtual Task<ValidationResult> ValidateAsync()
    {
        var result = new ValidationResult { IsValid = true };

        // Basic validation
        if (string.IsNullOrWhiteSpace(Name))
        {
            result.AddError("Stage name is required", nameof(Name));
        }

        if (Order < 0)
        {
            result.AddError("Stage order must be non-negative", nameof(Order));
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public virtual Task PrepareAsync(IPipelineContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Preparing stage: {StageName}", Name);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task CleanupAsync(IPipelineContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Cleaning up stage: {StageName}", Name);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the stage-specific logic. Must be implemented by derived classes.
    /// </summary>
    /// <param name="context">The pipeline execution context</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The number of records processed</returns>
    protected abstract Task<long> ExecuteStageAsync(IPipelineContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Reports progress for the current stage operation.
    /// </summary>
    /// <param name="context">The pipeline context</param>
    /// <param name="itemsProcessed">Number of items processed</param>
    /// <param name="totalItems">Total number of items (optional)</param>
    /// <param name="statusMessage">Status message (optional)</param>
    protected void ReportProgress(IPipelineContext context, long itemsProcessed, long? totalItems = null, string? statusMessage = null)
    {
        var percentComplete = totalItems.HasValue && totalItems > 0 
            ? (double)itemsProcessed / totalItems.Value * 100 
            : 0;

        var progress = new ProgressInfo
        {
            PercentComplete = Math.Min(percentComplete, 100),
            StatusMessage = statusMessage ?? $"Processing {Name}",
            ItemsProcessed = itemsProcessed,
            TotalItems = totalItems,
            CurrentStep = Name
        };

        context.ReportProgress(progress);
    }

    /// <summary>
    /// Adds an error to the execution context.
    /// </summary>
    /// <param name="context">The pipeline context</param>
    /// <param name="message">The error message</param>
    /// <param name="exception">The exception (optional)</param>
    /// <param name="errorCode">The error code (optional)</param>
    protected void AddError(IPipelineContext context, string message, Exception? exception = null, string? errorCode = null)
    {
        var error = new ExecutionError
        {
            Message = message,
            Exception = exception,
            Source = $"Stage: {Name}",
            ErrorCode = errorCode ?? "STAGE_ERROR",
            Severity = ErrorSeverity.Error
        };

        context.AddError(error);
    }

    /// <summary>
    /// Adds a warning to the execution context.
    /// </summary>
    /// <param name="context">The pipeline context</param>
    /// <param name="message">The warning message</param>
    /// <param name="warningCode">The warning code (optional)</param>
    protected void AddWarning(IPipelineContext context, string message, string? warningCode = null)
    {
        var warning = new ExecutionWarning
        {
            Message = message,
            Source = $"Stage: {Name}",
            WarningCode = warningCode ?? "STAGE_WARNING"
        };

        context.AddWarning(warning);
    }

    /// <summary>
    /// Thread-safe method to set the stage status.
    /// </summary>
    /// <param name="status">The new status</param>
    private void SetStatus(StageStatus status)
    {
        lock (_statusLock)
        {
            _status = status;
        }
    }

    /// <summary>
    /// Returns a string representation of the stage.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        return $"Stage[{Name}, Type={StageType}, Order={Order}, Status={Status}]";
    }
}

/// <summary>
/// A simple stage implementation for demonstration purposes.
/// </summary>
public class DemoStage : PipelineStage
{
    private readonly int _recordCount;
    private readonly TimeSpan _processingDelay;

    /// <summary>
    /// Initializes a new instance of the DemoStage class.
    /// </summary>
    /// <param name="id">The stage identifier</param>
    /// <param name="name">The stage name</param>
    /// <param name="stageType">The stage type</param>
    /// <param name="order">The stage order</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="recordCount">Number of records to simulate processing</param>
    /// <param name="processingDelay">Delay per record to simulate processing time</param>
    public DemoStage(
        Guid id,
        string name,
        StageType stageType,
        int order,
        ILogger logger,
        int recordCount = 100,
        TimeSpan? processingDelay = null)
        : base(id, name, $"Demo {stageType} stage", stageType, order, logger)
    {
        _recordCount = recordCount;
        _processingDelay = processingDelay ?? TimeSpan.FromMilliseconds(10);
    }

    /// <inheritdoc />
    protected override async Task<long> ExecuteStageAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Demo stage processing {RecordCount} records with {Delay}ms delay per record",
            _recordCount, _processingDelay.TotalMilliseconds);

        for (int i = 0; i < _recordCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Simulate processing
            await Task.Delay(_processingDelay, cancellationToken);

            // Report progress every 10 records
            if (i % 10 == 0)
            {
                ReportProgress(context, i + 1, _recordCount, $"Processing record {i + 1}");
            }
        }

        // Final progress report
        ReportProgress(context, _recordCount, _recordCount, "Processing completed");

        return _recordCount;
    }
}
