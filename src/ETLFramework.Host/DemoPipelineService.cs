using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Pipeline;
using ETLFramework.Configuration.Models;
using ETLFramework.Connectors;
using ETLFramework.Connectors.Database;

namespace ETLFramework.Host;

/// <summary>
/// Demo hosted service that demonstrates the ETL Framework pipeline execution.
/// </summary>
public class DemoPipelineService : BackgroundService
{
    private readonly ILogger<DemoPipelineService> _logger;
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly PipelineBuilder _pipelineBuilder;
    private readonly IConnectorFactory _connectorFactory;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the DemoPipelineService class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="orchestrator">The pipeline orchestrator</param>
    /// <param name="pipelineBuilder">The pipeline builder</param>
    /// <param name="connectorFactory">The connector factory</param>
    /// <param name="serviceProvider">The service provider</param>
    public DemoPipelineService(
        ILogger<DemoPipelineService> logger,
        IPipelineOrchestrator orchestrator,
        PipelineBuilder pipelineBuilder,
        IConnectorFactory connectorFactory,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _pipelineBuilder = pipelineBuilder ?? throw new ArgumentNullException(nameof(pipelineBuilder));
        _connectorFactory = connectorFactory ?? throw new ArgumentNullException(nameof(connectorFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting ETL Framework Demo");

            // Wait a moment for the host to fully start
            await Task.Delay(1000, stoppingToken);

            // Create a demo pipeline
            var pipeline = await CreateDemoPipelineAsync();

            // Create a demo configuration
            var configuration = CreateDemoConfiguration();

            // Create pipeline logger
            var pipelineLogger = _logger;

            // Create pipeline context
            var context = new PipelineContext(Guid.NewGuid(), configuration, pipelineLogger, stoppingToken);

            // Execute the pipeline
            _logger.LogInformation("Executing demo pipeline: {PipelineName}", pipeline.Name);

            var result = await _orchestrator.ExecutePipelineAsync(pipeline, context, stoppingToken);

            // Report results
            if (result.IsSuccess)
            {
                _logger.LogInformation("Demo pipeline completed successfully!");
                _logger.LogInformation("Records processed: {RecordsProcessed}", result.RecordsProcessed);
                _logger.LogInformation("Execution time: {Duration}", result.Duration);
            }
            else
            {
                _logger.LogWarning("Demo pipeline completed with errors:");
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("  - {ErrorMessage}", error.Message);
                }
            }

            // Demonstrate file connectors
            await DemonstrateFileConnectorsAsync(stoppingToken);

            // Demonstrate database connectors
            await DemonstrateDatabaseConnectorsAsync(stoppingToken);

            // Demonstrate cloud storage connectors
            await DemonstrateCloudStorageConnectorsAsync(stoppingToken);

            // Demonstrate enhanced connector factory system
            await DemonstrateConnectorFactorySystemAsync(stoppingToken);

            // Demonstrate transformation framework
            await DemonstrateTransformationFrameworkAsync(stoppingToken);

            // Wait a bit before shutting down
            await Task.Delay(2000, stoppingToken);

            _logger.LogInformation("ETL Framework Demo completed");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Demo pipeline service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in demo pipeline service");
        }
    }

    /// <summary>
    /// Creates a demo pipeline for testing.
    /// </summary>
    /// <returns>A configured demo pipeline</returns>
    private Task<IPipeline> CreateDemoPipelineAsync()
    {
        var pipeline = _pipelineBuilder
            .WithName("Demo ETL Pipeline")
            .WithDescription("A demonstration pipeline showing ETL Framework capabilities")
            .AddExtractStage("Extract Customer Data", 1, 50)
            .AddTransformStage("Transform Customer Data", 2, 50)
            .AddLoadStage("Load Customer Data", 3, 50)
            .Build();

        _logger.LogInformation("Created demo pipeline with {StageCount} stages", pipeline.Stages.Count);

        return Task.FromResult(pipeline);
    }

    /// <summary>
    /// Creates a demo configuration for the pipeline.
    /// </summary>
    /// <returns>A demo pipeline configuration</returns>
    private IPipelineConfiguration CreateDemoConfiguration()
    {
        var config = new PipelineConfiguration
        {
            Name = "Demo Pipeline Configuration",
            Description = "Configuration for demo pipeline execution",
            Version = "1.0.0",
            Author = "ETL Framework Demo",
            IsEnabled = true,
            ErrorHandling = new ErrorHandlingConfiguration
            {
                StopOnError = false,
                MaxErrors = 10
            },
            Retry = new RetryConfiguration
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromSeconds(1)
            },
            Timeout = TimeSpan.FromMinutes(5)
        };

