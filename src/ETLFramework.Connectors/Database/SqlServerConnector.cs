using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Connectors.Database;

/// <summary>
/// SQL Server database connector with advanced features like bulk operations and connection pooling.
/// </summary>
public class SqlServerConnector : BaseDatabaseConnector
{
    /// <summary>
    /// Initializes a new instance of the SqlServerConnector class.
    /// </summary>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    public SqlServerConnector(IConnectorConfiguration configuration, ILogger<SqlServerConnector> logger)
        : base(Guid.NewGuid(), configuration.Name, "SqlServer", configuration, logger)
    {
    }

    /// <inheritdoc />
    protected override DbConnection CreateConnection()
    {
        var connectionString = Configuration.ConnectionString;
        
        // Apply connection pooling settings if specified
        var builder = new SqlConnectionStringBuilder(connectionString);
        
        if (Configuration.UseConnectionPooling)
        {
            builder.Pooling = true;
            builder.MaxPoolSize = Configuration.MaxPoolSize;
            builder.MinPoolSize = Configuration.MinPoolSize;
        }
        else
        {
            builder.Pooling = false;
        }

        if (Configuration.ConnectionTimeout.HasValue)
        {
            builder.ConnectTimeout = (int)Configuration.ConnectionTimeout.Value.TotalSeconds;
        }

        if (Configuration.CommandTimeout.HasValue)
        {
            builder.CommandTimeout = (int)Configuration.CommandTimeout.Value.TotalSeconds;
        }

        // Security settings
        builder.TrustServerCertificate = Configuration.GetConnectionProperty<bool?>("trustServerCertificate") ?? false;
        builder.Encrypt = Configuration.GetConnectionProperty<bool?>("encrypt") ?? true;

        return new SqlConnection(builder.ConnectionString);
    }

    /// <inheritdoc />
    protected override DbDataAdapter CreateDataAdapter(DbCommand command)
    {
        return new SqlDataAdapter((SqlCommand)command);
    }

    /// <inheritdoc />
    protected override string GetLimitSyntax(string query, int limit)
    {
        // SQL Server uses TOP for limiting results
        if (query.ToUpperInvariant().Contains("SELECT"))
        {
            return query.Replace("SELECT", $"SELECT TOP {limit}", StringComparison.OrdinalIgnoreCase);
        }
        return query;
    }

    /// <inheritdoc />
    protected override string GetTableExistsQuery(string tableName)
    {
        return $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
    }

    /// <inheritdoc />
    protected override string GetTableSchemaQuery(string tableName)
    {
        return $"SELECT TOP 0 * FROM {tableName}";
    }

    /// <inheritdoc />
    protected override string BuildUpsertStatement(string tableName, DataRecord record)
    {
        var keyColumn = Configuration.GetConnectionProperty<string>("keyColumn") ?? "Id";
        var columns = string.Join(", ", record.Fields.Keys);
        var parameters = string.Join(", ", record.Fields.Keys.Select(k => $"@{k}"));
        var updateClause = string.Join(", ", record.Fields.Keys.Where(k => k != keyColumn).Select(k => $"target.{k} = source.{k}"));

        if (string.IsNullOrEmpty(updateClause))
        {
            // If no columns to update (only key column), just do INSERT
            return $"IF NOT EXISTS (SELECT 1 FROM {tableName} WHERE {keyColumn} = @{keyColumn}) " +
                   $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";
        }

        return $@"
            MERGE {tableName} AS target
            USING (SELECT {parameters}) AS source ({columns})
            ON target.{keyColumn} = source.{keyColumn}
            WHEN MATCHED THEN
                UPDATE SET {updateClause}
            WHEN NOT MATCHED THEN
                INSERT ({columns}) VALUES ({parameters});";
    }

    /// <inheritdoc />
    protected override string MapDataTypeToSql(Type dataType)
    {
        return dataType.Name switch
        {
            nameof(String) => "NVARCHAR(255)",
            nameof(Int32) => "INT",
            nameof(Int64) => "BIGINT",
            nameof(Decimal) => "DECIMAL(18,2)",
            nameof(Double) => "FLOAT",
            nameof(Single) => "REAL",
            nameof(DateTime) => "DATETIME2",
            nameof(DateTimeOffset) => "DATETIMEOFFSET",
            nameof(Boolean) => "BIT",
            nameof(Guid) => "UNIQUEIDENTIFIER",
            nameof(Byte) => "TINYINT",
            nameof(SByte) => "SMALLINT",
            nameof(Int16) => "SMALLINT",
            nameof(UInt16) => "INT",
            nameof(UInt32) => "BIGINT",
            nameof(UInt64) => "DECIMAL(20,0)",
            _ => "NVARCHAR(255)"
        };
    }

    /// <inheritdoc />
    protected override void ValidateConfigurationInternal(ValidationResult result)
    {
        base.ValidateConfigurationInternal(result);

        var connectionString = Configuration.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            result.AddError("Connection string is required for SQL Server connector", nameof(Configuration.ConnectionString));
            return;
        }

