# ETL Framework - Configuration Guide

This guide covers all configuration options and formats supported by the ETL Framework.

## Configuration Formats

The ETL Framework supports both JSON and YAML configuration formats with environment variable substitution.

### JSON Configuration

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Sample Data Pipeline",
  "description": "A comprehensive ETL pipeline example",
  "version": "1.0.0",
  "author": "ETL Framework Team",
  "isEnabled": true,
  "stages": [...],
  "globalSettings": {...},
  "errorHandling": {...},
  "retry": {...},
  "timeout": "01:00:00",
  "schedule": {...},
  "notifications": {...},
  "tags": [...]
}
```

### YAML Configuration

```yaml
name: "Sample Data Pipeline"
description: "A comprehensive ETL pipeline example"
version: "1.0.0"
author: "ETL Framework Team"
isEnabled: true

stages:
  - name: "Extract Data"
    stageType: "Extract"
    order: 1
    # ... stage configuration

globalSettings:
  parallelism: 4
  bufferSize: 10000

errorHandling:
  stopOnError: false
  maxErrors: 100

retry:
  maxAttempts: 3
  delay: "00:00:10"
```

## Environment Variable Substitution

Configuration files support environment variable substitution using the following syntax:

- `${VAR_NAME}`: Required variable (throws error if not found)
- `${VAR_NAME:default_value}`: Variable with default value
- `${VAR_NAME:}`: Variable with empty string default

### Examples

```json
{
  "connectionString": "Server=${DB_SERVER:localhost};Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD}",
  "filePath": "${DATA_PATH:/data/input}/customers.csv",
  "batchSize": "${BATCH_SIZE:1000}",
  "enableLogging": "${ENABLE_LOGGING:true}"
}
```

## Pipeline Configuration

### Basic Pipeline Properties

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Customer Data Pipeline",
  "description": "Process customer data from CSV to database",
  "version": "1.0.0",
  "author": "Data Engineering Team",
  "isEnabled": true,
  "tags": ["customer-data", "daily-etl", "production"]
}
```

**Properties:**
- `id` (string, optional): Unique pipeline identifier (auto-generated if not provided)
- `name` (string, required): Human-readable pipeline name
- `description` (string, optional): Pipeline description
- `version` (string, optional): Pipeline version (default: "1.0.0")
- `author` (string, optional): Pipeline author
- `isEnabled` (boolean, optional): Whether pipeline is enabled (default: true)
- `tags` (string[], optional): Tags for categorization and filtering

### Pipeline Stages

Pipelines consist of ordered stages that execute sequentially:

```json
{
  "stages": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "name": "Extract CSV Data",
      "description": "Extract data from CSV file",
      "stageType": "Extract",
      "order": 1,
      "isEnabled": true,
      "connectorConfiguration": {
        // Connector configuration
      },
      "settings": {
        "skipEmptyRows": true,
        "trimWhitespace": true
      }
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "name": "Transform Data",
      "description": "Apply transformations",
      "stageType": "Transform",
      "order": 2,
      "isEnabled": true,
      "transformationConfiguration": {
        // Transformation configuration
      },
      "settings": {
        "continueOnValidationError": true,
        "logValidationErrors": true
      }
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440003",
      "name": "Load to Database",
      "description": "Load data to destination",
      "stageType": "Load",
      "order": 3,
      "isEnabled": true,
      "connectorConfiguration": {
        // Connector configuration
      },
      "settings": {
        "createTableIfNotExists": true,
        "truncateBeforeLoad": false
      }
    }
  ]
}
```

**Stage Properties:**
- `id` (string, optional): Unique stage identifier
- `name` (string, required): Stage name
- `description` (string, optional): Stage description
- `stageType` (string, required): Stage type ("Extract", "Transform", "Load")
- `order` (int, required): Execution order
- `isEnabled` (boolean, optional): Whether stage is enabled (default: true)
- `connectorConfiguration` (object, conditional): Required for Extract/Load stages
- `transformationConfiguration` (object, conditional): Required for Transform stages
- `settings` (object, optional): Stage-specific settings

### Global Settings

Configure pipeline-wide settings:

```json
{
  "globalSettings": {
    "parallelism": 4,
    "maxDegreeOfParallelism": 8,
    "bufferSize": 10000,
    "enableMetrics": true,
    "metricsInterval": "00:01:00",
    "enableParallelStages": false,
    "memoryLimit": "2GB",
    "tempDirectory": "${TEMP_PATH:/tmp/etl}",
    "culture": "en-US",
    "timeZone": "UTC"
  }
}
```

