using System.Text;

namespace ETLFramework.Connectors.CloudStorage;

/// <summary>
/// Represents a file or blob in cloud storage with its metadata and content.
/// </summary>
public class CloudFile
{
    /// <summary>
    /// Initializes a new instance of the CloudFile class.
    /// </summary>
    public CloudFile()
    {
        Metadata = new Dictionary<string, string>();
        Tags = new Dictionary<string, string>();
    }

    /// <summary>
    /// Gets or sets the unique identifier of the file.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the file (including path/prefix).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container or bucket name.
    /// </summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path/key of the file in cloud storage.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the content type/MIME type of the file.
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Gets or sets the content encoding of the file.
    /// </summary>
    public string? ContentEncoding { get; set; }

    /// <summary>
    /// Gets or sets the ETag of the file for versioning and caching.
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// Gets or sets the last modified date and time.
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// Gets or sets the creation date and time.
    /// </summary>
    public DateTimeOffset? CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets custom metadata associated with the file.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Gets or sets tags associated with the file.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; }

    /// <summary>
    /// Gets or sets the content stream of the file.
    /// </summary>
    public Stream? Content { get; set; }

    /// <summary>
    /// Gets or sets the URL to access the file.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets whether the file is a directory/folder.
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the storage class or tier (e.g., Hot, Cool, Archive).
    /// </summary>
    public string? StorageClass { get; set; }

    /// <summary>
    /// Gets or sets the server-side encryption information.
    /// </summary>
    public string? Encryption { get; set; }

    /// <summary>
    /// Gets the file extension from the name.
    /// </summary>
    public string Extension => System.IO.Path.GetExtension(Name);

    /// <summary>
    /// Gets the file name without path.
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(Name);

    /// <summary>
    /// Gets the directory path of the file.
    /// </summary>
    public string Directory => System.IO.Path.GetDirectoryName(Name) ?? string.Empty;

    /// <summary>
    /// Reads the content as a string using the specified encoding.
    /// </summary>
    /// <param name="encoding">The encoding to use (defaults to UTF-8)</param>
    /// <returns>The content as a string</returns>
    public async Task<string> ReadAsStringAsync(Encoding? encoding = null)
    {
        if (Content == null)
            return string.Empty;

        encoding ??= Encoding.UTF8;
        
        using var reader = new StreamReader(Content, encoding, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        
        // Reset stream position for potential reuse
        if (Content.CanSeek)
            Content.Position = 0;
            
        return content;
    }

    /// <summary>
    /// Reads the content as a byte array.
    /// </summary>
    /// <returns>The content as a byte array</returns>
    public async Task<byte[]> ReadAsBytesAsync()
    {
        if (Content == null)
            return Array.Empty<byte>();

        using var memoryStream = new MemoryStream();
        await Content.CopyToAsync(memoryStream);
        
        // Reset stream position for potential reuse
        if (Content.CanSeek)
            Content.Position = 0;
            
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Sets the content from a string using the specified encoding.
    /// </summary>
    /// <param name="content">The content string</param>
    /// <param name="encoding">The encoding to use (defaults to UTF-8)</param>
    public void SetContent(string content, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(content);
        Content = new MemoryStream(bytes);
        Size = bytes.Length;
        
        if (string.IsNullOrEmpty(ContentType))
        {
            ContentType = "text/plain";
        }
    }

    /// <summary>
    /// Sets the content from a byte array.
    /// </summary>
    /// <param name="content">The content bytes</param>
    public void SetContent(byte[] content)
    {
        Content = new MemoryStream(content);
        Size = content.Length;
    }

    /// <summary>
    /// Sets the content from a stream.
    /// </summary>
    /// <param name="content">The content stream</param>
    /// <param name="size">The size of the content (optional)</param>
    public void SetContent(Stream content, long? size = null)
    {
        Content = content;
        Size = size ?? (content.CanSeek ? content.Length : 0);
    }

    /// <summary>
    /// Determines the content type based on the file extension.
    /// </summary>
    /// <returns>The inferred content type</returns>
    public string InferContentType()
    {
        var extension = Extension.ToLowerInvariant();
        
        return extension switch
        {
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Creates a copy of the CloudFile with the same metadata but without content.
    /// </summary>
    /// <returns>A copy of the CloudFile without content</returns>
    public CloudFile CreateMetadataCopy()
    {
        return new CloudFile
        {
            Id = Id,
            Name = Name,
            Container = Container,
            Path = Path,
            Size = Size,
            ContentType = ContentType,
            ContentEncoding = ContentEncoding,
            ETag = ETag,
            LastModified = LastModified,
            CreatedOn = CreatedOn,
            Metadata = new Dictionary<string, string>(Metadata),
            Tags = new Dictionary<string, string>(Tags),
            Url = Url,
            IsDirectory = IsDirectory,
            StorageClass = StorageClass,
            Encryption = Encryption
        };
    }

    /// <summary>
    /// Disposes the content stream if it exists.
    /// </summary>
    public void Dispose()
    {
        Content?.Dispose();
        Content = null;
    }

    /// <summary>
    /// Returns a string representation of the CloudFile.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        return $"CloudFile[{Container}/{Name}, Size={Size}, Type={ContentType}]";
    }
}

/// <summary>
/// Represents the result of a cloud storage operation.
/// </summary>
public class CloudStorageResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of files processed.
    /// </summary>
    public long FilesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total bytes transferred.
    /// </summary>
    public long BytesTransferred { get; set; }

    /// <summary>
    /// Gets or sets the duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets any errors that occurred during the operation.
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of processed files.
    /// </summary>
    public List<CloudFile> ProcessedFiles { get; set; } = new List<CloudFile>();
}

/// <summary>
/// Represents options for cloud storage operations.
/// </summary>
public class CloudStorageOptions
{
    /// <summary>
    /// Gets or sets the maximum number of concurrent operations.
    /// </summary>
    public int MaxConcurrency { get; set; } = 5;

    /// <summary>
    /// Gets or sets the timeout for individual operations.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether to overwrite existing files.
    /// </summary>
    public bool OverwriteExisting { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to create containers/buckets if they don't exist.
    /// </summary>
    public bool CreateContainerIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets the buffer size for streaming operations.
    /// </summary>
    public int BufferSize { get; set; } = 64 * 1024; // 64KB

    /// <summary>
    /// Gets or sets whether to preserve file metadata.
    /// </summary>
    public bool PreserveMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the storage class/tier for uploaded files.
    /// </summary>
    public string? StorageClass { get; set; }
}
