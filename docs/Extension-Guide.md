# ETL Framework - Extension Guide

This guide covers how to extend the ETL Framework with custom connectors, transformations, and plugins.

## Table of Contents

1. [Extension Architecture](#extension-architecture)
2. [Custom Connectors](#custom-connectors)
3. [Custom Transformations](#custom-transformations)
4. [Plugin Development](#plugin-development)
5. [Service Registration](#service-registration)
6. [Configuration Extensions](#configuration-extensions)
7. [Testing Extensions](#testing-extensions)
8. [Best Practices](#best-practices)

## Extension Architecture

The ETL Framework is built with extensibility as a core principle, using a plugin-based architecture that allows you to:

- **Custom Connectors**: Create connectors for new data sources and destinations
- **Custom Transformations**: Implement business-specific data transformation logic
- **Plugin System**: Package extensions as reusable plugins
- **Dependency Injection**: Leverage built-in DI for service registration
- **Configuration Support**: Extend configuration schemas for custom components

### Core Extension Interfaces

```csharp
// Plugin interface
public interface IETLPlugin
{
    string Name { get; }
    Version Version { get; }
    void ConfigureServices(IServiceCollection services);
    void Configure(IETLFrameworkBuilder builder);
}

// Framework builder interface
public interface IETLFrameworkBuilder
{
    IETLFrameworkBuilder AddConnector<T>(string connectorType) where T : class, IConnector;
    IETLFrameworkBuilder AddTransformation<T>() where T : class, ITransformationRule;
    IETLFrameworkBuilder AddConfigurationProvider<T>() where T : class, IConfigurationProvider;
}
```

## Custom Connectors

### Creating a Custom Source Connector

```csharp
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

public class CustomApiConnector : ISourceConnector<ApiRecord>, IDestinationConnector<ApiRecord>
{
    private readonly IConnectorConfiguration _configuration;
    private readonly ILogger<CustomApiConnector> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public CustomApiConnector(
        IConnectorConfiguration configuration,
        ILogger<CustomApiConnector> logger,
        HttpClient httpClient)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Extract configuration properties
        _baseUrl = _configuration.ConnectionString;
        _apiKey = _configuration.ConnectionProperties.GetValueOrDefault("apiKey")?.ToString() 
                  ?? throw new ArgumentException("API key is required");

        // Configure HTTP client
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(
            _configuration.ConnectionTimeout?.TotalSeconds ?? 30);
    }

    #region IConnector Implementation

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "Custom API Connector";
    public string ConnectorType => "CustomApi";
    public IConnectorConfiguration Configuration => _configuration;

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing connection to API: {BaseUrl}", _baseUrl);
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/health", cancellationToken);
            var isHealthy = response.IsSuccessStatusCode;
            
            _logger.LogInformation("Connection test result: {IsHealthy}", isHealthy);
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for API: {BaseUrl}", _baseUrl);
            return false;
        }
    }

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Opening connection to API: {BaseUrl}", _baseUrl);
        
        // Perform any initialization logic here
        var isConnected = await TestConnectionAsync(cancellationToken);
        if (!isConnected)
        {
            throw new InvalidOperationException($"Failed to connect to API: {_baseUrl}");
        }
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Closing connection to API: {BaseUrl}", _baseUrl);
        // Perform cleanup if needed
        return Task.CompletedTask;
    }

    public async Task<ValidationResult> ValidateConfigurationAsync()
    {
        var result = new ValidationResult();

        // Validate base URL
        if (string.IsNullOrEmpty(_baseUrl) || !Uri.IsWellFormedUriString(_baseUrl, UriKind.Absolute))
        {
            result.AddError("Invalid base URL");
        }

        // Validate API key
        if (string.IsNullOrEmpty(_apiKey))
        {
            result.AddError("API key is required");
        }

        // Test connection if configuration is valid
        if (result.IsValid)
        {
            var connectionTest = await TestConnectionAsync();
            if (!connectionTest)
            {
                result.AddError("Connection test failed");
            }
        }

        return result;
    }

    public async Task<ConnectorMetadata> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/metadata", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                
                return new ConnectorMetadata
                {
                    Version = metadata?.GetValueOrDefault("version")?.ToString(),
                    Properties = metadata ?? new Dictionary<string, object>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve metadata from API");
        }

        return new ConnectorMetadata();
    }

    #endregion

    #region ISourceConnector Implementation

    public async IAsyncEnumerable<ApiRecord> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting to read data from API: {BaseUrl}", _baseUrl);

        var endpoint = _configuration.ConnectionProperties.GetValueOrDefault("endpoint")?.ToString() ?? "data";
        var pageSize = int.Parse(_configuration.ConnectionProperties.GetValueOrDefault("pageSize")?.ToString() ?? "100");
        var page = 1;
        var hasMoreData = true;

        while (hasMoreData && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var url = $"{_baseUrl}/{endpoint}?page={page}&pageSize={pageSize}";
                _logger.LogDebug("Fetching data from: {Url}", url);

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<ApiRecord>>(content);

                if (apiResponse?.Data != null && apiResponse.Data.Any())
                {
                    foreach (var record in apiResponse.Data)
                    {
                        yield return record;
                    }

                    hasMoreData = apiResponse.HasNextPage;
                    page++;
                }
                else
                {
                    hasMoreData = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading data from API page {Page}", page);
                throw;
            }
        }

        _logger.LogInformation("Completed reading data from API");
    }

    public async IAsyncEnumerable<IEnumerable<ApiRecord>> ReadBatchAsync(
        int batchSize, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var batch = new List<ApiRecord>(batchSize);

        await foreach (var record in ReadAsync(cancellationToken))
        {
            batch.Add(record);

            if (batch.Count >= batchSize)
            {
                yield return batch.ToList();
                batch.Clear();
            }
        }

        // Return remaining records
        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    public async Task<DataSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        // Return schema information for the API records
        return new DataSchema
        {
            Fields = new List<DataField>
            {
                new() { Name = "Id", DataType = typeof(int), IsRequired = true, IsPrimaryKey = true },
                new() { Name = "Name", DataType = typeof(string), IsRequired = true, MaxLength = 255 },
                new() { Name = "Email", DataType = typeof(string), IsRequired = false, MaxLength = 255 },
                new() { Name = "CreatedAt", DataType = typeof(DateTime), IsRequired = true },
                new() { Name = "UpdatedAt", DataType = typeof(DateTime), IsRequired = false }
            }
        };
    }

    #endregion

    #region IDestinationConnector Implementation

    public async Task WriteAsync(IAsyncEnumerable<ApiRecord> records, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting to write data to API: {BaseUrl}", _baseUrl);

        var endpoint = _configuration.ConnectionProperties.GetValueOrDefault("writeEndpoint")?.ToString() ?? "data";
        var batchSize = _configuration.BatchSize;
        var batch = new List<ApiRecord>(batchSize);

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            batch.Add(record);

            if (batch.Count >= batchSize)
            {
                await WriteBatchAsync(batch, endpoint, cancellationToken);
                batch.Clear();
            }
        }

        // Write remaining records
        if (batch.Count > 0)
        {
            await WriteBatchAsync(batch, endpoint, cancellationToken);
        }

        _logger.LogInformation("Completed writing data to API");
    }

    private async Task WriteBatchAsync(IEnumerable<ApiRecord> records, string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(records);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogDebug("Successfully wrote batch of {Count} records", records.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write batch of {Count} records", records.Count());
            throw;
        }
    }

    public async Task<long> GetRecordCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = _configuration.ConnectionProperties.GetValueOrDefault("countEndpoint")?.ToString() ?? "count";
            var response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var countResponse = JsonSerializer.Deserialize<CountResponse>(content);
                return countResponse?.Count ?? 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get record count from API");
        }

        return 0;
    }

    #endregion

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Supporting models
public class ApiRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ApiResponse<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public bool HasNextPage { get; set; }
    public int TotalCount { get; set; }
}

public class CountResponse
{
    public long Count { get; set; }
}
```

### Database Connector Example

```csharp
public class CustomDatabaseConnector : ISourceConnector<DataRecord>, IDestinationConnector<DataRecord>
{
    private readonly IConnectorConfiguration _configuration;
    private readonly ILogger<CustomDatabaseConnector> _logger;
    private IDbConnection? _connection;

    public CustomDatabaseConnector(
        IConnectorConfiguration configuration,
        ILogger<CustomDatabaseConnector> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "Custom Database Connector";
    public string ConnectorType => "CustomDatabase";
    public IConnectorConfiguration Configuration => _configuration;

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);
            return connection.State == ConnectionState.Open;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return false;
        }
    }

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        _connection = CreateConnection();
        await _connection.OpenAsync(cancellationToken);
        _logger.LogDebug("Database connection opened");
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
            _connection = null;
            _logger.LogDebug("Database connection closed");
        }
    }

    public async IAsyncEnumerable<DataRecord> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not opened");

        var query = _configuration.ConnectionProperties.GetValueOrDefault("query")?.ToString()
                   ?? throw new ArgumentException("Query is required");

        using var command = _connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = (int)(_configuration.CommandTimeout?.TotalSeconds ?? 300);

        // Add parameters if specified
        if (_configuration.ConnectionProperties.TryGetValue("parameters", out var parametersObj) 
            && parametersObj is Dictionary<string, object> parameters)
        {
            foreach (var param in parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = param.Key;
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var record = new DataRecord();
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var fieldName = reader.GetName(i);
                var fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                record.SetValue(fieldName, fieldValue);
            }
            
            yield return record;
        }
    }

    public async Task WriteAsync(IAsyncEnumerable<DataRecord> records, CancellationToken cancellationToken = default)
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not opened");

        var tableName = _configuration.ConnectionProperties.GetValueOrDefault("tableName")?.ToString()
                       ?? throw new ArgumentException("Table name is required");

        var batchSize = _configuration.BatchSize;
        var batch = new List<DataRecord>(batchSize);

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            batch.Add(record);

            if (batch.Count >= batchSize)
            {
                await WriteBatchAsync(batch, tableName, cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await WriteBatchAsync(batch, tableName, cancellationToken);
        }
    }

    private async Task WriteBatchAsync(IEnumerable<DataRecord> records, string tableName, CancellationToken cancellationToken)
    {
        // Implementation depends on specific database type
        // This is a simplified example using bulk insert
        
        var recordList = records.ToList();
        if (!recordList.Any()) return;

        var firstRecord = recordList.First();
        var columns = firstRecord.GetFieldNames().ToList();
        
        var insertSql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ";
        var valuesClauses = new List<string>();
        var parameters = new List<IDbDataParameter>();
        
        for (int i = 0; i < recordList.Count; i++)
        {
            var record = recordList[i];
            var paramNames = columns.Select(col => $"@p{i}_{col}").ToList();
            valuesClauses.Add($"({string.Join(", ", paramNames)})");
            
            foreach (var column in columns)
            {
                var param = _connection!.CreateCommand().CreateParameter();
                param.ParameterName = $"@p{i}_{column}";
                param.Value = record.GetValue(column) ?? DBNull.Value;
                parameters.Add(param);
            }
        }
        
        insertSql += string.Join(", ", valuesClauses);
        
        using var command = _connection.CreateCommand();
        command.CommandText = insertSql;
        command.CommandTimeout = (int)(_configuration.CommandTimeout?.TotalSeconds ?? 300);
        
        foreach (var param in parameters)
        {
            command.Parameters.Add(param);
        }
        
        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Inserted {RowsAffected} rows into {TableName}", rowsAffected, tableName);
    }

    private IDbConnection CreateConnection()
    {
        // Factory method to create appropriate connection type
        // This would be implemented based on the specific database provider
        var connectionString = _configuration.ConnectionString;
        
        return _configuration.ConnectorType.ToLower() switch
        {
            "sqlserver" => new SqlConnection(connectionString),
            "mysql" => new MySqlConnection(connectionString),
            "postgresql" => new NpgsqlConnection(connectionString),
            _ => throw new NotSupportedException($"Database type {_configuration.ConnectorType} not supported")
        };
    }

    public async Task<ValidationResult> ValidateConfigurationAsync()
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(_configuration.ConnectionString))
        {
            result.AddError("Connection string is required");
        }

        if (!_configuration.ConnectionProperties.ContainsKey("tableName"))
        {
            result.AddError("Table name is required");
        }

        return result;
    }

    public async Task<ConnectorMetadata> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        return new ConnectorMetadata
        {
            Version = "1.0.0",
            Properties = new Dictionary<string, object>
            {
                ["DatabaseType"] = _configuration.ConnectorType,
                ["ConnectionString"] = _configuration.ConnectionString
            }
        };
    }

    public async Task<DataSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not opened");

        var tableName = _configuration.ConnectionProperties.GetValueOrDefault("tableName")?.ToString()
                       ?? throw new ArgumentException("Table name is required");

        // Query database schema
        var schemaQuery = $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";

        using var command = _connection.CreateCommand();
        command.CommandText = schemaQuery;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var fields = new List<DataField>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var field = new DataField
            {
                Name = reader.GetString("COLUMN_NAME"),
                DataType = MapDatabaseTypeToClrType(reader.GetString("DATA_TYPE")),
                IsRequired = reader.GetString("IS_NULLABLE") == "NO",
                MaxLength = reader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? null : reader.GetInt32("CHARACTER_MAXIMUM_LENGTH")
            };
            fields.Add(field);
        }

        return new DataSchema { Fields = fields };
    }

    private Type MapDatabaseTypeToClrType(string databaseType)
    {
        return databaseType.ToLower() switch
        {
            "int" or "integer" => typeof(int),
            "bigint" => typeof(long),
            "varchar" or "nvarchar" or "text" => typeof(string),
            "datetime" or "timestamp" => typeof(DateTime),
            "decimal" or "numeric" => typeof(decimal),
            "bit" or "boolean" => typeof(bool),
            _ => typeof(object)
        };
    }

    public async Task<long> GetRecordCountAsync(CancellationToken cancellationToken = default)
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not opened");

        var tableName = _configuration.ConnectionProperties.GetValueOrDefault("tableName")?.ToString()
                       ?? throw new ArgumentException("Table name is required");

        using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

## Custom Transformations

### Creating Custom Transformation Rules

```csharp
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

public class CustomBusinessRuleTransformation : ITransformationRule
{
    private readonly ILogger<CustomBusinessRuleTransformation> _logger;

    public CustomBusinessRuleTransformation(ILogger<CustomBusinessRuleTransformation> logger)
    {
        _logger = logger;
    }

    public string Name => "CustomBusinessRule";
    public string Description => "Applies custom business rules to customer data";

    public async Task<TransformationResult> ApplyAsync(
        DataRecord record,
        ITransformationRuleConfiguration config)
    {
        try
        {
            _logger.LogDebug("Applying custom business rule to record");

            // Extract configuration settings
            var settings = config.Settings;
            var customerTypeRules = settings.GetValueOrDefault("customerTypeRules") as Dictionary<string, object>;
            var discountRules = settings.GetValueOrDefault("discountRules") as Dictionary<string, object>;

            // Apply customer type classification
            var customerType = DetermineCustomerType(record, customerTypeRules);
            record.SetValue("CustomerType", customerType);

            // Calculate discount based on customer type and purchase history
            var discount = CalculateDiscount(record, customerType, discountRules);
            record.SetValue("Discount", discount);

            // Apply credit limit based on customer type
            var creditLimit = CalculateCreditLimit(record, customerType);
            record.SetValue("CreditLimit", creditLimit);

            // Validate business rules
            var validationResult = ValidateBusinessRules(record);
            if (!validationResult.IsValid)
            {
                return new TransformationResult
                {
                    IsSuccess = false,
                    Error = string.Join("; ", validationResult.Errors),
                    TransformedRecord = record
                };
            }

            _logger.LogDebug("Successfully applied custom business rule");

            return new TransformationResult
            {
                IsSuccess = true,
                TransformedRecord = record
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying custom business rule");

            return new TransformationResult
            {
                IsSuccess = false,
                Error = ex.Message,
                TransformedRecord = record
            };
        }
    }

    private string DetermineCustomerType(DataRecord record, Dictionary<string, object>? rules)
    {
        var totalOrders = record.GetValue<int>("TotalOrders");
        var totalSpent = record.GetValue<decimal>("TotalSpent");
        var yearsAsCustomer = record.GetValue<int>("YearsAsCustomer");

        // Apply configurable rules or use defaults
        if (rules != null)
        {
            // Use configured thresholds
            var vipOrderThreshold = Convert.ToInt32(rules.GetValueOrDefault("vipOrderThreshold", 50));
            var vipSpentThreshold = Convert.ToDecimal(rules.GetValueOrDefault("vipSpentThreshold", 10000));
            var premiumOrderThreshold = Convert.ToInt32(rules.GetValueOrDefault("premiumOrderThreshold", 20));
            var premiumSpentThreshold = Convert.ToDecimal(rules.GetValueOrDefault("premiumSpentThreshold", 5000));

            return (totalOrders, totalSpent, yearsAsCustomer) switch
            {
                var (orders, spent, years) when orders >= vipOrderThreshold && spent >= vipSpentThreshold => "VIP",
                var (orders, spent, years) when orders >= premiumOrderThreshold && spent >= premiumSpentThreshold => "Premium",
                var (orders, spent, years) when orders >= 5 && spent >= 1000 => "Regular",
                _ => "New"
            };
        }

        // Default classification logic
        return (totalOrders, totalSpent, yearsAsCustomer) switch
        {
            (>= 50, >= 10000, >= 2) => "VIP",
            (>= 20, >= 5000, >= 1) => "Premium",
            (>= 5, >= 1000, _) => "Regular",
            _ => "New"
        };
    }

    private decimal CalculateDiscount(DataRecord record, string customerType, Dictionary<string, object>? rules)
    {
        var baseDiscount = customerType switch
        {
            "VIP" => 0.15m,
            "Premium" => 0.10m,
            "Regular" => 0.05m,
            _ => 0.00m
        };

        // Apply additional discounts based on rules
        if (rules != null)
        {
            var loyaltyBonus = Convert.ToDecimal(rules.GetValueOrDefault("loyaltyBonus", 0.02m));
            var yearsAsCustomer = record.GetValue<int>("YearsAsCustomer");

            if (yearsAsCustomer >= 5)
            {
                baseDiscount += loyaltyBonus;
            }

            // Volume discount
            var totalSpent = record.GetValue<decimal>("TotalSpent");
            var volumeThreshold = Convert.ToDecimal(rules.GetValueOrDefault("volumeThreshold", 50000));
            var volumeDiscount = Convert.ToDecimal(rules.GetValueOrDefault("volumeDiscount", 0.05m));

            if (totalSpent >= volumeThreshold)
            {
                baseDiscount += volumeDiscount;
            }
        }

        // Cap discount at maximum
        return Math.Min(baseDiscount, 0.25m);
    }

    private decimal CalculateCreditLimit(DataRecord record, string customerType)
    {
        var annualIncome = record.GetValue<decimal>("AnnualIncome");
        var creditScore = record.GetValue<int>("CreditScore");

        var baseMultiplier = customerType switch
        {
            "VIP" => 0.5m,
            "Premium" => 0.3m,
            "Regular" => 0.2m,
            _ => 0.1m
        };

        var creditLimit = annualIncome * baseMultiplier;

        // Adjust based on credit score
        if (creditScore >= 750)
            creditLimit *= 1.2m;
        else if (creditScore >= 650)
            creditLimit *= 1.0m;
        else if (creditScore >= 550)
            creditLimit *= 0.8m;
        else
            creditLimit *= 0.5m;

        // Set minimum and maximum limits
        return Math.Max(1000, Math.Min(creditLimit, 100000));
    }

    private ValidationResult ValidateBusinessRules(DataRecord record)
    {
        var result = new ValidationResult();

        // Validate customer type
        var customerType = record.GetValue<string>("CustomerType");
        if (!new[] { "VIP", "Premium", "Regular", "New" }.Contains(customerType))
        {
            result.AddError($"Invalid customer type: {customerType}");
        }

        // Validate discount range
        var discount = record.GetValue<decimal>("Discount");
        if (discount < 0 || discount > 0.25m)
        {
            result.AddError($"Discount out of range: {discount:P}");
        }

        // Validate credit limit
        var creditLimit = record.GetValue<decimal>("CreditLimit");
        if (creditLimit < 1000 || creditLimit > 100000)
        {
            result.AddError($"Credit limit out of range: {creditLimit:C}");
        }

        return result;
    }

    public Task<bool> CanApplyAsync(DataRecord record, ITransformationRuleConfiguration config)
    {
        // Check if record has required fields
        var requiredFields = new[] { "TotalOrders", "TotalSpent", "YearsAsCustomer", "AnnualIncome", "CreditScore" };
        var hasAllFields = requiredFields.All(field => record.HasField(field));

        return Task.FromResult(hasAllFields);
    }

    public Task<DataSchema> GetOutputSchemaAsync(DataSchema inputSchema, ITransformationRuleConfiguration config)
    {
        var outputSchema = inputSchema.Clone();

        // Add new fields that this transformation creates
        outputSchema.Fields.Add(new DataField
        {
            Name = "CustomerType",
            DataType = typeof(string),
            IsRequired = true,
            MaxLength = 20
        });

        outputSchema.Fields.Add(new DataField
        {
            Name = "Discount",
            DataType = typeof(decimal),
            IsRequired = true
        });

        outputSchema.Fields.Add(new DataField
        {
            Name = "CreditLimit",
            DataType = typeof(decimal),
            IsRequired = true
        });

        return Task.FromResult(outputSchema);
    }
}

### Advanced Transformation with External Services

```csharp
public class AddressValidationTransformation : ITransformationRule
{
    private readonly ILogger<AddressValidationTransformation> _logger;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public AddressValidationTransformation(
        ILogger<AddressValidationTransformation> logger,
        HttpClient httpClient,
        IMemoryCache cache)
    {
        _logger = logger;
        _httpClient = httpClient;
        _cache = cache;
    }

    public string Name => "AddressValidation";
    public string Description => "Validates and standardizes addresses using external service";

    public async Task<TransformationResult> ApplyAsync(
        DataRecord record,
        ITransformationRuleConfiguration config)
    {
        try
        {
            var address = ExtractAddress(record);
            var cacheKey = $"address_{address.GetHashCode()}";

            // Check cache first
            if (_cache.TryGetValue(cacheKey, out ValidatedAddress? cachedAddress))
            {
                ApplyValidatedAddress(record, cachedAddress!);
                return new TransformationResult { IsSuccess = true, TransformedRecord = record };
            }

            // Validate address using external service
            var validatedAddress = await ValidateAddressAsync(address, config);

            if (validatedAddress != null)
            {
                // Cache the result
                _cache.Set(cacheKey, validatedAddress, TimeSpan.FromHours(24));

                ApplyValidatedAddress(record, validatedAddress);

                return new TransformationResult { IsSuccess = true, TransformedRecord = record };
            }
            else
            {
                // Mark address as invalid but continue processing
                record.SetValue("AddressValidationStatus", "Invalid");
                return new TransformationResult { IsSuccess = true, TransformedRecord = record };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating address");

            // On error, mark as unvalidated and continue
            record.SetValue("AddressValidationStatus", "Error");
            return new TransformationResult { IsSuccess = true, TransformedRecord = record };
        }
    }

    private Address ExtractAddress(DataRecord record)
    {
        return new Address
        {
            Street = record.GetValue<string>("Street") ?? "",
            City = record.GetValue<string>("City") ?? "",
            State = record.GetValue<string>("State") ?? "",
            ZipCode = record.GetValue<string>("ZipCode") ?? "",
            Country = record.GetValue<string>("Country") ?? "US"
        };
    }

    private async Task<ValidatedAddress?> ValidateAddressAsync(Address address, ITransformationRuleConfiguration config)
    {
        var apiKey = config.Settings.GetValueOrDefault("apiKey")?.ToString();
        var serviceUrl = config.Settings.GetValueOrDefault("serviceUrl")?.ToString() ?? "https://api.addressvalidation.com";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Address validation API key not configured");
            return null;
        }

        var requestData = new
        {
            street = address.Street,
            city = address.City,
            state = address.State,
            zipCode = address.ZipCode,
            country = address.Country
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        var response = await _httpClient.PostAsync($"{serviceUrl}/validate", content);

        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ValidatedAddress>(responseJson);
        }

        return null;
    }

    private void ApplyValidatedAddress(DataRecord record, ValidatedAddress validatedAddress)
    {
        record.SetValue("Street", validatedAddress.StandardizedStreet);
        record.SetValue("City", validatedAddress.StandardizedCity);
        record.SetValue("State", validatedAddress.StandardizedState);
        record.SetValue("ZipCode", validatedAddress.StandardizedZipCode);
        record.SetValue("Country", validatedAddress.StandardizedCountry);
        record.SetValue("Latitude", validatedAddress.Latitude);
        record.SetValue("Longitude", validatedAddress.Longitude);
        record.SetValue("AddressValidationStatus", validatedAddress.IsValid ? "Valid" : "Invalid");
        record.SetValue("AddressValidationScore", validatedAddress.ConfidenceScore);
    }

    // Supporting classes
    public class Address
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string Country { get; set; } = "";
    }

    public class ValidatedAddress
    {
        public string StandardizedStreet { get; set; } = "";
        public string StandardizedCity { get; set; } = "";
        public string StandardizedState { get; set; } = "";
        public string StandardizedZipCode { get; set; } = "";
        public string StandardizedCountry { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsValid { get; set; }
        public double ConfidenceScore { get; set; }
    }
}

## Plugin Development

### Creating a Complete Plugin

```csharp
// Plugin assembly attribute
[assembly: ETLPlugin]

namespace MyCompany.ETL.Plugins
{
    /// <summary>
    /// Custom ETL plugin that provides specialized connectors and transformations
    /// for financial data processing.
    /// </summary>
    public class FinancialDataPlugin : IETLPlugin
    {
        public string Name => "Financial Data Processing Plugin";
        public Version Version => new(1, 2, 0);
        public string Description => "Provides connectors and transformations for financial data processing";
        public string Author => "MyCompany Data Team";

        public void ConfigureServices(IServiceCollection services)
        {
            // Register custom connectors
            services.AddTransient<BankingApiConnector>();
            services.AddTransient<CreditBureauConnector>();
            services.AddTransient<RegulatoryReportingConnector>();

            // Register custom transformations
            services.AddTransient<CreditScoreCalculationTransformation>();
            services.AddTransient<RiskAssessmentTransformation>();
            services.AddTransient<ComplianceValidationTransformation>();

            // Register supporting services
            services.AddSingleton<ICreditScoringService, CreditScoringService>();
            services.AddSingleton<IRiskCalculationEngine, RiskCalculationEngine>();
            services.AddHttpClient<BankingApiConnector>();

            // Register configuration validators
            services.AddTransient<IConfigurationValidator, FinancialDataConfigurationValidator>();
        }

        public void Configure(IETLFrameworkBuilder builder)
        {
            // Register connectors with the framework
            builder.AddConnector<BankingApiConnector>("BankingApi");
            builder.AddConnector<CreditBureauConnector>("CreditBureau");
            builder.AddConnector<RegulatoryReportingConnector>("RegulatoryReporting");

            // Register transformations
            builder.AddTransformation<CreditScoreCalculationTransformation>();
            builder.AddTransformation<RiskAssessmentTransformation>();
            builder.AddTransformation<ComplianceValidationTransformation>();

            // Register configuration providers
            builder.AddConfigurationProvider<FinancialDataConfigurationProvider>();

            // Register custom pipeline stages
            builder.AddPipelineStage<ComplianceValidationStage>();
        }
    }

    /// <summary>
    /// Plugin metadata attribute for additional information
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ETLPluginAttribute : Attribute
    {
        public string? MinimumFrameworkVersion { get; set; }
        public string? MaximumFrameworkVersion { get; set; }
        public string[]? Dependencies { get; set; }
        public string[]? Tags { get; set; }
    }
}
```

### Plugin Discovery and Loading

```csharp
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceCollection _services;
    private readonly List<IETLPlugin> _loadedPlugins = new();
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();

    public PluginManager(ILogger<PluginManager> logger, IServiceCollection services)
    {
        _logger = logger;
        _services = services;
    }

    public async Task<int> DiscoverAndLoadPluginsAsync(string pluginDirectory)
    {
        var pluginCount = 0;

        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogWarning("Plugin directory does not exist: {PluginDirectory}", pluginDirectory);
            return 0;
        }

        var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);

        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                var assembly = await LoadPluginAssemblyAsync(pluginFile);
                if (assembly != null)
                {
                    var plugins = await DiscoverPluginsInAssemblyAsync(assembly);
                    foreach (var plugin in plugins)
                    {
                        await LoadPluginAsync(plugin);
                        pluginCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from file: {PluginFile}", pluginFile);
            }
        }

        _logger.LogInformation("Loaded {PluginCount} plugins from {PluginDirectory}", pluginCount, pluginDirectory);
        return pluginCount;
    }

    private async Task<Assembly?> LoadPluginAssemblyAsync(string assemblyPath)
    {
        try
        {
            // Use AssemblyLoadContext for plugin isolation
            var loadContext = new PluginLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            // Validate assembly
            if (await ValidatePluginAssemblyAsync(assembly))
            {
                _loadedAssemblies[assemblyPath] = assembly;
                return assembly;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assembly: {AssemblyPath}", assemblyPath);
        }

        return null;
    }

    private async Task<bool> ValidatePluginAssemblyAsync(Assembly assembly)
    {
        // Check for ETLPlugin attribute
        var pluginAttribute = assembly.GetCustomAttribute<ETLPluginAttribute>();
        if (pluginAttribute == null)
        {
            _logger.LogWarning("Assembly does not have ETLPlugin attribute: {AssemblyName}", assembly.FullName);
            return false;
        }

        // Validate framework version compatibility
        if (!string.IsNullOrEmpty(pluginAttribute.MinimumFrameworkVersion))
        {
            var minVersion = Version.Parse(pluginAttribute.MinimumFrameworkVersion);
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (currentVersion < minVersion)
            {
                _logger.LogError("Plugin requires minimum framework version {MinVersion}, current version is {CurrentVersion}",
                    minVersion, currentVersion);
                return false;
            }
        }

        return true;
    }

    private async Task<IEnumerable<IETLPlugin>> DiscoverPluginsInAssemblyAsync(Assembly assembly)
    {
        var plugins = new List<IETLPlugin>();

        try
        {
            var pluginTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IETLPlugin).IsAssignableFrom(t));

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = Activator.CreateInstance(pluginType) as IETLPlugin;
                    if (plugin != null)
                    {
                        plugins.Add(plugin);
                        _logger.LogDebug("Discovered plugin: {PluginName} v{Version}", plugin.Name, plugin.Version);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create instance of plugin type: {PluginType}", pluginType.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering plugins in assembly: {AssemblyName}", assembly.FullName);
        }

        return plugins;
    }

    private async Task LoadPluginAsync(IETLPlugin plugin)
    {
        try
        {
            _logger.LogInformation("Loading plugin: {PluginName} v{Version}", plugin.Name, plugin.Version);

            // Configure services
            plugin.ConfigureServices(_services);

            // Configure framework (this would be called after service provider is built)
            // plugin.Configure(frameworkBuilder);

            _loadedPlugins.Add(plugin);

            _logger.LogInformation("Successfully loaded plugin: {PluginName}", plugin.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin: {PluginName}", plugin.Name);
            throw;
        }
    }

    public IEnumerable<IETLPlugin> GetLoadedPlugins()
    {
        return _loadedPlugins.AsReadOnly();
    }

    public IETLPlugin? GetPlugin(string name)
    {
        return _loadedPlugins.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}

// Plugin load context for assembly isolation
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}
```

## Service Registration

### Framework Builder Implementation

```csharp
public class ETLFrameworkBuilder : IETLFrameworkBuilder
{
    private readonly IServiceCollection _services;
    private readonly IConnectorFactory _connectorFactory;
    private readonly ITransformationEngine _transformationEngine;
    private readonly ILogger<ETLFrameworkBuilder> _logger;

    public ETLFrameworkBuilder(
        IServiceCollection services,
        IConnectorFactory connectorFactory,
        ITransformationEngine transformationEngine,
        ILogger<ETLFrameworkBuilder> logger)
    {
        _services = services;
        _connectorFactory = connectorFactory;
        _transformationEngine = transformationEngine;
        _logger = logger;
    }

    public IETLFrameworkBuilder AddConnector<T>(string connectorType) where T : class, IConnector
    {
        try
        {
            // Register connector with DI container
            _services.AddTransient<T>();

            // Register with connector factory
            _connectorFactory.RegisterConnector(connectorType, config =>
            {
                var serviceProvider = _services.BuildServiceProvider();
                var connector = ActivatorUtilities.CreateInstance<T>(serviceProvider, config);
                return connector;
            });

            _logger.LogInformation("Registered connector: {ConnectorType} -> {ConnectorClass}", connectorType, typeof(T).Name);
            return this;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register connector: {ConnectorType}", connectorType);
            throw;
        }
    }

    public IETLFrameworkBuilder AddTransformation<T>() where T : class, ITransformationRule
    {
        try
        {
            // Register transformation with DI container
            _services.AddTransient<T>();

            // Register with transformation engine
            _transformationEngine.RegisterTransformationRule(typeof(T), config =>
            {
                var serviceProvider = _services.BuildServiceProvider();
                return ActivatorUtilities.CreateInstance<T>(serviceProvider);
            });

            _logger.LogInformation("Registered transformation: {TransformationType}", typeof(T).Name);
            return this;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register transformation: {TransformationType}", typeof(T).Name);
            throw;
        }
    }

    public IETLFrameworkBuilder AddConfigurationProvider<T>() where T : class, IConfigurationProvider
    {
        try
        {
            _services.AddSingleton<T>();
            _services.AddSingleton<IConfigurationProvider, T>();

            _logger.LogInformation("Registered configuration provider: {ProviderType}", typeof(T).Name);
            return this;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register configuration provider: {ProviderType}", typeof(T).Name);
            throw;
        }
    }

    public IETLFrameworkBuilder AddPipelineStage<T>() where T : class, IPipelineStage
    {
        try
        {
            _services.AddTransient<T>();

            // Register stage factory
            _services.AddTransient<Func<IStageConfiguration, T>>(provider =>
                config => ActivatorUtilities.CreateInstance<T>(provider, config));

            _logger.LogInformation("Registered pipeline stage: {StageType}", typeof(T).Name);
            return this;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register pipeline stage: {StageType}", typeof(T).Name);
            throw;
        }
    }
}

// Extension methods for service registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddETLFramework(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IConnectorFactory, ConnectorFactory>();
        services.AddSingleton<ITransformationEngine, TransformationEngine>();
        services.AddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();

        // Configuration providers
        services.AddSingleton<IConfigurationProvider, JsonConfigurationProvider>();
        services.AddSingleton<IConfigurationProvider, YamlConfigurationProvider>();

        // Plugin management
        services.AddSingleton<IPluginManager, PluginManager>();

        // Framework builder
        services.AddSingleton<IETLFrameworkBuilder, ETLFrameworkBuilder>();

        return services;
    }

    public static IServiceCollection AddETLConnectors(this IServiceCollection services)
    {
        // Register built-in connectors
        services.AddTransient<CsvConnector>();
        services.AddTransient<JsonConnector>();
        services.AddTransient<XmlConnector>();
        services.AddTransient<SqlServerConnector>();
        services.AddTransient<MySqlConnector>();
        services.AddTransient<PostgreSqlConnector>();

        return services;
    }

    public static IServiceCollection AddETLTransformations(this IServiceCollection services)
    {
        // Register built-in transformations
        services.AddTransient<FieldMappingTransformation>();
        services.AddTransient<DataValidationTransformation>();
        services.AddTransient<DataCleaningTransformation>();
        services.AddTransient<StringTransformation>();
        services.AddTransient<CalculatedFieldTransformation>();

        return services;
    }

    public static IServiceCollection AddETLMessaging(this IServiceCollection services)
    {
        // Register messaging services
        services.AddSingleton<IMessageBroker, RabbitMQBroker>();
        services.AddSingleton<IPipelineEventPublisher, PipelineEventPublisher>();
        services.AddSingleton<IEventDrivenPipelineOrchestrator, EventDrivenPipelineOrchestrator>();

        return services;
    }
}

## Configuration Extensions

### Custom Configuration Provider

```csharp
public class DatabaseConfigurationProvider : IConfigurationProvider
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DatabaseConfigurationProvider> _logger;

    public DatabaseConfigurationProvider(IDbConnection connection, ILogger<DatabaseConfigurationProvider> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public string Name => "Database Configuration Provider";
    public IEnumerable<string> SupportedFormats => new[] { "database", "db" };

    public async Task<IPipelineConfiguration> LoadPipelineConfigurationAsync(string source)
    {
        try
        {
            var query = "SELECT ConfigurationJson FROM PipelineConfigurations WHERE Name = @Name";
            var configJson = await _connection.QuerySingleOrDefaultAsync<string>(query, new { Name = source });

            if (string.IsNullOrEmpty(configJson))
            {
                throw new ConfigurationNotFoundException($"Pipeline configuration not found: {source}");
            }

            var config = JsonSerializer.Deserialize<PipelineConfiguration>(configJson);
            return config ?? throw new InvalidOperationException("Failed to deserialize configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pipeline configuration from database: {Source}", source);
            throw;
        }
    }

    public async Task<IConnectorConfiguration> LoadConnectorConfigurationAsync(string source)
    {
        try
        {
            var query = "SELECT ConfigurationJson FROM ConnectorConfigurations WHERE Name = @Name";
            var configJson = await _connection.QuerySingleOrDefaultAsync<string>(query, new { Name = source });

            if (string.IsNullOrEmpty(configJson))
            {
                throw new ConfigurationNotFoundException($"Connector configuration not found: {source}");
            }

            var config = JsonSerializer.Deserialize<ConnectorConfiguration>(configJson);
            return config ?? throw new InvalidOperationException("Failed to deserialize configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load connector configuration from database: {Source}", source);
            throw;
        }
    }

    public async Task SavePipelineConfigurationAsync(string name, IPipelineConfiguration configuration)
    {
        try
        {
            var configJson = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });

            var upsertQuery = @"
                MERGE PipelineConfigurations AS target
                USING (SELECT @Name AS Name, @ConfigJson AS ConfigurationJson) AS source
                ON target.Name = source.Name
                WHEN MATCHED THEN
                    UPDATE SET ConfigurationJson = source.ConfigurationJson, ModifiedAt = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT (Name, ConfigurationJson, CreatedAt, ModifiedAt)
                    VALUES (source.Name, source.ConfigurationJson, GETUTCDATE(), GETUTCDATE());";

            await _connection.ExecuteAsync(upsertQuery, new { Name = name, ConfigJson = configJson });

            _logger.LogInformation("Saved pipeline configuration to database: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save pipeline configuration to database: {Name}", name);
            throw;
        }
    }
}
```

## Testing Extensions

### Unit Testing Custom Connectors

```csharp
[TestClass]
public class CustomApiConnectorTests
{
    private Mock<ILogger<CustomApiConnector>> _mockLogger;
    private Mock<HttpMessageHandler> _mockHttpHandler;
    private HttpClient _httpClient;
    private IConnectorConfiguration _configuration;
    private CustomApiConnector _connector;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<CustomApiConnector>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);

        _configuration = new ConnectorConfiguration
        {
            ConnectorType = "CustomApi",
            ConnectionString = "https://api.example.com",
            ConnectionProperties = new Dictionary<string, object>
            {
                ["apiKey"] = "test-api-key",
                ["endpoint"] = "data",
                ["pageSize"] = "100"
            }
        };

        _connector = new CustomApiConnector(_configuration, _mockLogger.Object, _httpClient);
    }

    [TestMethod]
    public async Task TestConnectionAsync_ValidConfiguration_ReturnsTrue()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        _mockHttpHandler.Setup(h => h.SendAsync(
            It.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/health")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _connector.TestConnectionAsync();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ReadAsync_ValidResponse_ReturnsRecords()
    {
        // Arrange
        var testData = new ApiResponse<ApiRecord>
        {
            Data = new[]
            {
                new ApiRecord { Id = 1, Name = "Test 1", Email = "test1@example.com" },
                new ApiRecord { Id = 2, Name = "Test 2", Email = "test2@example.com" }
            },
            HasNextPage = false
        };

        var responseJson = JsonSerializer.Serialize(testData);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler.Setup(h => h.SendAsync(
            It.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/data")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMessage);

        await _connector.OpenAsync();

        // Act
        var records = new List<ApiRecord>();
        await foreach (var record in _connector.ReadAsync())
        {
            records.Add(record);
        }

        // Assert
        Assert.AreEqual(2, records.Count);
        Assert.AreEqual("Test 1", records[0].Name);
        Assert.AreEqual("test1@example.com", records[0].Email);
    }

    [TestMethod]
    public async Task ValidateConfigurationAsync_MissingApiKey_ReturnsInvalid()
    {
        // Arrange
        var invalidConfig = new ConnectorConfiguration
        {
            ConnectorType = "CustomApi",
            ConnectionString = "https://api.example.com",
            ConnectionProperties = new Dictionary<string, object>()
        };

        var connector = new CustomApiConnector(invalidConfig, _mockLogger.Object, _httpClient);

        // Act
        var result = await connector.ValidateConfigurationAsync();

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("API key")));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connector?.Dispose();
        _httpClient?.Dispose();
    }
}
```

### Integration Testing

```csharp
[TestClass]
public class CustomConnectorIntegrationTests
{
    private IServiceProvider _serviceProvider;
    private IConnectorFactory _connectorFactory;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Register ETL Framework services
        services.AddETLFramework();
        services.AddETLConnectors();

        // Register test-specific services
        services.AddSingleton<HttpClient>();
        services.AddLogging(builder => builder.AddConsole());

        // Register custom connector
        services.AddTransient<CustomApiConnector>();

        _serviceProvider = services.BuildServiceProvider();
        _connectorFactory = _serviceProvider.GetRequiredService<IConnectorFactory>();

        // Register custom connector with factory
        _connectorFactory.RegisterConnector("CustomApi", config =>
            new CustomApiConnector(
                config,
                _serviceProvider.GetRequiredService<ILogger<CustomApiConnector>>(),
                _serviceProvider.GetRequiredService<HttpClient>()));
    }

    [TestMethod]
    public async Task EndToEndPipelineTest_WithCustomConnector()
    {
        // Arrange
        var sourceConfig = new ConnectorConfiguration
        {
            ConnectorType = "CustomApi",
            ConnectionString = "https://jsonplaceholder.typicode.com",
            ConnectionProperties = new Dictionary<string, object>
            {
                ["endpoint"] = "posts",
                ["pageSize"] = "10"
            }
        };

        var destinationConfig = new ConnectorConfiguration
        {
            ConnectorType = "CSV",
            ConnectionString = "FilePath=test-output.csv",
            ConnectionProperties = new Dictionary<string, object>
            {
                ["hasHeaders"] = true,
                ["delimiter"] = ",",
                ["encoding"] = "UTF-8"
            }
        };

        // Act
        var sourceConnector = _connectorFactory.CreateSourceConnector<object>(sourceConfig);
        var destinationConnector = _connectorFactory.CreateDestinationConnector<object>(destinationConfig);

        await sourceConnector.OpenAsync();
        await destinationConnector.OpenAsync();

        var records = new List<object>();
        await foreach (var record in sourceConnector.ReadAsync())
        {
            records.Add(record);
        }

        await destinationConnector.WriteAsync(records.ToAsyncEnumerable());

        await sourceConnector.CloseAsync();
        await destinationConnector.CloseAsync();

        // Assert
        Assert.IsTrue(records.Count > 0);
        Assert.IsTrue(File.Exists("test-output.csv"));

        // Cleanup
        if (File.Exists("test-output.csv"))
        {
            File.Delete("test-output.csv");
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }
}
```

### Performance Testing

```csharp
[TestClass]
public class CustomConnectorPerformanceTests
{
    [TestMethod]
    public async Task PerformanceTest_LargeDataSet()
    {
        // Arrange
        var config = new ConnectorConfiguration
        {
            ConnectorType = "CustomApi",
            ConnectionString = "https://api.example.com",
            ConnectionProperties = new Dictionary<string, object>
            {
                ["apiKey"] = "test-key",
                ["endpoint"] = "large-dataset",
                ["pageSize"] = "1000"
            },
            BatchSize = 1000
        };

        var connector = new CustomApiConnector(
            config,
            Mock.Of<ILogger<CustomApiConnector>>(),
            new HttpClient());

        var stopwatch = Stopwatch.StartNew();
        var recordCount = 0;

        // Act
        await connector.OpenAsync();

        await foreach (var batch in connector.ReadBatchAsync(1000))
        {
            recordCount += batch.Count();

            // Simulate processing time
            await Task.Delay(10);
        }

        stopwatch.Stop();

        // Assert
        var recordsPerSecond = recordCount / stopwatch.Elapsed.TotalSeconds;

        Assert.IsTrue(recordsPerSecond > 100, $"Performance below threshold: {recordsPerSecond:F2} records/second");
        Assert.IsTrue(stopwatch.Elapsed.TotalMinutes < 5, $"Processing took too long: {stopwatch.Elapsed.TotalMinutes:F2} minutes");

        Console.WriteLine($"Processed {recordCount} records in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"Throughput: {recordsPerSecond:F2} records/second");
    }
}
```

## Best Practices

### 1. Design Principles

- **Single Responsibility**: Each connector/transformation should have a single, well-defined purpose
- **Dependency Injection**: Use DI for all dependencies to improve testability
- **Configuration-Driven**: Make components configurable rather than hard-coded
- **Error Handling**: Implement comprehensive error handling with meaningful messages

### 2. Performance Considerations

- **Async/Await**: Use async patterns throughout for better scalability
- **Batching**: Implement batching for high-throughput scenarios
- **Memory Management**: Dispose resources properly and avoid memory leaks
- **Connection Pooling**: Use connection pooling for database connectors

### 3. Security

- **Input Validation**: Validate all configuration inputs
- **Secure Connections**: Use encrypted connections where possible
- **Credential Management**: Never hard-code credentials
- **Least Privilege**: Request only necessary permissions

### 4. Testing

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test end-to-end scenarios
- **Performance Tests**: Validate performance under load
- **Error Scenarios**: Test error handling and edge cases

### 5. Documentation

- **XML Documentation**: Document all public APIs
- **Configuration Examples**: Provide sample configurations
- **Usage Guides**: Create guides for common scenarios
- **Troubleshooting**: Document common issues and solutions

This comprehensive extension guide provides the foundation for creating robust, scalable extensions to the ETL Framework. The examples demonstrate real-world patterns and best practices that can be adapted to specific requirements.
```
```
