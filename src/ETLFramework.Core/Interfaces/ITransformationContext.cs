using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Interface for transformation context that maintains state and metadata during transformations.
/// </summary>
public interface ITransformationContext
{
    /// <summary>
    /// Gets the unique identifier for this transformation context.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of the transformation context.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the current record index being processed.
    /// </summary>
    long CurrentRecordIndex { get; }

    /// <summary>
    /// Gets the total number of records to be processed.
    /// </summary>
    long? TotalRecords { get; }

    /// <summary>
    /// Gets the transformation start time.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets the current transformation execution time.
    /// </summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Gets the transformation statistics.
    /// </summary>
    TransformationStatistics Statistics { get; }

    /// <summary>
    /// Gets the transformation configuration.
    /// </summary>
    IDictionary<string, object> Configuration { get; }

    /// <summary>
    /// Gets the transformation variables (mutable state).
    /// </summary>
    IDictionary<string, object> Variables { get; }

    /// <summary>
    /// Gets the transformation metadata.
    /// </summary>
    IDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets the cancellation token for the transformation.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets or sets the current data schema.
    /// </summary>
    DataSchema? CurrentSchema { get; set; }

    /// <summary>
    /// Gets the error collection for the transformation.
    /// </summary>
    IList<TransformationError> Errors { get; }

    /// <summary>
    /// Gets the warning collection for the transformation.
    /// </summary>
    IList<TransformationWarning> Warnings { get; }

    /// <summary>
    /// Advances to the next record.
    /// </summary>
    /// <param name="record">The current record being processed</param>
    void AdvanceRecord(DataRecord record);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="value">The configuration value</param>
    void SetConfiguration(string key, object value);

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if not found</param>
    /// <returns>The configuration value</returns>
    T GetConfiguration<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Sets a variable value.
    /// </summary>
    /// <param name="key">The variable key</param>
    /// <param name="value">The variable value</param>
    void SetVariable(string key, object value);

    /// <summary>
    /// Gets a variable value.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="key">The variable key</param>
    /// <param name="defaultValue">The default value if not found</param>
    /// <returns>The variable value</returns>
    T GetVariable<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Sets a metadata value.
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <param name="value">The metadata value</param>
    void SetMetadata(string key, object value);

    /// <summary>
    /// Gets a metadata value.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="key">The metadata key</param>
    /// <param name="defaultValue">The default value if not found</param>
    /// <returns>The metadata value</returns>
    T GetMetadata<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Adds an error to the context.
    /// </summary>
    /// <param name="error">The transformation error</param>
    void AddError(TransformationError error);

    /// <summary>
    /// Adds an error to the context.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="exception">The optional exception</param>
    /// <param name="recordIndex">The optional record index</param>
    /// <param name="fieldName">The optional field name</param>
    void AddError(string message, Exception? exception = null, long? recordIndex = null, string? fieldName = null);

    /// <summary>
    /// Adds a warning to the context.
    /// </summary>
    /// <param name="warning">The transformation warning</param>
    void AddWarning(TransformationWarning warning);

    /// <summary>
    /// Adds a warning to the context.
    /// </summary>
    /// <param name="message">The warning message</param>
    /// <param name="recordIndex">The optional record index</param>
    /// <param name="fieldName">The optional field name</param>
    void AddWarning(string message, long? recordIndex = null, string? fieldName = null);

    /// <summary>
    /// Creates a child context for nested transformations.
    /// </summary>
    /// <param name="name">The child context name</param>
    /// <returns>A new child transformation context</returns>
    ITransformationContext CreateChildContext(string name);

    /// <summary>
    /// Clones the current context.
    /// </summary>
    /// <returns>A cloned transformation context</returns>
    ITransformationContext Clone();
}

/// <summary>
/// Represents transformation statistics.
/// </summary>
public class TransformationStatistics
{
    /// <summary>
    /// Gets or sets the number of records processed.
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of records transformed successfully.
    /// </summary>
    public long RecordsTransformed { get; set; }

    /// <summary>
    /// Gets or sets the number of records skipped.
    /// </summary>
    public long RecordsSkipped { get; set; }

    /// <summary>
    /// Gets or sets the number of records that failed transformation.
    /// </summary>
    public long RecordsFailed { get; set; }

    /// <summary>
    /// Gets or sets the number of fields transformed.
    /// </summary>
    public long FieldsTransformed { get; set; }

    /// <summary>
    /// Gets or sets the total processing time.
    /// </summary>
    public TimeSpan TotalProcessingTime { get; set; }

    /// <summary>
    /// Gets or sets the average processing time per record.
    /// </summary>
    public TimeSpan AverageProcessingTimePerRecord { get; set; }

    /// <summary>
    /// Gets or sets the throughput in records per second.
    /// </summary>
    public double ThroughputRecordsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets custom performance metrics.
    /// </summary>
    public Dictionary<string, object> CustomMetrics { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Calculates derived statistics.
    /// </summary>
    public void CalculateDerivedStatistics()
    {
        if (RecordsProcessed > 0 && TotalProcessingTime.TotalSeconds > 0)
        {
            AverageProcessingTimePerRecord = TimeSpan.FromTicks(TotalProcessingTime.Ticks / RecordsProcessed);
            ThroughputRecordsPerSecond = RecordsProcessed / TotalProcessingTime.TotalSeconds;
        }
    }
}

/// <summary>
/// Represents a transformation error that extends the base execution error with transformation-specific information.
/// </summary>
public class TransformationError : Models.ExecutionError
{
    /// <summary>
    /// Initializes a new instance of the TransformationError class.
    /// </summary>
    public TransformationError()
    {
        ErrorCode = "TRANSFORMATION_ERROR";
        Source = "Transformation";
    }

    /// <summary>
    /// Initializes a new instance of the TransformationError class with a message.
    /// </summary>
    /// <param name="message">The error message</param>
    public TransformationError(string message) : this()
    {
        Message = message;
    }

    /// <summary>
    /// Initializes a new instance of the TransformationError class with a message and exception.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="exception">The exception that caused the error</param>
    public TransformationError(string message, Exception exception) : this(message)
    {
        Exception = exception;
    }

    /// <summary>
    /// Gets or sets the record index where the error occurred.
    /// </summary>
    public long? RecordIndex { get; set; }

    /// <summary>
    /// Gets or sets the field name where the error occurred.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Gets or sets the transformation ID that caused the error.
    /// </summary>
    public string? TransformationId { get; set; }
}

/// <summary>
/// Represents a transformation warning that extends the base execution warning with transformation-specific information.
/// </summary>
public class TransformationWarning : Models.ExecutionWarning
{
    /// <summary>
    /// Initializes a new instance of the TransformationWarning class.
    /// </summary>
    public TransformationWarning()
    {
        WarningCode = "TRANSFORMATION_WARNING";
        Source = "Transformation";
    }

    /// <summary>
    /// Initializes a new instance of the TransformationWarning class with a message.
    /// </summary>
    /// <param name="message">The warning message</param>
    public TransformationWarning(string message) : this()
    {
        Message = message;
    }

    /// <summary>
    /// Gets or sets the record index where the warning occurred.
    /// </summary>
    public long? RecordIndex { get; set; }

    /// <summary>
    /// Gets or sets the field name where the warning occurred.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Gets or sets the transformation ID that caused the warning.
    /// </summary>
    public string? TransformationId { get; set; }
}