        // Add some global settings
        config.GlobalSettings["DemoMode"] = true;
        config.GlobalSettings["LogLevel"] = "Information";
        config.GlobalSettings["BatchSize"] = 10;

        // Add tags
        config.Tags.Add("demo");
        config.Tags.Add("test");
        config.Tags.Add("etl-framework");

        _logger.LogInformation("Created demo configuration: {ConfigName}", config.Name);

        return config;
    }

    /// <summary>
    /// Demonstrates file connectors by reading from sample data files.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateFileConnectorsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== File Connectors Demonstration ===");

            // Demonstrate CSV connector
            await DemonstrateCsvConnectorAsync(cancellationToken);

            // Demonstrate JSON connector
            await DemonstrateJsonConnectorAsync(cancellationToken);

            // Demonstrate XML connector
            await DemonstrateXmlConnectorAsync(cancellationToken);

            _logger.LogInformation("=== File Connectors Demonstration Complete ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demonstrating file connectors");
        }
    }

    /// <summary>
    /// Demonstrates CSV connector functionality.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateCsvConnectorAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- CSV Connector Demo ---");

        try
        {
            // Create CSV connector configuration
            var csvConfig = ConnectorFactory.CreateTestConfiguration(
                "CSV",
                "Customer CSV Source",
                "samples/data/customers.csv",
                new Dictionary<string, object>
                {
                    ["hasHeaders"] = true,
                    ["delimiter"] = ","
                });

            // Create CSV connector
            var csvConnector = _connectorFactory.CreateSourceConnector<DataRecord>(csvConfig);

            // Test connection
            var testResult = await csvConnector.TestConnectionAsync(cancellationToken);
            _logger.LogInformation("CSV Connection Test: {IsSuccessful} - {Message}", testResult.IsSuccessful, testResult.Message);

            if (testResult.IsSuccessful)
            {
                // Get schema
                var schema = await csvConnector.GetSchemaAsync(cancellationToken);
                _logger.LogInformation("CSV Schema: {FieldCount} fields detected", schema.Fields.Count);

                // Read records
                var recordCount = 0;
                await foreach (var record in csvConnector.ReadAsync(cancellationToken))
                {
                    recordCount++;
                    if (recordCount <= 3) // Log first 3 records
                    {
                        _logger.LogInformation("CSV Record {RecordNumber}: {FieldCount} fields", record.RowNumber, record.Fields.Count);
                    }
                }
                _logger.LogInformation("CSV Total Records Read: {RecordCount}", recordCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CSV connector demo");
        }
    }

    /// <summary>
    /// Demonstrates JSON connector functionality.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateJsonConnectorAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- JSON Connector Demo ---");

        try
        {
            // Create JSON connector configuration
            var jsonConfig = ConnectorFactory.CreateTestConfiguration(
                "JSON",
                "Product JSON Source",
                "samples/data/products.json",
                new Dictionary<string, object>
                {
                    ["arrayFormat"] = true
                });

            // Create JSON connector
            var jsonConnector = _connectorFactory.CreateSourceConnector<DataRecord>(jsonConfig);

            // Test connection
            var testResult = await jsonConnector.TestConnectionAsync(cancellationToken);
            _logger.LogInformation("JSON Connection Test: {IsSuccessful} - {Message}", testResult.IsSuccessful, testResult.Message);

            if (testResult.IsSuccessful)
            {
                // Get estimated record count
                var estimatedCount = await jsonConnector.GetEstimatedRecordCountAsync(cancellationToken);
                _logger.LogInformation("JSON Estimated Records: {EstimatedCount}", estimatedCount);

                // Read records
                var recordCount = 0;
                await foreach (var record in jsonConnector.ReadAsync(cancellationToken))
                {
                    recordCount++;
                    if (recordCount <= 2) // Log first 2 records
                    {
                        _logger.LogInformation("JSON Record {RecordNumber}: {FieldCount} fields", record.RowNumber, record.Fields.Count);
                    }
                }
                _logger.LogInformation("JSON Total Records Read: {RecordCount}", recordCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in JSON connector demo");
        }
    }

    /// <summary>
    /// Demonstrates XML connector functionality.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateXmlConnectorAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- XML Connector Demo ---");

        try
        {
            // Create XML connector configuration
            var xmlConfig = ConnectorFactory.CreateTestConfiguration(
                "XML",
                "Order XML Source",
                "samples/data/orders.xml",
                new Dictionary<string, object>
                {
                    ["rootElement"] = "orders",
                    ["recordElement"] = "order"
                });

            // Create XML connector
            var xmlConnector = _connectorFactory.CreateSourceConnector<DataRecord>(xmlConfig);

            // Test connection
            var testResult = await xmlConnector.TestConnectionAsync(cancellationToken);
            _logger.LogInformation("XML Connection Test: {IsSuccessful} - {Message}", testResult.IsSuccessful, testResult.Message);

            if (testResult.IsSuccessful)
            {
                // Get schema
                var schema = await xmlConnector.GetSchemaAsync(cancellationToken);
                _logger.LogInformation("XML Schema: {FieldCount} fields detected", schema.Fields.Count);

                // Read records
                var recordCount = 0;
                await foreach (var record in xmlConnector.ReadAsync(cancellationToken))
                {
                    recordCount++;
                    if (recordCount <= 2) // Log first 2 records
                    {
                        _logger.LogInformation("XML Record {RecordNumber}: {FieldCount} fields", record.RowNumber, record.Fields.Count);
                    }
                }
                _logger.LogInformation("XML Total Records Read: {RecordCount}", recordCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in XML connector demo");
        }
    }

    /// <summary>
    /// Demonstrates database connectors by creating and using SQLite databases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateDatabaseConnectorsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== Database Connectors Demonstration ===");

            // Demonstrate SQLite connector (easiest to test without external dependencies)
            await DemonstrateSqliteConnectorAsync(cancellationToken);

            _logger.LogInformation("=== Database Connectors Demonstration Complete ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demonstrating database connectors");
        }
    }

    /// <summary>
    /// Demonstrates SQLite connector functionality.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateSqliteConnectorAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- SQLite Connector Demo ---");

        try
        {
            // Create in-memory SQLite connector configuration
            var sqliteConfig = ConnectorFactory.CreateTestConfiguration(
                "SQLite",
                "Demo SQLite Database",
                "Data Source=:memory:",
                new Dictionary<string, object>
                {
                    ["createTableIfNotExists"] = true,
                    ["tableName"] = "DemoTable"
                });

            // Create SQLite connector
            var sqliteConnector = _connectorFactory.CreateSourceConnector<DataRecord>(sqliteConfig);

            // Test connection
            var testResult = await sqliteConnector.TestConnectionAsync(cancellationToken);
            _logger.LogInformation("SQLite Connection Test: {IsSuccessful} - {Message}", testResult.IsSuccessful, testResult.Message);

            if (testResult.IsSuccessful)
            {
                // Open connection and create sample data
                await sqliteConnector.OpenAsync(cancellationToken);

                // Cast to SQLite connector to access specific methods
                if (sqliteConnector is SqliteConnector sqliteConn)
                {
                    // Create sample table with data
                    await sqliteConn.CreateSampleTableAsync("DemoTable", cancellationToken);

                    // Update configuration to point to the table
                    sqliteConfig.SetConnectionProperty("tableName", "DemoTable");

                    // Get record count
                    var recordCount = await sqliteConnector.GetEstimatedRecordCountAsync(cancellationToken);
                    _logger.LogInformation("SQLite Record Count: {RecordCount}", recordCount);

                    // Get schema
                    var schema = await sqliteConnector.GetSchemaAsync(cancellationToken);
                    _logger.LogInformation("SQLite Schema: {FieldCount} fields detected", schema.Fields.Count);

                    // Read records
                    var readCount = 0;
                    await foreach (var record in sqliteConnector.ReadAsync(cancellationToken))
                    {
                        readCount++;
                        if (readCount <= 3) // Log first 3 records
                        {
                            _logger.LogInformation("SQLite Record {RecordNumber}: ID={Id}, Name={Name}, Value={Value}",
                                record.RowNumber,
                                record.Fields.TryGetValue("Id", out var id) ? id : null,
                                record.Fields.TryGetValue("Name", out var name) ? name : null,
                                record.Fields.TryGetValue("Value", out var value) ? value : null);
                        }
                    }
                    _logger.LogInformation("SQLite Total Records Read: {RecordCount}", readCount);

                    // Demonstrate write operations
                    await DemonstrateSqliteWriteOperationsAsync(sqliteConn, cancellationToken);
                }

                await sqliteConnector.CloseAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SQLite connector demo");
        }
    }

    /// <summary>
    /// Demonstrates SQLite write operations.
    /// </summary>
    /// <param name="sqliteConnector">The SQLite connector</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateSqliteWriteOperationsAsync(SqliteConnector sqliteConnector, CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- SQLite Write Operations Demo ---");

        try
        {
            // Create destination connector for writing
            var writeConfig = ConnectorFactory.CreateTestConfiguration(
                "SQLite",
                "Demo SQLite Write",
                "Data Source=:memory:",
                new Dictionary<string, object>
                {
                    ["createTableIfNotExists"] = true,
                    ["tableName"] = "WriteTestTable"
                });

            var writeConnector = _connectorFactory.CreateDestinationConnector<DataRecord>(writeConfig);
            await writeConnector.OpenAsync(cancellationToken);

            // Create schema for the new table
            var schema = new DataSchema
            {
                Name = "WriteTestTable"
            };
            schema.Fields.Add(new DataField { Name = "Id", DataType = typeof(int), IsRequired = true });
            schema.Fields.Add(new DataField { Name = "Name", DataType = typeof(string), IsRequired = true });
            schema.Fields.Add(new DataField { Name = "Description", DataType = typeof(string), IsRequired = false });
            schema.Fields.Add(new DataField { Name = "CreatedDate", DataType = typeof(DateTime), IsRequired = true });

            // Prepare the destination
            await writeConnector.PrepareAsync(schema, cancellationToken);

            // Create sample records to write
            var recordsToWrite = new List<DataRecord>();
            for (int i = 1; i <= 5; i++)
            {
                var record = new DataRecord
                {
                    RowNumber = i,
                    Source = "Demo"
                };
                record.Fields["Id"] = i;
                record.Fields["Name"] = $"Test Item {i}";
                record.Fields["Description"] = $"This is test item number {i}";
                record.Fields["CreatedDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                recordsToWrite.Add(record);
            }

            // Write records
            var writeResult = await writeConnector.WriteBatchAsync(recordsToWrite, cancellationToken);
            _logger.LogInformation("SQLite Write Result: Success={IsSuccessful}, Records={RecordsWritten}",
                writeResult.IsSuccessful, writeResult.RecordsWritten);

            await writeConnector.CloseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SQLite write operations demo");
        }
    }

    /// <summary>
    /// Demonstrates cloud storage connectors by showing their configuration and capabilities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateCloudStorageConnectorsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== Cloud Storage Connectors Demonstration ===");

            // Demonstrate Azure Blob Storage connector configuration
            await DemonstrateAzureBlobConnectorAsync(cancellationToken);

            // Demonstrate AWS S3 connector configuration
            await DemonstrateAwsS3ConnectorAsync(cancellationToken);

            _logger.LogInformation("=== Cloud Storage Connectors Demonstration Complete ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demonstrating cloud storage connectors");
        }
    }

    /// <summary>
    /// Demonstrates Azure Blob Storage connector configuration and capabilities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateAzureBlobConnectorAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- Azure Blob Storage Connector Demo ---");

        try
        {
            // Create Azure Blob Storage connector configuration (using Azurite emulator connection string)
            var azureBlobConfig = ConnectorFactory.CreateTestConfiguration(
                "AzureBlob",
                "Demo Azure Blob Storage",
                "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
                new Dictionary<string, object>
                {
                    ["container"] = "demo-container",
                    ["createContainerIfNotExists"] = true,
                    ["filePattern"] = "*.csv"
                });

            // Create Azure Blob Storage connector
            var azureBlobConnector = _connectorFactory.CreateSourceConnector<DataRecord>(azureBlobConfig);

            // Test connection (this will fail if Azurite is not running, which is expected)
            var testResult = await azureBlobConnector.TestConnectionAsync(cancellationToken);
            _logger.LogInformation("Azure Blob Storage Connection Test: {IsSuccessful} - {Message}", testResult.IsSuccessful, testResult.Message);

            if (!testResult.IsSuccessful)
            {
                _logger.LogInformation("Azure Blob Storage connector configured but Azurite emulator not available");
                _logger.LogInformation("To test Azure Blob Storage: Install Azurite and run 'azurite --silent --location c:\\azurite --debug c:\\azurite\\debug.log'");
            }

            // Show connector metadata
            var metadata = await azureBlobConnector.GetMetadataAsync(cancellationToken);
            _logger.LogInformation("Azure Blob Storage Connector Version: {Version}", metadata.Version);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Blob Storage connector demo failed (expected if Azurite not running)");
        }
    }

    /// <summary>
    /// Demonstrates AWS S3 connector configuration and capabilities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateAwsS3ConnectorAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- AWS S3 Connector Demo ---");

        try
        {
            // Create AWS S3 connector configuration (using default credential chain)
            var awsS3Config = ConnectorFactory.CreateTestConfiguration(
                "AwsS3",
                "Demo AWS S3",
                "Region=us-east-1",
                new Dictionary<string, object>
                {
                    ["bucket"] = "demo-bucket",
                    ["createContainerIfNotExists"] = true,
                    ["prefix"] = "data/",
                    ["filePattern"] = "*.json"
                });

            // Create AWS S3 connector
            var awsS3Connector = _connectorFactory.CreateSourceConnector<DataRecord>(awsS3Config);

            // Test connection (this will fail if AWS credentials are not configured, which is expected)
            var testResult = await awsS3Connector.TestConnectionAsync(cancellationToken);
            _logger.LogInformation("AWS S3 Connection Test: {IsSuccessful} - {Message}", testResult.IsSuccessful, testResult.Message);

            if (!testResult.IsSuccessful)
            {
                _logger.LogInformation("AWS S3 connector configured but AWS credentials not available");
                _logger.LogInformation("To test AWS S3: Configure AWS credentials using AWS CLI, environment variables, or IAM roles");
            }

            // Show connector metadata
            var metadata = await awsS3Connector.GetMetadataAsync(cancellationToken);
            _logger.LogInformation("AWS S3 Connector Version: {Version}", metadata.Version);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AWS S3 connector demo failed (expected if AWS credentials not configured)");
        }
    }

    /// <summary>
    /// Demonstrates the enhanced connector factory system capabilities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateConnectorFactorySystemAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== Enhanced Connector Factory System Demonstration ===");

            // Demonstrate connector registry
            await DemonstrateConnectorRegistryAsync(cancellationToken);

            // Demonstrate connector templates
            await DemonstrateConnectorTemplatesAsync(cancellationToken);

            // Demonstrate health checking
            await DemonstrateConnectorHealthCheckingAsync(cancellationToken);

            _logger.LogInformation("=== Enhanced Connector Factory System Demonstration Complete ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demonstrating enhanced connector factory system");
        }
    }

    /// <summary>
    /// Demonstrates the connector registry capabilities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateConnectorRegistryAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- Connector Registry Demo ---");

        try
        {
            if (_connectorFactory is not ConnectorFactory factory)
            {
                _logger.LogWarning("Connector factory is not the enhanced ConnectorFactory type");
                return;
            }

            var registry = factory.Registry;

            // Show registry statistics
            var stats = registry.GetStats();
            _logger.LogInformation("Registry Statistics:");
            _logger.LogInformation("  Total Connectors: {TotalConnectors}", stats.TotalConnectors);
            _logger.LogInformation("  Available Connectors: {AvailableConnectors}", stats.AvailableConnectors);
            _logger.LogInformation("  Categories: {CategoriesCount}", stats.CategoriesCount);

            foreach (var category in stats.Categories)
            {
                _logger.LogInformation("    {Category}: {Count} connectors", category.Key, category.Value);
            }

            // Demonstrate connector discovery
            _logger.LogInformation("Available Connector Types:");
            foreach (var connectorType in _connectorFactory.GetSupportedConnectorTypes())
            {
                var descriptor = registry.GetDescriptor(connectorType);
                if (descriptor != null)
                {
                    _logger.LogInformation("  {ConnectorType}: {DisplayName} - {Description}",
                        connectorType, descriptor.DisplayName, descriptor.Description);
                }
            }

            // Demonstrate searching
            var fileConnectors = registry.GetByCategory("File System");
            _logger.LogInformation("File System Connectors: {Count}", fileConnectors.Count());

            var readConnectors = registry.GetByOperation(ETLFramework.Connectors.Factory.ConnectorOperation.Read);
            _logger.LogInformation("Connectors supporting Read operation: {Count}", readConnectors.Count());

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in connector registry demo");
        }
    }

    /// <summary>
    /// Demonstrates connector templates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateConnectorTemplatesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- Connector Templates Demo ---");

        try
        {
            if (_connectorFactory is not ConnectorFactory factory)
            {
                _logger.LogWarning("Connector factory is not the enhanced ConnectorFactory type");
                return;
            }

            // Show available templates
            var templates = factory.GetAvailableTemplates();
            _logger.LogInformation("Available Templates: {Count}", templates.Count);

            foreach (var template in templates.Take(3)) // Show first 3 templates
            {
                _logger.LogInformation("  {TemplateName} ({Category}): {Description}",
                    template.Name, template.Category, template.Description);

                _logger.LogInformation("    Parameters: {ParameterCount}", template.Parameters.Count);
                foreach (var param in template.Parameters.Take(2)) // Show first 2 parameters
                {
                    _logger.LogInformation("      {ParameterName} ({Type}): {Description}",
                        param.Name, param.Type.Name, param.Description);
                }
            }

            // Demonstrate template usage
            var csvTemplate = factory.CreateFromTemplate("csv", new Dictionary<string, object>
            {
                ["filePath"] = "demo.csv",
                ["hasHeaders"] = true,
                ["delimiter"] = ","
            });

            if (csvTemplate != null)
            {
                _logger.LogInformation("Created CSV connector from template: {ConnectorName}", csvTemplate.Name);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in connector templates demo");
        }
    }

    /// <summary>
    /// Demonstrates connector health checking.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateConnectorHealthCheckingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- Connector Health Checking Demo ---");

        try
        {
            // Create a test connector for health checking
            var sqliteConfig = ConnectorFactory.CreateTestConfiguration(
                "SQLite",
                "Health Check Test",
                "Data Source=:memory:",
                new Dictionary<string, object>
                {
                    ["tableName"] = "HealthTest"
                });

            var testConnector = _connectorFactory.CreateSourceConnector<DataRecord>(sqliteConfig);

            if (_connectorFactory is not ConnectorFactory healthFactory)
            {
                _logger.LogWarning("Connector factory is not the enhanced ConnectorFactory type");
                return;
            }

            // Perform individual health check
            var healthChecker = healthFactory.HealthChecker;
            var healthResult = await healthChecker.CheckHealthAsync(testConnector, cancellationToken);

            _logger.LogInformation("Health Check Results for {ConnectorName}:", healthResult.ConnectorName);
            _logger.LogInformation("  Overall Status: {Status}", healthResult.OverallStatus);
            _logger.LogInformation("  Duration: {Duration}ms", healthResult.Duration.TotalMilliseconds);

            if (healthResult.ConnectivityCheck != null)
            {
                _logger.LogInformation("  Connectivity: {Status} - {Message}",
                    healthResult.ConnectivityCheck.Status, healthResult.ConnectivityCheck.Message);
            }

            if (healthResult.ConfigurationCheck != null)
            {
                _logger.LogInformation("  Configuration: {Status} - {Message}",
                    healthResult.ConfigurationCheck.Status, healthResult.ConfigurationCheck.Message);
            }

            if (healthResult.MetadataCheck != null)
            {
                _logger.LogInformation("  Metadata: {Status} - {Message}",
                    healthResult.MetadataCheck.Status, healthResult.MetadataCheck.Message);
            }

            if (healthResult.Errors.Count > 0)
            {
                _logger.LogInformation("  Errors: {ErrorCount}", healthResult.Errors.Count);
                foreach (var error in healthResult.Errors.Take(2))
                {
                    _logger.LogInformation("    - {Error}", error);
                }
            }

            if (healthResult.Warnings.Count > 0)
            {
                _logger.LogInformation("  Warnings: {WarningCount}", healthResult.Warnings.Count);
                foreach (var warning in healthResult.Warnings.Take(2))
                {
                    _logger.LogInformation("    - {Warning}", warning);
                }
            }

            await testConnector.CloseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in connector health checking demo");
        }
    }

    /// <summary>
    /// Demonstrates the transformation framework capabilities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateTransformationFrameworkAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== Transformation Framework Demonstration ===");

            // Demonstrate field transformations
            await DemonstrateFieldTransformationsAsync(cancellationToken);

            // Demonstrate transformation pipeline
            await DemonstrateTransformationPipelineAsync(cancellationToken);

            // Demonstrate transformation processor
            await DemonstrateTransformationProcessorAsync(cancellationToken);

            _logger.LogInformation("=== Transformation Framework Demonstration Complete ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demonstrating transformation framework");
        }
    }

    /// <summary>
    /// Demonstrates field transformations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateFieldTransformationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- Field Transformations Demo ---");

        try
        {
            // Create sample data
            var sampleRecord = new DataRecord();
            sampleRecord.SetField("firstName", "john");
            sampleRecord.SetField("lastName", "DOE");
            sampleRecord.SetField("email", "  JOHN.DOE@EXAMPLE.COM  ");
            sampleRecord.SetField("age", "25");
            sampleRecord.SetField("salary", "50000.75");
            sampleRecord.SetField("bonus", "5000.25");

            _logger.LogInformation("Original Record:");
            _logger.LogInformation("  firstName: {firstName}", sampleRecord.GetField<string>("firstName"));
            _logger.LogInformation("  lastName: {lastName}", sampleRecord.GetField<string>("lastName"));
            _logger.LogInformation("  email: {email}", sampleRecord.GetField<string>("email"));
            _logger.LogInformation("  age: {age}", sampleRecord.GetField<string>("age"));
            _logger.LogInformation("  salary: {salary}", sampleRecord.GetField<string>("salary"));
            _logger.LogInformation("  bonus: {bonus}", sampleRecord.GetField<string>("bonus"));

            // Create transformation context
            var context = new ETLFramework.Transformation.Models.TransformationContext("FieldTransformationsDemo", cancellationToken);

            // String transformations
            var uppercaseTransform = new ETLFramework.Transformation.Transformations.FieldTransformations.UppercaseTransformation("firstName");
            var lowercaseTransform = new ETLFramework.Transformation.Transformations.FieldTransformations.LowercaseTransformation("lastName");
            var trimTransform = new ETLFramework.Transformation.Transformations.FieldTransformations.TrimTransformation("email");

            // Numeric transformations
            var roundTransform = new ETLFramework.Transformation.Transformations.FieldTransformations.RoundTransformation("salary", 0);
            var addTransform = new ETLFramework.Transformation.Transformations.FieldTransformations.AddTransformation("age", 1, "ageNextYear");

            // Calculate transformation
            var calculateTransform = new ETLFramework.Transformation.Transformations.FieldTransformations.CalculateTransformation(
                "salary", "bonus", ETLFramework.Transformation.Transformations.FieldTransformations.MathOperation.Add, "totalCompensation");

            // Concatenate transformation
            var concatenateTransform = new ETLFramework.Transformation.Transformations.FieldTransformations.ConcatenateTransformation(
                new[] { "firstName", "lastName" }, "fullName", " ");

            // Apply transformations
            var transformations = new ETLFramework.Transformation.Interfaces.ITransformation[]
            {
                uppercaseTransform,
                lowercaseTransform,
                trimTransform,
                roundTransform,
                addTransform,
                calculateTransform,
                concatenateTransform
            };

            var currentRecord = sampleRecord;
            foreach (var transformation in transformations)
            {
                var result = await transformation.TransformAsync(currentRecord, context, cancellationToken);
                if (result.IsSuccessful && result.OutputRecord != null)
                {
                    currentRecord = result.OutputRecord;
                    _logger.LogInformation("Applied {TransformationName}: Success", transformation.Name);
                }
                else
                {
                    _logger.LogWarning("Applied {TransformationName}: Failed - {Errors}",
                        transformation.Name, string.Join(", ", result.Errors.Select(e => e.Message)));
                }
            }

            _logger.LogInformation("Transformed Record:");
            _logger.LogInformation("  firstName: {firstName}", currentRecord.GetField<string>("firstName"));
            _logger.LogInformation("  lastName: {lastName}", currentRecord.GetField<string>("lastName"));
            _logger.LogInformation("  email: {email}", currentRecord.GetField<string>("email"));
            _logger.LogInformation("  salary: {salary}", currentRecord.GetField<string>("salary"));
            _logger.LogInformation("  ageNextYear: {ageNextYear}", currentRecord.GetField<string>("ageNextYear"));
            _logger.LogInformation("  totalCompensation: {totalCompensation}", currentRecord.GetField<string>("totalCompensation"));
            _logger.LogInformation("  fullName: {fullName}", currentRecord.GetField<string>("fullName"));

            // Show transformation statistics
            var stats = context.Statistics;
            _logger.LogInformation("Transformation Statistics:");
            _logger.LogInformation("  Records Processed: {RecordsProcessed}", stats.RecordsProcessed);
            _logger.LogInformation("  Records Transformed: {RecordsTransformed}", stats.RecordsTransformed);
            _logger.LogInformation("  Fields Transformed: {FieldsTransformed}", stats.FieldsTransformed);
            _logger.LogInformation("  Total Processing Time: {TotalProcessingTime}ms", stats.TotalProcessingTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in field transformations demo");
        }
    }

    /// <summary>
    /// Demonstrates transformation pipeline.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateTransformationPipelineAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- Transformation Pipeline Demo ---");

        try
        {
            // Create sample data
            var records = new List<DataRecord>();
            for (int i = 1; i <= 3; i++)
            {
                var record = new DataRecord();
                record.SetField("id", i.ToString());
                record.SetField("name", $"user{i}");
                record.SetField("score", (i * 10).ToString());
                records.Add(record);
            }

            _logger.LogInformation("Input Records: {RecordCount}", records.Count);
            foreach (var record in records)
            {
                _logger.LogInformation("  Record {id}: name={name}, score={score}",
                    record.GetField<string>("id"), record.GetField<string>("name"), record.GetField<string>("score"));
            }

            // Create transformation processor
            var processor = new ETLFramework.Transformation.Processors.TransformationProcessor(
                _serviceProvider.GetRequiredService<ILogger<ETLFramework.Transformation.Processors.TransformationProcessor>>());

            // Create transformation pipeline
            var pipeline = new ETLFramework.Transformation.Pipeline.TransformationPipeline(
                "DemoPipeline",
                processor,
                _serviceProvider.GetRequiredService<ILogger<ETLFramework.Transformation.Pipeline.TransformationPipeline>>());

            // Create transformation stages
            var stage1 = new ETLFramework.Transformation.Pipeline.TransformationStage(
                "StringProcessing",
                1,
                processor,
                _serviceProvider.GetRequiredService<ILogger<ETLFramework.Transformation.Pipeline.TransformationStage>>());

            stage1.AddTransformation(new ETLFramework.Transformation.Transformations.FieldTransformations.UppercaseTransformation("name"));

            var stage2 = new ETLFramework.Transformation.Pipeline.TransformationStage(
                "NumericProcessing",
                2,
                processor,
                _serviceProvider.GetRequiredService<ILogger<ETLFramework.Transformation.Pipeline.TransformationStage>>());

            stage2.AddTransformation(new ETLFramework.Transformation.Transformations.FieldTransformations.MultiplyTransformation("score", 2, "doubledScore"));

            // Add stages to pipeline
            pipeline.AddStage(stage1);
            pipeline.AddStage(stage2);

            // Create transformation context
            var context = new ETLFramework.Transformation.Models.TransformationContext("PipelineDemo", cancellationToken);

            // Execute pipeline
            var results = await pipeline.ExecuteAsync(records, context, cancellationToken);

            _logger.LogInformation("Pipeline Results: {ResultCount}", results.Count());
            foreach (var result in results.Where(r => r.IsSuccessful && r.OutputRecord != null))
            {
                var outputRecord = result.OutputRecord!;
                _logger.LogInformation("  Result Record {id}: name={name}, score={score}, doubledScore={doubledScore}",
                    outputRecord.GetField<string>("id"),
                    outputRecord.GetField<string>("name"),
                    outputRecord.GetField<string>("score"),
                    outputRecord.GetField<string>("doubledScore"));
            }

            // Show pipeline statistics
            var pipelineStats = pipeline.GetStatistics();
            _logger.LogInformation("Pipeline Statistics:");
            _logger.LogInformation("  Total Executions: {TotalExecutions}", pipelineStats.TotalExecutions);
            _logger.LogInformation("  Total Records Processed: {TotalRecordsProcessed}", pipelineStats.TotalRecordsProcessed);
            _logger.LogInformation("  Success Rate: {SuccessRate:F1}%", pipelineStats.SuccessRate);
            _logger.LogInformation("  Total Execution Time: {TotalExecutionTime}ms", pipelineStats.TotalExecutionTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transformation pipeline demo");
        }
    }

    /// <summary>
    /// Demonstrates transformation processor.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    private async Task DemonstrateTransformationProcessorAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- Transformation Processor Demo ---");

        try
        {
            // Create sample data
            var records = new List<DataRecord>();
            for (int i = 1; i <= 5; i++)
            {
                var record = new DataRecord();
                record.SetField("value", (i * 100).ToString());
                record.SetField("text", $"item{i}");
                records.Add(record);
            }

            _logger.LogInformation("Processing {RecordCount} records with transformation processor", records.Count);

            // Create transformations
            var transformations = new ETLFramework.Transformation.Interfaces.ITransformation[]
            {
                new ETLFramework.Transformation.Transformations.FieldTransformations.RoundTransformation("value", 0),
                new ETLFramework.Transformation.Transformations.FieldTransformations.UppercaseTransformation("text"),
                new ETLFramework.Transformation.Transformations.FieldTransformations.ConcatenateTransformation(
                    new[] { "text", "value" }, "combined", "_")
            };

            // Create processor and context
            var processor = new ETLFramework.Transformation.Processors.TransformationProcessor(
                _serviceProvider.GetRequiredService<ILogger<ETLFramework.Transformation.Processors.TransformationProcessor>>());

            var context = new ETLFramework.Transformation.Models.TransformationContext("ProcessorDemo", cancellationToken);

            // Validate transformations
            var validationResult = processor.ValidateTransformations(transformations, context);
            _logger.LogInformation("Transformation Validation: {IsValid}", validationResult.IsValid);

            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    _logger.LogWarning("  Validation Error: {Error}", error);
                }
            }

            // Process records
            var results = await processor.ProcessRecordsAsync(records, transformations, context, cancellationToken);

            _logger.LogInformation("Processing Results: {ResultCount}", results.Count());
            foreach (var result in results.Where(r => r.IsSuccessful && r.OutputRecord != null))
            {
                var outputRecord = result.OutputRecord!;
                _logger.LogInformation("  Processed Record: value={value}, text={text}, combined={combined}",
                    outputRecord.GetField<string>("value"),
                    outputRecord.GetField<string>("text"),
                    outputRecord.GetField<string>("combined"));
            }

            // Show processor statistics
            var processorStats = processor.GetStatistics();
            _logger.LogInformation("Processor Statistics:");
            _logger.LogInformation("  Total Records Processed: {TotalRecordsProcessed}", processorStats.TotalRecordsProcessed);
            _logger.LogInformation("  Total Transformations Executed: {TotalTransformationsExecuted}", processorStats.TotalTransformationsExecuted);
            _logger.LogInformation("  Success Rate: {SuccessRate:F1}%", processorStats.SuccessRate);
            _logger.LogInformation("  Throughput: {Throughput:F1} records/second", processorStats.ThroughputRecordsPerSecond);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transformation processor demo");
        }
    }
}