**Global Settings Properties:**
- `parallelism` (int): Number of parallel processing threads
- `maxDegreeOfParallelism` (int): Maximum degree of parallelism
- `bufferSize` (int): Internal buffer size for data processing
- `enableMetrics` (boolean): Enable performance metrics collection
- `metricsInterval` (timespan): Metrics collection interval
- `enableParallelStages` (boolean): Allow parallel stage execution
- `memoryLimit` (string): Maximum memory usage limit
- `tempDirectory` (string): Temporary directory for processing
- `culture` (string): Culture for data formatting
- `timeZone` (string): Time zone for date/time processing

### Error Handling Configuration

Configure how the pipeline handles errors:

```json
{
  "errorHandling": {
    "stopOnError": false,
    "maxErrors": 100,
    "errorThreshold": 0.05,
    "logErrors": true,
    "errorLogLevel": "Warning",
    "continueOnStageFailure": false,
    "errorOutputPath": "${ERROR_PATH:/logs/errors}",
    "notifyOnError": true,
    "errorNotificationThreshold": 10
  }
}
```

**Error Handling Properties:**
- `stopOnError` (boolean): Stop pipeline on first error
- `maxErrors` (int): Maximum number of errors before stopping
- `errorThreshold` (decimal): Error rate threshold (0.0-1.0)
- `logErrors` (boolean): Log error details
- `errorLogLevel` (string): Log level for errors
- `continueOnStageFailure` (boolean): Continue to next stage on failure
- `errorOutputPath` (string): Path for error output files
- `notifyOnError` (boolean): Send notifications on errors
- `errorNotificationThreshold` (int): Error count threshold for notifications

### Retry Configuration

Configure retry behavior for transient failures:

```json
{
  "retry": {
    "maxAttempts": 3,
    "delay": "00:00:10",
    "backoffMultiplier": 2.0,
    "maxDelay": "00:05:00",
    "retryOnExceptions": [
      "System.TimeoutException",
      "System.Net.Http.HttpRequestException",
      "Microsoft.Data.SqlClient.SqlException"
    ],
    "jitterEnabled": true
  }
}
```

**Retry Properties:**
- `maxAttempts` (int): Maximum retry attempts
- `delay` (timespan): Initial delay between retries
- `backoffMultiplier` (decimal): Exponential backoff multiplier
- `maxDelay` (timespan): Maximum delay between retries
- `retryOnExceptions` (string[]): Exception types to retry on
- `jitterEnabled` (boolean): Add random jitter to delays

### Timeout Configuration

```json
{
  "timeout": "01:00:00",
  "stageTimeouts": {
    "Extract": "00:15:00",
    "Transform": "00:30:00",
    "Load": "00:15:00"
  },
  "connectionTimeout": "00:00:30",
  "commandTimeout": "00:05:00"
}
```

### Scheduling Configuration

Configure pipeline scheduling:

```json
{
  "schedule": {
    "cronExpression": "0 2 * * *",
    "isEnabled": false,
    "timeZone": "UTC",
    "startDate": "2024-01-01T00:00:00Z",
    "endDate": "2024-12-31T23:59:59Z",
    "maxConcurrentExecutions": 1,
    "skipIfRunning": true,
    "retryFailedExecutions": true,
    "retryDelay": "00:15:00"
  }
}
```

**Schedule Properties:**
- `cronExpression` (string): Cron expression for scheduling
- `isEnabled` (boolean): Enable/disable scheduling
- `timeZone` (string): Time zone for schedule
- `startDate` (datetime): Schedule start date
- `endDate` (datetime): Schedule end date
- `maxConcurrentExecutions` (int): Maximum concurrent executions
- `skipIfRunning` (boolean): Skip execution if already running
- `retryFailedExecutions` (boolean): Retry failed scheduled executions
- `retryDelay` (timespan): Delay before retrying failed executions

### Notification Configuration

Configure notifications for pipeline events:

```json
{
  "notifications": {
    "enableEmailNotifications": true,
    "emailRecipients": [
      "admin@company.com",
      "dataops@company.com"
    ],
    "emailSettings": {
      "smtpServer": "${SMTP_SERVER:localhost}",
      "smtpPort": "${SMTP_PORT:587}",
      "username": "${SMTP_USER}",
      "password": "${SMTP_PASSWORD}",
      "enableSsl": true,
      "fromAddress": "etl-framework@company.com",
      "fromName": "ETL Framework"
    },
    "notifyOnSuccess": false,
    "notifyOnFailure": true,
    "notifyOnWarning": true,
    "includeExecutionDetails": true,
    "includeErrorDetails": true,
    "webhookUrl": "${WEBHOOK_URL}",
    "slackWebhookUrl": "${SLACK_WEBHOOK_URL}",
    "teamsWebhookUrl": "${TEAMS_WEBHOOK_URL}"
  }
}
```

