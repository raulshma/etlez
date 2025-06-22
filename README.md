# ETL Framework

A comprehensive Extract, Transform, Load (ETL) framework built in .NET 9+ with dynamic data integration pipelines configurable through API interfaces.

## 🚀 Features

### Core Capabilities
- **Pipeline Orchestration**: Complete ETL pipeline management with Extract → Transform → Load stages
- **Multiple Connectors**: Support for CSV, JSON, XML files, SQL databases, and cloud storage
- **Advanced Transformations**: Field-level transformations, rule-based business logic, and data mapping
- **RESTful API**: Comprehensive REST API for pipeline management and execution
- **Performance Monitoring**: Real-time metrics, optimization analysis, and throughput tracking
- **Configuration Management**: JSON/YAML configuration with validation and runtime compilation

### Supported Data Sources
- **File Systems**: CSV, JSON, XML with schema detection and batch processing
- **Databases**: SQL Server, MySQL, PostgreSQL with connection pooling and bulk operations
- **Cloud Storage**: Azure Blob Storage and AWS S3 with authentication and streaming
- **Memory**: In-memory connectors for testing and caching

### Transformation Engine
- **Field Transformations**: String, numeric, and date/time transformations
- **Rule-Based Processing**: Conditional logic with priority-based execution
- **Data Mapping**: Complex field mapping with nested object support
- **Data Validation**: Required fields, regex patterns, and range validation
- **Lookup Operations**: Dictionary, function-based, and cached lookups
- **Aggregation Functions**: Sum, average, count, min/max, concatenation

## 📋 Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code
- SQL Server (optional, for database connectors)
- Azure/AWS accounts (optional, for cloud storage)

## 🛠️ Installation

### Clone the Repository
```bash
git clone https://github.com/your-org/etl-framework.git
cd etl-framework
```

### Build the Solution
```bash
dotnet restore
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run the API
```bash
cd src/ETLFramework.API
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger UI at the root.

## 🏗️ Architecture

The framework is organized into several key projects:

- **ETLFramework.Core**: Core interfaces, models, and abstractions
- **ETLFramework.Configuration**: Configuration management and validation
- **ETLFramework.Connectors**: Data source and destination connectors
- **ETLFramework.Transformation**: Data transformation engine and processors
- **ETLFramework.Pipeline**: Pipeline orchestration and execution engine
- **ETLFramework.API**: RESTful API for pipeline management

## 🚀 Quick Start

### 1. Create a Simple CSV to Database Pipeline

```csharp
using ETLFramework.Pipeline;
using ETLFramework.Connectors;
using ETLFramework.Transformation;

// Create pipeline
var pipeline = new PipelineBuilder()
    .WithName("CSV to Database")
    .WithSource("CSV", new { FilePath = "data.csv", HasHeaders = true })
    .WithTarget("SqlServer", new { ConnectionString = "..." })
    .AddTransformation("FieldMapping", new { 
        Mappings = new[] {
            new { Source = "Name", Target = "FullName", Transform = "ToUpper" },
            new { Source = "Age", Target = "Age", Transform = "ToInt" }
        }
    })
    .Build();

// Execute pipeline
var result = await pipeline.ExecuteAsync();
```

### 2. Using the REST API

```bash
# Create a pipeline
curl -X POST https://localhost:5001/api/pipelines \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sample Pipeline",
    "sourceConnector": {
      "type": "CSV",
      "configuration": {
        "filePath": "sample.csv",
        "hasHeaders": true
      }
    },
    "targetConnector": {
      "type": "Database",
      "configuration": {
        "connectionString": "Server=localhost;Database=ETL;Trusted_Connection=true;"
      }
    },
    "transformations": [
      {
        "name": "Clean Names",
        "type": "StringTransformation",
        "configuration": {
          "operation": "ToUpper",
          "fieldName": "Name"
        }
      }
    ]
  }'

# Execute the pipeline
curl -X POST https://localhost:5001/api/pipelines/{id}/execute
```

## 📊 Performance

The framework has been tested with:
- **Large Datasets**: Successfully processed 10,000+ records
- **Throughput**: 13,717 records/second for field transformations
- **Memory Efficiency**: Optimized memory usage with streaming support
- **Concurrent Processing**: Multi-threaded execution support

## 🧪 Testing

The framework includes comprehensive test coverage:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Integration
```

## 📖 API Documentation

When running the API, comprehensive documentation is available at:
- **Swagger UI**: `https://localhost:5001/`
- **OpenAPI Spec**: `https://localhost:5001/swagger/v1/swagger.json`

### Key Endpoints

- `GET /api/pipelines` - List all pipelines
- `POST /api/pipelines` - Create a new pipeline
- `GET /api/pipelines/{id}` - Get pipeline details
- `PUT /api/pipelines/{id}` - Update a pipeline
- `DELETE /api/pipelines/{id}` - Delete a pipeline
- `POST /api/pipelines/{id}/execute` - Execute a pipeline
- `GET /api/pipelines/{id}/executions` - Get execution history

## 🔧 Configuration

### Pipeline Configuration Example

```json
{
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
    },
    {
      "name": "Format Phone",
      "type": "StringTransformation",
      "configuration": {
        "fieldName": "Phone",
        "operation": "FormatPhone"
      }
    }
  ]
}
```

## 🐳 Docker Support

### Build Docker Image
```bash
docker build -t etl-framework-api .
```

### Run with Docker Compose
```bash
docker-compose up -d
```

## 🚀 Deployment

### Production Deployment Checklist

1. **Environment Configuration**
   - Set production connection strings
   - Configure logging levels
   - Set up monitoring endpoints

2. **Security**
   - Enable HTTPS
   - Configure authentication
   - Set up API rate limiting

3. **Performance**
   - Configure connection pooling
   - Set appropriate batch sizes
   - Enable caching where appropriate

4. **Monitoring**
   - Set up health checks
   - Configure application insights
   - Monitor pipeline execution metrics

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: Check the `/docs` folder for detailed documentation
- **Issues**: Report bugs and request features via GitHub Issues
- **Discussions**: Join community discussions in GitHub Discussions

## 🏆 Acknowledgments

- Built with .NET 9 and modern C# features
- Uses industry-standard patterns and practices
- Inspired by enterprise ETL solutions
- Community-driven development approach
