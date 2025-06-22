using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using ETLFramework.Connectors.FileSystem;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace ETLFramework.Connectors.CloudStorage;

/// <summary>
/// Base implementation for cloud storage connectors providing common functionality.
/// </summary>
public abstract class BaseCloudStorageConnector : BaseConnector, ISourceConnector<DataRecord>, IDestinationConnector<DataRecord>
{
    private readonly Dictionary<string, IConnector> _fileConnectorCache;
    private CloudStorageOptions _options;

    /// <summary>
    /// Initializes a new instance of the BaseCloudStorageConnector class.
    /// </summary>
    /// <param name="id">The connector identifier</param>
    /// <param name="name">The connector name</param>
    /// <param name="connectorType">The connector type</param>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    protected BaseCloudStorageConnector(
        Guid id,
        string name,
        string connectorType,
        IConnectorConfiguration configuration,
        ILogger logger)
        : base(id, name, connectorType, configuration, logger)
    {
        _fileConnectorCache = new Dictionary<string, IConnector>();
        _options = CreateCloudStorageOptions();
    }

    /// <inheritdoc />
    public WriteMode[] SupportedWriteModes => new[] { WriteMode.Insert, WriteMode.Replace };

    /// <summary>
    /// Gets the cloud storage options.
    /// </summary>
    protected CloudStorageOptions Options => _options;

    /// <summary>
    /// Lists files in the specified container/bucket with optional prefix filtering.
    /// </summary>
    /// <param name="container">The container or bucket name</param>
    /// <param name="prefix">Optional prefix to filter files</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An enumerable of cloud files</returns>
    protected abstract Task<IEnumerable<CloudFile>> ListFilesAsync(string container, string? prefix = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from cloud storage.
    /// </summary>
    /// <param name="cloudFile">The cloud file to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The downloaded cloud file with content</returns>
    protected abstract Task<CloudFile> DownloadFileAsync(CloudFile cloudFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file to cloud storage.
    /// </summary>
    /// <param name="cloudFile">The cloud file to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The uploaded cloud file with updated metadata</returns>
    protected abstract Task<CloudFile> UploadFileAsync(CloudFile cloudFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from cloud storage.
    /// </summary>
    /// <param name="cloudFile">The cloud file to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file was deleted successfully</returns>
    protected abstract Task<bool> DeleteFileAsync(CloudFile cloudFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a container/bucket exists.
    /// </summary>
    /// <param name="container">The container or bucket name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the container exists</returns>
    protected abstract Task<bool> ContainerExistsAsync(string container, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a container/bucket if it doesn't exist.
    /// </summary>
    /// <param name="container">The container or bucket name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the container was created or already exists</returns>
    protected abstract Task<bool> CreateContainerAsync(string container, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public async IAsyncEnumerable<DataRecord> ReadAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var container = Configuration.GetConnectionProperty<string>("container") ??
                       Configuration.GetConnectionProperty<string>("bucket") ??
                       throw new ConnectorException("Container or bucket name must be specified");

        var prefix = Configuration.GetConnectionProperty<string>("prefix");
        var filePattern = Configuration.GetConnectionProperty<string>("filePattern") ?? "*";

        Logger.LogInformation("Reading files from cloud storage: {Container}, Prefix: {Prefix}", container, prefix);

        var files = await ListFilesAsync(container, prefix, cancellationToken);
        var filteredFiles = FilterFilesByPattern(files, filePattern);

        var recordNumber = 0L;
        foreach (var file in filteredFiles)
        {
            if (file.IsDirectory)
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            // Download the file content
            var downloadedFile = await DownloadFileAsync(file, cancellationToken);

            // Get appropriate file connector based on file extension
            var fileConnector = GetFileConnector(downloadedFile);

            if (fileConnector is ISourceConnector<DataRecord> sourceConnector)
            {
                // Read records from the downloaded file
                await foreach (var record in sourceConnector.ReadAsync(cancellationToken))
                {
                    // Enhance record with cloud file metadata
                    record.Source = $"{ConnectorType}:{container}/{file.Name}";
                    record.RowNumber = ++recordNumber;

                    // Add cloud file metadata to record
                    record.Fields["_CloudFile_Name"] = file.Name;
                    record.Fields["_CloudFile_Container"] = file.Container;
                    record.Fields["_CloudFile_Size"] = file.Size;
                    record.Fields["_CloudFile_LastModified"] = file.LastModified?.ToString("yyyy-MM-dd HH:mm:ss");

                    yield return record;
                }
            }

            // Clean up downloaded content
            downloadedFile.Dispose();
        }

        Logger.LogInformation("Completed reading from cloud storage: {Container}, Records: {RecordCount}", container, recordNumber);
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
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            var container = Configuration.GetConnectionProperty<string>("container") ??
                           Configuration.GetConnectionProperty<string>("bucket");

            if (string.IsNullOrEmpty(container))
                return null;

            var prefix = Configuration.GetConnectionProperty<string>("prefix");
            var filePattern = Configuration.GetConnectionProperty<string>("filePattern") ?? "*";

            var files = await ListFilesAsync(container, prefix, cancellationToken);
            var filteredFiles = FilterFilesByPattern(files, filePattern).Where(f => !f.IsDirectory);

            return filteredFiles.Count();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not estimate record count for cloud storage");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DataSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var container = Configuration.GetConnectionProperty<string>("container") ??
                       Configuration.GetConnectionProperty<string>("bucket") ??
                       throw new ConnectorException("Container or bucket name must be specified");

        var schema = new DataSchema
        {
            Name = $"{ConnectorType}_{container}"
        };

        // Add standard cloud file metadata fields
        schema.Fields.Add(new DataField { Name = "_CloudFile_Name", DataType = typeof(string), IsRequired = false });
        schema.Fields.Add(new DataField { Name = "_CloudFile_Container", DataType = typeof(string), IsRequired = false });
        schema.Fields.Add(new DataField { Name = "_CloudFile_Size", DataType = typeof(long), IsRequired = false });
        schema.Fields.Add(new DataField { Name = "_CloudFile_LastModified", DataType = typeof(string), IsRequired = false });

        try
        {
            // Try to get schema from the first file
            var prefix = Configuration.GetConnectionProperty<string>("prefix");
            var files = await ListFilesAsync(container, prefix, cancellationToken);
            var firstFile = files.FirstOrDefault(f => !f.IsDirectory);

            if (firstFile != null)
            {
                var downloadedFile = await DownloadFileAsync(firstFile, cancellationToken);
                var fileConnector = GetFileConnector(downloadedFile);

                if (fileConnector is ISourceConnector<DataRecord> sourceConnector)
                {
                    var fileSchema = await sourceConnector.GetSchemaAsync(cancellationToken);

                    // Merge file schema with cloud metadata schema
                    foreach (var field in fileSchema.Fields)
                    {
                        schema.Fields.Add(field);
                    }
                }

                downloadedFile.Dispose();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not determine detailed schema from files, using basic schema");
        }

        return schema;
    }

    /// <inheritdoc />
    public async Task<WriteResult> WriteAsync(IAsyncEnumerable<DataRecord> data, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var container = Configuration.GetConnectionProperty<string>("container") ??
                       Configuration.GetConnectionProperty<string>("bucket") ??
                       throw new ConnectorException("Container or bucket name must be specified");

        var fileName = Configuration.GetConnectionProperty<string>("fileName") ??
                      $"output_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

        var fileFormat = Configuration.GetConnectionProperty<string>("fileFormat") ?? "csv";

        Logger.LogInformation("Writing to cloud storage: {Container}/{FileName}", container, fileName);

        var recordsWritten = 0L;

        try
        {
            // Ensure container exists
            if (_options.CreateContainerIfNotExists)
            {
                await CreateContainerAsync(container, cancellationToken);
            }

            // Create appropriate file connector for the output format
            var fileConnector = CreateFileConnectorForFormat(fileFormat, fileName);

            if (fileConnector is IDestinationConnector<DataRecord> destinationConnector)
            {
                // Write data to memory stream using file connector
                using var memoryStream = new MemoryStream();

                // For now, use a simple approach - collect all records and write them
                var allRecords = new List<DataRecord>();
                await foreach (var record in data)
                {
                    allRecords.Add(record);
                    recordsWritten++;
                }

                // Write records to memory stream based on format
                await WriteRecordsToStreamAsync(allRecords, memoryStream, fileFormat, cancellationToken);

                // Create cloud file from memory stream
                memoryStream.Position = 0;
                var cloudFile = new CloudFile
                {
                    Name = fileName,
                    Container = container,
                    Path = fileName,
                    ContentType = GetContentTypeForFormat(fileFormat),
                    Content = new MemoryStream(memoryStream.ToArray())
                };

                // Upload to cloud storage
                await UploadFileAsync(cloudFile, cancellationToken);
                cloudFile.Dispose();
            }

            Logger.LogInformation("Completed writing to cloud storage: {Container}/{FileName}, Records: {RecordsWritten}",
                container, fileName, recordsWritten);

            return new WriteResult
            {
                IsSuccessful = true,
                RecordsWritten = recordsWritten,
                Message = $"Successfully wrote {recordsWritten} records to {container}/{fileName}"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error writing to cloud storage: {Container}/{FileName}", container, fileName);

            throw ConnectorException.CreateWriteFailure(
                $"Failed to write to cloud storage {container}/{fileName}: {ex.Message}",
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
    public async Task PrepareAsync(DataSchema schema, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var container = Configuration.GetConnectionProperty<string>("container") ??
                       Configuration.GetConnectionProperty<string>("bucket");

        if (!string.IsNullOrEmpty(container) && _options.CreateContainerIfNotExists)
        {
            await CreateContainerAsync(container, cancellationToken);
            Logger.LogDebug("Ensured container exists: {Container}", container);
        }
    }

    /// <inheritdoc />
    public Task FinalizeAsync(CancellationToken cancellationToken = default)
    {
        // Clean up file connector cache
        foreach (var connector in _fileConnectorCache.Values)
        {
            if (connector is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _fileConnectorCache.Clear();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates cloud storage options from configuration.
    /// </summary>
    /// <returns>Cloud storage options</returns>
    protected virtual CloudStorageOptions CreateCloudStorageOptions()
    {
        return new CloudStorageOptions
        {
            MaxConcurrency = Configuration.GetConnectionProperty<int?>("maxConcurrency") ?? 5,
            Timeout = TimeSpan.FromSeconds(Configuration.GetConnectionProperty<int?>("timeoutSeconds") ?? 300),
            OverwriteExisting = Configuration.GetConnectionProperty<bool?>("overwriteExisting") ?? true,
            CreateContainerIfNotExists = Configuration.GetConnectionProperty<bool?>("createContainerIfNotExists") ?? true,
            BufferSize = Configuration.GetConnectionProperty<int?>("bufferSize") ?? 64 * 1024,
            PreserveMetadata = Configuration.GetConnectionProperty<bool?>("preserveMetadata") ?? true,
            StorageClass = Configuration.GetConnectionProperty<string>("storageClass")
        };
    }

    /// <summary>
    /// Filters files by pattern matching.
    /// </summary>
    /// <param name="files">The files to filter</param>
    /// <param name="pattern">The pattern to match</param>
    /// <returns>Filtered files</returns>
    protected virtual IEnumerable<CloudFile> FilterFilesByPattern(IEnumerable<CloudFile> files, string pattern)
    {
        if (pattern == "*" || string.IsNullOrEmpty(pattern))
            return files;

        // Simple pattern matching - could be enhanced with regex
        return files.Where(f => f.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the appropriate file connector for a cloud file based on its extension.
    /// </summary>
    /// <param name="cloudFile">The cloud file</param>
    /// <returns>A file connector instance</returns>
    protected virtual IConnector GetFileConnector(CloudFile cloudFile)
    {
        var extension = cloudFile.Extension.ToLowerInvariant();
        var cacheKey = $"{extension}_{cloudFile.ContentType}";

        if (_fileConnectorCache.TryGetValue(cacheKey, out var cachedConnector))
        {
            return cachedConnector;
        }

        var config = CreateFileConnectorConfiguration(cloudFile);
        IConnector connector = extension switch
        {
            ".csv" => new CsvConnector(config, CreateTypedLogger<CsvConnector>()),
            ".json" => new JsonConnector(config, CreateTypedLogger<JsonConnector>()),
            ".xml" => new XmlConnector(config, CreateTypedLogger<XmlConnector>()),
            _ => new CsvConnector(config, CreateTypedLogger<CsvConnector>()) // Default to CSV
        };

        _fileConnectorCache[cacheKey] = connector;
        return connector;
    }

    /// <summary>
    /// Creates a file connector configuration for a cloud file.
    /// </summary>
    /// <param name="cloudFile">The cloud file</param>
    /// <returns>A connector configuration</returns>
    protected virtual IConnectorConfiguration CreateFileConnectorConfiguration(CloudFile cloudFile)
    {
        var config = ConnectorFactory.CreateTestConfiguration(
            GetConnectorTypeForFile(cloudFile),
            $"CloudFile_{cloudFile.Name}",
            cloudFile.Name);

        // Set content stream as the "file"
        config.SetConnectionProperty("contentStream", cloudFile.Content);

        return config;
    }

    /// <summary>
    /// Gets the connector type for a cloud file based on its extension.
    /// </summary>
    /// <param name="cloudFile">The cloud file</param>
    /// <returns>The connector type</returns>
    protected virtual string GetConnectorTypeForFile(CloudFile cloudFile)
    {
        return cloudFile.Extension.ToLowerInvariant() switch
        {
            ".csv" => "CSV",
            ".json" => "JSON",
            ".xml" => "XML",
            _ => "CSV"
        };
    }

    /// <summary>
    /// Creates a file connector for the specified format.
    /// </summary>
    /// <param name="format">The file format</param>
    /// <param name="fileName">The file name</param>
    /// <returns>A file connector instance</returns>
    protected virtual IConnector CreateFileConnectorForFormat(string format, string fileName)
    {
        var config = ConnectorFactory.CreateTestConfiguration(
            format.ToUpperInvariant(),
            $"Output_{format}",
            fileName);

        return format.ToLowerInvariant() switch
        {
            "csv" => new CsvConnector(config, CreateTypedLogger<CsvConnector>()),
            "json" => new JsonConnector(config, CreateTypedLogger<JsonConnector>()),
            "xml" => new XmlConnector(config, CreateTypedLogger<XmlConnector>()),
            _ => new CsvConnector(config, CreateTypedLogger<CsvConnector>())
        };
    }

    /// <summary>
    /// Creates a temporary file configuration for writing to a stream.
    /// </summary>
    /// <param name="format">The file format</param>
    /// <param name="stream">The target stream</param>
    /// <returns>A connector configuration</returns>
    protected virtual IConnectorConfiguration CreateTempFileConfiguration(string format, Stream stream)
    {
        var config = ConnectorFactory.CreateTestConfiguration(
            format.ToUpperInvariant(),
            "TempOutput",
            "temp");

        config.SetConnectionProperty("outputStream", stream);
        return config;
    }

    /// <summary>
    /// Creates a file connector for the specified configuration.
    /// </summary>
    /// <param name="config">The connector configuration</param>
    /// <returns>A file connector instance</returns>
    protected virtual IConnector CreateFileConnectorForConfiguration(IConnectorConfiguration config)
    {
        return config.ConnectorType.ToUpperInvariant() switch
        {
            "CSV" => new CsvConnector(config, CreateTypedLogger<CsvConnector>()),
            "JSON" => new JsonConnector(config, CreateTypedLogger<JsonConnector>()),
            "XML" => new XmlConnector(config, CreateTypedLogger<XmlConnector>()),
            _ => new CsvConnector(config, CreateTypedLogger<CsvConnector>())
        };
    }

    /// <summary>
    /// Gets the content type for the specified file format.
    /// </summary>
    /// <param name="format">The file format</param>
    /// <returns>The content type</returns>
    protected virtual string GetContentTypeForFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "csv" => "text/csv",
            "json" => "application/json",
            "xml" => "application/xml",
            _ => "text/plain"
        };
    }

    /// <summary>
    /// Writes records to a stream in the specified format.
    /// </summary>
    /// <param name="records">The records to write</param>
    /// <param name="stream">The target stream</param>
    /// <param name="format">The output format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual async Task WriteRecordsToStreamAsync(List<DataRecord> records, Stream stream, string format, CancellationToken cancellationToken)
    {
        using var writer = new StreamWriter(stream, leaveOpen: true);

        switch (format.ToLowerInvariant())
        {
            case "csv":
                await WriteCsvAsync(records, writer, cancellationToken);
                break;
            case "json":
                await WriteJsonAsync(records, writer, cancellationToken);
                break;
            case "xml":
                await WriteXmlAsync(records, writer, cancellationToken);
                break;
            default:
                await WriteCsvAsync(records, writer, cancellationToken);
                break;
        }

        await writer.FlushAsync();
    }

    /// <summary>
    /// Writes records as CSV format.
    /// </summary>
    /// <param name="records">The records to write</param>
    /// <param name="writer">The text writer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual async Task WriteCsvAsync(List<DataRecord> records, TextWriter writer, CancellationToken cancellationToken)
    {
        if (records.Count == 0) return;

        // Write header
        var fields = records[0].Fields.Keys.Where(k => !k.StartsWith("_CloudFile_")).ToList();
        await writer.WriteLineAsync(string.Join(",", fields.Select(EscapeCsvField)));

        // Write data rows
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = fields.Select(field =>
                record.Fields.TryGetValue(field, out var value) ?
                EscapeCsvField(value?.ToString() ?? "") : "");

            await writer.WriteLineAsync(string.Join(",", values));
        }
    }

    /// <summary>
    /// Writes records as JSON format.
    /// </summary>
    /// <param name="records">The records to write</param>
    /// <param name="writer">The text writer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual async Task WriteJsonAsync(List<DataRecord> records, TextWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteLineAsync("[");

        for (int i = 0; i < records.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = records[i];
            var fields = record.Fields.Where(kvp => !kvp.Key.StartsWith("_CloudFile_"));

            await writer.WriteAsync("  {");
            var fieldList = fields.ToList();

            for (int j = 0; j < fieldList.Count; j++)
            {
                var field = fieldList[j];
                var value = field.Value?.ToString() ?? "";
                await writer.WriteAsync($"\"{EscapeJsonString(field.Key)}\": \"{EscapeJsonString(value)}\"");

                if (j < fieldList.Count - 1)
                    await writer.WriteAsync(", ");
            }

            await writer.WriteAsync("}");
            if (i < records.Count - 1)
                await writer.WriteAsync(",");
            await writer.WriteLineAsync();
        }

        await writer.WriteLineAsync("]");
    }

    /// <summary>
    /// Writes records as XML format.
    /// </summary>
    /// <param name="records">The records to write</param>
    /// <param name="writer">The text writer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual async Task WriteXmlAsync(List<DataRecord> records, TextWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteLineAsync("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        await writer.WriteLineAsync("<Records>");

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await writer.WriteLineAsync("  <Record>");

            var fields = record.Fields.Where(kvp => !kvp.Key.StartsWith("_CloudFile_"));
            foreach (var field in fields)
            {
                var value = field.Value?.ToString() ?? "";
                await writer.WriteLineAsync($"    <{EscapeXmlElementName(field.Key)}>{EscapeXmlContent(value)}</{EscapeXmlElementName(field.Key)}>");
            }

            await writer.WriteLineAsync("  </Record>");
        }

        await writer.WriteLineAsync("</Records>");
    }

    /// <summary>
    /// Escapes a CSV field value.
    /// </summary>
    /// <param name="value">The value to escape</param>
    /// <returns>The escaped value</returns>
    protected virtual string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Escapes a JSON string value.
    /// </summary>
    /// <param name="value">The value to escape</param>
    /// <returns>The escaped value</returns>
    protected virtual string EscapeJsonString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
    }

    /// <summary>
    /// Escapes an XML element name.
    /// </summary>
    /// <param name="name">The element name</param>
    /// <returns>The escaped element name</returns>
    protected virtual string EscapeXmlElementName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Field";

        // Replace invalid XML element name characters
        return name.Replace(" ", "_")
                  .Replace("-", "_")
                  .Replace(".", "_");
    }

    /// <summary>
    /// Escapes XML content.
    /// </summary>
    /// <param name="value">The value to escape</param>
    /// <returns>The escaped value</returns>
    protected virtual string EscapeXmlContent(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&apos;");
    }

    /// <summary>
    /// Creates a typed logger for file connectors.
    /// </summary>
    /// <typeparam name="T">The logger type</typeparam>
    /// <returns>A typed logger</returns>
    protected virtual ILogger<T> CreateTypedLogger<T>()
    {
        return new TypedLoggerWrapper<T>(Logger);
    }
}

/// <summary>
/// A wrapper to convert ILogger to ILogger<T>.
/// </summary>
/// <typeparam name="T">The logger type</typeparam>
internal class TypedLoggerWrapper<T> : ILogger<T>
{
    private readonly ILogger _logger;

    public TypedLoggerWrapper(ILogger logger)
    {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
