# ETL Framework - Comprehensive Documentation

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Getting Started](#getting-started)
4. [Core Components](#core-components)
5. [Configuration](#configuration)
6. [Connectors](#connectors)
7. [Transformations](#transformations)
8. [Pipeline Management](#pipeline-management)
9. [REST API](#rest-api)
10. [Deployment](#deployment)
11. [Monitoring & Logging](#monitoring--logging)
12. [Extensibility](#extensibility)
13. [Best Practices](#best-practices)
14. [Troubleshooting](#troubleshooting)

## Overview

The ETL Framework is a comprehensive Extract, Transform, Load (ETL) solution built on .NET 9+ that provides dynamic data integration pipelines configurable through API interfaces. It supports multiple data sources, advanced transformations, and cloud-native deployment scenarios.

### Key Features

- **Pipeline Orchestration**: Complete ETL pipeline management with Extract → Transform → Load stages
- **Multiple Connectors**: Support for CSV, JSON, XML files, SQL databases, and cloud storage
- **Advanced Transformations**: Field-level transformations, rule-based business logic, and data mapping
- **RESTful API**: Comprehensive REST API for pipeline management and execution
- **Performance Monitoring**: Real-time metrics, optimization analysis, and throughput tracking
- **Configuration Management**: JSON/YAML configuration with validation and runtime compilation
- **Cloud-Native**: Docker support, Kubernetes deployment, and horizontal scaling
- **Extensible Architecture**: Plugin-based system for custom connectors and transformations

### Technical Stack

- **.NET 9+**: Modern C# with latest language features
- **Entity Framework Core + Dapper**: Hybrid data access approach
- **Supported Databases**: SQL Server, MySQL, PostgreSQL, SQLite
- **Cloud Storage**: Azure Blob Storage, Amazon S3
- **File Formats**: CSV, JSON, XML, Parquet
- **Message Brokers**: RabbitMQ, Azure Service Bus, Amazon SQS
- **Authentication**: API Keys, JWT, OAuth 2.0
- **Deployment**: Standalone, Windows Service, Docker, Kubernetes

## Architecture

The ETL Framework follows a modular, plugin-based architecture with clear separation of concerns:

```
ETLFramework.sln
├── src/
│   ├── ETLFramework.Core/           # Core abstractions and interfaces
│   ├── ETLFramework.Pipeline/       # Pipeline orchestration engine
│   ├── ETLFramework.Configuration/  # Configuration management
│   ├── ETLFramework.Connectors/     # Data connectors
│   ├── ETLFramework.Transformation/ # Data transformation engine
│   ├── ETLFramework.Messaging/      # Message-based communication
│   ├── ETLFramework.API/           # RESTful API layer
│   └── ETLFramework.Host/          # Application host
├── tests/                          # Comprehensive test suite
└── samples/                        # Sample implementations
```

### Core Interfaces

The framework is built around key abstractions:

- **IPipeline**: Represents an executable ETL pipeline
- **IPipelineStage**: Individual stages within a pipeline
- **IConnector**: Base interface for all data connectors
- **ISourceConnector<T>**: Connectors that read data
- **IDestinationConnector<T>**: Connectors that write data
- **ITransformationEngine**: Data transformation processing
- **IPipelineConfiguration**: Pipeline configuration management

## Getting Started

### Prerequisites

- .NET 9 SDK or later
- Visual Studio 2022 or VS Code
- Docker (optional, for containerized deployment)

### Installation

1. **Clone the repository:**
```bash
git clone https://github.com/your-org/etl-framework.git
cd etl-framework
```

2. **Build the solution:**
```bash
dotnet build
```

3. **Run the API:**
```bash
cd src/ETLFramework.API
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger UI at the root.

### Quick Start Example

#### 1. Programmatic Pipeline Creation

```csharp
using ETLFramework.Pipeline;
using ETLFramework.Connectors;
using ETLFramework.Configuration;

// Create pipeline configuration
var config = new PipelineConfiguration
{
    Name = "Customer Data Pipeline",
    Description = "Process customer data from CSV to database"
};

// Configure source connector
var sourceConfig = new ConnectorConfiguration
{
    ConnectorType = "CSV",
    ConnectionString = "FilePath=customers.csv",
    ConnectionProperties = new Dictionary<string, object>
    {
        ["hasHeaders"] = true,
        ["delimiter"] = ",",
        ["encoding"] = "UTF-8"
    }
};

// Configure destination connector
var destConfig = new ConnectorConfiguration
{
    ConnectorType = "SqlServer",
    ConnectionString = "Server=localhost;Database=CRM;Trusted_Connection=true;",
    ConnectionProperties = new Dictionary<string, object>
    {
        ["tableName"] = "Customers",
        ["batchSize"] = 1000
    }
};

// Create and execute pipeline
var orchestrator = serviceProvider.GetRequiredService<IPipelineOrchestrator>();
var pipeline = await orchestrator.CreatePipelineAsync(config);
var result = await pipeline.ExecuteAsync();
```

#### 2. REST API Usage

**Create a Pipeline:**
```bash
curl -X POST https://localhost:5001/api/pipelines \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sample Pipeline",
    "description": "Process customer data",
    "sourceConnector": {
      "type": "CSV",
      "configuration": {
        "filePath": "customers.csv",
        "hasHeaders": true,
        "delimiter": ",",
        "encoding": "UTF-8"
      }
    },
    "targetConnector": {
      "type": "SqlServer",
      "configuration": {
        "connectionString": "Server=localhost;Database=CRM;Trusted_Connection=true;",
        "tableName": "Customers",
        "batchSize": 1000
      }
    },
    "transformations": [
      {
        "name": "Validate Email",
        "type": "DataValidation",
        "configuration": {
          "fieldName": "Email",
          "pattern": "^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$"
        }
      }
    ]
  }'
```

**Execute a Pipeline:**
```bash
curl -X POST https://localhost:5001/api/pipelines/{pipeline-id}/execute \
  -H "Content-Type: application/json" \
  -d '{
    "parameters": {
      "batchSize": 500
    },
    "async": true
  }'
```

## Core Components

### Pipeline Engine

The pipeline engine orchestrates the execution of ETL stages:

- **Extract Stage**: Reads data from source connectors
- **Transform Stage**: Applies transformations and business rules
- **Load Stage**: Writes data to destination connectors

### Pipeline Context

Each pipeline execution has a context that maintains:
- Execution metadata
- Shared variables
- Error tracking
- Performance metrics

### Error Handling

The framework provides comprehensive error handling:
- **Stop on Error**: Halt pipeline execution on first error
- **Continue on Error**: Log errors and continue processing
- **Max Errors**: Stop after reaching error threshold
- **Retry Logic**: Configurable retry attempts with delays

## Configuration

The framework supports both JSON and YAML configuration formats with environment variable substitution.

### Pipeline Configuration Structure

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Sample Data Pipeline",
  "description": "A sample ETL pipeline",
  "version": "1.0.0",
  "isEnabled": true,
  "stages": [
    {
      "name": "Extract CSV Data",
      "stageType": "Extract",
      "order": 1,
      "connectorConfiguration": {
        "connectorType": "CSV",
        "connectionString": "FilePath=${DATA_PATH}/customers.csv",
        "connectionProperties": {
          "hasHeaders": true,
          "delimiter": ",",
          "encoding": "UTF-8"
        }
      }
    }
  ],
  "globalSettings": {
    "parallelism": 4,
    "bufferSize": 10000,
    "enableMetrics": true
  },
  "errorHandling": {
    "stopOnError": false,
    "maxErrors": 100
  },
  "retry": {
    "maxAttempts": 3,
    "delay": "00:00:10"
  }
}
```

### Environment Variables

Configuration supports environment variable substitution:
- `${VAR_NAME}`: Required variable
- `${VAR_NAME:default}`: Variable with default value

### Configuration Validation

All configurations are validated at runtime:
- Schema validation
- Connection testing
- Dependency checking
- Performance optimization suggestions

## Connectors

The framework provides a rich set of built-in connectors for various data sources and destinations.

### File System Connectors

#### CSV Connector
```json
{
  "connectorType": "CSV",
  "connectionString": "FilePath=data/customers.csv",
  "connectionProperties": {
    "hasHeaders": true,
    "delimiter": ",",
    "encoding": "UTF-8",
    "quoteChar": "\"",
    "escapeChar": "\\",
    "skipEmptyRows": true,
    "trimWhitespace": true
  },
  "batchSize": 1000
}
```

#### JSON Connector
```json
{
  "connectorType": "JSON",
  "connectionString": "FilePath=data/products.json",
  "connectionProperties": {
    "jsonPath": "$.products[*]",
    "encoding": "UTF-8",
    "prettyPrint": false
  }
}
```

#### XML Connector
```json
{
  "connectorType": "XML",
  "connectionString": "FilePath=data/orders.xml",
  "connectionProperties": {
    "rootElement": "orders",
    "recordElement": "order",
    "encoding": "UTF-8",
    "validateSchema": false
  }
}
```

### Database Connectors

#### SQL Server Connector
```json
{
  "connectorType": "SqlServer",
  "connectionString": "Server=${DB_SERVER};Database=${DB_NAME};Integrated Security=true;TrustServerCertificate=true",
  "connectionTimeout": "00:00:30",
  "commandTimeout": "00:05:00",
  "connectionProperties": {
    "tableName": "Customers",
    "writeMode": "Upsert",
    "keyColumns": ["CustomerId"],
    "createTableIfNotExists": true
  },
  "batchSize": 500,
  "useConnectionPooling": true,
  "maxPoolSize": 50
}
```

#### MySQL Connector
```json
{
  "connectorType": "MySQL",
  "connectionString": "Server=${MYSQL_HOST};Database=${MYSQL_DB};Uid=${MYSQL_USER};Pwd=${MYSQL_PASS};",
  "connectionProperties": {
    "tableName": "products",
    "writeMode": "Insert",
    "bulkInsert": true
  }
}
```

#### PostgreSQL Connector
```json
{
  "connectorType": "PostgreSQL",
  "connectionString": "Host=${PG_HOST};Database=${PG_DB};Username=${PG_USER};Password=${PG_PASS}",
  "connectionProperties": {
    "schema": "public",
    "tableName": "orders",
    "writeMode": "Merge"
  }
}
```

### Cloud Storage Connectors

#### Azure Blob Storage
```json
{
  "connectorType": "AzureBlob",
  "connectionString": "${AZURE_STORAGE_CONNECTION_STRING}",
  "connectionProperties": {
    "containerName": "data-files",
    "blobPrefix": "etl/input/",
    "downloadPath": "./temp/downloads"
  }
}
```

#### Amazon S3
```json
{
  "connectorType": "AwsS3",
  "connectionProperties": {
    "accessKey": "${AWS_ACCESS_KEY}",
    "secretKey": "${AWS_SECRET_KEY}",
    "region": "${AWS_REGION}",
    "bucketName": "etl-data-bucket",
    "prefix": "input/"
  }
}
```

### Connector Operations

All connectors support standard operations:

**Source Operations:**
- `ReadAsync()`: Read data records
- `ReadBatchAsync()`: Read data in batches
- `GetSchemaAsync()`: Retrieve data schema
- `TestConnectionAsync()`: Validate connection

**Destination Operations:**
- `WriteAsync()`: Write data records
- `WriteBatchAsync()`: Write data in batches
- `CreateSchemaAsync()`: Create destination schema
- `TruncateAsync()`: Clear destination data

**Write Modes:**
- **Insert**: Add new records only
- **Update**: Update existing records
- **Upsert**: Insert new or update existing
- **Merge**: Advanced merge operations
- **Replace**: Replace all data

## Transformations

The transformation engine provides powerful data processing capabilities.

### Built-in Transformation Types

#### Field Mapping
```json
{
  "ruleType": "FieldMapping",
  "settings": {
    "mappings": {
      "customer_id": "CustomerId",
      "first_name": "FirstName",
      "last_name": "LastName",
      "email_address": "Email"
    }
  }
}
```

#### Data Validation
```json
{
  "ruleType": "DataValidation",
  "settings": {
    "validations": {
      "Email": {
        "required": true,
        "pattern": "^[\\w\\.-]+@[\\w\\.-]+\\.[a-zA-Z]{2,}$"
      },
      "Phone": {
        "required": false,
        "pattern": "^\\+?[1-9]\\d{1,14}$"
      },
      "Age": {
        "required": true,
        "minValue": 0,
        "maxValue": 150
      }
    }
  }
}
```

#### Data Cleaning
```json
{
  "ruleType": "DataCleaning",
  "settings": {
    "operations": {
      "FirstName": ["trim", "titleCase"],
      "LastName": ["trim", "titleCase"],
      "Email": ["trim", "toLowerCase"],
      "Phone": ["trim", "removeNonDigits", "formatPhone"]
    }
  }
}
```

#### String Transformations
```json
{
  "ruleType": "StringTransformation",
  "settings": {
    "fieldName": "ProductName",
    "operations": [
      {
        "type": "replace",
        "pattern": "\\s+",
        "replacement": " "
      },
      {
        "type": "toUpper"
      }
    ]
  }
}
```

#### Calculated Fields
```json
{
  "ruleType": "CalculatedField",
  "settings": {
    "fieldName": "FullName",
    "expression": "CONCAT(FirstName, ' ', LastName)",
    "dataType": "string"
  }
}
```

#### Conditional Logic
```json
{
  "ruleType": "ConditionalTransformation",
  "settings": {
    "conditions": [
      {
        "condition": "Age >= 18",
        "trueValue": "Adult",
        "falseValue": "Minor",
        "targetField": "AgeGroup"
      }
    ]
  }
}
```

### Custom Transformations

Create custom transformations by implementing `ITransformationRule`:

```csharp
public class CustomTransformation : ITransformationRule
{
    public string Name => "CustomTransformation";

    public Task<TransformationResult> ApplyAsync(
        DataRecord record,
        ITransformationRuleConfiguration config)
    {
        // Custom transformation logic
        return Task.FromResult(new TransformationResult
        {
            IsSuccess = true,
            TransformedRecord = record
        });
    }
}
```

## Pipeline Management

### Pipeline Lifecycle

1. **Creation**: Define pipeline configuration
2. **Validation**: Validate configuration and connections
3. **Compilation**: Compile pipeline stages
4. **Execution**: Run pipeline with monitoring
5. **Completion**: Generate execution report

### Pipeline Status

- **Draft**: Pipeline is being configured
- **Ready**: Pipeline is validated and ready
- **Running**: Pipeline is currently executing
- **Completed**: Pipeline finished successfully
- **Failed**: Pipeline encountered errors
- **Cancelled**: Pipeline was manually stopped

### Execution Context

Each pipeline execution maintains context:

```csharp
public interface IPipelineContext
{
    Guid ExecutionId { get; }
    Guid PipelineId { get; }
    DateTimeOffset StartTime { get; }
    IDictionary<string, object> Variables { get; }
    IList<ExecutionError> Errors { get; }
    ExecutionMetrics Metrics { get; }
}
```

### Parallel Processing

Configure parallel processing for improved performance:

```json
{
  "globalSettings": {
    "parallelism": 4,
    "maxDegreeOfParallelism": 8,
    "bufferSize": 10000,
    "enableParallelStages": true
  }
}
```

## REST API

The ETL Framework provides a comprehensive REST API for pipeline management and execution.

### Base URL
```
https://localhost:5001/api
```

### Authentication
The API supports multiple authentication methods:
- API Keys (Header: `X-API-Key`)
- JWT Bearer tokens
- OAuth 2.0

### Pipeline Endpoints

#### Get All Pipelines
```http
GET /api/pipelines?page=1&pageSize=20&search=customer&isEnabled=true
```

**Response:**
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Customer Data Pipeline",
      "description": "Process customer data",
      "isEnabled": true,
      "createdAt": "2024-01-15T10:30:00Z",
      "modifiedAt": "2024-01-15T10:30:00Z",
      "lastExecutedAt": "2024-01-15T12:00:00Z",
      "statistics": {
        "totalExecutions": 25,
        "successfulExecutions": 24,
        "failedExecutions": 1,
        "averageExecutionTime": "00:02:30"
      }
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

#### Get Pipeline by ID
```http
GET /api/pipelines/{id}
```

#### Create Pipeline
```http
POST /api/pipelines
Content-Type: application/json

{
  "name": "New Pipeline",
  "description": "Pipeline description",
  "sourceConnector": {
    "type": "CSV",
    "configuration": {
      "filePath": "data.csv",
      "hasHeaders": true
    }
  },
  "targetConnector": {
    "type": "SqlServer",
    "configuration": {
      "connectionString": "Server=localhost;Database=ETL;Trusted_Connection=true;",
      "tableName": "ProcessedData"
    }
  },
  "transformations": [
    {
      "name": "Clean Data",
      "type": "DataCleaning",
      "configuration": {
        "operations": {
          "Name": ["trim", "titleCase"]
        }
      }
    }
  ],
  "isEnabled": true
}
```

#### Update Pipeline
```http
PUT /api/pipelines/{id}
Content-Type: application/json
```

#### Delete Pipeline
```http
DELETE /api/pipelines/{id}
```

#### Execute Pipeline
```http
POST /api/pipelines/{id}/execute
Content-Type: application/json

{
  "parameters": {
    "batchSize": 500,
    "skipValidation": false
  },
  "async": true,
  "timeoutSeconds": 3600
}
```

**Response:**
```json
{
  "executionId": "123e4567-e89b-12d3-a456-426614174000",
  "pipelineId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Running",
  "startTime": "2024-01-15T14:30:00Z",
  "estimatedDuration": "00:02:30"
}
```

#### Get Pipeline Executions
```http
GET /api/pipelines/{id}/executions?page=1&pageSize=10
```

#### Get Execution Details
```http
GET /api/pipelines/{pipelineId}/executions/{executionId}
```

### Connector Endpoints

#### Get Supported Connector Types
```http
GET /api/connectors/types
```

#### Test Connector Connection
```http
POST /api/connectors/test
Content-Type: application/json

{
  "connectorType": "SqlServer",
  "connectionString": "Server=localhost;Database=Test;Trusted_Connection=true;",
  "connectionProperties": {}
}
```

#### Get Connector Template
```http
GET /api/connectors/template/{connectorType}
```

### Monitoring Endpoints

#### Get System Health
```http
GET /api/monitoring/health
```

#### Get Pipeline Metrics
```http
GET /api/monitoring/pipelines/{id}/metrics
```

#### Get Execution Logs
```http
GET /api/monitoring/executions/{executionId}/logs
```

### Error Responses

All API endpoints return consistent error responses:

```json
{
  "error": {
    "code": "PIPELINE_NOT_FOUND",
    "message": "Pipeline with ID '123' not found",
    "details": {
      "pipelineId": "123",
      "timestamp": "2024-01-15T14:30:00Z"
    }
  }
}
```

Common HTTP status codes:
- `200 OK`: Success
- `201 Created`: Resource created
- `400 Bad Request`: Invalid request
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Access denied
- `404 Not Found`: Resource not found
- `409 Conflict`: Resource conflict
- `500 Internal Server Error`: Server error

## Deployment

The ETL Framework supports multiple deployment scenarios.

### Standalone Application

Run as a console application:

```bash
cd src/ETLFramework.Host
dotnet run
```

### Windows Service

Install as a Windows Service:

```bash
# Publish the application
dotnet publish -c Release -o ./publish

# Install as service (requires admin privileges)
sc create "ETL Framework" binPath="C:\path\to\publish\ETLFramework.Host.exe"
sc start "ETL Framework"
```

### Docker Deployment

#### Build Docker Image
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/ETLFramework.API/ETLFramework.API.csproj", "src/ETLFramework.API/"]
COPY ["src/ETLFramework.Core/ETLFramework.Core.csproj", "src/ETLFramework.Core/"]
# ... copy other project files
RUN dotnet restore "src/ETLFramework.API/ETLFramework.API.csproj"
COPY . .
WORKDIR "/src/src/ETLFramework.API"
RUN dotnet build "ETLFramework.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ETLFramework.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ETLFramework.API.dll"]
```

#### Docker Compose
```yaml
version: '3.8'
services:
  etl-api:
    build: .
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=ETL;User Id=sa;Password=YourPassword123;
    depends_on:
      - sql-server
      - redis
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs

  sql-server:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123
    ports:
      - "1433:1433"
    volumes:
      - sql-data:/var/opt/mssql

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  sql-data:
```

### Kubernetes Deployment

#### Deployment Manifest
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: etl-framework
  labels:
    app: etl-framework
spec:
  replicas: 3
  selector:
    matchLabels:
      app: etl-framework
  template:
    metadata:
      labels:
        app: etl-framework
    spec:
      containers:
      - name: etl-api
        image: etl-framework:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: etl-secrets
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: etl-framework-service
spec:
  selector:
    app: etl-framework
  ports:
  - protocol: TCP
    port: 80
    targetPort: 80
  type: LoadBalancer
```

### Environment Configuration

Configure the application using environment variables:

```bash
# Database Configuration
export ConnectionStrings__DefaultConnection="Server=localhost;Database=ETL;Trusted_Connection=true;"

# Logging Configuration
export Serilog__MinimumLevel="Information"
export Serilog__WriteTo__0__Name="Console"
export Serilog__WriteTo__1__Name="File"
export Serilog__WriteTo__1__Args__path="logs/etl-.log"

# API Configuration
export ASPNETCORE_URLS="https://localhost:5001;http://localhost:5000"
export ASPNETCORE_ENVIRONMENT="Production"

# ETL Framework Configuration
export ETL__MaxConcurrentPipelines="10"
export ETL__DefaultTimeout="01:00:00"
export ETL__EnableMetrics="true"
```

## Monitoring & Logging

The framework provides comprehensive monitoring and logging capabilities.

### Structured Logging

Uses Serilog for structured logging with multiple sinks:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "ETLFramework": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/etl-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ]
  }
}
```

### Performance Metrics

Track key performance indicators:

- **Pipeline Execution Time**: Total time for pipeline completion
- **Throughput**: Records processed per second
- **Error Rate**: Percentage of failed records
- **Resource Usage**: CPU, memory, and I/O utilization
- **Connection Pool Statistics**: Database connection usage

### Health Checks

Built-in health checks for system components:

```csharp
// Health check endpoint: /health
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0050000",
      "data": {
        "connectionString": "Server=localhost;Database=ETL;..."
      }
    },
    "connectors": {
      "status": "Healthy",
      "duration": "00:00:00.0030000",
      "data": {
        "registeredConnectors": 8,
        "healthyConnectors": 8
      }
    }
  }
}
```

### Prometheus Integration

Export metrics to Prometheus:

```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'etl-framework'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: '/metrics'
    scrape_interval: 5s
```

### Alerting Rules

Configure alerting for critical events:

```yaml
# etl_rules.yml
groups:
  - name: etl_framework
    rules:
      - alert: PipelineExecutionFailed
        expr: etl_pipeline_execution_failures_total > 0
        for: 0m
        labels:
          severity: critical
        annotations:
          summary: "ETL Pipeline execution failed"
          description: "Pipeline {{ $labels.pipeline_name }} has failed execution"

      - alert: HighErrorRate
        expr: (etl_pipeline_errors_total / etl_pipeline_records_total) > 0.05
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High error rate in ETL pipeline"
          description: "Error rate is {{ $value | humanizePercentage }} for pipeline {{ $labels.pipeline_name }}"
```

## Extensibility

The framework is designed for extensibility through a plugin-based architecture.

### Custom Connectors

Create custom connectors by implementing the connector interfaces:

```csharp
public class CustomApiConnector : ISourceConnector<ApiRecord>, IDestinationConnector<ApiRecord>
{
    private readonly IConnectorConfiguration _configuration;
    private readonly ILogger<CustomApiConnector> _logger;
    private readonly HttpClient _httpClient;

    public CustomApiConnector(
        IConnectorConfiguration configuration,
        ILogger<CustomApiConnector> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "Custom API Connector";
    public string ConnectorType => "CustomApi";
    public IConnectorConfiguration Configuration => _configuration;

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_configuration.ConnectionString}/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test API connection");
            return false;
        }
    }

    public async IAsyncEnumerable<ApiRecord> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var endpoint = _configuration.ConnectionProperties["endpoint"].ToString();
        var response = await _httpClient.GetAsync($"{_configuration.ConnectionString}/{endpoint}", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var records = JsonSerializer.Deserialize<ApiRecord[]>(content);

            foreach (var record in records ?? Array.Empty<ApiRecord>())
            {
                yield return record;
            }
        }
    }

    public async Task WriteAsync(IAsyncEnumerable<ApiRecord> records, CancellationToken cancellationToken = default)
    {
        var endpoint = _configuration.ConnectionProperties["endpoint"].ToString();

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            var json = JsonSerializer.Serialize(record);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_configuration.ConnectionString}/{endpoint}", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }

    public Task<DataSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        // Return schema information
        return Task.FromResult(new DataSchema
        {
            Fields = new List<DataField>
            {
                new() { Name = "Id", DataType = typeof(int), IsRequired = true },
                new() { Name = "Name", DataType = typeof(string), IsRequired = true },
                new() { Name = "CreatedAt", DataType = typeof(DateTime), IsRequired = false }
            }
        });
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
```

### Register Custom Connector

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<IConnectorFactory>(provider =>
{
    var factory = new ConnectorFactory(provider, provider.GetRequiredService<ILogger<ConnectorFactory>>());

    // Register custom connector
    factory.RegisterConnector("CustomApi", config => new CustomApiConnector(
        config,
        provider.GetRequiredService<ILogger<CustomApiConnector>>(),
        provider.GetRequiredService<HttpClient>()));

    return factory;
});
```

### Custom Transformations

Create custom transformations:

```csharp
public class CustomBusinessRuleTransformation : ITransformationRule
{
    public string Name => "CustomBusinessRule";

    public Task<TransformationResult> ApplyAsync(
        DataRecord record,
        ITransformationRuleConfiguration config)
    {
        try
        {
            // Apply custom business logic
            var customerType = DetermineCustomerType(record);
            record.SetValue("CustomerType", customerType);

            var discount = CalculateDiscount(record);
            record.SetValue("Discount", discount);

            return Task.FromResult(new TransformationResult
            {
                IsSuccess = true,
                TransformedRecord = record
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TransformationResult
            {
                IsSuccess = false,
                Error = ex.Message,
                TransformedRecord = record
            });
        }
    }

    private string DetermineCustomerType(DataRecord record)
    {
        var totalOrders = record.GetValue<int>("TotalOrders");
        var totalSpent = record.GetValue<decimal>("TotalSpent");

        return (totalOrders, totalSpent) switch
        {
            (>= 50, >= 10000) => "VIP",
            (>= 20, >= 5000) => "Premium",
            (>= 5, >= 1000) => "Regular",
            _ => "New"
        };
    }

    private decimal CalculateDiscount(DataRecord record)
    {
        var customerType = record.GetValue<string>("CustomerType");

        return customerType switch
        {
            "VIP" => 0.15m,
            "Premium" => 0.10m,
            "Regular" => 0.05m,
            _ => 0.00m
        };
    }
}
```

### Plugin Architecture

Create plugins as separate assemblies:

```csharp
// MyETLPlugin.dll
[assembly: ETLPlugin]

public class MyETLPlugin : IETLPlugin
{
    public string Name => "My Custom ETL Plugin";
    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<CustomBusinessRuleTransformation>();
        services.AddTransient<CustomApiConnector>();
    }

    public void Configure(IETLFrameworkBuilder builder)
    {
        builder.AddTransformation<CustomBusinessRuleTransformation>();
        builder.AddConnector<CustomApiConnector>("CustomApi");
    }
}
```

## Best Practices

### Configuration Management

1. **Use Environment Variables**: Store sensitive information in environment variables
2. **Configuration Validation**: Always validate configurations before execution
3. **Version Control**: Keep configuration files in version control
4. **Environment-Specific Configs**: Use different configurations for dev/test/prod

### Performance Optimization

1. **Batch Processing**: Use appropriate batch sizes for optimal performance
2. **Parallel Processing**: Enable parallelism for CPU-intensive operations
3. **Connection Pooling**: Use connection pooling for database connectors
4. **Memory Management**: Monitor memory usage and implement proper disposal patterns

### Error Handling

1. **Graceful Degradation**: Design pipelines to handle partial failures
2. **Retry Logic**: Implement exponential backoff for transient failures
3. **Dead Letter Queues**: Use dead letter queues for failed messages
4. **Comprehensive Logging**: Log all errors with sufficient context

### Security

1. **Principle of Least Privilege**: Grant minimum required permissions
2. **Secure Connections**: Use encrypted connections for data transfer
3. **Credential Management**: Use secure credential storage (Azure Key Vault, etc.)
4. **Input Validation**: Validate all input data and configurations

### Testing

1. **Unit Tests**: Test individual components in isolation
2. **Integration Tests**: Test end-to-end pipeline execution
3. **Performance Tests**: Validate performance under load
4. **Data Quality Tests**: Verify data integrity and accuracy

## Troubleshooting

### Common Issues

#### Pipeline Execution Failures

**Symptom**: Pipeline fails during execution
**Possible Causes**:
- Invalid configuration
- Connection issues
- Data format problems
- Insufficient permissions

**Solutions**:
1. Check pipeline configuration validation
2. Test connector connections
3. Verify data source format
4. Review security permissions

#### Performance Issues

**Symptom**: Slow pipeline execution
**Possible Causes**:
- Large batch sizes
- Inefficient transformations
- Database bottlenecks
- Memory constraints

**Solutions**:
1. Optimize batch sizes
2. Review transformation logic
3. Add database indexes
4. Increase memory allocation

#### Connection Timeouts

**Symptom**: Connector connection timeouts
**Possible Causes**:
- Network latency
- Database overload
- Incorrect timeout settings
- Firewall restrictions

**Solutions**:
1. Increase timeout values
2. Check network connectivity
3. Optimize database queries
4. Review firewall rules

### Diagnostic Tools

#### Health Check Endpoint
```bash
curl https://localhost:5001/health
```

#### Pipeline Validation
```bash
curl -X POST https://localhost:5001/api/pipelines/validate \
  -H "Content-Type: application/json" \
  -d @pipeline-config.json
```

#### Connection Testing
```bash
curl -X POST https://localhost:5001/api/connectors/test \
  -H "Content-Type: application/json" \
  -d '{
    "connectorType": "SqlServer",
    "connectionString": "Server=localhost;Database=Test;Trusted_Connection=true;"
  }'
```

### Log Analysis

Use structured logging queries to diagnose issues:

```sql
-- Find failed pipeline executions
SELECT * FROM Logs
WHERE Level = 'Error'
  AND SourceContext LIKE 'ETLFramework.Pipeline%'
  AND Timestamp > DATEADD(hour, -24, GETDATE())

-- Analyze performance metrics
SELECT
  PipelineId,
  AVG(ExecutionTimeMs) as AvgExecutionTime,
  COUNT(*) as ExecutionCount
FROM PipelineExecutions
WHERE CreatedAt > DATEADD(day, -7, GETDATE())
GROUP BY PipelineId
```

### Support Resources

- **Documentation**: Comprehensive API documentation at `/swagger`
- **Sample Configurations**: Example configurations in `/samples`
- **Unit Tests**: Reference implementations in test projects
- **Community**: GitHub discussions and issues
- **Professional Support**: Enterprise support available

---

## Conclusion

The ETL Framework provides a robust, scalable solution for data integration needs. Its modular architecture, comprehensive API, and extensive configuration options make it suitable for both simple data migration tasks and complex enterprise data processing workflows.

For additional support or questions, please refer to the project documentation or contact the development team.
