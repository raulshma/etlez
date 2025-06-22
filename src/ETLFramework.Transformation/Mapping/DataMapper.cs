using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Transformation.Mapping;

/// <summary>
/// Default implementation of data mapper.
/// </summary>
public class DataMapper : IDataMapper
{
    private readonly ILogger<DataMapper> _logger;
    private readonly List<IFieldMapping> _mappings;

    /// <summary>
    /// Initializes a new instance of the DataMapper class.
    /// </summary>
    /// <param name="id">The mapper ID</param>
    /// <param name="name">The mapper name</param>
    /// <param name="description">The mapper description</param>
    /// <param name="logger">The logger instance</param>
    public DataMapper(string id, string name, string description, ILogger<DataMapper> logger)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mappings = new List<IFieldMapping>();
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public IReadOnlyList<IFieldMapping> Mappings => _mappings.AsReadOnly();

    /// <summary>
    /// Adds a field mapping to this mapper.
    /// </summary>
    /// <param name="mapping">The field mapping to add</param>
    public void AddMapping(IFieldMapping mapping)
    {
        if (mapping == null) throw new ArgumentNullException(nameof(mapping));
        _mappings.Add(mapping);
        _logger.LogDebug("Added field mapping {MappingId}: {SourcePath} -> {TargetPath}", 
            mapping.Id, mapping.SourcePath, mapping.TargetPath);
    }

    /// <summary>
    /// Removes a field mapping from this mapper.
    /// </summary>
    /// <param name="mappingId">The ID of the mapping to remove</param>
    /// <returns>True if the mapping was removed</returns>
    public bool RemoveMapping(string mappingId)
    {
        var mapping = _mappings.FirstOrDefault(m => m.Id == mappingId);
        if (mapping != null)
        {
            _mappings.Remove(mapping);
            _logger.LogDebug("Removed field mapping {MappingId}", mappingId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all mappings from this mapper.
    /// </summary>
    public void ClearMappings()
    {
        var count = _mappings.Count;
        _mappings.Clear();
        _logger.LogDebug("Cleared {MappingCount} field mappings", count);
    }

    /// <inheritdoc />
    public async Task<DataRecord> MapAsync(DataRecord sourceRecord, CancellationToken cancellationToken = default)
    {
        if (sourceRecord == null) throw new ArgumentNullException(nameof(sourceRecord));

        var startTime = DateTimeOffset.UtcNow;
        var targetRecord = new DataRecord();

        _logger.LogDebug("Starting data mapping with {MappingCount} field mappings", _mappings.Count);

        try
        {
            foreach (var mapping in _mappings)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await mapping.MapValueAsync(sourceRecord, targetRecord, cancellationToken);
                    _logger.LogTrace("Successfully mapped field {SourcePath} -> {TargetPath}", 
                        mapping.SourcePath, mapping.TargetPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map field {SourcePath} -> {TargetPath}: {Error}", 
                        mapping.SourcePath, mapping.TargetPath, ex.Message);

                    if (mapping.IsRequired)
                    {
                        throw new InvalidOperationException(
                            $"Required field mapping failed: {mapping.SourcePath} -> {mapping.TargetPath}", ex);
                    }
                }
            }

            var duration = DateTimeOffset.UtcNow - startTime;
            _logger.LogDebug("Data mapping completed in {Duration}ms. Mapped {FieldCount} fields", 
                duration.TotalMilliseconds, targetRecord.Fields.Count);

            return targetRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data mapping failed: {Error}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DataRecord>> MapBatchAsync(IEnumerable<DataRecord> sourceRecords, CancellationToken cancellationToken = default)
    {
        if (sourceRecords == null) throw new ArgumentNullException(nameof(sourceRecords));

        var results = new List<DataRecord>();
        var recordList = sourceRecords.ToList();

        _logger.LogDebug("Starting batch data mapping for {RecordCount} records", recordList.Count);

        foreach (var sourceRecord in recordList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var mappedRecord = await MapAsync(sourceRecord, cancellationToken);
                results.Add(mappedRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map record: {Error}", ex.Message);
                throw;
            }
        }

        _logger.LogDebug("Batch data mapping completed. Mapped {RecordCount} records", results.Count);
        return results;
    }

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(Id))
        {
            result.AddError("Mapper ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            result.AddError("Mapper name cannot be empty");
        }

        if (_mappings.Count == 0)
        {
            result.AddWarning("Mapper has no field mappings defined");
        }

        // Check for duplicate target paths
        var duplicateTargets = _mappings
            .GroupBy(m => m.TargetPath)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateTarget in duplicateTargets)
        {
            result.AddError($"Duplicate target path: {duplicateTarget}");
        }

        // Validate individual mappings
        foreach (var mapping in _mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.SourcePath) && mapping.MappingType != MappingType.Constant)
            {
                result.AddError($"Mapping {mapping.Id} has empty source path");
            }

            if (string.IsNullOrWhiteSpace(mapping.TargetPath))
            {
                result.AddError($"Mapping {mapping.Id} has empty target path");
            }
        }

        return result;
    }

    /// <summary>
    /// Gets statistics about this mapper.
    /// </summary>
    /// <returns>Mapper statistics</returns>
    public DataMapperStatistics GetStatistics()
    {
        return new DataMapperStatistics
        {
            TotalMappings = _mappings.Count,
            RequiredMappings = _mappings.Count(m => m.IsRequired),
            OptionalMappings = _mappings.Count(m => !m.IsRequired),
            MappingsByType = _mappings.GroupBy(m => m.MappingType)
                .ToDictionary(g => g.Key, g => g.Count()),
            TransformationMappings = _mappings.Count(m => m.Transformation != null)
        };
    }
}

