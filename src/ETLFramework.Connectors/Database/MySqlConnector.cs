using System.Data;
using System.Data.Common;
using MySqlConnector;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Connectors.Database;

/// <summary>
/// MySQL database connector with support for connection pooling and bulk operations.
/// </summary>
public class MySqlDatabaseConnector : BaseDatabaseConnector
{
    /// <summary>
    /// Initializes a new instance of the MySqlDatabaseConnector class.
    /// </summary>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    public MySqlDatabaseConnector(IConnectorConfiguration configuration, ILogger<MySqlDatabaseConnector> logger)
        : base(Guid.NewGuid(), configuration.Name, "MySQL", configuration, logger)
    {
    }

    /// <inheritdoc />
    protected override DbConnection CreateConnection()
    {
        var connectionString = Configuration.ConnectionString;
        
        // Apply connection pooling settings if specified
        var builder = new MySqlConnectionStringBuilder(connectionString);
        
        if (Configuration.UseConnectionPooling)
        {
            builder.Pooling = true;
            builder.MaximumPoolSize = (uint)Configuration.MaxPoolSize;
            builder.MinimumPoolSize = (uint)Configuration.MinPoolSize;
        }
        else
        {
            builder.Pooling = false;
        }

        if (Configuration.ConnectionTimeout.HasValue)
        {
            builder.ConnectionTimeout = (uint)Configuration.ConnectionTimeout.Value.TotalSeconds;
        }

        if (Configuration.CommandTimeout.HasValue)
        {
            builder.DefaultCommandTimeout = (uint)Configuration.CommandTimeout.Value.TotalSeconds;
        }

        // Security and performance settings
        builder.SslMode = Configuration.GetConnectionProperty<bool?>("useSSL") == true ? MySqlSslMode.Required : MySqlSslMode.Preferred;
        builder.AllowUserVariables = true; // Allow user variables for complex operations
        builder.UseAffectedRows = false; // Return matched rows instead of affected rows

        return new MySqlConnection(builder.ConnectionString);
    }

    /// <inheritdoc />
    protected override DbDataAdapter CreateDataAdapter(DbCommand command)
    {
        return new MySqlDataAdapter((MySqlCommand)command);
    }

    /// <inheritdoc />
    protected override string GetLimitSyntax(string query, int limit)
    {
        return $"{query} LIMIT {limit}";
    }