## Connector Configuration

### File System Connectors

#### CSV Connector Configuration

```json
{
  "connectorConfiguration": {
    "id": "550e8400-e29b-41d4-a716-446655440010",
    "name": "CSV Source Connector",
    "connectorType": "CSV",
    "description": "Connector for reading CSV files",
    "connectionString": "FilePath=${DATA_PATH:/data/input}/customers.csv",
    "connectionProperties": {
      "hasHeaders": true,
      "delimiter": ",",
      "quoteChar": "\"",
      "escapeChar": "\\",
      "encoding": "UTF-8",
      "skipEmptyRows": true,
      "trimWhitespace": true,
      "dateFormat": "yyyy-MM-dd",
      "numberFormat": "en-US",
      "nullValues": ["", "NULL", "null", "N/A"]
    },
    "batchSize": 1000,
    "enableDetailedLogging": false,
    "tags": ["csv", "source"]
  }
}
```

#### JSON Connector Configuration

```json
{
  "connectorConfiguration": {
    "connectorType": "JSON",
    "connectionString": "FilePath=${DATA_PATH}/products.json",
    "connectionProperties": {
      "jsonPath": "$.products[*]",
      "encoding": "UTF-8",
      "prettyPrint": false,
      "arrayHandling": "ProcessElements",
      "dateFormat": "ISO8601",
      "numberFormat": "en-US",
      "booleanFormat": "TrueFalse"
    }
  }
}
```

#### XML Connector Configuration

```json
{
  "connectorConfiguration": {
    "connectorType": "XML",
    "connectionString": "FilePath=${DATA_PATH}/orders.xml",
    "connectionProperties": {
      "rootElement": "orders",
      "recordElement": "order",
      "encoding": "UTF-8",
      "validateSchema": false,
      "schemaPath": "${SCHEMA_PATH}/order-schema.xsd",
      "namespaceHandling": "Ignore",
      "attributeHandling": "Include"
    }
  }
}
```

### Database Connectors

#### SQL Server Connector Configuration

```json
{
  "connectorConfiguration": {
    "connectorType": "SqlServer",
    "connectionString": "Server=${DB_SERVER};Database=${DB_NAME};Integrated Security=true;TrustServerCertificate=true",
    "connectionTimeout": "00:00:30",
    "commandTimeout": "00:05:00",
    "maxRetryAttempts": 3,
    "retryDelay": "00:00:05",
    "useConnectionPooling": true,
    "maxPoolSize": 50,
    "minPoolSize": 5,
    "connectionProperties": {
      "tableName": "Customers",
      "schema": "dbo",
      "writeMode": "Upsert",
      "keyColumns": ["CustomerId"],
      "query": "SELECT * FROM Customers WHERE ModifiedDate > @LastRunDate",
      "parameters": {
        "LastRunDate": "2024-01-01"
      },
      "bulkInsert": true,
      "bulkInsertTimeout": "00:10:00",
      "createTableIfNotExists": true,
      "truncateBeforeLoad": false
    },
    "batchSize": 500,
    "enableDetailedLogging": true
  }
}
```

#### MySQL Connector Configuration

```json
{
  "connectorConfiguration": {
    "connectorType": "MySQL",
    "connectionString": "Server=${MYSQL_HOST};Database=${MYSQL_DB};Uid=${MYSQL_USER};Pwd=${MYSQL_PASS};",
    "connectionProperties": {
      "tableName": "products",
      "writeMode": "Insert",
      "bulkInsert": true,
      "onDuplicateKeyUpdate": true,
      "charset": "utf8mb4"
    }
  }
}
```

#### PostgreSQL Connector Configuration

```json
{
  "connectorConfiguration": {
    "connectorType": "PostgreSQL",
    "connectionString": "Host=${PG_HOST};Database=${PG_DB};Username=${PG_USER};Password=${PG_PASS}",
    "connectionProperties": {
      "schema": "public",
      "tableName": "orders",
      "writeMode": "Merge",
      "conflictResolution": "Update",
      "copyOptions": "CSV HEADER"
    }
  }
}
```

### Cloud Storage Connectors

#### Azure Blob Storage Configuration

