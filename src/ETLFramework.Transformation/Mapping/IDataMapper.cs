using ETLFramework.Core.Models;

namespace ETLFramework.Transformation.Mapping;

/// <summary>
/// Interface for data mapping operations.
/// </summary>
public interface IDataMapper
{
    /// <summary>
    /// Gets the unique identifier for this mapper.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of the mapper.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this mapper does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the field mappings defined in this mapper.
    /// </summary>
    IReadOnlyList<IFieldMapping> Mappings { get; }

    /// <summary>
    /// Maps data from source record to target record.
    /// </summary>
    /// <param name="sourceRecord">The source data record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The mapped target record</returns>
    Task<DataRecord> MapAsync(DataRecord sourceRecord, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps multiple records in batch.
    /// </summary>
    /// <param name="sourceRecords">The source data records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The mapped target records</returns>
    Task<IEnumerable<DataRecord>> MapBatchAsync(IEnumerable<DataRecord> sourceRecords, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the mapper configuration.
    /// </summary>
    /// <returns>Validation result</returns>
    ValidationResult Validate();
}

/// <summary>
/// Interface for field mapping operations.
/// </summary>
public interface IFieldMapping
{
    /// <summary>
    /// Gets the unique identifier for this mapping.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of the mapping.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the source field path.
    /// </summary>
    string SourcePath { get; }

    /// <summary>
    /// Gets the target field path.
    /// </summary>
    string TargetPath { get; }

    /// <summary>
    /// Gets the mapping type.
    /// </summary>
    MappingType MappingType { get; }

    /// <summary>
    /// Gets the transformation to apply during mapping.
    /// </summary>
    IFieldTransformation? Transformation { get; }

    /// <summary>
    /// Gets the default value to use if source is null or missing.
    /// </summary>
    object? DefaultValue { get; }

    /// <summary>
    /// Gets whether this mapping is required.
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Maps a value from source to target.
    /// </summary>
    /// <param name="sourceRecord">The source record</param>
    /// <param name="targetRecord">The target record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    Task MapValueAsync(DataRecord sourceRecord, DataRecord targetRecord, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for field transformations during mapping.
/// </summary>
public interface IFieldTransformation
{
    /// <summary>
    /// Gets the transformation name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Transforms a value.
    /// </summary>
    /// <param name="value">The value to transform</param>
    /// <param name="sourceRecord">The source record for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformed value</returns>
    Task<object?> TransformAsync(object? value, DataRecord sourceRecord, CancellationToken cancellationToken = default);
}

/// <summary>
/// Types of field mappings.
/// </summary>
public enum MappingType
{
    /// <summary>
    /// Direct field-to-field mapping.
    /// </summary>
    Direct,

    /// <summary>
    /// Mapping with transformation.
    /// </summary>
    Transform,

    /// <summary>
    /// Constant value mapping.
    /// </summary>
    Constant,

    /// <summary>
    /// Conditional mapping based on source values.
    /// </summary>
    Conditional,

    /// <summary>
    /// Lookup mapping using external data.
    /// </summary>
    Lookup,

    /// <summary>
    /// Aggregation mapping combining multiple source fields.
    /// </summary>
    Aggregate,

    /// <summary>
    /// Custom mapping with user-defined logic.
    /// </summary>
    Custom
}

/// <summary>
/// Interface for schema mapping operations.
/// </summary>
public interface ISchemaMapper
{
    /// <summary>
    /// Gets the source schema.
    /// </summary>
    DataSchema SourceSchema { get; }

    /// <summary>
    /// Gets the target schema.
    /// </summary>
    DataSchema TargetSchema { get; }

    /// <summary>
    /// Creates field mappings based on schema analysis.
    /// </summary>
    /// <param name="autoMapStrategy">The auto-mapping strategy</param>
    /// <returns>Generated field mappings</returns>
    IEnumerable<IFieldMapping> GenerateAutoMappings(AutoMapStrategy autoMapStrategy = AutoMapStrategy.ByName);

    /// <summary>
    /// Validates that source and target schemas are compatible.
    /// </summary>
    /// <returns>Validation result</returns>
    ValidationResult ValidateSchemaCompatibility();
}

/// <summary>
/// Strategies for automatic field mapping.
/// </summary>
public enum AutoMapStrategy
{
    /// <summary>
    /// Map fields by exact name match.
    /// </summary>
    ByName,

    /// <summary>
    /// Map fields by name similarity (fuzzy matching).
    /// </summary>
    BySimilarity,

    /// <summary>
    /// Map fields by data type compatibility.
    /// </summary>
    ByType,

    /// <summary>
    /// Map fields by position in schema.
    /// </summary>
    ByPosition,

    /// <summary>
    /// Combine multiple strategies.
    /// </summary>
    Hybrid
}
