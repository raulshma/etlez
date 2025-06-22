namespace ETLFramework.Core.Models;

/// <summary>
/// Represents a single data record in the ETL pipeline.
/// Contains field values and metadata about the record.
/// </summary>
public class DataRecord
{
    /// <summary>
    /// Initializes a new instance of the DataRecord class.
    /// </summary>
    public DataRecord()
    {
        Fields = new Dictionary<string, object?>();
        Metadata = new Dictionary<string, object>();
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the DataRecord class with the specified fields.
    /// </summary>
    /// <param name="fields">The field values for this record</param>
    public DataRecord(IDictionary<string, object?> fields) : this()
    {
        Fields = new Dictionary<string, object?>(fields);
    }

    /// <summary>
    /// Gets or sets the unique identifier for this data record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the field values for this record.
    /// </summary>
    public IDictionary<string, object?> Fields { get; set; }

    /// <summary>
    /// Gets or sets metadata associated with this record.
    /// </summary>
    public IDictionary<string, object> Metadata { get; set; }

    /// <summary>
    /// Gets or sets when this record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when this record was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the source information for this record.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the row number or position of this record in the source.
    /// </summary>
    public long? RowNumber { get; set; }

    /// <summary>
    /// Gets a field value by name.
    /// </summary>
    /// <typeparam name="T">The type of the field value</typeparam>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>The field value, or default if not found</returns>
    public T? GetField<T>(string fieldName)
    {
        if (Fields.TryGetValue(fieldName, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Sets a field value.
    /// </summary>
    /// <typeparam name="T">The type of the field value</typeparam>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="value">The field value</param>
    public void SetField<T>(string fieldName, T value)
    {
        Fields[fieldName] = value;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Checks if a field exists in this record.
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>True if the field exists, false otherwise</returns>
    public bool HasField(string fieldName)
    {
        return Fields.ContainsKey(fieldName);
    }

    /// <summary>
    /// Removes a field from this record.
    /// </summary>
    /// <param name="fieldName">The name of the field to remove</param>
    /// <returns>True if the field was removed, false if it didn't exist</returns>
    public bool RemoveField(string fieldName)
    {
        var removed = Fields.Remove(fieldName);
        if (removed)
        {
            ModifiedAt = DateTimeOffset.UtcNow;
        }
        return removed;
    }

    /// <summary>
    /// Gets a metadata value by key.
    /// </summary>
    /// <typeparam name="T">The type of the metadata value</typeparam>
    /// <param name="key">The metadata key</param>
    /// <returns>The metadata value, or default if not found</returns>
    public T? GetMetadata<T>(string key)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Sets a metadata value.
    /// </summary>
    /// <typeparam name="T">The type of the metadata value</typeparam>
    /// <param name="key">The metadata key</param>
    /// <param name="value">The metadata value</param>
    public void SetMetadata<T>(string key, T value)
    {
        Metadata[key] = value!;
    }

    /// <summary>
    /// Creates a deep copy of this data record.
    /// </summary>
    /// <returns>A new DataRecord instance with the same values</returns>
    public DataRecord Clone()
    {
        var clone = new DataRecord
        {
            Id = Id,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            Source = Source,
            RowNumber = RowNumber,
            Fields = new Dictionary<string, object?>(Fields),
            Metadata = new Dictionary<string, object>(Metadata)
        };
        return clone;
    }

    /// <summary>
    /// Returns a string representation of this data record.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        return $"DataRecord[Id={Id}, Fields={Fields.Count}, Source={Source}]";
    }
}
