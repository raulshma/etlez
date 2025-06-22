# ETL Framework - API Reference

This document provides detailed information about the ETL Framework REST API endpoints.

## Base Information

- **Base URL**: `https://localhost:5001/api`
- **Content Type**: `application/json`
- **Authentication**: API Key, JWT Bearer Token, or OAuth 2.0

## Authentication

### API Key Authentication
```http
GET /api/pipelines
X-API-Key: your-api-key-here
```

### JWT Bearer Token
```http
GET /api/pipelines
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Pipeline Management

### Get All Pipelines

**Endpoint**: `GET /api/pipelines`

**Parameters**:
- `page` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Items per page (default: 20, max: 100)
- `search` (string, optional): Search term for pipeline name/description
- `isEnabled` (bool, optional): Filter by enabled status
- `tags` (string[], optional): Filter by tags

**Example Request**:
```http
GET /api/pipelines?page=1&pageSize=10&search=customer&isEnabled=true
```

**Response**:
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Customer Data Pipeline",
      "description": "Process customer data from CSV to database",
      "isEnabled": true,
      "createdAt": "2024-01-15T10:30:00Z",
      "modifiedAt": "2024-01-15T10:30:00Z",
      "lastExecutedAt": "2024-01-15T12:00:00Z",
      "tags": ["customer", "daily"],
      "statistics": {
        "totalExecutions": 25,
        "successfulExecutions": 24,
        "failedExecutions": 1,
        "averageExecutionTime": "00:02:30",
        "lastExecutionStatus": "Success"
      }
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### Get Pipeline by ID

**Endpoint**: `GET /api/pipelines/{id}`

**Parameters**:
- `id` (string, required): Pipeline ID

**Response**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Customer Data Pipeline",
  "description": "Process customer data from CSV to database",
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
  ],
  "isEnabled": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "modifiedAt": "2024-01-15T10:30:00Z"
}
```

### Create Pipeline

**Endpoint**: `POST /api/pipelines`

**Request Body**:
```json
{
  "name": "New Pipeline",
  "description": "Pipeline description",
  "sourceConnector": {
    "type": "CSV",
    "configuration": {
      "filePath": "data.csv",
      "hasHeaders": true,
      "delimiter": ",",
      "encoding": "UTF-8"
    }
  },
  "targetConnector": {
    "type": "SqlServer",
    "configuration": {
      "connectionString": "Server=localhost;Database=ETL;Trusted_Connection=true;",
      "tableName": "ProcessedData",
      "writeMode": "Insert",
      "batchSize": 1000
    }
  },
  "transformations": [
    {
      "name": "Clean Data",
      "type": "DataCleaning",
      "configuration": {
        "operations": {
          "Name": ["trim", "titleCase"],
          "Email": ["trim", "toLowerCase"]
        }
      }
    }
  ],
  "configuration": {
    "timeout": "01:00:00",
    "maxDegreeOfParallelism": 4,
    "enableMetrics": true
  },
  "isEnabled": true,
  "tags": ["import", "customer-data"]
}
```