/// <summary>
/// Statistics about data mapper execution.
/// </summary>
public class DataMapperStatistics
{
    /// <summary>
    /// Gets or sets the total number of mappings.
    /// </summary>
    public int TotalMappings { get; set; }

    /// <summary>
    /// Gets or sets the number of required mappings.
    /// </summary>
    public int RequiredMappings { get; set; }

    /// <summary>
    /// Gets or sets the number of optional mappings.
    /// </summary>
    public int OptionalMappings { get; set; }

    /// <summary>
    /// Gets or sets the distribution of mappings by type.
    /// </summary>
    public Dictionary<MappingType, int> MappingsByType { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of mappings with transformations.
    /// </summary>
    public int TransformationMappings { get; set; }
}

/// <summary>
/// Builder for creating data mappers with a fluent API.
/// </summary>
public class DataMapperBuilder
{
    private readonly DataMapper _mapper;
    private int _mappingCounter = 0;

    /// <summary>
    /// Initializes a new instance of the DataMapperBuilder class.
    /// </summary>
    /// <param name="id">The mapper ID</param>
    /// <param name="name">The mapper name</param>
    /// <param name="description">The mapper description</param>
    /// <param name="logger">The logger instance</param>
    public DataMapperBuilder(string id, string name, string description, ILogger<DataMapper> logger)
    {
        _mapper = new DataMapper(id, name, description, logger);
    }

    /// <summary>
    /// Creates a new data mapper builder.
    /// </summary>
    /// <param name="id">The mapper ID</param>
    /// <param name="name">The mapper name</param>
    /// <param name="description">The mapper description</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A new mapper builder</returns>
    public static DataMapperBuilder Create(string id, string name, string description, ILogger<DataMapper> logger)
    {
        return new DataMapperBuilder(id, name, description, logger);
    }

    /// <summary>
    /// Adds a direct field mapping.
    /// </summary>
    /// <param name="sourcePath">The source field path</param>
    /// <param name="targetPath">The target field path</param>
    /// <param name="isRequired">Whether the mapping is required</param>
    /// <returns>This builder instance</returns>
    public DataMapperBuilder MapField(string sourcePath, string targetPath, bool isRequired = false)
    {
        var mapping = new FieldMapping($"mapping_{++_mappingCounter}", $"Map {sourcePath} to {targetPath}", sourcePath, targetPath)
        {
            IsRequired = isRequired
        };
        _mapper.AddMapping(mapping);
        return this;
    }

    /// <summary>
    /// Adds a field mapping with transformation.
    /// </summary>
    /// <param name="sourcePath">The source field path</param>
    /// <param name="targetPath">The target field path</param>
    /// <param name="transformation">The transformation to apply</param>
    /// <param name="isRequired">Whether the mapping is required</param>
    /// <returns>This builder instance</returns>
    public DataMapperBuilder MapFieldWithTransform(string sourcePath, string targetPath, IFieldTransformation transformation, bool isRequired = false)
    {
        var mapping = new FieldMapping($"mapping_{++_mappingCounter}", $"Transform {sourcePath} to {targetPath}", sourcePath, targetPath, MappingType.Transform)
        {
            Transformation = transformation,
            IsRequired = isRequired
        };
        _mapper.AddMapping(mapping);
        return this;
    }

    /// <summary>
    /// Adds a constant value mapping.
    /// </summary>
    /// <param name="targetPath">The target field path</param>
    /// <param name="constantValue">The constant value</param>
    /// <returns>This builder instance</returns>
    public DataMapperBuilder MapConstant(string targetPath, object? constantValue)
    {
        var mapping = new ConstantFieldMapping($"mapping_{++_mappingCounter}", $"Constant {targetPath}", targetPath, constantValue);
        _mapper.AddMapping(mapping);
        return this;
    }

    /// <summary>
    /// Adds a conditional field mapping.
    /// </summary>
    /// <param name="targetPath">The target field path</param>
    /// <param name="condition">The condition function</param>
    /// <param name="trueSourcePath">Source path when condition is true</param>
    /// <param name="falseSourcePath">Source path when condition is false</param>
    /// <returns>This builder instance</returns>
    public DataMapperBuilder MapConditional(string targetPath, Func<DataRecord, bool> condition, string trueSourcePath, string falseSourcePath)
    {
        var mapping = new ConditionalFieldMapping($"mapping_{++_mappingCounter}", $"Conditional {targetPath}", targetPath, condition, trueSourcePath, falseSourcePath);
        _mapper.AddMapping(mapping);
        return this;
    }

    /// <summary>
    /// Adds a custom field mapping.
    /// </summary>
    /// <param name="sourcePath">The source field path</param>
    /// <param name="targetPath">The target field path</param>
    /// <param name="customMappingFunc">The custom mapping function</param>
    /// <returns>This builder instance</returns>
    public DataMapperBuilder MapCustom(string sourcePath, string targetPath, Func<DataRecord, CancellationToken, Task<object?>> customMappingFunc)
    {
        var mapping = new CustomFieldMapping($"mapping_{++_mappingCounter}", $"Custom {sourcePath} to {targetPath}", sourcePath, targetPath, customMappingFunc);
        _mapper.AddMapping(mapping);
        return this;
    }

    /// <summary>
    /// Builds the data mapper.
    /// </summary>
    /// <returns>The constructed data mapper</returns>
    public DataMapper Build()
    {
        return _mapper;
    }
}
