# ETL Framework Playground

An interactive console application for testing and exploring all ETL Framework capabilities.

## Overview

The ETL Framework Playground provides a comprehensive testing environment where you can:

- **Test all connector types** with sample data
- **Experiment with transformations** and data processing
- **Build complete ETL pipelines** interactively
- **Validate data quality** with various rules
- **Test error handling** and recovery scenarios
- **Benchmark performance** with different data sizes
- **Explore rule-based processing** with conditional logic

## Features

### 🔌 Connector Playground
- **File System Connectors**: CSV, JSON, XML
- **Database Connectors**: SQLite, SQL Server, MySQL
- **Cloud Storage Connectors**: Azure Blob, AWS S3
- **Health Checks**: Test connectivity and configuration
- **Performance Testing**: Benchmark read/write operations
- **Custom Configuration**: Interactive connector setup

### 🔧 Transformation Playground
- **String Transformations**: ToUpper, ToLower, Trim, Replace, etc.
- **Numeric Transformations**: ToInt, ToDecimal, Round, Math operations
- **Date/Time Transformations**: ParseDate, FormatDate, AddDays, ToUtc
- **Field Mapping**: Rename, combine, split fields
- **Complex Transformations**: Chained operations and conditional logic
- **Custom Transformation Builder**: Create custom transformation logic

### ⚙️ Pipeline Playground
- **Pipeline Builder**: Interactive pipeline construction
- **Stage Management**: Extract, Transform, Load stages
- **Execution Monitoring**: Real-time progress tracking
- **Error Handling**: Comprehensive error management
- **Configuration Testing**: Various pipeline configurations

### ✅ Validation Playground
- **Required Field Validation**: Ensure mandatory fields are present
- **Regex Pattern Validation**: Custom pattern matching
- **Range Validation**: Numeric and date range checks
- **Custom Validation Rules**: User-defined validation logic
- **Data Quality Reports**: Comprehensive validation results

### 📋 Rule Engine Playground
- **Conditional Rules**: If-then-else logic
- **Priority-based Execution**: Rule ordering and precedence
- **Business Logic**: Complex business rule scenarios
- **Rule Validation**: Test rule configurations
- **Interactive Rule Builder**: Create rules interactively

### ⚡ Performance Playground
- **Throughput Testing**: Records per second benchmarks
- **Memory Usage Analysis**: Resource consumption monitoring
- **Scalability Tests**: Performance with different data sizes
- **Connector Performance**: Compare connector efficiency
- **Transformation Performance**: Benchmark transformation speed

### ❌ Error Handling Playground
- **Error Recovery**: Test recovery mechanisms
- **Retry Logic**: Configurable retry strategies
- **Fault Tolerance**: System resilience testing
- **Error Reporting**: Comprehensive error tracking
- **Problematic Data**: Test with intentionally flawed data

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code (optional)
- SQL Server (optional, for database connector testing)
- Azure/AWS accounts (optional, for cloud storage testing)

### Running the Playground

1. **Build the solution:**
   ```bash
   dotnet build
   ```

2. **Run the playground:**
   ```bash
   cd src/ETLFramework.Playground
   dotnet run
   ```

3. **Navigate the interactive menu:**
   - Use arrow keys to navigate
   - Press Enter to select options
   - Press Esc to go back to previous menu

### Sample Data

The playground includes several types of sample data:

- **Customer Data**: Names, emails, addresses, demographics
- **Product Data**: SKUs, prices, categories, inventory
- **Order Data**: Transactions, payments, shipping information
- **Employee Data**: HR records, departments, salaries
- **Problematic Data**: Intentionally flawed data for validation testing

### Configuration

Configure the playground through `appsettings.json`:

```json
{
  "Playground": {
    "DefaultDataSize": "Medium",
    "EnableColorOutput": true,
    "ShowProgressBars": true,
    "AutoCleanup": true,
    "ExportDirectory": "./exports",
    "SampleDataDirectory": "./Data"
  },
  "ConnectionStrings": {
    "DefaultSQLite": "Data Source=playground.db",
    "SqlServer": "Server=(localdb)\\mssqllocaldb;Database=ETLPlayground;Trusted_Connection=true;",
    "MySQL": "Server=localhost;Database=ETLPlayground;Uid=root;Pwd=password;"
  }
}
```

## Usage Examples

### Testing CSV Connector

1. Select "🔌 Connector Playground"
2. Choose "📄 File System Connectors"
3. Select "CSV" connector type
4. The playground will:
   - Generate sample customer data
   - Create a temporary CSV file
   - Test read and write operations
   - Display performance metrics
   - Show sample results

### Building a Pipeline

1. Select "⚙️ Pipeline Playground"
2. Choose "Interactive Pipeline Builder"
3. Configure source connector
4. Add transformation steps
5. Configure target connector
6. Execute and monitor the pipeline

### Testing Data Validation

1. Select "✅ Validation Playground"
2. Choose validation type (required fields, regex, range)
3. Generate or load test data
4. Apply validation rules
5. Review validation results and error reports

## Advanced Features

### Custom Transformations

Create custom transformation logic:

```csharp
// Example: Custom email domain extraction
var emailDomainTransform = new CustomTransformation(
    "ExtractEmailDomain",
    (value, record) => {
        var email = value?.ToString();
        return email?.Split('@').LastOrDefault();
    }
);
```

### Performance Benchmarking

Test performance with different data sizes:

- **Small**: 100 records
- **Medium**: 1,000 records  
- **Large**: 10,000 records
- **Extra Large**: 100,000 records

### Export Capabilities

Export test results and configurations:

- **CSV Export**: Tabular data export
- **JSON Export**: Structured data export
- **Configuration Export**: Save playground settings
- **Performance Reports**: Benchmark results

## Troubleshooting

### Common Issues

1. **Database Connection Errors**
   - Ensure database server is running
   - Verify connection strings in appsettings.json
   - Check network connectivity

2. **Cloud Storage Access**
   - Verify credentials in configuration
   - Ensure proper permissions
   - Check network connectivity

3. **Performance Issues**
   - Reduce data size for testing
   - Check available memory
   - Monitor system resources

### Logging

The playground uses Serilog for comprehensive logging:

- **Console Output**: Real-time feedback
- **File Logging**: Detailed logs in `logs/` directory
- **Structured Logging**: JSON-formatted log entries

## Contributing

The playground is designed to be extensible. To add new features:

1. Create new playground modules implementing `IPlaygroundModule`
2. Add module registration in `Program.cs`
3. Update the main menu in `PlaygroundHost.cs`
4. Add comprehensive testing and documentation

## Support

For issues, questions, or feature requests:

1. Check the main ETL Framework documentation
2. Review the playground logs for detailed error information
3. Use the built-in help system within the playground
4. Refer to the sample configurations and data

---

**Happy Testing!** 🚀

The ETL Framework Playground provides a comprehensive environment for exploring all framework capabilities. Whether you're learning the framework, testing new features, or validating configurations, the playground offers an interactive and user-friendly experience.
