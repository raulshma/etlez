using System.Text;
using System.Xml;
using System.Xml.Linq;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Connectors.FileSystem;

/// <summary>
/// XML file connector that can read from and write to XML files.
/// Supports configurable root and record element names.
/// </summary>
public class XmlConnector : BaseConnector, ISourceConnector<DataRecord>, IDestinationConnector<DataRecord>
{
    private string? _filePath;
    private string _rootElementName;
    private string _recordElementName;
    private DataSchema? _schema;

    /// <summary>
    /// Initializes a new instance of the XmlConnector class.
    /// </summary>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    public XmlConnector(IConnectorConfiguration configuration, ILogger<XmlConnector> logger)
        : base(Guid.NewGuid(), configuration.Name, "XML", configuration, logger)
    {
        ExtractFilePathFromConnectionString(configuration.ConnectionString);
        _rootElementName = configuration.GetConnectionProperty<string>("rootElement") ?? "root";
        _recordElementName = configuration.GetConnectionProperty<string>("recordElement") ?? "record";
    }

    /// <inheritdoc />
    public WriteMode[] SupportedWriteModes => new[] { WriteMode.Insert, WriteMode.Replace };

    /// <inheritdoc />
    public async IAsyncEnumerable<DataRecord> ReadAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            throw ConnectorException.CreateReadFailure($"XML file not found: {_filePath}", Id, ConnectorType);
        }

        Logger.LogInformation("Reading XML file: {FilePath}", _filePath);

        XDocument document;
        try
        {
            document = await LoadXmlDocumentAsync(_filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error reading XML file: {FilePath}", _filePath);
            throw ConnectorException.CreateReadFailure($"Failed to read XML file: {ex.Message}", Id, ConnectorType);
        }

        var recordNumber = 0L;
        foreach (var element in document.Descendants(_recordElementName))
        {
            cancellationToken.ThrowIfCancellationRequested();

            DataRecord? record = null;
            try
            {
                record = CreateDataRecordFromXmlElement(element, ++recordNumber);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error reading XML record at position {RecordNumber}: {Error}", recordNumber, ex.Message);
            }

            if (record != null)
            {
                yield return record;
            }
        }

        Logger.LogInformation("Completed reading XML file: {FilePath}, Records: {RecordCount}", _filePath, recordNumber);
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
            var document = await LoadXmlDocumentAsync(_filePath, cancellationToken);
            return document.Descendants(_recordElementName).Count();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not estimate record count for XML file: {FilePath}", _filePath);
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
            throw ConnectorException.CreateReadFailure($"XML file not found: {_filePath}", Id, ConnectorType);
        }

        Logger.LogDebug("Detecting schema for XML file: {FilePath}", _filePath);

        try
        {
            var document = await LoadXmlDocumentAsync(_filePath, cancellationToken);
            var schema = new DataSchema
            {
                Name = Path.GetFileNameWithoutExtension(_filePath)
            };

            var sampleElement = document.Descendants(_recordElementName).FirstOrDefault();
            if (sampleElement != null)
            {
                // Extract field information from attributes and child elements
                foreach (var attribute in sampleElement.Attributes())
                {
                    schema.Fields.Add(new DataField
                    {
                        Name = $"@{attribute.Name.LocalName}",
                        DataType = typeof(string),
                        IsRequired = false
                    });
                }

                foreach (var childElement in sampleElement.Elements())
                {
                    schema.Fields.Add(new DataField
                    {
                        Name = childElement.Name.LocalName,
                        DataType = typeof(string),
                        IsRequired = false
                    });
                }
            }

            _schema = schema;
            Logger.LogDebug("Detected {FieldCount} fields in XML schema", schema.Fields.Count);

            return schema;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error detecting schema for XML file: {FilePath}", _filePath);
            throw ConnectorException.CreateReadFailure($"Failed to detect XML schema: {ex.Message}", Id, ConnectorType);
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

        Logger.LogInformation("Writing to XML file: {FilePath}", _filePath);

        var recordsWritten = 0L;

        try
        {
            var rootElement = new XElement(_rootElementName);

            await foreach (var record in data)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var recordElement = CreateXmlElementFromDataRecord(record);
                rootElement.Add(recordElement);
                recordsWritten++;

                if (recordsWritten % 1000 == 0)
                {
                    Logger.LogDebug("Written {RecordsWritten} records to XML file", recordsWritten);
                }
            }

            var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), rootElement);
            
            using var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            await document.SaveAsync(fileStream, SaveOptions.None, cancellationToken);

            Logger.LogInformation("Completed writing to XML file: {FilePath}, Records: {RecordsWritten}", _filePath, recordsWritten);

            return new WriteResult
            {
                IsSuccessful = true,
                RecordsWritten = recordsWritten,
                Message = $"Successfully wrote {recordsWritten} records to XML file"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error writing to XML file: {FilePath}", _filePath);
            
            throw ConnectorException.CreateWriteFailure(
                $"Failed to write to XML file: {ex.Message}",
                Id,
                ConnectorType);
        }
    }

    /// <inheritdoc />
    public async Task<WriteResult> WriteBatchAsync(IEnumerable<DataRecord> batch, CancellationToken cancellationToken = default)
    {
        async IAsyncEnumerable<DataRecord> ConvertToAsyncEnumerable()
        {
            foreach (var record in batch)
            {
                yield return record;
            }
        }

        return await WriteAsync(ConvertToAsyncEnumerable(), cancellationToken);
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
        metadata.Properties["RootElement"] = _rootElementName;
        metadata.Properties["RecordElement"] = _recordElementName;
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

        if (string.IsNullOrWhiteSpace(_rootElementName))
        {
            result.AddError("Root element name cannot be empty", "RootElement");
        }

        if (string.IsNullOrWhiteSpace(_recordElementName))
        {
            result.AddError("Record element name cannot be empty", "RecordElement");
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

    private async Task<XDocument> LoadXmlDocumentAsync(string filePath, CancellationToken cancellationToken)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken);
    }

    private DataRecord CreateDataRecordFromXmlElement(XElement element, long recordNumber)
    {
        var record = new DataRecord
        {
            RowNumber = recordNumber,
            Source = _filePath
        };

        // Add attributes with @ prefix
        foreach (var attribute in element.Attributes())
        {
            record.Fields[$"@{attribute.Name.LocalName}"] = attribute.Value;
        }

        // Add child elements
        foreach (var childElement in element.Elements())
        {
            record.Fields[childElement.Name.LocalName] = childElement.Value;
        }

        // If no child elements, use the element value directly
        if (!element.HasElements && !string.IsNullOrEmpty(element.Value))
        {
            record.Fields["_value"] = element.Value;
        }

        return record;
    }

    private XElement CreateXmlElementFromDataRecord(DataRecord record)
    {
        var element = new XElement(_recordElementName);

        foreach (var field in record.Fields)
        {
            var value = field.Value?.ToString() ?? "";

            if (field.Key.StartsWith("@"))
            {
                // Attribute
                var attributeName = field.Key.Substring(1);
                element.SetAttributeValue(attributeName, value);
            }
            else if (field.Key == "_value")
            {
                // Element value
                element.Value = value;
            }
            else
            {
                // Child element
                element.Add(new XElement(field.Key, value));
            }
        }

        return element;
    }
}
