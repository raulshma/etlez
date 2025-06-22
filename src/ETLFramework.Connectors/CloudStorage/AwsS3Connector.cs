using Amazon.S3;
using Amazon.S3.Model;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Connectors.CloudStorage;

/// <summary>
/// AWS S3 connector for cloud-based file operations.
/// </summary>
public class AwsS3Connector : BaseCloudStorageConnector
{
    private IAmazonS3? _s3Client;
    private readonly object _clientLock = new object();

    /// <summary>
    /// Initializes a new instance of the AwsS3Connector class.
    /// </summary>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    public AwsS3Connector(IConnectorConfiguration configuration, ILogger<AwsS3Connector> logger)
        : base(Guid.NewGuid(), configuration.Name, "AwsS3", configuration, logger)
    {
    }

    /// <inheritdoc />
    protected override async Task<ConnectionTestResult> TestConnectionInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = GetS3Client();
            
            // Test connection by listing buckets
            var response = await client.ListBucketsAsync(cancellationToken);
            
            return new ConnectionTestResult
            {
                IsSuccessful = true,
                Message = $"AWS S3 connection successful. Found {response.Buckets.Count} accessible buckets"
            };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult
            {
                IsSuccessful = false,
                Message = $"AWS S3 connection failed: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    protected override Task OpenInternalAsync(CancellationToken cancellationToken)
    {
        lock (_clientLock)
        {
            if (_s3Client == null)
            {
                _s3Client = CreateS3Client();
                Logger.LogDebug("AWS S3 client initialized");
            }
        }
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task CloseInternalAsync(CancellationToken cancellationToken)
    {
        lock (_clientLock)
        {
            _s3Client?.Dispose();
            _s3Client = null;
            Logger.LogDebug("AWS S3 client disposed");
        }
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<ConnectorMetadata> GetMetadataInternalAsync(CancellationToken cancellationToken)
    {
        var client = GetS3Client();
        var metadata = new ConnectorMetadata
        {
            Version = "1.0.0"
        };

        try
        {
            var bucketsResponse = await client.ListBucketsAsync(cancellationToken);
            metadata.Properties["AccessibleBuckets"] = bucketsResponse.Buckets.Count;
            metadata.Properties["ServiceUrl"] = client.Config.ServiceURL;
            metadata.Properties["Region"] = client.Config.RegionEndpoint?.SystemName ?? "Unknown";
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not retrieve AWS S3 service information");
        }

        return metadata;
    }

    /// <inheritdoc />
    protected override async Task<IEnumerable<CloudFile>> ListFilesAsync(string container, string? prefix = null, CancellationToken cancellationToken = default)
    {
        var client = GetS3Client();
        var files = new List<CloudFile>();

        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = container,
                Prefix = prefix,
                MaxKeys = 1000 // AWS S3 default limit
            };

            ListObjectsV2Response response;
            do
            {
                response = await client.ListObjectsV2Async(request, cancellationToken);

                foreach (var obj in response.S3Objects)
                {
                    var cloudFile = new CloudFile
                    {
                        Id = obj.Key,
                        Name = obj.Key,
                        Container = container,
                        Path = obj.Key,
                        Size = obj.Size ?? 0,
                        ContentType = "application/octet-stream", // S3 doesn't return content type in list
                        ETag = obj.ETag?.Trim('"'),
                        LastModified = obj.LastModified,
                        IsDirectory = obj.Key.EndsWith('/') && (obj.Size ?? 0) == 0,
                        StorageClass = obj.StorageClass?.Value
                    };

                    files.Add(cloudFile);
                }

                request.ContinuationToken = response.NextContinuationToken;
            }
            while (response.IsTruncated == true);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Logger.LogWarning("Bucket '{Bucket}' not found", container);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing objects in bucket '{Bucket}'", container);
            throw;
        }

        Logger.LogDebug("Listed {FileCount} objects from bucket '{Bucket}' with prefix '{Prefix}'", 
            files.Count, container, prefix);

        return files;
    }

    /// <inheritdoc />
    protected override async Task<CloudFile> DownloadFileAsync(CloudFile cloudFile, CancellationToken cancellationToken = default)
    {
        var client = GetS3Client();

        try
        {
            Logger.LogDebug("Downloading object: {ObjectKey}", cloudFile.Name);

            var request = new GetObjectRequest
            {
                BucketName = cloudFile.Container,
                Key = cloudFile.Name
            };

            var response = await client.GetObjectAsync(request, cancellationToken);
            var downloadedFile = cloudFile.CreateMetadataCopy();
            
            // Copy content to memory stream
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            
            downloadedFile.Content = memoryStream;
            downloadedFile.Size = memoryStream.Length;

            // Update metadata from response
            downloadedFile.ContentType = response.Headers.ContentType;
            downloadedFile.ContentEncoding = response.Headers.ContentEncoding;
            downloadedFile.ETag = response.ETag?.Trim('"');
            downloadedFile.LastModified = response.LastModified;

            // Add S3-specific metadata
            if (response.Metadata != null)
            {
                foreach (var key in response.Metadata.Keys)
                {
                    downloadedFile.Metadata[key] = response.Metadata[key];
                }
            }

            Logger.LogDebug("Downloaded object: {ObjectKey}, Size: {Size} bytes", cloudFile.Name, downloadedFile.Size);

            return downloadedFile;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Logger.LogWarning("Object '{ObjectKey}' not found in bucket '{Bucket}'", cloudFile.Name, cloudFile.Container);
            throw new FileNotFoundException($"Object '{cloudFile.Name}' not found in bucket '{cloudFile.Container}'");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error downloading object '{ObjectKey}' from bucket '{Bucket}'", cloudFile.Name, cloudFile.Container);
            throw;
        }
    }

    /// <inheritdoc />
    protected override async Task<CloudFile> UploadFileAsync(CloudFile cloudFile, CancellationToken cancellationToken = default)
    {
        var client = GetS3Client();

        try
        {
            Logger.LogDebug("Uploading object: {ObjectKey}, Size: {Size} bytes", cloudFile.Name, cloudFile.Size);

            if (cloudFile.Content == null)
            {
                throw new InvalidOperationException("CloudFile content cannot be null for upload");
            }

            var request = new PutObjectRequest
            {
                BucketName = cloudFile.Container,
                Key = cloudFile.Name,
                InputStream = cloudFile.Content,
                ContentType = cloudFile.ContentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

            // Set content encoding if specified
            if (!string.IsNullOrEmpty(cloudFile.ContentEncoding))
            {
                request.Headers.ContentEncoding = cloudFile.ContentEncoding;
            }

            // Add metadata
            if (cloudFile.Metadata.Count > 0)
            {
                foreach (var kvp in cloudFile.Metadata)
                {
                    request.Metadata.Add(kvp.Key, kvp.Value);
                }
            }

            // Set storage class if specified
            if (!string.IsNullOrEmpty(Options.StorageClass))
            {
                try
                {
                    var storageClass = (S3StorageClass)Enum.Parse(typeof(S3StorageClass), Options.StorageClass, true);
                    request.StorageClass = storageClass;
                }
                catch (ArgumentException)
                {
                    Logger.LogWarning("Invalid storage class specified: {StorageClass}", Options.StorageClass);
                }
            }

            // Upload the object
            cloudFile.Content.Position = 0;
            var response = await client.PutObjectAsync(request, cancellationToken);

            // Update cloud file with response metadata
            cloudFile.ETag = response.ETag?.Trim('"');
            cloudFile.Url = $"https://{cloudFile.Container}.s3.amazonaws.com/{cloudFile.Name}";

            Logger.LogDebug("Uploaded object: {ObjectKey}, ETag: {ETag}", cloudFile.Name, cloudFile.ETag);

            return cloudFile;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uploading object '{ObjectKey}' to bucket '{Bucket}'", cloudFile.Name, cloudFile.Container);
            throw;
        }
    }

    /// <inheritdoc />
    protected override async Task<bool> DeleteFileAsync(CloudFile cloudFile, CancellationToken cancellationToken = default)
    {
        var client = GetS3Client();

        try
        {
            Logger.LogDebug("Deleting object: {ObjectKey}", cloudFile.Name);

            var request = new DeleteObjectRequest
            {
                BucketName = cloudFile.Container,
                Key = cloudFile.Name
            };

            await client.DeleteObjectAsync(request, cancellationToken);
            
            Logger.LogDebug("Deleted object: {ObjectKey}", cloudFile.Name);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting object '{ObjectKey}' from bucket '{Bucket}'", cloudFile.Name, cloudFile.Container);
            throw;
        }
    }

    /// <inheritdoc />
    protected override async Task<bool> ContainerExistsAsync(string container, CancellationToken cancellationToken = default)
    {
        var client = GetS3Client();

        try
        {
            await client.GetBucketLocationAsync(container, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error checking if bucket '{Bucket}' exists", container);
            return false;
        }
    }

    /// <inheritdoc />
    protected override async Task<bool> CreateContainerAsync(string container, CancellationToken cancellationToken = default)
    {
        var client = GetS3Client();

        try
        {
            // Check if bucket already exists
            if (await ContainerExistsAsync(container, cancellationToken))
            {
                Logger.LogDebug("Bucket '{Bucket}' already exists", container);
                return true;
            }

            Logger.LogDebug("Creating bucket: {Bucket}", container);

            var request = new PutBucketRequest
            {
                BucketName = container,
                UseClientRegion = true
            };

            await client.PutBucketAsync(request, cancellationToken);
            
            Logger.LogInformation("Created bucket: {Bucket}", container);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating bucket '{Bucket}'", container);
            throw;
        }
    }

    /// <inheritdoc />
    protected override void ValidateConfigurationInternal(ValidationResult result)
    {
        base.ValidateConfigurationInternal(result);

        // For AWS S3, we can use various authentication methods:
        // 1. Access Key + Secret Key in connection string
        // 2. IAM roles (for EC2/Lambda)
        // 3. AWS credentials file
        // 4. Environment variables

        var connectionString = Configuration.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Connection string is optional for AWS S3 if using IAM roles or environment variables
            Logger.LogDebug("No connection string provided, will use default AWS credential chain");
        }
        else
        {
            // Validate connection string format if provided
            try
            {
                var parts = connectionString.Split(';');
                var hasAccessKey = parts.Any(p => p.StartsWith("AccessKey=", StringComparison.OrdinalIgnoreCase));
                var hasSecretKey = parts.Any(p => p.StartsWith("SecretKey=", StringComparison.OrdinalIgnoreCase));

                if (hasAccessKey && !hasSecretKey)
                {
                    result.AddError("SecretKey is required when AccessKey is specified", nameof(Configuration.ConnectionString));
                }
                else if (!hasAccessKey && hasSecretKey)
                {
                    result.AddError("AccessKey is required when SecretKey is specified", nameof(Configuration.ConnectionString));
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Invalid AWS S3 connection string format: {ex.Message}", nameof(Configuration.ConnectionString));
            }
        }
    }

    /// <summary>
    /// Gets or creates the S3 client.
    /// </summary>
    /// <returns>The S3 client</returns>
    private IAmazonS3 GetS3Client()
    {
        if (_s3Client == null)
        {
            lock (_clientLock)
            {
                _s3Client ??= CreateS3Client();
            }
        }

        return _s3Client;
    }

    /// <summary>
    /// Creates a new S3 client.
    /// </summary>
    /// <returns>A new S3 client</returns>
    private IAmazonS3 CreateS3Client()
    {
        var connectionString = Configuration.ConnectionString;
        
        var config = new AmazonS3Config
        {
            MaxErrorRetry = 3,
            Timeout = TimeSpan.FromMinutes(5)
        };

        // Parse connection string if provided
        if (!string.IsNullOrEmpty(connectionString))
        {
            var parts = connectionString.Split(';');
            string? accessKey = null;
            string? secretKey = null;

            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();

                    switch (key.ToLowerInvariant())
                    {
                        case "accesskey":
                            accessKey = value;
                            break;
                        case "secretkey":
                            secretKey = value;
                            break;
                        case "region":
                            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(value);
                            break;
                        case "serviceurl":
                            config.ServiceURL = value;
                            break;
                    }
                }
            }

            // Create client with explicit credentials if provided
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
                return new AmazonS3Client(credentials, config);
            }
        }

        // Use default credential chain (IAM roles, environment variables, etc.)
        return new AmazonS3Client(config);
    }

