using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Connectors.Database;

/// <summary>
/// SQLite database connector for lightweight database operations.
/// </summary>
public class SqliteConnector : BaseDatabaseConnector
{
    /// <summary>
    /// Initializes a new instance of the SqliteConnector class.
    /// </summary>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    public SqliteConnector(IConnectorConfiguration configuration, ILogger<SqliteConnector> logger)
        : base(Guid.NewGuid(), configuration.Name, "SQLite", configuration, logger)
    {
    }

    /// <inheritdoc />
    protected override DbConnection CreateConnection()
    {
        var connectionString = Configuration.ConnectionString;
        
        // Handle in-memory databases
        if (connectionString.Equals(":memory:", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Data Source=:memory:", StringComparison.OrdinalIgnoreCase))
        {
            return new SqliteConnection(connectionString);
        }

        // Handle file-based databases
        if (!connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            // Treat as file path
            connectionString = $"Data Source={connectionString}";
        }

        // Ensure directory exists for file-based databases
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.DataSource) && 
            !builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            var directory = Path.GetDirectoryName(builder.DataSource);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Logger.LogDebug("Created directory for SQLite database: {Directory}", directory);
            }
        }

        return new SqliteConnection(connectionString);
    }

    /// <inheritdoc />
    protected override DbDataAdapter CreateDataAdapter(DbCommand command)
    {
        // SQLite doesn't have a built-in data adapter, create a generic one
        throw new NotSupportedException("SQLite does not support DbDataAdapter. Use direct command execution instead.");
    }

    /// <inheritdoc />
    protected override string GetLimitSyntax(string query, int limit)
    {
        return $"{query} LIMIT {limit}";
    }

    /// <inheritdoc />
    protected override string GetTableExistsQuery(string tableName)
    {
        return $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
    }

    /// <inheritdoc />
    protected override string GetTableSchemaQuery(string tableName)
    {
        return $"SELECT * FROM {tableName} LIMIT 0";
    }

    /// <inheritdoc />
    protected override string BuildUpsertStatement(string tableName, DataRecord record)
    {
        var keyColumn = Configuration.GetConnectionProperty<string>("keyColumn") ?? "Id";
        var columns = string.Join(", ", record.Fields.Keys);
        var parameters = string.Join(", ", record.Fields.Keys.Select(k => $"@{k}"));
        var updateClause = string.Join(", ", record.Fields.Keys.Where(k => k != keyColumn).Select(k => $"{k} = excluded.{k}"));

        if (string.IsNullOrEmpty(updateClause))
        {
            // If no columns to update (only key column), just do INSERT OR IGNORE
            return $"INSERT OR IGNORE INTO {tableName} ({columns}) VALUES ({parameters})";
        }

        return $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}) " +
               $"ON CONFLICT({keyColumn}) DO UPDATE SET {updateClause}";
    }

    /// <inheritdoc />
    protected override string MapDataTypeToSql(Type dataType)
    {
        return dataType.Name switch
        {
            nameof(String) => "TEXT",
            nameof(Int32) => "INTEGER",
            nameof(Int64) => "INTEGER",
            nameof(Decimal) => "REAL",
            nameof(Double) => "REAL",
            nameof(Single) => "REAL",
            nameof(DateTime) => "TEXT", // SQLite stores dates as text
            nameof(Boolean) => "INTEGER", // SQLite uses INTEGER for boolean (0/1)
            nameof(Guid) => "TEXT",
            nameof(Byte) => "INTEGER",
            nameof(SByte) => "INTEGER",
            nameof(Int16) => "INTEGER",
            nameof(UInt16) => "INTEGER",
            nameof(UInt32) => "INTEGER",
            nameof(UInt64) => "INTEGER",
            _ => "TEXT"
        };
    }

    /// <inheritdoc />
    protected override void ValidateConfigurationInternal(ValidationResult result)
    {
        base.ValidateConfigurationInternal(result);

        var connectionString = Configuration.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            result.AddError("Connection string is required for SQLite connector", nameof(Configuration.ConnectionString));
            return;
        }

        // Validate connection string format
        try
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            if (string.IsNullOrEmpty(builder.DataSource))
            {
                result.AddError("Data Source must be specified in SQLite connection string", nameof(Configuration.ConnectionString));
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Invalid SQLite connection string: {ex.Message}", nameof(Configuration.ConnectionString));
        }
    }

    /// <summary>
    /// Creates an in-memory SQLite database for testing purposes.
    /// </summary>
    /// <param name="name">The connector name</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A configured SQLite connector</returns>
    public static SqliteConnector CreateInMemory(string name, ILogger<SqliteConnector> logger)
    {
        var config = ConnectorFactory.CreateTestConfiguration(
            "SQLite",
            name,
            "Data Source=:memory:",
            new Dictionary<string, object>
            {
                ["createTableIfNotExists"] = true
            });

        return new SqliteConnector(config, logger);
    }

    /// <summary>
    /// Creates a file-based SQLite database connector.
    /// </summary>
    /// <param name="name">The connector name</param>
    /// <param name="filePath">The database file path</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A configured SQLite connector</returns>
    public static SqliteConnector CreateFile(string name, string filePath, ILogger<SqliteConnector> logger)
    {
        var config = ConnectorFactory.CreateTestConfiguration(
            "SQLite",
            name,
            $"Data Source={filePath}",
            new Dictionary<string, object>
            {
                ["createTableIfNotExists"] = true
            });

        return new SqliteConnector(config, logger);
    }

    /// <summary>
    /// Executes a custom SQL command and returns the number of affected rows.
    /// </summary>
    /// <param name="sql">The SQL command to execute</param>
    /// <param name="parameters">Optional parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of affected rows</returns>
    public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        using var command = Connection!.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30);
        command.Transaction = CurrentTransaction;

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{param.Key}";
                parameter.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Executes a custom SQL query and returns the result as a scalar value.
    /// </summary>
    /// <param name="sql">The SQL query to execute</param>
    /// <param name="parameters">Optional parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The scalar result</returns>
    public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        using var command = Connection!.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30);
        command.Transaction = CurrentTransaction;

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{param.Key}";
                parameter.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        return await command.ExecuteScalarAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a list of all tables in the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of table names</returns>
    public async Task<List<string>> GetTablesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var tables = new List<string>();
        
        using var command = Connection!.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        command.CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30);
        command.Transaction = CurrentTransaction;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    /// <summary>
    /// Creates a sample table with test data for demonstration purposes.
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CreateSampleTableAsync(string tableName = "SampleData", CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        // Create table
        var createTableSql = $@"
            CREATE TABLE IF NOT EXISTS {tableName} (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Value REAL,
                IsActive INTEGER,
                CreatedDate TEXT
            )";

        await ExecuteNonQueryAsync(createTableSql, cancellationToken: cancellationToken);

        // Insert sample data
        var insertSql = $@"
            INSERT OR REPLACE INTO {tableName} (Id, Name, Value, IsActive, CreatedDate) VALUES
            (1, 'Sample Item 1', 10.5, 1, '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}'),
            (2, 'Sample Item 2', 20.75, 0, '{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd HH:mm:ss}'),
            (3, 'Sample Item 3', 15.25, 1, '{DateTime.UtcNow.AddDays(-2):yyyy-MM-dd HH:mm:ss}'),
            (4, 'Sample Item 4', 8.0, 1, '{DateTime.UtcNow.AddDays(-3):yyyy-MM-dd HH:mm:ss}'),
            (5, 'Sample Item 5', 12.5, 0, '{DateTime.UtcNow.AddDays(-4):yyyy-MM-dd HH:mm:ss}')";

        await ExecuteNonQueryAsync(insertSql, cancellationToken: cancellationToken);

        Logger.LogInformation("Created sample table '{TableName}' with 5 records", tableName);
    }
}