**Response**: `201 Created`
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "New Pipeline",
  "description": "Pipeline description",
  "isEnabled": true,
  "createdAt": "2024-01-15T14:30:00Z",
  "modifiedAt": "2024-01-15T14:30:00Z"
}
```

### Update Pipeline

**Endpoint**: `PUT /api/pipelines/{id}`

**Parameters**:
- `id` (string, required): Pipeline ID

**Request Body**: Same as Create Pipeline

**Response**: `200 OK` with updated pipeline data

### Delete Pipeline

**Endpoint**: `DELETE /api/pipelines/{id}`

**Parameters**:
- `id` (string, required): Pipeline ID

**Response**: `204 No Content`

### Execute Pipeline

**Endpoint**: `POST /api/pipelines/{id}/execute`

**Parameters**:
- `id` (string, required): Pipeline ID

**Request Body**:
```json
{
  "parameters": {
    "batchSize": 500,
    "skipValidation": false,
    "customParameter": "value"
  },
  "async": true,
  "timeoutSeconds": 3600,
  "priority": "Normal"
}
```

**Response**: `200 OK`
```json
{
  "executionId": "123e4567-e89b-12d3-a456-426614174000",
  "pipelineId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Running",
  "startTime": "2024-01-15T14:30:00Z",
  "estimatedDuration": "00:02:30",
  "progress": {
    "currentStage": "Extract",
    "completedStages": 0,
    "totalStages": 3,
    "recordsProcessed": 0,
    "estimatedTotalRecords": 1000
  }
}
```

### Get Pipeline Executions

**Endpoint**: `GET /api/pipelines/{id}/executions`

**Parameters**:
- `id` (string, required): Pipeline ID
- `page` (int, optional): Page number
- `pageSize` (int, optional): Items per page
- `status` (string, optional): Filter by execution status
- `startDate` (datetime, optional): Filter executions after this date
- `endDate` (datetime, optional): Filter executions before this date

**Response**:
```json
{
  "data": [
    {
      "executionId": "123e4567-e89b-12d3-a456-426614174000",
      "pipelineId": "550e8400-e29b-41d4-a716-446655440000",
      "status": "Completed",
      "startTime": "2024-01-15T14:30:00Z",
      "endTime": "2024-01-15T14:32:30Z",
      "duration": "00:02:30",
      "recordsProcessed": 1000,
      "recordsSuccessful": 995,
      "recordsFailed": 5,
      "parameters": {
        "batchSize": 500
      }
    }
  ],
  "totalCount": 25,
  "page": 1,
  "pageSize": 20
}
```

### Get Execution Details

**Endpoint**: `GET /api/pipelines/{pipelineId}/executions/{executionId}`

**Parameters**:
- `pipelineId` (string, required): Pipeline ID
- `executionId` (string, required): Execution ID

**Response**:
```json
{
  "executionId": "123e4567-e89b-12d3-a456-426614174000",
  "pipelineId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "startTime": "2024-01-15T14:30:00Z",
  "endTime": "2024-01-15T14:32:30Z",
  "duration": "00:02:30",
  "stages": [
    {
      "stageName": "Extract CSV Data",
      "status": "Completed",
      "startTime": "2024-01-15T14:30:00Z",
      "endTime": "2024-01-15T14:30:45Z",
      "duration": "00:00:45",
      "recordsProcessed": 1000,
      "recordsSuccessful": 1000,
      "recordsFailed": 0
    },
    {
      "stageName": "Transform Data",
      "status": "Completed",
      "startTime": "2024-01-15T14:30:45Z",
      "endTime": "2024-01-15T14:31:30Z",
      "duration": "00:00:45",
      "recordsProcessed": 1000,
      "recordsSuccessful": 995,
      "recordsFailed": 5
    },
    {
      "stageName": "Load to Database",
      "status": "Completed",
      "startTime": "2024-01-15T14:31:30Z",
      "endTime": "2024-01-15T14:32:30Z",
      "duration": "00:01:00",
      "recordsProcessed": 995,
      "recordsSuccessful": 995,
      "recordsFailed": 0
    }
  ],
  "metrics": {
    "totalRecordsProcessed": 1000,
    "totalRecordsSuccessful": 995,
    "totalRecordsFailed": 5,
    "averageRecordsPerSecond": 6.67,
    "peakMemoryUsage": "256MB",
    "averageCpuUsage": "45%"
  },
  "errors": [
    {
      "stageName": "Transform Data",
      "recordNumber": 156,
      "errorMessage": "Invalid email format",
      "errorCode": "VALIDATION_ERROR",
      "timestamp": "2024-01-15T14:31:15Z"
    }
  ]
}
```

## Connector Management

### Get Supported Connector Types

**Endpoint**: `GET /api/connectors/types`

**Response**:
```json
{
  "connectorTypes": [
    {
      "type": "CSV",
      "name": "CSV File Connector",
      "description": "Read/write CSV files",
      "capabilities": ["Source", "Destination"],
      "configurationSchema": {
        "filePath": {
          "type": "string",
          "required": true,
          "description": "Path to the CSV file"
        },
        "hasHeaders": {
          "type": "boolean",
          "required": false,
          "default": true,
          "description": "Whether the CSV file has headers"
        }
      }
    }
  ]
}
```

### Test Connector Connection

**Endpoint**: `POST /api/connectors/test`

**Request Body**:
```json
{
  "connectorType": "SqlServer",
  "connectionString": "Server=localhost;Database=Test;Trusted_Connection=true;",
  "connectionProperties": {
    "tableName": "TestTable"
  }
}
```

**Response**:
```json
{
  "isSuccessful": true,
  "message": "Connection successful",
  "details": {
    "serverVersion": "Microsoft SQL Server 2022",
    "databaseName": "Test",
    "connectionTime": "00:00:00.123"
  }
}
```

### Get Connector Template

**Endpoint**: `GET /api/connectors/template/{connectorType}`

**Parameters**:
- `connectorType` (string, required): Type of connector
- `parameters` (query string, optional): Template parameters

**Example**: `GET /api/connectors/template/SqlServer?serverName=localhost&databaseName=TestDB`

**Response**:
```json
{
  "connectorType": "SqlServer",
  "connectionString": "Server=localhost;Database=TestDB;Trusted_Connection=true;TrustServerCertificate=true",
  "connectionTimeout": "00:00:30",
  "commandTimeout": "00:05:00",
  "connectionProperties": {
    "tableName": "",
    "writeMode": "Insert",
    "batchSize": 1000
  },
  "recommendedSettings": {
    "useConnectionPooling": true,
    "maxPoolSize": 100,
    "enableDetailedLogging": false
  }
}
```

## Monitoring and Health

### System Health Check

**Endpoint**: `GET /api/monitoring/health`

**Response**:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0050000",
      "data": {
        "connectionString": "Server=localhost;Database=ETL;...",
        "serverVersion": "Microsoft SQL Server 2022"
      }
    },
    "connectors": {
      "status": "Healthy",
      "duration": "00:00:00.0030000",
      "data": {
        "registeredConnectors": 8,
        "healthyConnectors": 8
      }
    },
    "memory": {
      "status": "Healthy",
      "duration": "00:00:00.0010000",
      "data": {
        "totalMemory": "8GB",
        "usedMemory": "2.1GB",
        "availableMemory": "5.9GB"
      }
    }
  }
}
```