```json
{
  "connectorConfiguration": {
    "connectorType": "AzureBlob",
    "connectionString": "${AZURE_STORAGE_CONNECTION_STRING}",
    "connectionProperties": {
      "containerName": "data-files",
      "blobPrefix": "etl/input/",
      "downloadPath": "./temp/downloads",
      "uploadPath": "./temp/uploads",
      "deleteAfterProcessing": false,
      "createContainerIfNotExists": true,
      "accessTier": "Hot",
      "metadata": {
        "source": "etl-framework",
        "environment": "production"
      }
    }
  }
}
```

#### Amazon S3 Configuration

```json
{
  "connectorConfiguration": {
    "connectorType": "AwsS3",
    "connectionProperties": {
      "accessKey": "${AWS_ACCESS_KEY}",
      "secretKey": "${AWS_SECRET_KEY}",
      "region": "${AWS_REGION}",
      "bucketName": "etl-data-bucket",
      "prefix": "input/",
      "downloadPath": "./temp/downloads",
      "uploadPath": "./temp/uploads",
      "deleteAfterProcessing": false,
      "createBucketIfNotExists": false,
      "storageClass": "STANDARD",
      "serverSideEncryption": "AES256"
    }
  }
}
```

## Transformation Configuration

### Field Mapping Configuration

```json
{
  "transformationConfiguration": {
    "rules": [
      {
        "ruleType": "FieldMapping",
        "settings": {
          "mappings": {
            "customer_id": "CustomerId",
            "first_name": "FirstName",
            "last_name": "LastName",
            "email_address": "Email",
            "phone_number": "Phone"
          },
          "caseSensitive": false,
          "ignoreUnmappedFields": false,
          "defaultValues": {
            "Status": "Active",
            "CreatedDate": "NOW()"
          }
        }
      }
    ]
  }
}
```

### Data Validation Configuration

```json
{
  "transformationConfiguration": {
    "rules": [
      {
        "ruleType": "DataValidation",
        "settings": {
          "validations": {
            "Email": {
              "required": true,
              "pattern": "^[\\w\\.-]+@[\\w\\.-]+\\.[a-zA-Z]{2,}$",
              "maxLength": 255
            },
            "Phone": {
              "required": false,
              "pattern": "^\\+?[1-9]\\d{1,14}$",
              "minLength": 10,
              "maxLength": 15
            },
            "Age": {
              "required": true,
              "dataType": "integer",
              "minValue": 0,
              "maxValue": 150
            },
            "Salary": {
              "required": false,
              "dataType": "decimal",
              "minValue": 0,
              "maxValue": 1000000,
              "precision": 2
            }
          },
          "validationMode": "StrictMode",
          "continueOnValidationError": true,
          "logValidationErrors": true,
          "validationErrorAction": "LogAndContinue"
        }
      }
    ]
  }
}
```

### Data Cleaning Configuration

```json
{
  "transformationConfiguration": {
    "rules": [
      {
        "ruleType": "DataCleaning",
        "settings": {
          "operations": {
            "FirstName": ["trim", "titleCase", "removeExtraSpaces"],
            "LastName": ["trim", "titleCase", "removeExtraSpaces"],
            "Email": ["trim", "toLowerCase"],
            "Phone": ["trim", "removeNonDigits", "formatPhone"],
            "PostalCode": ["trim", "toUpper", "padLeft:6:0"]
          },
          "customOperations": {
            "formatPhone": {
              "type": "regex",
              "pattern": "(\\d{3})(\\d{3})(\\d{4})",
              "replacement": "($1) $2-$3"
            }
          }
        }
      }
    ]
  }
}
```

## Configuration Validation

The framework validates all configurations at runtime:

### Schema Validation
- JSON Schema validation for structure
- Required field validation
- Data type validation
- Format validation (dates, timeouts, etc.)

### Connection Testing
- Test database connections
- Validate file paths and permissions
- Check cloud storage credentials
- Verify API endpoints

### Dependency Checking
- Validate connector dependencies
- Check transformation rule dependencies
- Verify stage execution order
- Validate field mappings

### Performance Optimization
- Suggest optimal batch sizes
- Recommend connection pool settings
- Identify potential bottlenecks
- Memory usage optimization

## Configuration Best Practices

1. **Use Environment Variables**: Store sensitive information in environment variables
2. **Version Control**: Keep configuration files in version control
3. **Environment-Specific Configs**: Use different configurations for dev/test/prod
4. **Validate Early**: Test configurations before deployment
5. **Document Changes**: Maintain change logs for configuration updates
6. **Monitor Performance**: Regularly review and optimize configuration settings
7. **Security**: Never store passwords or secrets in configuration files
8. **Backup**: Maintain backups of working configurations
