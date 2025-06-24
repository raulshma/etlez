# ETLFramework.Data

This project provides persistent storage capabilities for the ETL Framework using PostgreSQL and Entity Framework Core. It implements the repository pattern to provide a clean abstraction over data access operations.

## Features

- **PostgreSQL Integration**: Uses Npgsql provider for PostgreSQL database connectivity
- **Entity Framework Core**: Modern ORM with migrations support
- **Repository Pattern**: Clean abstraction for data access operations
- **JSON Support**: Stores complex objects as JSONB columns for optimal performance
- **Pluggable Design**: Easy integration into any .NET API project
- **Health Checks**: Built-in database health monitoring
- **Migration Support**: Automatic database schema management

## Database Schema

### Pipelines Table
- `Id` (varchar(50), PK): Unique pipeline identifier
- `Name` (varchar(100)): Pipeline name
- `Description` (varchar(500)): Optional description
- `SourceConnectorJson` (jsonb): Source connector configuration
- `TargetConnectorJson` (jsonb): Target connector configuration
- `TransformationsJson` (jsonb): Array of transformation configurations
- `ConfigurationJson` (jsonb): Pipeline-specific configuration
- `IsEnabled` (boolean): Whether the pipeline is active
- `CreatedAt` (timestamptz): Creation timestamp
- `ModifiedAt` (timestamptz): Last modification timestamp
- `LastExecutedAt` (timestamptz): Last execution timestamp

### Executions Table
- `Id` (int, PK, Identity): Database primary key
- `ExecutionId` (varchar(50), Unique): Unique execution identifier
- `PipelineId` (varchar(50), FK): Reference to pipeline
- `Status` (varchar(50)): Execution status (Running, Completed, Failed, Cancelled)
- `StartTime` (timestamptz): Execution start time
- `EndTime` (timestamptz): Execution end time
- `RecordsProcessed` (bigint): Number of records processed
- `SuccessfulRecords` (bigint): Number of successful records
- `FailedRecords` (bigint): Number of failed records
- `ErrorMessage` (varchar(2000)): Error message if failed
- `ParametersJson` (jsonb): Execution parameters

## Indexes

The schema includes optimized indexes for:
- Pipeline name and status queries
- Execution status and time-based queries
- Foreign key relationships
- Unique constraints

## Getting Started

### 1. Install NuGet Package

```bash
dotnet add package ETLFramework.Data
```

### 2. Configure Connection String

Add to your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=etlframework;Username=postgres;Password=password;Port=5432"
  }
}
```

### 3. Register Services

In your `Program.cs`:

```csharp
using ETLFramework.Data.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add ETL Framework data services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddETLFrameworkData(connectionString);

var app = builder.Build();

// Ensure database is created and up to date
await app.Services.EnsureETLDatabaseAsync();
```

### 4. Use Repositories

Inject repositories into your services:

```csharp
public class MyService
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IExecutionRepository _executionRepository;

    public MyService(IPipelineRepository pipelineRepository, IExecutionRepository executionRepository)
    {
        _pipelineRepository = pipelineRepository;
        _executionRepository = executionRepository;
    }

    public async Task<Pipeline?> GetPipelineAsync(string id)
    {
        return await _pipelineRepository.GetByIdAsync(id);
    }
}
```

## Configuration Options

### Basic Configuration

```csharp
builder.Services.AddETLFrameworkData(connectionString);
```

### Advanced Configuration

```csharp
builder.Services.AddETLFrameworkData(connectionString, options =>
{
    options.EnableSensitiveDataLogging(); // Development only
    options.EnableDetailedErrors();       // Development only
});
```

### Custom DbContext Configuration

```csharp
builder.Services.AddETLFrameworkData(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
    });
});
```

### In-Memory Database (Testing)

```csharp
builder.Services.AddETLFrameworkDataInMemory("TestDatabase");
```

## Health Checks

The package provides built-in health check capabilities:

```csharp
// Check if database can be connected to
var canConnect = await serviceProvider.CanConnectToETLDatabaseAsync();

// Get detailed health information
var healthInfo = await serviceProvider.GetETLDatabaseHealthAsync();
```

## Migrations

### Create Migration

```bash
dotnet ef migrations add MigrationName --project src/ETLFramework.Data
```

### Update Database

```bash
dotnet ef database update --project src/ETLFramework.Data
```

### Production Deployment

For production deployments, use the programmatic approach:

```csharp
await app.Services.EnsureETLDatabaseAsync();
```

## Repository Interfaces

### IPipelineRepository

- `GetByIdAsync(string id)`: Get pipeline by ID
- `GetPagedAsync(int page, int pageSize, string? search, bool? isEnabled)`: Get paginated pipelines
- `CreateAsync(Pipeline pipeline)`: Create new pipeline
- `UpdateAsync(Pipeline pipeline)`: Update existing pipeline
- `DeleteAsync(string id)`: Delete pipeline
- `ExistsAsync(string id)`: Check if pipeline exists
- `UpdateLastExecutedAsync(string id, DateTimeOffset lastExecutedAt)`: Update last execution time
- `GetEnabledPipelinesAsync()`: Get all enabled pipelines

### IExecutionRepository

- `GetByExecutionIdAsync(string executionId)`: Get execution by execution ID
- `GetByPipelineAndExecutionIdAsync(string pipelineId, string executionId)`: Get specific execution
- `GetByPipelineIdAsync(string pipelineId, int page, int pageSize)`: Get paginated executions
- `CreateAsync(Execution execution)`: Create new execution
- `UpdateAsync(Execution execution)`: Update existing execution
- `GetLatestByPipelineIdAsync(string pipelineId)`: Get latest execution for pipeline
- `GetRunningExecutionsAsync(string pipelineId)`: Get running executions
- `GetStatisticsAsync(string pipelineId)`: Get execution statistics
- `DeleteOldExecutionsAsync(DateTimeOffset olderThan)`: Cleanup old executions

## Best Practices

1. **Connection Strings**: Store connection strings securely using Azure Key Vault or similar
2. **Migrations**: Always test migrations in a staging environment first
3. **Indexing**: Monitor query performance and add indexes as needed
4. **Cleanup**: Implement regular cleanup of old execution records
5. **Monitoring**: Use the health check endpoints for monitoring
6. **Logging**: Enable EF Core logging in development for debugging

## Troubleshooting

### Common Issues

1. **Connection Failures**: Verify PostgreSQL server is running and connection string is correct
2. **Migration Errors**: Ensure database user has sufficient privileges
3. **Performance Issues**: Check indexes and query patterns
4. **JSON Serialization**: Verify complex objects serialize correctly to JSONB

### Logging

Enable detailed EF Core logging in development:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```