        // Validate connection string format
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrEmpty(builder.DataSource))
            {
                result.AddError("Server/Data Source must be specified in SQL Server connection string", nameof(Configuration.ConnectionString));
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Invalid SQL Server connection string: {ex.Message}", nameof(Configuration.ConnectionString));
        }
    }

    /// <summary>
    /// Performs bulk insert operation for better performance with large datasets.
    /// </summary>
    /// <param name="data">The data to insert</param>
    /// <param name="tableName">The target table name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of records inserted</returns>
    public async Task<long> BulkInsertAsync(IAsyncEnumerable<DataRecord> data, string tableName, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (Connection is not SqlConnection sqlConnection)
        {
            throw new InvalidOperationException("Bulk insert requires SQL Server connection");
        }

        Logger.LogInformation("Starting bulk insert to table: {TableName}", tableName);

        var recordsInserted = 0L;
        var batchSize = Configuration.BatchSize;
        var dataTable = new DataTable();
        var isSchemaInitialized = false;

        using var bulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, (SqlTransaction?)CurrentTransaction)
        {
            DestinationTableName = tableName,
            BatchSize = batchSize,
            BulkCopyTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 300)
        };

        await foreach (var record in data)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!isSchemaInitialized)
            {
                // Initialize DataTable schema from first record
                foreach (var field in record.Fields)
                {
                    var column = new DataColumn(field.Key, field.Value?.GetType() ?? typeof(object));
                    dataTable.Columns.Add(column);
                    bulkCopy.ColumnMappings.Add(field.Key, field.Key);
                }
                isSchemaInitialized = true;
            }

            // Add record to DataTable
            var row = dataTable.NewRow();
            foreach (var field in record.Fields)
            {
                row[field.Key] = field.Value ?? DBNull.Value;
            }
            dataTable.Rows.Add(row);

            recordsInserted++;

            // Write batch when batch size is reached
            if (dataTable.Rows.Count >= batchSize)
            {
                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
                dataTable.Clear();
                
                Logger.LogDebug("Bulk inserted batch of {BatchSize} records, total: {TotalRecords}", batchSize, recordsInserted);
            }
        }

        // Write remaining records
        if (dataTable.Rows.Count > 0)
        {
            await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
        }

        Logger.LogInformation("Completed bulk insert to table: {TableName}, Records: {RecordsInserted}", tableName, recordsInserted);

        return recordsInserted;
    }

    /// <summary>
    /// Executes a stored procedure with parameters.
    /// </summary>
    /// <param name="procedureName">The stored procedure name</param>
    /// <param name="parameters">The procedure parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the stored procedure</returns>
    public async Task<object?> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        using var command = new SqlCommand(procedureName, (SqlConnection)Connection!)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30),
            Transaction = (SqlTransaction?)CurrentTransaction
        };

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
            }
        }

        return await command.ExecuteScalarAsync(cancellationToken);
    }

    /// <summary>
    /// Gets detailed table information including column metadata.
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed table schema information</returns>
    public async Task<TableInfo> GetTableInfoAsync(string tableName, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var tableInfo = new TableInfo { Name = tableName };

        // Get column information
        var columnQuery = @"
            SELECT 
                COLUMN_NAME,
                DATA_TYPE,
                IS_NULLABLE,
                CHARACTER_MAXIMUM_LENGTH,
                NUMERIC_PRECISION,
                NUMERIC_SCALE,
                COLUMN_DEFAULT
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = @TableName
            ORDER BY ORDINAL_POSITION";

        using var command = new SqlCommand(columnQuery, (SqlConnection)Connection!)
        {
            CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30),
            Transaction = (SqlTransaction?)CurrentTransaction
        };
        command.Parameters.AddWithValue("@TableName", tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var column = new ColumnInfo
            {
                Name = reader.GetString("COLUMN_NAME"),
                DataType = reader.GetString("DATA_TYPE"),
                IsNullable = reader.GetString("IS_NULLABLE") == "YES",
                MaxLength = reader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? null : reader.GetInt32("CHARACTER_MAXIMUM_LENGTH"),
                Precision = reader.IsDBNull("NUMERIC_PRECISION") ? null : reader.GetByte("NUMERIC_PRECISION"),
                Scale = reader.IsDBNull("NUMERIC_SCALE") ? null : reader.GetInt32("NUMERIC_SCALE"),
                DefaultValue = reader.IsDBNull("COLUMN_DEFAULT") ? null : reader.GetString("COLUMN_DEFAULT")
            };
            tableInfo.Columns.Add(column);
        }

        return tableInfo;
    }

    /// <summary>
    /// Creates a SQL Server connector for local development.
    /// </summary>
    /// <param name="name">The connector name</param>
    /// <param name="database">The database name</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A configured SQL Server connector</returns>
    public static SqlServerConnector CreateLocalDb(string name, string database, ILogger<SqlServerConnector> logger)
    {
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true";
        
        var config = ConnectorFactory.CreateTestConfiguration(
            "SqlServer",
            name,
            connectionString,
            new Dictionary<string, object>
            {
                ["createTableIfNotExists"] = true,
                ["trustServerCertificate"] = true,
                ["encrypt"] = false
            });

        return new SqlServerConnector(config, logger);
    }
}

/// <summary>
/// Represents detailed table information.
/// </summary>
public class TableInfo
{
    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of columns in the table.
    /// </summary>
    public List<ColumnInfo> Columns { get; } = new List<ColumnInfo>();
}

/// <summary>
/// Represents detailed column information.
/// </summary>
public class ColumnInfo
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the column is nullable.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets the maximum length for character types.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the numeric precision.
    /// </summary>
    public byte? Precision { get; set; }

    /// <summary>
    /// Gets or sets the numeric scale.
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public string? DefaultValue { get; set; }
}
