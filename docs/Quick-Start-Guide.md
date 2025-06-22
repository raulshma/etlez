# ETL Framework - Quick Start Guide

This guide will help you get up and running with the ETL Framework in minutes.

## Prerequisites

- .NET 9 SDK or later
- Visual Studio 2022 or VS Code (optional)
- SQL Server, MySQL, or PostgreSQL (for database examples)

## Installation

### 1. Clone and Build

```bash
git clone https://github.com/your-org/etl-framework.git
cd etl-framework
dotnet build
```

### 2. Run the API

```bash
cd src/ETLFramework.API
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001` (root path)

## Your First Pipeline

### Scenario: CSV to Database

Let's create a simple pipeline that reads customer data from a CSV file and loads it into a SQL Server database.

### Step 1: Prepare Sample Data

Create a CSV file `customers.csv`:
```csv
customer_id,first_name,last_name,email,phone
1,John,Doe,john.doe@email.com,555-0101
2,Jane,Smith,jane.smith@email.com,555-0102
3,Bob,Johnson,bob.johnson@email.com,555-0103
```

### Step 2: Create Pipeline via API

```bash
curl -X POST https://localhost:5001/api/pipelines \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Customer Import Pipeline",
    "description": "Import customers from CSV to database",
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
        "connectionString": "Server=localhost;Database=SampleDB;Trusted_Connection=true;TrustServerCertificate=true",
        "tableName": "Customers",
        "writeMode": "Insert",
        "createTableIfNotExists": true
      }
    },
    "transformations": [
      {
        "name": "Field Mapping",
        "type": "FieldMapping",
        "configuration": {
          "mappings": {
            "customer_id": "CustomerId",
            "first_name": "FirstName",
            "last_name": "LastName",
            "email": "Email",
            "phone": "Phone"
          }
        }
      },
      {
        "name": "Data Validation",
        "type": "DataValidation",
        "configuration": {
          "validations": {
            "Email": {
              "required": true,
              "pattern": "^[\\w\\.-]+@[\\w\\.-]+\\.[a-zA-Z]{2,}$"
            }
          }
        }
      }
    ],
    "isEnabled": true
  }'
```

### Step 3: Execute Pipeline

```bash
# Replace {pipeline-id} with the ID returned from step 2
curl -X POST https://localhost:5001/api/pipelines/{pipeline-id}/execute \
  -H "Content-Type: application/json" \
  -d '{
    "async": false
  }'
```

### Step 4: Check Results

```bash
# Get pipeline execution history
curl https://localhost:5001/api/pipelines/{pipeline-id}/executions
```

## Configuration File Approach

Alternatively, you can define pipelines using configuration files:

### Create `customer-pipeline.json`:

```json
{
  "name": "Customer Import Pipeline",
  "description": "Import customers from CSV to database",
  "version": "1.0.0",
  "isEnabled": true,
  "stages": [
    {
      "name": "Extract CSV Data",
      "stageType": "Extract",
      "order": 1,
      "connectorConfiguration": {
        "connectorType": "CSV",
        "connectionString": "FilePath=customers.csv",
        "connectionProperties": {
          "hasHeaders": true,
          "delimiter": ",",
          "encoding": "UTF-8"
        }
      }
    },
    {
      "name": "Transform Data",
      "stageType": "Transform",
      "order": 2,
      "transformationConfiguration": {
        "rules": [
          {
            "ruleType": "FieldMapping",
            "settings": {
              "mappings": {
                "customer_id": "CustomerId",
                "first_name": "FirstName",
                "last_name": "LastName",
                "email": "Email",
                "phone": "Phone"
              }
            }
          }
        ]
      }
    },
    {
      "name": "Load to Database",
      "stageType": "Load",
      "order": 3,
      "connectorConfiguration": {
        "connectorType": "SqlServer",
        "connectionString": "Server=localhost;Database=SampleDB;Trusted_Connection=true;TrustServerCertificate=true",
        "connectionProperties": {
          "tableName": "Customers",
          "writeMode": "Insert",
          "createTableIfNotExists": true
        }
      }
    }
  ]
}
```

### Load and Execute Configuration:

```bash
# Create pipeline from configuration file
curl -X POST https://localhost:5001/api/pipelines/from-config \
  -H "Content-Type: application/json" \
  -d @customer-pipeline.json

# Execute the created pipeline
curl -X POST https://localhost:5001/api/pipelines/{pipeline-id}/execute
```

## Programmatic Approach

You can also create and execute pipelines programmatically:

