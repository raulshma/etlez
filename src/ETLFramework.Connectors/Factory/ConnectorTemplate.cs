using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;

namespace ETLFramework.Connectors.Factory;

/// <summary>
/// Provides pre-built configuration templates for common connector scenarios.
/// </summary>
public static class ConnectorTemplate
{
    /// <summary>
    /// Creates a CSV file connector template.
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <param name="hasHeaders">Whether the CSV has headers</param>
    /// <param name="delimiter">The delimiter character</param>
    /// <returns>A connector configuration</returns>
    public static IConnectorConfiguration CreateCsvTemplate(string filePath, bool hasHeaders = true, string delimiter = ",")
    {
        return ConnectorFactory.CreateTestConfiguration(
            "CSV",
            "CSV File Connector",
            filePath,
            new Dictionary<string, object>
            {
                ["hasHeaders"] = hasHeaders,
                ["delimiter"] = delimiter,
                ["encoding"] = "UTF-8",
                ["trimFields"] = true
            });
    }

    /// <summary>
    /// Creates a JSON file connector template.
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <param name="format">The JSON format (Array or Lines)</param>
    /// <returns>A connector configuration</returns>
    public static IConnectorConfiguration CreateJsonTemplate(string filePath, string format = "Array")
    {
        return ConnectorFactory.CreateTestConfiguration(
            "JSON",
            "JSON File Connector",
            filePath,
            new Dictionary<string, object>
            {
                ["format"] = format,
                ["encoding"] = "UTF-8",
                ["indented"] = true
            });
    }

    /// <summary>
    /// Creates an XML file connector template.
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <param name="rootElement">The root element name</param>
    /// <param name="recordElement">The record element name</param>
    /// <returns>A connector configuration</returns>
    public static IConnectorConfiguration CreateXmlTemplate(string filePath, string rootElement = "Root", string recordElement = "Record")
    {
        return ConnectorFactory.CreateTestConfiguration(
            "XML",
            "XML File Connector",
            filePath,
            new Dictionary<string, object>
            {
                ["rootElement"] = rootElement,
                ["recordElement"] = recordElement,
                ["encoding"] = "UTF-8"
            });
    }

    /// <summary>
    /// Creates a SQLite database connector template.
    /// </summary>
    /// <param name="databasePath">The database file path</param>
    /// <param name="tableName">The table name</param>
    /// <returns>A connector configuration</returns>
    public static IConnectorConfiguration CreateSqliteTemplate(string databasePath, string tableName)
    {
        var connectionString = databasePath.Equals(":memory:", StringComparison.OrdinalIgnoreCase) 
            ? "Data Source=:memory:" 
            : $"Data Source={databasePath}";

        return ConnectorFactory.CreateTestConfiguration(
            "SQLite",
            "SQLite Database Connector",
            connectionString,
            new Dictionary<string, object>
            {
                ["tableName"] = tableName,
                ["createTableIfNotExists"] = true,
                ["commandTimeout"] = 30
            });
    }

    /// <summary>
    /// Creates a SQL Server database connector template.
    /// </summary>
    /// <param name="server">The server name</param>
    /// <param name="database">The database name</param>
    /// <param name="tableName">The table name</param>
    /// <param name="integratedSecurity">Whether to use integrated security</param>
    /// <returns>A connector configuration</returns>
    public static IConnectorConfiguration CreateSqlServerTemplate(string server, string database, string tableName, bool integratedSecurity = true)
    {
        var connectionString = integratedSecurity
            ? $"Server={server};Database={database};Integrated Security=true;TrustServerCertificate=true"
            : $"Server={server};Database={database};TrustServerCertificate=true";

        return ConnectorFactory.CreateTestConfiguration(
            "SqlServer",
            "SQL Server Database Connector",
            connectionString,
            new Dictionary<string, object>
            {
                ["tableName"] = tableName,
                ["createTableIfNotExists"] = false,
                ["commandTimeout"] = 30,
                ["useConnectionPooling"] = true,
                ["maxPoolSize"] = 100
            });
    }

