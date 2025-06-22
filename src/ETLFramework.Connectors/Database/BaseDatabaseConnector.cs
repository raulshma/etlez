using System.Data;
using System.Data.Common;
using System.Text;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Connectors.Database;

/// <summary>
/// Base implementation for database connectors providing common database functionality.
/// </summary>
public abstract class BaseDatabaseConnector : BaseConnector, ISourceConnector<DataRecord>, IDestinationConnector<DataRecord>
{
    private DbConnection? _connection;
    private DbTransaction? _currentTransaction;
    private readonly object _connectionLock = new object();

    /// <summary>
    /// Initializes a new instance of the BaseDatabaseConnector class.
    /// </summary>
    /// <param name="id">The connector identifier</param>
    /// <param name="name">The connector name</param>
    /// <param name="connectorType">The connector type</param>
    /// <param name="configuration">The connector configuration</param>
    /// <param name="logger">The logger instance</param>
    protected BaseDatabaseConnector(
        Guid id,
        string name,
        string connectorType,
        IConnectorConfiguration configuration,
        ILogger logger)
        : base(id, name, connectorType, configuration, logger)
    {
    }

    /// <inheritdoc />
    public WriteMode[] SupportedWriteModes => new[] { WriteMode.Insert, WriteMode.Update, WriteMode.Upsert, WriteMode.Replace };

    /// <summary>
    /// Gets the current database connection.
    /// </summary>
    protected DbConnection? Connection => _connection;

    /// <summary>
    /// Gets the current transaction.
    /// </summary>
    protected DbTransaction? CurrentTransaction => _currentTransaction;

    /// <summary>
    /// Creates a database connection instance. Must be implemented by derived classes.
    /// </summary>
    /// <returns>A new database connection</returns>
    protected abstract DbConnection CreateConnection();

    /// <summary>
    /// Creates a data adapter for the specific database type. Must be implemented by derived classes.
    /// </summary>
    /// <param name="command">The command to create adapter for</param>
    /// <returns>A data adapter instance</returns>
    protected abstract DbDataAdapter CreateDataAdapter(DbCommand command);

    /// <summary>
    /// Gets the SQL syntax for limiting query results. Must be implemented by derived classes.
    /// </summary>
    /// <param name="query">The base query</param>
    /// <param name="limit">The limit count</param>
    /// <returns>The query with limit syntax</returns>
    protected abstract string GetLimitSyntax(string query, int limit);

    /// <summary>
    /// Gets the SQL syntax for checking if a table exists. Must be implemented by derived classes.
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <returns>The query to check table existence</returns>
    protected abstract string GetTableExistsQuery(string tableName);

    /// <summary>
    /// Gets the SQL syntax for getting table schema. Must be implemented by derived classes.
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <returns>The query to get table schema</returns>
    protected abstract string GetTableSchemaQuery(string tableName);

    /// <inheritdoc />
    protected override async Task<ConnectionTestResult> TestConnectionInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var testConnection = CreateConnection();
            await testConnection.OpenAsync(cancellationToken);
            
