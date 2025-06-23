using ETLFramework.Core.Models;
using ETLFramework.Core.Interfaces;

namespace ETLFramework.Transformation.Models;

/// <summary>
/// Implementation of transformation context that maintains state and metadata during transformations.
/// </summary>
public class TransformationContext : ITransformationContext
{
    private readonly object _lockObject = new object();
    private long _currentRecordIndex;

    /// <summary>
    /// Initializes a new instance of the TransformationContext class.
    /// </summary>
    /// <param name="name">The context name</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public TransformationContext(string name, CancellationToken cancellationToken = default)
    {
        Id = Guid.NewGuid();
        Name = name;
        StartTime = DateTimeOffset.UtcNow;
        CancellationToken = cancellationToken;
        Statistics = new TransformationStatistics();
        Configuration = new Dictionary<string, object>();
        Variables = new Dictionary<string, object>();
        Metadata = new Dictionary<string, object>();
        Errors = new List<TransformationError>();
        Warnings = new List<TransformationWarning>();
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public long CurrentRecordIndex 
    { 
        get 
        { 
            lock (_lockObject) 
            { 
                return _currentRecordIndex; 
            } 
        } 
    }

    /// <inheritdoc />
    public long? TotalRecords { get; set; }

    /// <inheritdoc />
    public DateTimeOffset StartTime { get; }

    /// <inheritdoc />
    public TimeSpan ElapsedTime => DateTimeOffset.UtcNow - StartTime;

    /// <inheritdoc />
    public TransformationStatistics Statistics { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Configuration { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Variables { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Metadata { get; }

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc />
    public DataSchema? CurrentSchema { get; set; }

    /// <inheritdoc />
    public IList<TransformationError> Errors { get; }

    /// <inheritdoc />
    public IList<TransformationWarning> Warnings { get; }

    /// <inheritdoc />
    public void AdvanceRecord(DataRecord record)
    {
        lock (_lockObject)
        {
            _currentRecordIndex++;
            Statistics.RecordsProcessed++;
        }

        // Update current schema if needed (schema detection can be added later)
        // For now, we'll infer schema from the record fields if needed
    }

    /// <inheritdoc />
    public void SetConfiguration(string key, object value)
    {
        Configuration[key] = value;
    }

    /// <inheritdoc />
    public T GetConfiguration<T>(string key, T defaultValue = default!)
    {
        if (Configuration.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <inheritdoc />
    public void SetVariable(string key, object value)
    {
        Variables[key] = value;
    }

    /// <inheritdoc />
    public T GetVariable<T>(string key, T defaultValue = default!)
    {
        if (Variables.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <inheritdoc />
    public void SetMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    /// <inheritdoc />
    public T GetMetadata<T>(string key, T defaultValue = default!)
    {
        if (Metadata.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <inheritdoc />
    public void AddError(TransformationError error)
    {
        error.RecordIndex = CurrentRecordIndex;
        Errors.Add(error);
        Statistics.RecordsFailed++;
    }

    /// <inheritdoc />
    public void AddError(string message, Exception? exception = null, long? recordIndex = null, string? fieldName = null)
    {
        var error = new TransformationError
        {
            Message = message,
            Exception = exception,
            RecordIndex = recordIndex ?? CurrentRecordIndex,
            FieldName = fieldName
        };
        AddError(error);
    }

    /// <inheritdoc />
    public void AddWarning(TransformationWarning warning)
    {
        warning.RecordIndex = CurrentRecordIndex;
        Warnings.Add(warning);
    }

    /// <inheritdoc />
    public void AddWarning(string message, long? recordIndex = null, string? fieldName = null)
    {
        var warning = new TransformationWarning
        {
            Message = message,
            RecordIndex = recordIndex ?? CurrentRecordIndex,
            FieldName = fieldName
        };
        AddWarning(warning);
    }

    /// <inheritdoc />
    public ITransformationContext CreateChildContext(string name)
    {
        var childContext = new TransformationContext($"{Name}.{name}", CancellationToken)
        {
            TotalRecords = TotalRecords,
            CurrentSchema = CurrentSchema
        };

        // Copy configuration and metadata to child
        foreach (var kvp in Configuration)
        {
            childContext.Configuration[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in Metadata)
        {
            childContext.Metadata[kvp.Key] = kvp.Value;
        }

        return childContext;
    }

    /// <inheritdoc />
    public ITransformationContext Clone()
    {
        var clonedContext = new TransformationContext(Name, CancellationToken)
        {
            TotalRecords = TotalRecords,
            CurrentSchema = CurrentSchema
        };

        // Copy configuration
        foreach (var kvp in Configuration)
        {
            clonedContext.Configuration[kvp.Key] = kvp.Value;
        }

        // Copy variables
        foreach (var kvp in Variables)
        {
            clonedContext.Variables[kvp.Key] = kvp.Value;
        }

        // Copy metadata
        foreach (var kvp in Metadata)
        {
            clonedContext.Metadata[kvp.Key] = kvp.Value;
        }

        // Copy statistics
        clonedContext.Statistics.RecordsProcessed = Statistics.RecordsProcessed;
        clonedContext.Statistics.RecordsTransformed = Statistics.RecordsTransformed;
        clonedContext.Statistics.RecordsSkipped = Statistics.RecordsSkipped;
        clonedContext.Statistics.RecordsFailed = Statistics.RecordsFailed;
        clonedContext.Statistics.FieldsTransformed = Statistics.FieldsTransformed;
        clonedContext.Statistics.TotalProcessingTime = Statistics.TotalProcessingTime;
        clonedContext.Statistics.MemoryUsageBytes = Statistics.MemoryUsageBytes;

        foreach (var kvp in Statistics.CustomMetrics)
        {
            clonedContext.Statistics.CustomMetrics[kvp.Key] = kvp.Value;
        }

        return clonedContext;
    }

    /// <summary>
    /// Updates the transformation statistics.
    /// </summary>
    /// <param name="recordsTransformed">Number of records transformed</param>
    /// <param name="fieldsTransformed">Number of fields transformed</param>
    /// <param name="processingTime">Processing time</param>
    public void UpdateStatistics(long recordsTransformed = 0, long fieldsTransformed = 0, TimeSpan? processingTime = null)
    {
        lock (_lockObject)
        {
            Statistics.RecordsTransformed += recordsTransformed;
            Statistics.FieldsTransformed += fieldsTransformed;
            
            if (processingTime.HasValue)
            {
                Statistics.TotalProcessingTime = Statistics.TotalProcessingTime.Add(processingTime.Value);
            }

            Statistics.CalculateDerivedStatistics();
        }
    }

    /// <summary>
    /// Marks a record as skipped.
    /// </summary>
    public void SkipRecord()
    {
        lock (_lockObject)
        {
            Statistics.RecordsSkipped++;
        }
    }

    /// <summary>
    /// Gets the current progress percentage.
    /// </summary>
    /// <returns>Progress percentage (0-100)</returns>
    public double GetProgressPercentage()
    {
        if (TotalRecords.HasValue && TotalRecords.Value > 0)
        {
            return (double)CurrentRecordIndex / TotalRecords.Value * 100;
        }
        return 0;
    }

    /// <summary>
    /// Gets the estimated time remaining.
    /// </summary>
    /// <returns>Estimated time remaining</returns>
    public TimeSpan? GetEstimatedTimeRemaining()
    {
        if (TotalRecords.HasValue && TotalRecords.Value > 0 && CurrentRecordIndex > 0)
        {
            var averageTimePerRecord = ElapsedTime.TotalMilliseconds / CurrentRecordIndex;
            var remainingRecords = TotalRecords.Value - CurrentRecordIndex;
            var estimatedRemainingMs = remainingRecords * averageTimePerRecord;
            return TimeSpan.FromMilliseconds(estimatedRemainingMs);
        }
        return null;
    }

    /// <summary>
    /// Gets a summary of the transformation context.
    /// </summary>
    /// <returns>Context summary</returns>
    public TransformationContextSummary GetSummary()
    {
        return new TransformationContextSummary
        {
            Id = Id,
            Name = Name,
            StartTime = StartTime,
            ElapsedTime = ElapsedTime,
            CurrentRecordIndex = CurrentRecordIndex,
            TotalRecords = TotalRecords,
            ProgressPercentage = GetProgressPercentage(),
            EstimatedTimeRemaining = GetEstimatedTimeRemaining(),
            Statistics = Statistics,
            ErrorCount = Errors.Count,
            WarningCount = Warnings.Count,
            HasSchema = CurrentSchema != null
        };
    }
}

/// <summary>
/// Represents a summary of transformation context.
/// </summary>
public class TransformationContextSummary
{
    /// <summary>
    /// Gets or sets the context ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the context name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// Gets or sets the current record index.
    /// </summary>
    public long CurrentRecordIndex { get; set; }

    /// <summary>
    /// Gets or sets the total records.
    /// </summary>
    public long? TotalRecords { get; set; }

    /// <summary>
    /// Gets or sets the progress percentage.
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// Gets or sets the estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Gets or sets the transformation statistics.
    /// </summary>
    public TransformationStatistics Statistics { get; set; } = new TransformationStatistics();

    /// <summary>
    /// Gets or sets the error count.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the warning count.
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Gets or sets whether the context has a schema.
    /// </summary>
    public bool HasSchema { get; set; }
}
