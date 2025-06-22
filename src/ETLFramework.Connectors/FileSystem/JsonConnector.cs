using System.Text;
using System.Text.Json;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace ETLFramework.Connectors.FileSystem;

/// <summary>
/// JSON file connector that can read from and write to JSON files.
/// Supports both single JSON objects and JSON arrays.
/// </summary>
public class JsonConnector : BaseConnector, ISourceConnector<DataRecord>, IDestinationConnector<DataRecord>
{
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _filePath;
    private DataSchema? _schema;
    private bool _isArrayFormat;

    /// <summary>
    /// Initializes a new instance of the JsonConnector class.
    /// </summary>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    public JsonConnector(IConnectorConfiguration configuration, ILogger<JsonConnector> logger)
        : base(Guid.NewGuid(), configuration.Name, "JSON", configuration, logger)
    {
        _jsonOptions = CreateJsonOptions(configuration);
        ExtractFilePathFromConnectionString(configuration.ConnectionString);
        _isArrayFormat = configuration.GetConnectionProperty<bool?>("arrayFormat") ?? true;
    }

    /// <inheritdoc />
    public WriteMode[] SupportedWriteModes => new[] { WriteMode.Insert, WriteMode.Replace };

    /// <inheritdoc />
    public async IAsyncEnumerable<DataRecord> ReadAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            throw ConnectorException.CreateReadFailure($"JSON file not found: {_filePath}", Id, ConnectorType);
        }

        Logger.LogInformation("Reading JSON file: {FilePath}", _filePath);

        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);

        if (_isArrayFormat)
        {
            await foreach (var record in ReadJsonArrayAsync(fileStream, cancellationToken))
            {
                yield return record;
            }
        }
        else
        {
            await foreach (var record in ReadJsonLinesAsync(fileStream, cancellationToken))
            {
                yield return record;
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IEnumerable<DataRecord>> ReadBatchAsync(int batchSize, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var batch = new List<DataRecord>(batchSize);

        await foreach (var record in ReadAsync(cancellationToken))
        {
            batch.Add(record);

            if (batch.Count >= batchSize)
            {
                yield return batch.ToList();
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    /// <inheritdoc />
    public async Task<long?> GetEstimatedRecordCountAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            return 0;
        }

        try
        {
            if (_isArrayFormat)
            {
                // For JSON arrays, we need to parse to count elements
                using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                using var document = await JsonDocument.ParseAsync(fileStream, cancellationToken: cancellationToken);

                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return document.RootElement.GetArrayLength();
                }
                else
                {
                    return 1; // Single object
                }
            }
            else
            {
                // For JSON Lines, count the number of lines
                var lineCount = 0L;
                using var reader = new StreamReader(_filePath, Encoding.UTF8);

                while (await reader.ReadLineAsync() != null)
                {
                    lineCount++;
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return lineCount;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not estimate record count for JSON file: {FilePath}", _filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DataSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        if (_schema != null)
        {
            return _schema;
        }

        await EnsureConnectedAsync(cancellationToken);

        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            throw ConnectorException.CreateReadFailure($"JSON file not found: {_filePath}", Id, ConnectorType);
        }

        Logger.LogDebug("Detecting schema for JSON file: {FilePath}", _filePath);

        var schema = new DataSchema
        {
            Name = Path.GetFileNameWithoutExtension(_filePath)
        };

        try
        {
            using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
            using var document = await JsonDocument.ParseAsync(fileStream, cancellationToken: cancellationToken);

            JsonElement sampleElement;

            if (document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0)
            {
                sampleElement = document.RootElement[0];
            }
            else if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                sampleElement = document.RootElement;
            }
            else
            {
                throw new InvalidOperationException("JSON file does not contain objects or arrays of objects");
            }

            // Extract field information from the sample element
            foreach (var property in sampleElement.EnumerateObject())
            {
                var dataType = GetDataTypeFromJsonElement(property.Value);

                schema.Fields.Add(new DataField
                {
                    Name = property.Name,
                    DataType = dataType,
                    IsRequired = false // JSON fields are generally optional
                });
            }

            _schema = schema;
            Logger.LogDebug("Detected {FieldCount} fields in JSON schema", schema.Fields.Count);

            return schema;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error detecting schema for JSON file: {FilePath}", _filePath);
            throw ConnectorException.CreateReadFailure($"Failed to detect JSON schema: {ex.Message}", Id, ConnectorType);
        }
    }

    /// <inheritdoc />
    public async Task<WriteResult> WriteAsync(IAsyncEnumerable<DataRecord> data, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (string.IsNullOrEmpty(_filePath))
        {
            throw ConnectorException.CreateWriteFailure("File path not specified", Id, ConnectorType);
        }

        Logger.LogInformation("Writing to JSON file: {FilePath}", _filePath);

        var recordsWritten = 0L;

        try
        {
            if (_isArrayFormat)
            {
                recordsWritten = await WriteJsonArrayAsync(data, cancellationToken);
            }
            else
            {
                recordsWritten = await WriteJsonLinesAsync(data, cancellationToken);
            }

            Logger.LogInformation("Completed writing to JSON file: {FilePath}, Records: {RecordsWritten}", _filePath, recordsWritten);

            return new WriteResult
            {
                IsSuccessful = true,
                RecordsWritten = recordsWritten,
                Message = $"Successfully wrote {recordsWritten} records to JSON file"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error writing to JSON file: {FilePath}", _filePath);

            throw ConnectorException.CreateWriteFailure(
                $"Failed to write to JSON file: {ex.Message}",
                Id,
                ConnectorType);
        }
    }

    /// <inheritdoc />
    public async Task<WriteResult> WriteBatchAsync(IEnumerable<DataRecord> batch, CancellationToken cancellationToken = default)
    {
        async IAsyncEnumerable<DataRecord> ConvertToAsyncEnumerable([EnumeratorCancellation] CancellationToken token)
        {
            foreach (var record in batch)
            {
                token.ThrowIfCancellationRequested();
                yield return record;
                await Task.CompletedTask; // optional: forces compiler to treat it as async
            }
        }

        return await WriteAsync(ConvertToAsyncEnumerable(cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task PrepareAsync(DataSchema schema, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_filePath))
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Logger.LogDebug("Created directory: {Directory}", directory);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task FinalizeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task<ConnectionTestResult> TestConnectionInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                return Task.FromResult(new ConnectionTestResult
                {
                    IsSuccessful = false,
                    Message = "File path not specified"
                });
            }

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                return Task.FromResult(new ConnectionTestResult
                {
                    IsSuccessful = false,
                    Message = $"Directory does not exist: {directory}"
                });
            }

            var canRead = File.Exists(_filePath);
            var canWrite = string.IsNullOrEmpty(directory) || Directory.Exists(directory);

            return Task.FromResult(new ConnectionTestResult
            {
                IsSuccessful = canRead || canWrite,
                Message = canRead ? "File exists and is readable" :
                         canWrite ? "Directory exists and is writable" :
                         "File does not exist and directory is not accessible"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ConnectionTestResult
            {
                IsSuccessful = false,
                Message = $"Connection test failed: {ex.Message}"
            });
        }
    }

    /// <inheritdoc />
    protected override Task OpenInternalAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task CloseInternalAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task<ConnectorMetadata> GetMetadataInternalAsync(CancellationToken cancellationToken)
    {
        var metadata = new ConnectorMetadata
        {
            Version = "1.0.0"
        };

        metadata.Properties["FilePath"] = _filePath ?? "";
        metadata.Properties["ArrayFormat"] = _isArrayFormat;
        metadata.Properties["Encoding"] = "UTF-8";

        if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
        {
            var fileInfo = new FileInfo(_filePath);
            metadata.Properties["FileSize"] = fileInfo.Length;
            metadata.Properties["LastModified"] = fileInfo.LastWriteTime;
        }

        return Task.FromResult(metadata);
    }

    /// <inheritdoc />
    protected override void ValidateConfigurationInternal(ValidationResult result)
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            result.AddError("File path must be specified in connection string", "ConnectionString");
        }
    }

    private void ExtractFilePathFromConnectionString(string connectionString)
    {
        if (connectionString.StartsWith("FilePath=", StringComparison.OrdinalIgnoreCase))
        {
            _filePath = connectionString.Substring("FilePath=".Length);
        }
        else
        {
            _filePath = connectionString;
        }
    }

    private JsonSerializerOptions CreateJsonOptions(IConnectorConfiguration configuration)
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = configuration.GetConnectionProperty<bool?>("writeIndented") ?? true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    private async IAsyncEnumerable<DataRecord> ReadJsonArrayAsync(Stream stream, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            var recordNumber = 0L;
            foreach (var element in document.RootElement.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return CreateDataRecordFromJsonElement(element, ++recordNumber);
            }
        }
        else if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            yield return CreateDataRecordFromJsonElement(document.RootElement, 1);
        }
    }

    private async IAsyncEnumerable<DataRecord> ReadJsonLinesAsync(Stream stream, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var recordNumber = 0L;

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            DataRecord? record = null;
            try
            {
                using var document = JsonDocument.Parse(line);
                record = CreateDataRecordFromJsonElement(document.RootElement, ++recordNumber);
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Error parsing JSON line {LineNumber}: {Line}", recordNumber + 1, line);
            }

            if (record != null)
            {
                yield return record;
            }
        }
    }

    private async Task<long> WriteJsonArrayAsync(IAsyncEnumerable<DataRecord> data, CancellationToken cancellationToken)
    {
        using var fileStream = new FileStream(_filePath!, FileMode.Create, FileAccess.Write);
        using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = _jsonOptions.WriteIndented });

        writer.WriteStartArray();

        var recordsWritten = 0L;
        await foreach (var record in data)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteDataRecordToJson(writer, record);
            recordsWritten++;

            if (recordsWritten % 1000 == 0)
            {
                Logger.LogDebug("Written {RecordsWritten} records to JSON file", recordsWritten);
            }
        }

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken);

        return recordsWritten;
    }

    private async Task<long> WriteJsonLinesAsync(IAsyncEnumerable<DataRecord> data, CancellationToken cancellationToken)
    {
        using var fileStream = new FileStream(_filePath!, FileMode.Create, FileAccess.Write);
        using var streamWriter = new StreamWriter(fileStream, Encoding.UTF8);

        var recordsWritten = 0L;
        await foreach (var record in data)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json = JsonSerializer.Serialize(record.Fields, _jsonOptions);
            await streamWriter.WriteLineAsync(json);
            recordsWritten++;

            if (recordsWritten % 1000 == 0)
            {
                Logger.LogDebug("Written {RecordsWritten} records to JSON file", recordsWritten);
            }
        }

        return recordsWritten;
    }

    private DataRecord CreateDataRecordFromJsonElement(JsonElement element, long recordNumber)
    {
        var record = new DataRecord
        {
            RowNumber = recordNumber,
            Source = _filePath
        };

        foreach (var property in element.EnumerateObject())
        {
            record.Fields[property.Name] = GetValueFromJsonElement(property.Value);
        }

        return record;
    }

    private void WriteDataRecordToJson(Utf8JsonWriter writer, DataRecord record)
    {
        writer.WriteStartObject();

        foreach (var field in record.Fields)
        {
            writer.WritePropertyName(field.Key);

            if (field.Value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, field.Value, field.Value.GetType(), _jsonOptions);
            }
        }

        writer.WriteEndObject();
    }

    private object? GetValueFromJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(GetValueFromJsonElement).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => GetValueFromJsonElement(p.Value)),
            _ => element.GetRawText()
        };
    }

    private Type GetDataTypeFromJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => typeof(string),
            JsonValueKind.Number => element.TryGetInt64(out _) ? typeof(long) : typeof(double),
            JsonValueKind.True or JsonValueKind.False => typeof(bool),
            JsonValueKind.Array => typeof(object[]),
            JsonValueKind.Object => typeof(Dictionary<string, object>),
            _ => typeof(object)
        };
    }
}
