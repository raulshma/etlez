using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// Initializes a new instance of the DemoPipelineService class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="orchestrator">The pipeline orchestrator</param>
    /// <param name="pipelineBuilder">The pipeline builder</param>
    /// <param name="connectorFactory">The connector factory</param>
    public DemoPipelineService(
        ILogger<DemoPipelineService> logger,
        IPipelineOrchestrator orchestrator,
        PipelineBuilder pipelineBuilder,
        IConnectorFactory connectorFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _pipelineBuilder = pipelineBuilder ?? throw new ArgumentNullException(nameof(pipelineBuilder));
        _connectorFactory = connectorFactory ?? throw new ArgumentNullException(nameof(connectorFactory));
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
}
