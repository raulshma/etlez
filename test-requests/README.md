# Pipeline Creation Test Requests

This directory contains comprehensive test requests for creating pipelines with target source transformation in the ETL Framework API.

## Files Overview

### Test Request Files

1. **`pipeline-creation-test-requests.json`** - Basic pipeline creation scenarios
   - CSV to Database pipeline with email validation and phone formatting
   - JSON to XML data migration with product data transformations
   - Complex employee data processing with multi-stage transformations
   - SQLite to JSON export with date and currency formatting

2. **`advanced-pipeline-test-requests.json`** - Advanced pipeline scenarios
   - Multi-source data aggregation with complex business logic
   - Advanced validation rules and lookup transformations
   - Date component extraction and calculated fields
   - Comprehensive aggregation and metadata addition

3. **`edge-case-pipeline-test-requests.json`** - Edge cases and error scenarios
   - Minimal configuration testing
   - Invalid connector types
   - Missing required configurations
   - Special characters and encoding tests
   - Disabled pipeline scenarios

### Test Execution Scripts

4. **`test-pipeline-creation.ps1`** - PowerShell script for automated testing
   - Sends HTTP requests to the API
   - Tests pipeline creation and execution
   - Generates comprehensive test reports
   - Supports different test types (basic, advanced, edge cases)

## Test Scenarios Covered

### Basic Scenarios
- **CSV to SQL Server**: Customer data with email validation and phone formatting
- **JSON to XML**: Product catalog conversion with data enrichment
- **Complex Employee Processing**: Multi-stage transformation with validation
- **SQLite to JSON**: Database export with custom formatting

### Advanced Scenarios
- **Multi-Source Aggregation**: Process multiple CSV files with complex aggregations
- **Data Validation**: Comprehensive validation rules for different data types
- **Lookup Transformations**: Standardize data using lookup tables
- **Calculated Fields**: Business logic implementation with expressions
- **Date Processing**: Extract date components and calculate business metrics

### Edge Cases
- **Minimal Configuration**: Test with only required fields
- **Invalid Configurations**: Test error handling for unsupported types
- **Missing Required Fields**: Validate required field enforcement
- **Special Characters**: Unicode and encoding support
- **Null Values**: Handle null configuration values
- **Disabled Pipelines**: Test disabled pipeline behavior

## Connector Types Tested

### Source Connectors
- **CSV**: File-based comma-separated values
- **JSON**: JavaScript Object Notation files
- **SQLite**: Lightweight database files
- **Multi-file CSV**: Pattern-based multiple file processing

### Target Connectors
- **SQL Server**: Microsoft SQL Server database
- **JSON**: JSON file output
- **XML**: Extensible Markup Language files
- **SQLite**: SQLite database files

## Transformation Types Tested

### Data Validation
- Required field validation
- Data type validation
- Pattern matching (regex)
- Value range validation
- Allowed values validation

### String Transformations
- Case conversion (Upper, Lower, Proper)
- Trimming whitespace
- String formatting
- Concatenation
- Substring operations

### Numeric Transformations
- Rounding and precision
- Currency formatting
- Mathematical operations
- Number validation

### Date/Time Transformations
- Date formatting
- Date component extraction (Year, Quarter, Month)
- Date calculations
- Time zone conversions

### Field Mapping
- Field renaming
- Constant value assignment
- Calculated fields
- Conditional mapping

### Advanced Transformations
- Lookup transformations
- Aggregation operations
- Custom expressions
- Hash generation

## Usage Instructions

### Using PowerShell Script

1. **Run All Tests**:
   ```powershell
   .\test-pipeline-creation.ps1 -BaseUrl "https://localhost:5001"
   ```

2. **Run Specific Test Type**:
   ```powershell
   # Basic tests only
   .\test-pipeline-creation.ps1 -BaseUrl "https://localhost:5001" -TestType "basic"
   
   # Advanced tests only
   .\test-pipeline-creation.ps1 -BaseUrl "https://localhost:5001" -TestType "advanced"
   
   # Edge case tests only
   .\test-pipeline-creation.ps1 -BaseUrl "https://localhost:5001" -TestType "edge"
   ```

### Manual Testing with curl

1. **Basic Pipeline Creation**:
   ```bash
   curl -X POST https://localhost:5001/api/pipelines \
     -H "Content-Type: application/json" \
     -d @pipeline-creation-test-requests.json[0]
   ```

2. **Test Pipeline Execution**:
   ```bash
   curl -X POST https://localhost:5001/api/pipelines/{pipeline-id}/execute \
     -H "Content-Type: application/json" \
     -d '{"parameters": {"testMode": true}}'
   ```

### Using Postman

1. Import the JSON files as Postman collections
2. Set the base URL variable to your API endpoint
3. Execute requests individually or as a collection
4. Review responses and validate expected behavior

## Expected Results

### Successful Scenarios
- Pipeline creation returns 201 Created status
- Response includes generated pipeline ID
- Pipeline appears in GET /api/pipelines list
- Pipeline can be executed successfully

### Error Scenarios
- Invalid configurations return 400 Bad Request
- Missing required fields trigger validation errors
- Unsupported connector types return appropriate errors
- Malformed JSON returns parsing errors

## Configuration Options

### Pipeline Configuration
- `batchSize`: Number of records to process in each batch
- `parallelProcessing`: Enable/disable parallel execution
- `errorHandling`: How to handle processing errors
- `timeout`: Maximum execution time
- `logLevel`: Logging verbosity level

### Connector Configuration
- **CSV**: filePath, hasHeaders, delimiter, encoding
- **JSON**: filePath, format, prettyPrint
- **SQL Server**: connectionString, tableName, batchSize
- **SQLite**: connectionString, query, timeout

### Transformation Configuration
- Field-specific settings (fieldName, targetField)
- Operation parameters (precision, format, pattern)
- Validation rules (required, type, minValue, maxValue)
- Lookup tables and default values

## Troubleshooting

### Common Issues
1. **Connection Errors**: Verify API is running and accessible
2. **SSL Certificate Issues**: Use `-SkipCertificateCheck` for development
3. **Authentication**: Ensure proper API authentication if required
4. **File Paths**: Use absolute paths for file-based connectors
5. **Database Connections**: Verify connection strings and permissions

### Debugging Tips
- Enable detailed logging in the API
- Use test mode for dry runs
- Validate JSON syntax before sending requests
- Check API documentation for latest schema changes
- Monitor API logs for detailed error information

## Contributing

When adding new test scenarios:
1. Follow the existing JSON structure
2. Include comprehensive transformation examples
3. Add both positive and negative test cases
4. Update this README with new scenarios
5. Test thoroughly before committing
