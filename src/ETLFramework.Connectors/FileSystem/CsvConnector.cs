using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace ETLFramework.Connectors.FileSystem;

/// <summary>
/// CSV file connector that can read from and write to CSV files.
/// </summary>
public class CsvConnector : BaseConnector, ISourceConnector<DataRecord>, IDestinationConnector<DataRecord>
{
    private readonly CsvConfiguration _csvConfig;
    private string? _filePath;
    private DataSchema? _schema;

    /// <summary>
    /// Initializes a new instance of the CsvConnector class.
    /// </summary>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    public CsvConnector(IConnectorConfiguration configuration, ILogger<CsvConnector> logger)
        : base(Guid.NewGuid(), configuration.Name, "CSV", configuration, logger)
    {
        _csvConfig = CreateCsvConfiguration(configuration);
        ExtractFilePathFromConnectionString(configuration.ConnectionString);
    }

    /// <inheritdoc />
    public WriteMode[] SupportedWriteModes => new[] { WriteMode.Insert, WriteMode.Replace, WriteMode.Append };

    /// <inheritdoc />
    public async IAsyncEnumerable<DataRecord> ReadAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            throw ConnectorException.CreateReadFailure($"CSV file not found: {_filePath}", Id, ConnectorType);
        }

        Logger.LogInformation("Reading CSV file: {FilePath}", _filePath);

        using var reader = new StreamReader(_filePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, _csvConfig);

        // Read header if configured
        if (_csvConfig.HasHeaderRecord)
        {
            await csv.ReadAsync();
            csv.ReadHeader();
        }

        var recordNumber = 0L;
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            DataRecord? record = null;
            try
            {
                record = CreateDataRecordFromCsv(csv, ++recordNumber);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error reading CSV record at line {LineNumber}: {Error}", recordNumber + 1, ex.Message);

                // Continue reading unless it's a critical error
                if (ex is OutOfMemoryException or StackOverflowException)
                {
                    throw;
                }
            }

            if (record != null)
            {
                yield return record;
            }
        }

        Logger.LogInformation("Completed reading CSV file: {FilePath}, Records: {RecordCount}", _filePath, recordNumber);
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
                yield return batch.ToList(); // Create a copy
                batch.Clear();
            }
        }

        // Return remaining records
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
            // Quick estimation by counting lines
            var lineCount = 0L;
            using var reader = new StreamReader(_filePath, Encoding.UTF8);

            while (await reader.ReadLineAsync() != null)
            {
                lineCount++;
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Subtract header line if present
            if (_csvConfig.HasHeaderRecord && lineCount > 0)
            {
                lineCount--;
            }

            return lineCount;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not estimate record count for CSV file: {FilePath}", _filePath);
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
            throw ConnectorException.CreateReadFailure($"CSV file not found: {_filePath}", Id, ConnectorType);
        }

        Logger.LogDebug("Detecting schema for CSV file: {FilePath}", _filePath);

        using var reader = new StreamReader(_filePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, _csvConfig);

        var schema = new DataSchema
        {
            Name = Path.GetFileNameWithoutExtension(_filePath)
        };

        if (_csvConfig.HasHeaderRecord)
        {
            await csv.ReadAsync();
            csv.ReadHeader();

            foreach (var header in csv.HeaderRecord ?? Array.Empty<string>())
            {
                schema.Fields.Add(new DataField
                {
                    Name = header,
                    DataType = typeof(string), // CSV fields are initially treated as strings
                    IsRequired = false
                });
            }
        }
        else
        {
            // Read first record to determine field count
            if (await csv.ReadAsync())
            {
                for (int i = 0; i < csv.Parser.Count; i++)
                {
                    schema.Fields.Add(new DataField
                    {
                        Name = $"Column{i + 1}",
                        DataType = typeof(string),
                        IsRequired = false
                    });
                }
            }
        }

        _schema = schema;
        Logger.LogDebug("Detected {FieldCount} fields in CSV schema", schema.Fields.Count);

        return schema;
    }

    /// <inheritdoc />
    public async Task<WriteResult> WriteAsync(IAsyncEnumerable<DataRecord> data, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (string.IsNullOrEmpty(_filePath))
        {
            throw ConnectorException.CreateWriteFailure("File path not specified", Id, ConnectorType);
        }

        Logger.LogInformation("Writing to CSV file: {FilePath}", _filePath);

        var recordsWritten = 0L;
        var writeMode = Configuration.GetConnectionProperty<string>("writeMode") ?? "Replace";

        // Determine file mode based on write mode
        var fileMode = writeMode.Equals("Append", StringComparison.OrdinalIgnoreCase) ? FileMode.Append : FileMode.Create;

        try
        {
            using var writer = new StreamWriter(_filePath, append: fileMode == FileMode.Append, Encoding.UTF8);
            using var csv = new CsvWriter(writer, _csvConfig);

            var isFirstRecord = true;
            var headers = new List<string>();

            await foreach (var record in data)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (isFirstRecord)
                {
                    headers = record.Fields.Keys.ToList();

                    // Write header if configured and not appending
                    if (_csvConfig.HasHeaderRecord && fileMode != FileMode.Append)
                    {
                        foreach (var header in headers)
                        {
                            csv.WriteField(header);
                        }
                        await csv.NextRecordAsync();
                    }

                    isFirstRecord = false;
                }

                // Write record fields
                foreach (var header in headers)
                {
                    var value = record.Fields.TryGetValue(header, out var fieldValue) ? fieldValue?.ToString() ?? "" : "";
                    csv.WriteField(value);
                }

                await csv.NextRecordAsync();
                recordsWritten++;

                // Log progress every 1000 records
                if (recordsWritten % 1000 == 0)
                {
                    Logger.LogDebug("Written {RecordsWritten} records to CSV file", recordsWritten);
                }
            }

            Logger.LogInformation("Completed writing to CSV file: {FilePath}, Records: {RecordsWritten}", _filePath, recordsWritten);

            return new WriteResult
            {
                IsSuccessful = true,
                RecordsWritten = recordsWritten,
                Message = $"Successfully wrote {recordsWritten} records to CSV file"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error writing to CSV file: {FilePath}", _filePath);

            throw ConnectorException.CreateWriteFailure(
                $"Failed to write to CSV file: {ex.Message}",
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
        // Ensure directory exists
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
        // Nothing to finalize for CSV files
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

            // For read operations, check if file exists
            // For write operations, check if directory is writable
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
        // CSV files don't require explicit connection opening
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task CloseInternalAsync(CancellationToken cancellationToken)
    {
        // CSV files don't require explicit connection closing
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
        metadata.Properties["HasHeader"] = _csvConfig.HasHeaderRecord;
        metadata.Properties["Delimiter"] = _csvConfig.Delimiter;
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
        // Support formats like "FilePath=C:\data\file.csv" or just "C:\data\file.csv"
        if (connectionString.StartsWith("FilePath=", StringComparison.OrdinalIgnoreCase))
        {
            _filePath = connectionString.Substring("FilePath=".Length);
        }
        else
        {
            _filePath = connectionString;
        }
    }

    private CsvConfiguration CreateCsvConfiguration(IConnectorConfiguration configuration)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = configuration.GetConnectionProperty<bool?>("hasHeaders") ?? true,
            Delimiter = configuration.GetConnectionProperty<string>("delimiter") ?? ",",
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null, // Ignore bad data
            MissingFieldFound = null // Ignore missing fields
        };

        return config;
    }

    private DataRecord CreateDataRecordFromCsv(CsvReader csv, long recordNumber)
    {
        var record = new DataRecord
        {
            RowNumber = recordNumber,
            Source = _filePath
        };

        if (_csvConfig.HasHeaderRecord && csv.HeaderRecord != null)
        {
            // Use headers as field names
            for (int i = 0; i < csv.HeaderRecord.Length && i < csv.Parser.Count; i++)
            {
                var fieldName = csv.HeaderRecord[i];
                var fieldValue = csv.GetField(i);
                record.Fields[fieldName] = fieldValue;
            }
        }
        else
        {
            // Use column indices as field names
            for (int i = 0; i < csv.Parser.Count; i++)
            {
                var fieldName = $"Column{i + 1}";
                var fieldValue = csv.GetField(i);
                record.Fields[fieldName] = fieldValue;
            }
        }

        return record;
    }
}