    /// <summary>
    /// Creates an AWS S3 connector with explicit credentials.
    /// </summary>
    /// <param name="name">The connector name</param>
    /// <param name="accessKey">The AWS access key</param>
    /// <param name="secretKey">The AWS secret key</param>
    /// <param name="region">The AWS region</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A configured AWS S3 connector</returns>
    public static AwsS3Connector CreateWithCredentials(string name, string accessKey, string secretKey, string region, ILogger<AwsS3Connector> logger)
    {
        var connectionString = $"AccessKey={accessKey};SecretKey={secretKey};Region={region}";
        
        var config = ConnectorFactory.CreateTestConfiguration(
            "AwsS3",
            name,
            connectionString,
            new Dictionary<string, object>
            {
                ["createContainerIfNotExists"] = true
            });

        return new AwsS3Connector(config, logger);
    }

    /// <summary>
    /// Creates an AWS S3 connector using default credential chain.
    /// </summary>
    /// <param name="name">The connector name</param>
    /// <param name="region">The AWS region</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A configured AWS S3 connector</returns>
    public static AwsS3Connector CreateWithDefaultCredentials(string name, string region, ILogger<AwsS3Connector> logger)
    {
        var connectionString = $"Region={region}";
        
        var config = ConnectorFactory.CreateTestConfiguration(
            "AwsS3",
            name,
            connectionString,
            new Dictionary<string, object>
            {
                ["createContainerIfNotExists"] = true
            });

        return new AwsS3Connector(config, logger);
    }
}