            // Test with a simple query
            using var command = testConnection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30);
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            return new ConnectionTestResult
            {
                IsSuccessful = true,
                Message = "Database connection successful"
            };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult
            {
                IsSuccessful = false,
                Message = $"Database connection failed: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    protected override async Task OpenInternalAsync(CancellationToken cancellationToken)
    {
        lock (_connectionLock)
        {
            if (_connection != null)
            {
                return; // Already open
            }

            _connection = CreateConnection();
        }

        await _connection.OpenAsync(cancellationToken);
        Logger.LogDebug("Database connection opened: {DatabaseName}", _connection.Database);
    }

    /// <inheritdoc />
    protected override async Task CloseInternalAsync(CancellationToken cancellationToken)
    {
        DbConnection? connectionToClose = null;
        DbTransaction? transactionToRollback = null;

        lock (_connectionLock)
        {
            connectionToClose = _connection;
            transactionToRollback = _currentTransaction;
            _connection = null;
            _currentTransaction = null;
        }

        if (transactionToRollback != null)
        {
            try
            {
                await transactionToRollback.RollbackAsync(cancellationToken);
                Logger.LogWarning("Rolled back uncommitted transaction during connection close");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error rolling back transaction during connection close");
            }
            finally
            {
                transactionToRollback.Dispose();
            }
        }

        if (connectionToClose != null)
        {
            try
            {
                await connectionToClose.CloseAsync();
                Logger.LogDebug("Database connection closed");
            }
            finally
            {
                connectionToClose.Dispose();
            }
        }
    }

    /// <inheritdoc />
    protected override async Task<ConnectorMetadata> GetMetadataInternalAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        var metadata = new ConnectorMetadata
        {
            Version = "1.0.0"
        };

        if (_connection != null)
        {
            metadata.Properties["ServerVersion"] = _connection.ServerVersion;
            metadata.Properties["Database"] = _connection.Database;
            metadata.Properties["DataSource"] = _connection.DataSource;
            metadata.Properties["State"] = _connection.State.ToString();
            metadata.Properties["ConnectionTimeout"] = _connection.ConnectionTimeout;
        }

        return metadata;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<DataRecord> ReadAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var tableName = Configuration.GetConnectionProperty<string>("tableName");
        if (string.IsNullOrEmpty(tableName))
        {
            throw ConnectorException.CreateReadFailure("Table name not specified in configuration", Id, ConnectorType);
        }

        var query = Configuration.GetConnectionProperty<string>("query") ?? $"SELECT * FROM {tableName}";
        
        Logger.LogInformation("Reading from database table: {TableName}", tableName);

        using var command = _connection!.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 300);
        command.Transaction = _currentTransaction;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var recordNumber = 0L;

        while (await reader.ReadAsync(cancellationToken))
        {
            var record = CreateDataRecordFromReader(reader, ++recordNumber);
            yield return record;
        }

        Logger.LogInformation("Completed reading from database table: {TableName}, Records: {RecordCount}", tableName, recordNumber);
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
        await EnsureConnectedAsync(cancellationToken);

        var tableName = Configuration.GetConnectionProperty<string>("tableName");
        if (string.IsNullOrEmpty(tableName))
        {
            return null;
        }

        try
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            command.CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30);
            command.Transaction = _currentTransaction;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not get record count for table: {TableName}", tableName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DataSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var tableName = Configuration.GetConnectionProperty<string>("tableName");
        if (string.IsNullOrEmpty(tableName))
        {
            throw ConnectorException.CreateReadFailure("Table name not specified in configuration", Id, ConnectorType);
        }

        Logger.LogDebug("Getting schema for table: {TableName}", tableName);

        var schema = new DataSchema
        {
            Name = tableName
        };

        try
        {
            var schemaQuery = GetTableSchemaQuery(tableName);
            using var command = _connection!.CreateCommand();
            command.CommandText = schemaQuery;
            command.CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30);
            command.Transaction = _currentTransaction;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var fieldName = reader.GetName(i);
                var fieldType = reader.GetFieldType(i);
                
                schema.Fields.Add(new DataField
                {
                    Name = fieldName,
                    DataType = fieldType,
                    IsRequired = false // Database schema would need more detailed inspection for this
                });
            }

            Logger.LogDebug("Retrieved schema for table {TableName}: {FieldCount} fields", tableName, schema.Fields.Count);
            return schema;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting schema for table: {TableName}", tableName);
            throw ConnectorException.CreateReadFailure($"Failed to get schema for table {tableName}: {ex.Message}", Id, ConnectorType);
        }
    }

    /// <inheritdoc />
    public async Task<WriteResult> WriteAsync(IAsyncEnumerable<DataRecord> data, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var tableName = Configuration.GetConnectionProperty<string>("tableName");
        if (string.IsNullOrEmpty(tableName))
        {
            throw ConnectorException.CreateWriteFailure("Table name not specified in configuration", Id, ConnectorType);
        }

        Logger.LogInformation("Writing to database table: {TableName}", tableName);

        var recordsWritten = 0L;
        var writeMode = Configuration.GetConnectionProperty<string>("writeMode") ?? "Insert";

        try
        {
            await foreach (var record in data)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await WriteRecordAsync(record, tableName, writeMode, cancellationToken);
                recordsWritten++;

                if (recordsWritten % 1000 == 0)
                {
                    Logger.LogDebug("Written {RecordsWritten} records to database table", recordsWritten);
                }
            }

            Logger.LogInformation("Completed writing to database table: {TableName}, Records: {RecordsWritten}", tableName, recordsWritten);

            return new WriteResult
            {
                IsSuccessful = true,
                RecordsWritten = recordsWritten,
                Message = $"Successfully wrote {recordsWritten} records to database table {tableName}"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error writing to database table: {TableName}", tableName);
            
            throw ConnectorException.CreateWriteFailure(
                $"Failed to write to database table {tableName}: {ex.Message}",
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
    public async Task PrepareAsync(DataSchema schema, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var tableName = Configuration.GetConnectionProperty<string>("tableName");
        var createTable = Configuration.GetConnectionProperty<bool?>("createTableIfNotExists") ?? false;

        if (createTable && !string.IsNullOrEmpty(tableName))
        {
            var tableExists = await CheckTableExistsAsync(tableName, cancellationToken);
            if (!tableExists)
            {
                await CreateTableAsync(tableName, schema, cancellationToken);
                Logger.LogInformation("Created table: {TableName}", tableName);
            }
        }
    }

    /// <inheritdoc />
    public Task FinalizeAsync(CancellationToken cancellationToken = default)
    {
        // Database operations are typically committed immediately or as part of transactions
        return Task.CompletedTask;
    }

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transaction instance</returns>
    public async Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress");
        }

        _currentTransaction = await _connection!.BeginTransactionAsync(cancellationToken);
        Logger.LogDebug("Database transaction started");
        
        return _currentTransaction;
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        await _currentTransaction.CommitAsync(cancellationToken);
        _currentTransaction.Dispose();
        _currentTransaction = null;
        
        Logger.LogDebug("Database transaction committed");
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        await _currentTransaction.RollbackAsync(cancellationToken);
        _currentTransaction.Dispose();
        _currentTransaction = null;
        
        Logger.LogDebug("Database transaction rolled back");
    }

    /// <summary>
    /// Creates a DataRecord from a database reader.
    /// </summary>
    /// <param name="reader">The database reader</param>
    /// <param name="recordNumber">The record number</param>
    /// <returns>A DataRecord instance</returns>
    protected virtual DataRecord CreateDataRecordFromReader(DbDataReader reader, long recordNumber)
    {
        var record = new DataRecord
        {
            RowNumber = recordNumber,
            Source = $"{ConnectorType}:{Configuration.ConnectionString}"
        };

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var fieldName = reader.GetName(i);
            var fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
            record.Fields[fieldName] = fieldValue;
        }

        return record;
    }

    /// <summary>
    /// Writes a single record to the database.
    /// </summary>
    /// <param name="record">The record to write</param>
    /// <param name="tableName">The target table name</param>
    /// <param name="writeMode">The write mode</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual async Task WriteRecordAsync(DataRecord record, string tableName, string writeMode, CancellationToken cancellationToken)
    {
        var sql = writeMode.ToLowerInvariant() switch
        {
            "insert" => BuildInsertStatement(tableName, record),
            "update" => BuildUpdateStatement(tableName, record),
            "upsert" => BuildUpsertStatement(tableName, record),
            _ => BuildInsertStatement(tableName, record)
        };

        using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30);
        command.Transaction = _currentTransaction;

        // Add parameters
        foreach (var field in record.Fields)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@{field.Key}";
            parameter.Value = field.Value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Builds an INSERT statement for the record.
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="record">The record</param>
    /// <returns>The INSERT SQL statement</returns>
    protected virtual string BuildInsertStatement(string tableName, DataRecord record)
    {
        var columns = string.Join(", ", record.Fields.Keys);
        var parameters = string.Join(", ", record.Fields.Keys.Select(k => $"@{k}"));
        return $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";
    }

    /// <summary>
    /// Builds an UPDATE statement for the record.
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="record">The record</param>
    /// <returns>The UPDATE SQL statement</returns>
    protected virtual string BuildUpdateStatement(string tableName, DataRecord record)
    {
        var keyColumn = Configuration.GetConnectionProperty<string>("keyColumn") ?? "Id";
        var setClause = string.Join(", ", record.Fields.Keys.Where(k => k != keyColumn).Select(k => $"{k} = @{k}"));
        return $"UPDATE {tableName} SET {setClause} WHERE {keyColumn} = @{keyColumn}";
    }

    /// <summary>
    /// Builds an UPSERT statement for the record.
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="record">The record</param>
    /// <returns>The UPSERT SQL statement</returns>
    protected virtual string BuildUpsertStatement(string tableName, DataRecord record)
    {
        // Default implementation - derived classes should override for database-specific syntax
        return BuildInsertStatement(tableName, record);
    }

    /// <summary>
    /// Checks if a table exists in the database.
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the table exists</returns>
    protected virtual async Task<bool> CheckTableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        try
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = GetTableExistsQuery(tableName);
            command.CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30);
            command.Transaction = _currentTransaction;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a table with the specified schema.
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="schema">The table schema</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual async Task CreateTableAsync(string tableName, DataSchema schema, CancellationToken cancellationToken)
    {
        var sql = BuildCreateTableStatement(tableName, schema);
        
        using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = (int)(Configuration.CommandTimeout?.TotalSeconds ?? 30);
        command.Transaction = _currentTransaction;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Builds a CREATE TABLE statement.
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="schema">The table schema</param>
    /// <returns>The CREATE TABLE SQL statement</returns>
    protected virtual string BuildCreateTableStatement(string tableName, DataSchema schema)
    {
        var columns = new StringBuilder();
        
        foreach (var field in schema.Fields)
        {
            if (columns.Length > 0)
                columns.Append(", ");
                
            var sqlType = MapDataTypeToSql(field.DataType);
            columns.Append($"{field.Name} {sqlType}");
            
            if (field.IsRequired)
                columns.Append(" NOT NULL");
        }

        return $"CREATE TABLE {tableName} ({columns})";
    }

    /// <summary>
    /// Maps a .NET data type to SQL data type.
    /// </summary>
    /// <param name="dataType">The .NET data type</param>
    /// <returns>The SQL data type</returns>
    protected virtual string MapDataTypeToSql(Type dataType)
    {
        return dataType.Name switch
        {
            nameof(String) => "NVARCHAR(255)",
            nameof(Int32) => "INT",
            nameof(Int64) => "BIGINT",
            nameof(Decimal) => "DECIMAL(18,2)",
            nameof(Double) => "FLOAT",
            nameof(DateTime) => "DATETIME",
            nameof(Boolean) => "BIT",
            nameof(Guid) => "UNIQUEIDENTIFIER",
            _ => "NVARCHAR(255)"
        };
    }
}