```csharp
using ETLFramework.Core.Interfaces;
using ETLFramework.Configuration.Models;
using ETLFramework.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Set up dependency injection
var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddETLFramework();
    })
    .Build();

// Get required services
var orchestrator = host.Services.GetRequiredService<IPipelineOrchestrator>();

// Create pipeline configuration
var config = new PipelineConfiguration
{
    Name = "Customer Import Pipeline",
    Description = "Import customers from CSV to database"
};

// Add extract stage
config.Stages.Add(new StageConfiguration
{
    Name = "Extract CSV Data",
    StageType = StageType.Extract,
    Order = 1,
    ConnectorConfiguration = new ConnectorConfiguration
    {
        ConnectorType = "CSV",
        ConnectionString = "FilePath=customers.csv",
        ConnectionProperties = new Dictionary<string, object>
        {
            ["hasHeaders"] = true,
            ["delimiter"] = ",",
            ["encoding"] = "UTF-8"
        }
    }
});

// Add transform stage
config.Stages.Add(new StageConfiguration
{
    Name = "Transform Data",
    StageType = StageType.Transform,
    Order = 2,
    TransformationConfiguration = new TransformationConfiguration
    {
        Rules = new List<ITransformationRuleConfiguration>
        {
            new FieldMappingRuleConfiguration
            {
                Mappings = new Dictionary<string, string>
                {
                    ["customer_id"] = "CustomerId",
                    ["first_name"] = "FirstName",
                    ["last_name"] = "LastName",
                    ["email"] = "Email",
                    ["phone"] = "Phone"
                }
            }
        }
    }
});

// Add load stage
config.Stages.Add(new StageConfiguration
{
    Name = "Load to Database",
    StageType = StageType.Load,
    Order = 3,
    ConnectorConfiguration = new ConnectorConfiguration
    {
        ConnectorType = "SqlServer",
        ConnectionString = "Server=localhost;Database=SampleDB;Trusted_Connection=true;TrustServerCertificate=true",
        ConnectionProperties = new Dictionary<string, object>
        {
            ["tableName"] = "Customers",
            ["writeMode"] = "Insert",
            ["createTableIfNotExists"] = true
        }
    }
});

// Create and execute pipeline
var pipeline = await orchestrator.CreatePipelineAsync(config);
var context = new PipelineContext(Guid.NewGuid(), pipeline.Id);
var result = await pipeline.ExecuteAsync(context);

Console.WriteLine($"Pipeline execution completed. Success: {result.IsSuccess}");
if (!result.IsSuccess)
{
    Console.WriteLine($"Errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
}
```

## Common Scenarios

### JSON to Database

```json
{
  "sourceConnector": {
    "type": "JSON",
    "configuration": {
      "filePath": "products.json",
      "jsonPath": "$.products[*]",
      "encoding": "UTF-8"
    }
  },
  "targetConnector": {
    "type": "SqlServer",
    "configuration": {
      "connectionString": "Server=localhost;Database=Inventory;Trusted_Connection=true;",
      "tableName": "Products"
    }
  }
}
```

### Database to CSV

```json
{
  "sourceConnector": {
    "type": "SqlServer",
    "configuration": {
      "connectionString": "Server=localhost;Database=Sales;Trusted_Connection=true;",
      "query": "SELECT * FROM Orders WHERE OrderDate >= @StartDate",
      "parameters": {
        "StartDate": "2024-01-01"
      }
    }
  },
  "targetConnector": {
    "type": "CSV",
    "configuration": {
      "filePath": "orders_export.csv",
      "hasHeaders": true,
      "delimiter": ",",
      "encoding": "UTF-8"
    }
  }
}
```

### Cloud Storage Integration

```json
{
  "sourceConnector": {
    "type": "AzureBlob",
    "configuration": {
      "connectionString": "${AZURE_STORAGE_CONNECTION_STRING}",
      "containerName": "data-files",
      "blobName": "customers.csv"
    }
  },
  "targetConnector": {
    "type": "AwsS3",
    "configuration": {
      "accessKey": "${AWS_ACCESS_KEY}",
      "secretKey": "${AWS_SECRET_KEY}",
      "region": "us-east-1",
      "bucketName": "processed-data",
      "key": "customers/processed_customers.csv"
    }
  }
}
```

## Next Steps

1. **Explore the API**: Visit the Swagger UI at `https://localhost:5001` to explore all available endpoints
2. **Review Sample Configurations**: Check the `samples/configurations/` directory for more examples
3. **Read the Full Documentation**: See `ETL-Framework-Documentation.md` for comprehensive details
4. **Set Up Monitoring**: Configure logging and metrics for production use
5. **Create Custom Connectors**: Extend the framework with your own data sources

## Getting Help

- **API Documentation**: Available at `https://localhost:5001` when running the API
- **Sample Code**: Check the `samples/` directory in the repository
- **Issues**: Report bugs or request features on GitHub
- **Community**: Join discussions in the project's GitHub Discussions

Happy ETL processing! 🚀