    /// <summary>
    /// Creates a MySQL database connector template.
    /// </summary>
    /// <param name="server">The server name</param>
    /// <param name="database">The database name</param>
    /// <param name="username">The username</param>
    /// <param name="password">The password</param>
    /// <param name="tableName">The table name</param>
    /// <returns>A connector configuration</returns>
    public static IConnectorConfiguration CreateMySqlTemplate(string server, string database, string username, string password, string tableName)
    {
        var connectionString = $"Server={server};Database={database};Uid={username};Pwd={password};";

        return ConnectorFactory.CreateTestConfiguration(
            "MySQL",
            "MySQL Database Connector",
            connectionString,
            new Dictionary<string, object>
            {
                ["tableName"] = tableName,
                ["createTableIfNotExists"] = false,
                ["commandTimeout"] = 30,
                ["useConnectionPooling"] = true,
                ["useSSL"] = false
            });
    }

    /// <summary>
    /// Creates an Azure Blob Storage connector template.
    /// </summary>
    /// <param name="connectionString">The Azure Storage connection string</param>
    /// <param name="containerName">The container name</param>
    /// <param name="prefix">Optional blob prefix</param>
    /// <returns>A connector configuration</returns>
    public static IConnectorConfiguration CreateAzureBlobTemplate(string connectionString, string containerName, string? prefix = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["container"] = containerName,
            ["createContainerIfNotExists"] = true,
            ["filePattern"] = "*",
            ["maxConcurrency"] = 5
        };

        if (!string.IsNullOrEmpty(prefix))
        {
            properties["prefix"] = prefix;
        }

