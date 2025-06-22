using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Connectors.CloudStorage;

/// <summary>
/// Azure Blob Storage connector for cloud-based file operations.
/// </summary>
public class AzureBlobConnector : BaseCloudStorageConnector
{
    private BlobServiceClient? _blobServiceClient;
    private readonly object _clientLock = new object();

    /// <summary>
    /// Initializes a new instance of the AzureBlobConnector class.
    /// </summary>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    public AzureBlobConnector(IConnectorConfiguration configuration, ILogger<AzureBlobConnector> logger)
        : base(Guid.NewGuid(), configuration.Name, "AzureBlob", configuration, logger)
    {
    }

    /// <inheritdoc />
    protected override async Task<ConnectionTestResult> TestConnectionInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = GetBlobServiceClient();
            
            // Test connection by getting account info
            var accountInfo = await client.GetAccountInfoAsync(cancellationToken);
            
            return new ConnectionTestResult
            {
                IsSuccessful = true,
                Message = $"Azure Blob Storage connection successful. Account Kind: {accountInfo.Value.AccountKind}"
            };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult
            {
                IsSuccessful = false,
                Message = $"Azure Blob Storage connection failed: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    protected override Task OpenInternalAsync(CancellationToken cancellationToken)
    {
        lock (_clientLock)
        {
            if (_blobServiceClient == null)
            {
                _blobServiceClient = CreateBlobServiceClient();
                Logger.LogDebug("Azure Blob Storage client initialized");
            }
        }
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task CloseInternalAsync(CancellationToken cancellationToken)
    {
        lock (_clientLock)
        {
            _blobServiceClient = null;
            Logger.LogDebug("Azure Blob Storage client disposed");
        }
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<ConnectorMetadata> GetMetadataInternalAsync(CancellationToken cancellationToken)
    {
        var client = GetBlobServiceClient();
        var metadata = new ConnectorMetadata
        {
            Version = "1.0.0"
        };

        try
        {
            var accountInfo = await client.GetAccountInfoAsync(cancellationToken);
            metadata.Properties["AccountKind"] = accountInfo.Value.AccountKind.ToString();
            metadata.Properties["SkuName"] = accountInfo.Value.SkuName.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not retrieve Azure Blob Storage account information");
        }

        return metadata;
    }

    /// <inheritdoc />
    protected override async Task<IEnumerable<CloudFile>> ListFilesAsync(string container, string? prefix = null, CancellationToken cancellationToken = default)
    {
        var client = GetBlobServiceClient();
        var containerClient = client.GetBlobContainerClient(container);

        var files = new List<CloudFile>();

        try
        {
            var blobs = containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken);
            
            await foreach (var blob in blobs)
            {
                var cloudFile = new CloudFile
                {
                    Id = blob.Name,
                    Name = blob.Name,
                    Container = container,
                    Path = blob.Name,
                    Size = blob.Properties.ContentLength ?? 0,
                    ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                    ContentEncoding = blob.Properties.ContentEncoding,
                    ETag = blob.Properties.ETag?.ToString(),
                    LastModified = blob.Properties.LastModified,
                    CreatedOn = blob.Properties.CreatedOn,
                    IsDirectory = false,
                    StorageClass = blob.Properties.AccessTier?.ToString(),
                    Encryption = blob.Properties.ServerEncrypted == true ? "AES256" : null
                };

                // Add metadata
                if (blob.Metadata != null)
                {
                    foreach (var kvp in blob.Metadata)
                    {
                        cloudFile.Metadata[kvp.Key] = kvp.Value;
                    }
                }

                // Add tags if available
                if (blob.Tags != null)
                {
                    foreach (var kvp in blob.Tags)
                    {
                        cloudFile.Tags[kvp.Key] = kvp.Value;
                    }
                }

                files.Add(cloudFile);
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            Logger.LogWarning("Container '{Container}' not found", container);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing files in container '{Container}'", container);
            throw;
        }

        Logger.LogDebug("Listed {FileCount} files from container '{Container}' with prefix '{Prefix}'", 
            files.Count, container, prefix);

        return files;
    }

    /// <inheritdoc />
    protected override async Task<CloudFile> DownloadFileAsync(CloudFile cloudFile, CancellationToken cancellationToken = default)
    {
        var client = GetBlobServiceClient();
        var containerClient = client.GetBlobContainerClient(cloudFile.Container);
        var blobClient = containerClient.GetBlobClient(cloudFile.Name);

        try
        {
            Logger.LogDebug("Downloading blob: {BlobName}", cloudFile.Name);

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            var downloadedFile = cloudFile.CreateMetadataCopy();
            
            // Copy content to memory stream
            var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            
            downloadedFile.Content = memoryStream;
            downloadedFile.Size = memoryStream.Length;

            // Update metadata from response
            var properties = response.Value.Details;
            downloadedFile.ContentType = properties.ContentType;
            downloadedFile.ContentEncoding = properties.ContentEncoding;
            downloadedFile.ETag = properties.ETag.ToString();
            downloadedFile.LastModified = properties.LastModified;

            Logger.LogDebug("Downloaded blob: {BlobName}, Size: {Size} bytes", cloudFile.Name, downloadedFile.Size);

            return downloadedFile;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            Logger.LogWarning("Blob '{BlobName}' not found in container '{Container}'", cloudFile.Name, cloudFile.Container);
            throw new FileNotFoundException($"Blob '{cloudFile.Name}' not found in container '{cloudFile.Container}'");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error downloading blob '{BlobName}' from container '{Container}'", cloudFile.Name, cloudFile.Container);
            throw;
        }
    }

    /// <inheritdoc />
    protected override async Task<CloudFile> UploadFileAsync(CloudFile cloudFile, CancellationToken cancellationToken = default)
    {
        var client = GetBlobServiceClient();
        var containerClient = client.GetBlobContainerClient(cloudFile.Container);
        var blobClient = containerClient.GetBlobClient(cloudFile.Name);

        try
        {
            Logger.LogDebug("Uploading blob: {BlobName}, Size: {Size} bytes", cloudFile.Name, cloudFile.Size);

            if (cloudFile.Content == null)
            {
                throw new InvalidOperationException("CloudFile content cannot be null for upload");
            }

            // Prepare upload options
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = cloudFile.ContentType,
                    ContentEncoding = cloudFile.ContentEncoding
                },
                Metadata = cloudFile.Metadata.Count > 0 ? cloudFile.Metadata : null,
                Tags = cloudFile.Tags.Count > 0 ? cloudFile.Tags : null
            };

            // Set access tier if specified
            if (!string.IsNullOrEmpty(Options.StorageClass))
            {
                if (Enum.TryParse<AccessTier>(Options.StorageClass, true, out var accessTier))
                {
                    uploadOptions.AccessTier = accessTier;
                }
            }

            // Upload the blob
            cloudFile.Content.Position = 0;
            var response = await blobClient.UploadAsync(cloudFile.Content, uploadOptions, cancellationToken);

            // Update cloud file with response metadata
            cloudFile.ETag = response.Value.ETag.ToString();
            cloudFile.LastModified = response.Value.LastModified;
            cloudFile.Url = blobClient.Uri.ToString();

            Logger.LogDebug("Uploaded blob: {BlobName}, ETag: {ETag}", cloudFile.Name, cloudFile.ETag);

            return cloudFile;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uploading blob '{BlobName}' to container '{Container}'", cloudFile.Name, cloudFile.Container);
            throw;
        }
    }

    /// <inheritdoc />
    protected override async Task<bool> DeleteFileAsync(CloudFile cloudFile, CancellationToken cancellationToken = default)
    {
        var client = GetBlobServiceClient();
        var containerClient = client.GetBlobContainerClient(cloudFile.Container);
        var blobClient = containerClient.GetBlobClient(cloudFile.Name);

        try
        {
            Logger.LogDebug("Deleting blob: {BlobName}", cloudFile.Name);

            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            
            if (response.Value)
            {
                Logger.LogDebug("Deleted blob: {BlobName}", cloudFile.Name);
            }
            else
            {
                Logger.LogWarning("Blob '{BlobName}' was not found for deletion", cloudFile.Name);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting blob '{BlobName}' from container '{Container}'", cloudFile.Name, cloudFile.Container);
            throw;
        }
    }

    /// <inheritdoc />
    protected override async Task<bool> ContainerExistsAsync(string container, CancellationToken cancellationToken = default)
    {
        var client = GetBlobServiceClient();
        var containerClient = client.GetBlobContainerClient(container);

        try
        {
            var response = await containerClient.ExistsAsync(cancellationToken);
            return response.Value;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error checking if container '{Container}' exists", container);
            return false;
        }
    }

    /// <inheritdoc />
    protected override async Task<bool> CreateContainerAsync(string container, CancellationToken cancellationToken = default)
    {
        var client = GetBlobServiceClient();
        var containerClient = client.GetBlobContainerClient(container);

        try
        {
            Logger.LogDebug("Creating container: {Container}", container);

            var response = await containerClient.CreateIfNotExistsAsync(
                publicAccessType: PublicAccessType.None,
                cancellationToken: cancellationToken);

            if (response != null)
            {
                Logger.LogInformation("Created container: {Container}", container);
                return true;
            }
            else
            {
                Logger.LogDebug("Container '{Container}' already exists", container);
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating container '{Container}'", container);
            throw;
        }
    }

    /// <inheritdoc />
    protected override void ValidateConfigurationInternal(ValidationResult result)
    {
        base.ValidateConfigurationInternal(result);

        var connectionString = Configuration.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            result.AddError("Connection string is required for Azure Blob Storage connector", nameof(Configuration.ConnectionString));
            return;
        }

        // Validate connection string format
        try
        {
            var client = new BlobServiceClient(connectionString);
            // Basic validation - the constructor will throw if the connection string is invalid
        }
        catch (Exception ex)
        {
            result.AddError($"Invalid Azure Blob Storage connection string: {ex.Message}", nameof(Configuration.ConnectionString));
        }
    }

    /// <summary>
    /// Gets or creates the blob service client.
    /// </summary>
    /// <returns>The blob service client</returns>
    private BlobServiceClient GetBlobServiceClient()
    {
        if (_blobServiceClient == null)
        {
            lock (_clientLock)
            {
                _blobServiceClient ??= CreateBlobServiceClient();
            }
        }

        return _blobServiceClient;
    }

    /// <summary>
    /// Creates a new blob service client.
    /// </summary>
    /// <returns>A new blob service client</returns>
    private BlobServiceClient CreateBlobServiceClient()
    {
        var connectionString = Configuration.ConnectionString;
        
        var clientOptions = new BlobClientOptions();
        
        // Configure retry options
        clientOptions.Retry.MaxRetries = 3;
        clientOptions.Retry.Delay = TimeSpan.FromSeconds(1);
        clientOptions.Retry.MaxDelay = TimeSpan.FromSeconds(10);

        return new BlobServiceClient(connectionString, clientOptions);
    }

    /// <summary>
    /// Creates an Azure Blob Storage connector for development using Azurite emulator.
    /// </summary>
    /// <param name="name">The connector name</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A configured Azure Blob Storage connector</returns>
    public static AzureBlobConnector CreateForAzurite(string name, ILogger<AzureBlobConnector> logger)
    {
        // Azurite default connection string
        var connectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";
        
        var config = ConnectorFactory.CreateTestConfiguration(
            "AzureBlob",
            name,
            connectionString,
            new Dictionary<string, object>
            {
                ["createContainerIfNotExists"] = true
            });

        return new AzureBlobConnector(config, logger);
    }

    /// <summary>
    /// Creates an Azure Blob Storage connector with connection string.
    /// </summary>
    /// <param name="name">The connector name</param>
    /// <param name="connectionString">The Azure Storage connection string</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A configured Azure Blob Storage connector</returns>
    public static AzureBlobConnector CreateWithConnectionString(string name, string connectionString, ILogger<AzureBlobConnector> logger)
    {
        var config = ConnectorFactory.CreateTestConfiguration(
            "AzureBlob",
            name,
            connectionString,
            new Dictionary<string, object>
            {
                ["createContainerIfNotExists"] = true
            });

        return new AzureBlobConnector(config, logger);
    }
}