### Get Pipeline Metrics

**Endpoint**: `GET /api/monitoring/pipelines/{id}/metrics`

**Parameters**:
- `id` (string, required): Pipeline ID
- `startDate` (datetime, optional): Start date for metrics
- `endDate` (datetime, optional): End date for metrics
- `granularity` (string, optional): Metrics granularity (Hour, Day, Week)

**Response**:
```json
{
  "pipelineId": "550e8400-e29b-41d4-a716-446655440000",
  "timeRange": {
    "startDate": "2024-01-01T00:00:00Z",
    "endDate": "2024-01-15T23:59:59Z"
  },
  "metrics": {
    "totalExecutions": 150,
    "successfulExecutions": 147,
    "failedExecutions": 3,
    "successRate": 98.0,
    "averageExecutionTime": "00:02:45",
    "totalRecordsProcessed": 150000,
    "averageRecordsPerExecution": 1000,
    "peakRecordsPerSecond": 15.5
  },
  "trends": [
    {
      "date": "2024-01-15",
      "executions": 10,
      "successRate": 100.0,
      "averageExecutionTime": "00:02:30",
      "recordsProcessed": 10000
    }
  ]
}
```

### Get Execution Logs

**Endpoint**: `GET /api/monitoring/executions/{executionId}/logs`

**Parameters**:
- `executionId` (string, required): Execution ID
- `level` (string, optional): Log level filter (Debug, Information, Warning, Error)
- `page` (int, optional): Page number
- `pageSize` (int, optional): Items per page

**Response**:
```json
{
  "data": [
    {
      "timestamp": "2024-01-15T14:30:00.123Z",
      "level": "Information",
      "message": "Starting pipeline execution",
      "source": "ETLFramework.Pipeline.Pipeline",
      "properties": {
        "PipelineId": "550e8400-e29b-41d4-a716-446655440000",
        "ExecutionId": "123e4567-e89b-12d3-a456-426614174000"
      }
    }
  ],
  "totalCount": 245,
  "page": 1,
  "pageSize": 50
}
```

## Error Responses

All API endpoints return consistent error responses:

### Validation Error (400 Bad Request)
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": {
      "Name": ["The Name field is required"],
      "SourceConnector.Type": ["The Type field is required"]
    }
  }
}
```

### Not Found (404 Not Found)
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

### Server Error (500 Internal Server Error)
```json
{
  "error": {
    "code": "INTERNAL_SERVER_ERROR",
    "message": "An unexpected error occurred",
    "details": {
      "correlationId": "abc123-def456-ghi789",
      "timestamp": "2024-01-15T14:30:00Z"
    }
  }
}
```

## Rate Limiting

The API implements rate limiting to ensure fair usage:

- **Rate Limit**: 1000 requests per hour per API key
- **Headers**: 
  - `X-RateLimit-Limit`: Maximum requests per hour
  - `X-RateLimit-Remaining`: Remaining requests in current window
  - `X-RateLimit-Reset`: Time when the rate limit resets

When rate limit is exceeded, the API returns `429 Too Many Requests`:

```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded. Try again later.",
    "details": {
      "retryAfter": 3600
    }
  }
}
```

## OpenAPI/Swagger

The complete API specification is available in OpenAPI 3.0 format at:
- **Swagger UI**: `https://localhost:5001/`
- **OpenAPI JSON**: `https://localhost:5001/swagger/v1/swagger.json`

This provides interactive documentation and the ability to test API endpoints directly from the browser.