        return ConnectorFactory.CreateTestConfiguration(
            "AzureBlob",
            "Azure Blob Storage Connector",
            connectionString,
            properties);
    }

    /// <summary>
    /// Creates an AWS S3 connector template.
    /// </summary>
    /// <param name="accessKey">The AWS access key</param>
    /// <param name="secretKey">The AWS secret key</param>
    /// <param name="region">The AWS region</param>
    /// <param name="bucketName">The bucket name</param>
    /// <param name="prefix">Optional object prefix</param>
    /// <returns>A connector configuration</returns>
    public static IConnectorConfiguration CreateAwsS3Template(string accessKey, string secretKey, string region, string bucketName, string? prefix = null)
    {
        var connectionString = $"AccessKey={accessKey};SecretKey={secretKey};Region={region}";

        var properties = new Dictionary<string, object>
        {
            ["bucket"] = bucketName,
            ["createContainerIfNotExists"] = true,
            ["filePattern"] = "*",
            ["maxConcurrency"] = 5
        };

        if (!string.IsNullOrEmpty(prefix))
        {
            properties["prefix"] = prefix;
        }

        return ConnectorFactory.CreateTestConfiguration(
            "AwsS3",
            "AWS S3 Connector",
            connectionString,
            properties);
    }

    /// <summary>
    /// Creates an AWS S3 connector template using default credentials.
    /// </summary>
    /// <param name="region">The AWS region</param>
    /// <param name="bucketName">The bucket name</param>
    /// <param name="prefix">Optional object prefix</param>
    /// <returns>A connector configuration</returns>
    public static IConnectorConfiguration CreateAwsS3DefaultCredentialsTemplate(string region, string bucketName, string? prefix = null)
    {
        var connectionString = $"Region={region}";

        var properties = new Dictionary<string, object>
        {
            ["bucket"] = bucketName,
            ["createContainerIfNotExists"] = true,
            ["filePattern"] = "*",
            ["maxConcurrency"] = 5
        };

        if (!string.IsNullOrEmpty(prefix))
        {
            properties["prefix"] = prefix;
        }

        return ConnectorFactory.CreateTestConfiguration(
            "AwsS3",
            "AWS S3 Connector (Default Credentials)",
            connectionString,
            properties);
    }

    /// <summary>
    /// Gets all available template types.
    /// </summary>
    /// <returns>A list of template types</returns>
    public static List<TemplateInfo> GetAvailableTemplates()
    {
        return new List<TemplateInfo>
        {
            new TemplateInfo
            {
                Name = "CSV",
                DisplayName = "CSV File",
                Description = "Comma-separated values file connector",
                Category = "File System",
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "filePath", Type = typeof(string), IsRequired = true, Description = "Path to the CSV file" },
                    new TemplateParameter { Name = "hasHeaders", Type = typeof(bool), IsRequired = false, DefaultValue = true, Description = "Whether the CSV has headers" },
                    new TemplateParameter { Name = "delimiter", Type = typeof(string), IsRequired = false, DefaultValue = ",", Description = "Field delimiter" }
                }
            },
            new TemplateInfo
            {
                Name = "JSON",
                DisplayName = "JSON File",
                Description = "JavaScript Object Notation file connector",
                Category = "File System",
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "filePath", Type = typeof(string), IsRequired = true, Description = "Path to the JSON file" },
                    new TemplateParameter { Name = "format", Type = typeof(string), IsRequired = false, DefaultValue = "Array", Description = "JSON format (Array or Lines)" }
                }
            },
            new TemplateInfo
            {
                Name = "XML",
                DisplayName = "XML File",
                Description = "Extensible Markup Language file connector",
                Category = "File System",
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "filePath", Type = typeof(string), IsRequired = true, Description = "Path to the XML file" },
                    new TemplateParameter { Name = "rootElement", Type = typeof(string), IsRequired = false, DefaultValue = "Root", Description = "Root element name" },
                    new TemplateParameter { Name = "recordElement", Type = typeof(string), IsRequired = false, DefaultValue = "Record", Description = "Record element name" }
                }
            },
            new TemplateInfo
            {
                Name = "SQLite",
                DisplayName = "SQLite Database",
                Description = "SQLite database connector",
                Category = "Database",
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "databasePath", Type = typeof(string), IsRequired = true, Description = "Path to the SQLite database file" },
                    new TemplateParameter { Name = "tableName", Type = typeof(string), IsRequired = true, Description = "Table name" }
                }
            },
            new TemplateInfo
            {
                Name = "SqlServer",
                DisplayName = "SQL Server Database",
                Description = "Microsoft SQL Server database connector",
                Category = "Database",
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "server", Type = typeof(string), IsRequired = true, Description = "SQL Server instance name" },
                    new TemplateParameter { Name = "database", Type = typeof(string), IsRequired = true, Description = "Database name" },
                    new TemplateParameter { Name = "tableName", Type = typeof(string), IsRequired = true, Description = "Table name" },
                    new TemplateParameter { Name = "integratedSecurity", Type = typeof(bool), IsRequired = false, DefaultValue = true, Description = "Use integrated security" }
                }
            },
            new TemplateInfo
            {
                Name = "MySQL",
                DisplayName = "MySQL Database",
                Description = "MySQL database connector",
                Category = "Database",
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "server", Type = typeof(string), IsRequired = true, Description = "MySQL server address" },
                    new TemplateParameter { Name = "database", Type = typeof(string), IsRequired = true, Description = "Database name" },
                    new TemplateParameter { Name = "username", Type = typeof(string), IsRequired = true, Description = "Username" },
                    new TemplateParameter { Name = "password", Type = typeof(string), IsRequired = true, Description = "Password" },
                    new TemplateParameter { Name = "tableName", Type = typeof(string), IsRequired = true, Description = "Table name" }
                }
            },
            new TemplateInfo
            {
                Name = "AzureBlob",
                DisplayName = "Azure Blob Storage",
                Description = "Azure Blob Storage connector",
                Category = "Cloud Storage",
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "connectionString", Type = typeof(string), IsRequired = true, Description = "Azure Storage connection string" },
                    new TemplateParameter { Name = "containerName", Type = typeof(string), IsRequired = true, Description = "Container name" },
                    new TemplateParameter { Name = "prefix", Type = typeof(string), IsRequired = false, Description = "Blob prefix filter" }
                }
            },
            new TemplateInfo
            {
                Name = "AwsS3",
                DisplayName = "AWS S3",
                Description = "Amazon S3 storage connector",
                Category = "Cloud Storage",
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "accessKey", Type = typeof(string), IsRequired = true, Description = "AWS access key" },
                    new TemplateParameter { Name = "secretKey", Type = typeof(string), IsRequired = true, Description = "AWS secret key" },
                    new TemplateParameter { Name = "region", Type = typeof(string), IsRequired = true, Description = "AWS region" },
                    new TemplateParameter { Name = "bucketName", Type = typeof(string), IsRequired = true, Description = "S3 bucket name" },
                    new TemplateParameter { Name = "prefix", Type = typeof(string), IsRequired = false, Description = "Object prefix filter" }
                }
            }
        };
    }
}

/// <summary>
/// Represents information about a connector template.
/// </summary>
public class TemplateInfo
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of template parameters.
    /// </summary>
    public List<TemplateParameter> Parameters { get; set; } = new List<TemplateParameter>();
}

/// <summary>
/// Represents a template parameter.
/// </summary>
public class TemplateParameter
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public Type Type { get; set; } = typeof(string);

    /// <summary>
    /// Gets or sets whether the parameter is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the parameter description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