    /// <inheritdoc />
    protected override string GetTableExistsQuery(string tableName)
    {
        return $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'";
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
        var updateClause = string.Join(", ", record.Fields.Keys.Where(k => k != keyColumn).Select(k => $"{k} = VALUES({k})"));

        if (string.IsNullOrEmpty(updateClause))
        {
            // If no columns to update (only key column), just do INSERT IGNORE
            return $"INSERT IGNORE INTO {tableName} ({columns}) VALUES ({parameters})";
        }

        return $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}) " +
               $"ON DUPLICATE KEY UPDATE {updateClause}";
    }

    /// <inheritdoc />
    protected override string MapDataTypeToSql(Type dataType)
    {
        return dataType.Name switch
        {
            nameof(String) => "VARCHAR(255)",
            nameof(Int32) => "INT",
            nameof(Int64) => "BIGINT",
            nameof(Decimal) => "DECIMAL(18,2)",
            nameof(Double) => "DOUBLE",
            nameof(Single) => "FLOAT",
            nameof(DateTime) => "DATETIME",
            nameof(DateTimeOffset) => "TIMESTAMP",
            nameof(Boolean) => "BOOLEAN",
            nameof(Guid) => "CHAR(36)",
            nameof(Byte) => "TINYINT UNSIGNED",
            nameof(SByte) => "TINYINT",
            nameof(Int16) => "SMALLINT",
            nameof(UInt16) => "SMALLINT UNSIGNED",
            nameof(UInt32) => "INT UNSIGNED",
            nameof(UInt64) => "BIGINT UNSIGNED",
            _ => "VARCHAR(255)"
        };
    }

    /// <inheritdoc />
    protected override void ValidateConfigurationInternal(ValidationResult result)
    {
        base.ValidateConfigurationInternal(result);

        var connectionString = Configuration.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            result.AddError("Connection string is required for MySQL connector", nameof(Configuration.ConnectionString));
            return;
        }

        // Validate connection string format
        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrEmpty(builder.Server))
            {
                result.AddError("Server must be specified in MySQL connection string", nameof(Configuration.ConnectionString));
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Invalid MySQL connection string: {ex.Message}", nameof(Configuration.ConnectionString));
        }
    }

    /// <summary>
    /// Performs bulk insert operation using MySQL's LOAD DATA INFILE equivalent.
    /// </summary>
    /// <param name="data">The data to insert</param>
    /// <param name="tableName">The target table name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of records inserted</returns>
    public async Task<long> BulkInsertAsync(IAsyncEnumerable<DataRecord> data, string tableName, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (Connection is not MySqlConnection mysqlConnection)
        {
            throw new InvalidOperationException("Bulk insert requires MySQL connection");
        }

        Logger.LogInformation("Starting bulk insert to table: {TableName}", tableName);

        var recordsInserted = 0L;
        var batchSize = Configuration.BatchSize;
        var batch = new List<DataRecord>(batchSize);

        await foreach (var record in data)
        {
            cancellationToken.ThrowIfCancellationRequested();

            batch.Add(record);

            if (batch.Count >= batchSize)
            {
                await InsertBatchAsync(batch, tableName, cancellationToken);
                recordsInserted += batch.Count;
                batch.Clear();
                
                Logger.LogDebug("Bulk inserted batch of {BatchSize} records, total: {TotalRecords}", batchSize, recordsInserted);
            }
        }

        // Insert remaining records
        if (batch.Count > 0)
        {
            await InsertBatchAsync(batch, tableName, cancellationToken);
            recordsInserted += batch.Count;
        }

        Logger.LogInformation("Completed bulk insert to table: {TableName}, Records: {RecordsInserted}", tableName, recordsInserted);

        return recordsInserted;
    }

    /// <summary>
    /// Inserts a batch of records using a single multi-value INSERT statement.
    /// </summary>
    /// <param name="batch">The batch of records to insert</param>
    /// <param name="tableName">The target table name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task InsertBatchAsync(List<DataRecord> batch, string tableName, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;

        var firstRecord = batch[0];
        var columns = string.Join(", ", firstRecord.Fields.Keys);
        var valuesList = new List<string>();
        var parameters = new List<MySqlParameter>();

        for (int i = 0; i < batch.Count; i++)
        {
            var record = batch[i];
            var parameterNames = new List<string>();

            foreach (var field in record.Fields)
            {
                var paramName = $"@p{i}_{field.Key}";
                parameterNames.Add(paramName);
                
                var parameter = new MySqlParameter(paramName, field.Value ?? DBNull.Value);
                parameters.Add(parameter);
            }

            valuesList.Add($"({string.Join(", ", parameterNames)})");
        }

        var sql = $"INSERT INTO {tableName} ({columns}) VALUES {string.Join(", ", valuesList)}";

        using var command = new MySqlCommand(sql, (MySqlConnection)Connection!)
        {
            CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 300),
            Transaction = (MySqlTransaction?)CurrentTransaction
        };

        command.Parameters.AddRange(parameters.ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Gets MySQL-specific database information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database information</returns>
    public async Task<MySqlDatabaseInfo> GetDatabaseInfoAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var info = new MySqlDatabaseInfo();

        // Get database name
        using var dbCommand = new MySqlCommand("SELECT DATABASE()", (MySqlConnection)Connection!)
        {
            CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30),
            Transaction = (MySqlTransaction?)CurrentTransaction
        };
        info.DatabaseName = (await dbCommand.ExecuteScalarAsync(cancellationToken))?.ToString() ?? "";

        // Get server version
        info.ServerVersion = Connection!.ServerVersion;

        // Get table count
        using var tableCommand = new MySqlCommand(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE()",
            (MySqlConnection)Connection!)
        {
            CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30),
            Transaction = (MySqlTransaction?)CurrentTransaction
        };
        info.TableCount = Convert.ToInt32(await tableCommand.ExecuteScalarAsync(cancellationToken));

        return info;
    }

    /// <summary>
    /// Gets a list of all tables in the current database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of table names</returns>
    public async Task<List<string>> GetTablesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var tables = new List<string>();
        
        using var command = new MySqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = DATABASE()",
            (MySqlConnection)Connection!)
        {
            CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30),
            Transaction = (MySqlTransaction?)CurrentTransaction
        };

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    /// <summary>
    /// Creates a MySQL connector for local development.
    /// </summary>
    /// <param name="name">The connector name</param>
    /// <param name="server">The server address</param>
    /// <param name="database">The database name</param>
    /// <param name="username">The username</param>
    /// <param name="password">The password</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A configured MySQL connector</returns>
    public static MySqlDatabaseConnector CreateLocal(string name, string server, string database, string username, string password, ILogger<MySqlDatabaseConnector> logger)
    {
        var connectionString = $"Server={server};Database={database};Uid={username};Pwd={password};";
        
        var config = ConnectorFactory.CreateTestConfiguration(
            "MySQL",
            name,
            connectionString,
            new Dictionary<string, object>
            {
                ["createTableIfNotExists"] = true,
                ["useSSL"] = false
            });

        return new MySqlDatabaseConnector(config, logger);
    }
}

/// <summary>
/// Represents MySQL-specific database information.
/// </summary>
public class MySqlDatabaseInfo
{
    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server version.
    /// </summary>
    public string ServerVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of tables in the database.
    /// </summary>
    public int TableCount { get; set; }
}
