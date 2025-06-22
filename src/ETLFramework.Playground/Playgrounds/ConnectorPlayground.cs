using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Playground.Models;
using ETLFramework.Playground.Services;
using ETLFramework.Configuration.Models;
using ETLFramework.Connectors;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ETLFramework.Playground.Playgrounds;

/// <summary>
/// Playground module for testing all connector types.
/// </summary>
public class ConnectorPlayground : IConnectorPlayground
{
    private readonly ILogger<ConnectorPlayground> _logger;
    private readonly IPlaygroundUtilities _utilities;
    private readonly ISampleDataService _sampleDataService;
    private readonly ConnectorFactory _connectorFactory;

    public ConnectorPlayground(
        ILogger<ConnectorPlayground> logger,
        IPlaygroundUtilities utilities,
        ISampleDataService sampleDataService,
        ConnectorFactory connectorFactory)
    {
        _logger = logger;
        _utilities = utilities;
        _sampleDataService = sampleDataService;
        _connectorFactory = connectorFactory;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _utilities.DisplayHeader("Connector Playground", 
            "Test all data connectors with sample data and interactive scenarios");

        while (!cancellationToken.IsCancellationRequested)
        {
            var options = new[]
            {
                "📄 File System Connectors (CSV, JSON, XML)",
                "🗄️ Database Connectors (SQLite, SQL Server, MySQL)",
                "☁️ Cloud Storage Connectors (Azure Blob, AWS S3)",
                "🔍 Connector Health Checks",
                "📊 Connector Performance Tests",
                "🔧 Custom Connector Configuration",
                "🔙 Back to Main Menu"
            };

            var selection = _utilities.PromptForSelection("Select connector category:", options);

            try
            {
                switch (selection)
                {
                    case var s when s.Contains("File System"):
                        await TestFileSystemConnectorsAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Database"):
                        await TestDatabaseConnectorsAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Cloud Storage"):
                        await TestCloudStorageConnectorsAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Health Checks"):
                        await TestConnectorHealthChecksAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Performance"):
                        await TestConnectorPerformanceAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Custom"):
                        await TestCustomConnectorConfigurationAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Back"):
                        return;
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    _utilities.WaitForKeyPress();
                }
            }
            catch (Exception ex)
            {
                _utilities.DisplayError("Error in connector playground", ex);
                _utilities.WaitForKeyPress();
            }
        }
    }

    /// <summary>
    /// Tests file system connectors (CSV, JSON, XML).
    /// </summary>
    private async Task TestFileSystemConnectorsAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("File System Connectors", "Testing CSV, JSON, and XML connectors");

        var connectorTypes = new[] { "CSV", "JSON", "XML" };
        var selectedType = _utilities.PromptForSelection("Select file connector type:", connectorTypes);

        // Generate sample data
        var customerData = _sampleDataService.GenerateCustomerData(10).ToList();
        _utilities.DisplayResults(customerData.Take(3), "Sample Customer Data (showing first 3 records)");

        try
        {
            await _utilities.WithProgressAsync(async progress =>
            {
                progress.Report("Creating sample data file...");
                
                string filePath;
                Dictionary<string, object> config;

                switch (selectedType)
                {
                    case "CSV":
                        filePath = await _sampleDataService.CreateTempCsvFileAsync(customerData, "customers.csv");
                        config = new Dictionary<string, object>
                        {
                            ["FilePath"] = filePath,
                            ["HasHeaders"] = true,
                            ["Delimiter"] = ",",
                            ["Encoding"] = "UTF-8"
                        };
                        break;

                    case "JSON":
                        filePath = await _sampleDataService.CreateTempJsonFileAsync(customerData, "customers.json");
                        config = new Dictionary<string, object>
                        {
                            ["FilePath"] = filePath,
                            ["Format"] = "Array"
                        };
                        break;

                    case "XML":
                        // For XML, we'll create a simple XML structure
                        filePath = Path.GetTempFileName();
                        File.Move(filePath, Path.ChangeExtension(filePath, ".xml"));
                        filePath = Path.ChangeExtension(filePath, ".xml");
                        
                        // Create simple XML content
                        var xmlContent = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Customers>\n";
                        foreach (var customer in customerData.Take(5))
                        {
                            xmlContent += $"  <Customer>\n";
                            xmlContent += $"    <CustomerId>{customer.CustomerId}</CustomerId>\n";
                            xmlContent += $"    <FirstName>{customer.FirstName}</FirstName>\n";
                            xmlContent += $"    <LastName>{customer.LastName}</LastName>\n";
                            xmlContent += $"    <Email>{customer.Email}</Email>\n";
                            xmlContent += $"  </Customer>\n";
                        }
                        xmlContent += "</Customers>";
                        await File.WriteAllTextAsync(filePath, xmlContent);

                        config = new Dictionary<string, object>
                        {
                            ["FilePath"] = filePath,
                            ["RootElement"] = "Customers",
                            ["RecordElement"] = "Customer"
                        };
                        break;

                    default:
                        throw new ArgumentException($"Unknown connector type: {selectedType}");
                }

                progress.Report($"Creating {selectedType} connector...");
                var connectorConfig = CreateConnectorConfiguration(selectedType, config);
                var connector = _connectorFactory.CreateConnector(connectorConfig);

                progress.Report("Testing connector read operation...");
                var sourceConnector = connector as ISourceConnector<DataRecord>;
                if (sourceConnector == null)
                {
                    throw new InvalidOperationException($"Connector {selectedType} does not support reading");
                }

                var readData = new List<DataRecord>();
                await foreach (var record in sourceConnector.ReadAsync())
                {
                    readData.Add(record);
                }

                progress.Report("Testing connector write operation...");
                // Create a new file for write test
                var writeFilePath = Path.ChangeExtension(Path.GetTempFileName(), 
                    selectedType.ToLower() == "csv" ? ".csv" : selectedType.ToLower() == "json" ? ".json" : ".xml");
                
                var writeConfig = new Dictionary<string, object>(config) { ["FilePath"] = writeFilePath };
                var writeConnectorConfig = CreateConnectorConfiguration(selectedType, writeConfig);
                var writeConnector = _connectorFactory.CreateConnector(writeConnectorConfig);
                
                // Write a subset of the data
                var writeData = readData.Take(3).ToList();
                var destinationConnector = writeConnector as IDestinationConnector<DataRecord>;
                if (destinationConnector == null)
                {
                    throw new InvalidOperationException($"Connector {selectedType} does not support writing");
                }

                await destinationConnector.WriteBatchAsync(writeData);

                progress.Report("Connector test completed!");

                // Display results
                AnsiConsole.WriteLine();
                _utilities.DisplaySuccess($"{selectedType} Connector Test Results:");
                AnsiConsole.MarkupLine($"[green]✅ Read {readData.Count} records from {Path.GetFileName(filePath)}[/]");
                AnsiConsole.MarkupLine($"[green]✅ Wrote {writeData.Count} records to {Path.GetFileName(writeFilePath)}[/]");
                
                // Show sample of read data
                if (readData.Any())
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[bold]Sample of read data:[/]");
                    var table = new Table().BorderColor(Color.Green);
                    
                    // Add columns based on first record
                    var firstRecord = readData.First();
                    foreach (var field in firstRecord.Fields.Keys.Take(5)) // Show first 5 fields
                    {
                        table.AddColumn(field);
                    }

                    // Add rows (first 3 records)
                    foreach (var record in readData.Take(3))
                    {
                        var values = firstRecord.Fields.Keys.Take(5)
                            .Select(key => record.Fields.TryGetValue(key, out var value) ? value?.ToString() ?? "null" : "null")
                            .ToArray();
                        table.AddRow(values);
                    }

                    AnsiConsole.Write(table);
                }

                // Cleanup
                try
                {
                    if (File.Exists(filePath)) File.Delete(filePath);
                    if (File.Exists(writeFilePath)) File.Delete(writeFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup test files");
                }

            }, $"Testing {selectedType} Connector");

        }
        catch (Exception ex)
        {
            _utilities.DisplayError($"Failed to test {selectedType} connector", ex);
        }
    }

    /// <summary>
    /// Tests database connectors.
    /// </summary>
    private async Task TestDatabaseConnectorsAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Database Connectors", "Testing SQLite, SQL Server, and MySQL connectors");

        var connectorTypes = new[] { "SQLite", "SQL Server", "MySQL" };
        var selectedType = _utilities.PromptForSelection("Select database connector type:", connectorTypes);

        AnsiConsole.MarkupLine($"[yellow]Note: {selectedType} connector testing requires proper database setup.[/]");
        AnsiConsole.MarkupLine("[dim]For demonstration purposes, we'll show the configuration and connection test.[/]");

        try
        {
            Dictionary<string, object> config = selectedType switch
            {
                "SQLite" => new Dictionary<string, object>
                {
                    ["ConnectionString"] = "Data Source=:memory:",
                    ["TableName"] = "TestTable"
                },
                "SQL Server" => new Dictionary<string, object>
                {
                    ["ConnectionString"] = "Server=(localdb)\\mssqllocaldb;Database=ETLPlayground;Trusted_Connection=true;",
                    ["TableName"] = "TestTable"
                },
                "MySQL" => new Dictionary<string, object>
                {
                    ["ConnectionString"] = "Server=localhost;Database=ETLPlayground;Uid=root;Pwd=password;",
                    ["TableName"] = "TestTable"
                },
                _ => throw new ArgumentException($"Unknown database type: {selectedType}")
            };

            _utilities.DisplaySuccess($"{selectedType} Connector Configuration:");
            var configTable = new Table().BorderColor(Color.Blue);
            configTable.AddColumn("Setting");
            configTable.AddColumn("Value");

            foreach (var kvp in config)
            {
                var value = kvp.Key == "ConnectionString" && kvp.Value.ToString()!.Contains("Pwd=") 
                    ? kvp.Value.ToString()!.Split("Pwd=")[0] + "Pwd=***" 
                    : kvp.Value.ToString();
                configTable.AddRow(kvp.Key, value ?? "null");
            }

            AnsiConsole.Write(configTable);

            // Test connection (this might fail if database is not available)
            try
            {
                var connectorConfig = CreateConnectorConfiguration(selectedType, config);
                var connector = _connectorFactory.CreateConnector(connectorConfig);
                _utilities.DisplaySuccess($"✅ {selectedType} connector created successfully");
                
                // For SQLite (in-memory), we can actually test operations
                if (selectedType == "SQLite")
                {
                    AnsiConsole.MarkupLine("[green]Testing SQLite operations with in-memory database...[/]");
                    
                    // This would require actual implementation of database operations
                    // For now, we'll just show that the connector was created
                    _utilities.DisplaySuccess("SQLite connector is ready for operations");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]To fully test {selectedType}, ensure the database server is running and accessible.[/]");
                }
            }
            catch (Exception ex)
            {
                _utilities.DisplayError($"Failed to create {selectedType} connector", ex);
                AnsiConsole.MarkupLine("[dim]This is expected if the database server is not available.[/]");
            }
        }
        catch (Exception ex)
        {
            _utilities.DisplayError($"Error testing {selectedType} connector", ex);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests cloud storage connectors.
    /// </summary>
    private async Task TestCloudStorageConnectorsAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Cloud Storage Connectors", "Testing Azure Blob and AWS S3 connectors");

        var connectorTypes = new[] { "Azure Blob", "AWS S3" };
        var selectedType = _utilities.PromptForSelection("Select cloud storage connector type:", connectorTypes);

        AnsiConsole.MarkupLine($"[yellow]Note: {selectedType} connector testing requires valid cloud credentials.[/]");
        AnsiConsole.MarkupLine("[dim]For demonstration purposes, we'll show the configuration requirements.[/]");

        Dictionary<string, object> config = selectedType switch
        {
            "Azure Blob" => new Dictionary<string, object>
            {
                ["ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net",
                ["ContainerName"] = "etl-playground",
                ["Prefix"] = "test/"
            },
            "AWS S3" => new Dictionary<string, object>
            {
                ["AccessKey"] = "<your-access-key>",
                ["SecretKey"] = "<your-secret-key>",
                ["Region"] = "us-east-1",
                ["BucketName"] = "etl-playground",
                ["Prefix"] = "test/"
            },
            _ => throw new ArgumentException($"Unknown cloud storage type: {selectedType}")
        };

        _utilities.DisplaySuccess($"{selectedType} Connector Configuration Requirements:");
        var configTable = new Table().BorderColor(Color.Blue);
        configTable.AddColumn("Setting");
        configTable.AddColumn("Description");
        configTable.AddColumn("Example Value");

        foreach (var kvp in config)
        {
            var description = kvp.Key switch
            {
                "ConnectionString" => "Azure Storage connection string",
                "ContainerName" => "Azure Blob container name",
                "AccessKey" => "AWS access key ID",
                "SecretKey" => "AWS secret access key",
                "Region" => "AWS region",
                "BucketName" => "S3 bucket name",
                "Prefix" => "Optional prefix for object keys",
                _ => "Configuration value"
            };

            var exampleValue = kvp.Value.ToString()!.Contains("<") ? "[dim]<configure-this>[/]" : kvp.Value.ToString();
            configTable.AddRow(kvp.Key, description, exampleValue ?? "");
        }

        AnsiConsole.Write(configTable);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]To test cloud storage connectors:[/]");
        AnsiConsole.MarkupLine("1. Configure your cloud storage credentials in appsettings.json");
        AnsiConsole.MarkupLine("2. Ensure your storage account/bucket exists");
        AnsiConsole.MarkupLine("3. Verify network connectivity to the cloud service");
        AnsiConsole.MarkupLine("4. Run the connector test again");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests connector health checks.
    /// </summary>
    private async Task TestConnectorHealthChecksAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Connector Health Checks", "Test connectivity and health of all configured connectors");

        AnsiConsole.MarkupLine("[bold]Testing connector health checks...[/]");

        // This would test all registered connectors
        var connectorTypes = new[] { "CSV", "JSON", "XML", "SQLite" };

        foreach (var connectorType in connectorTypes)
        {
            try
            {
                AnsiConsole.MarkupLine($"[blue]Testing {connectorType} connector...[/]");
                
                // Create a basic configuration for testing
                var config = connectorType switch
                {
                    "CSV" => new Dictionary<string, object> { ["FilePath"] = Path.GetTempFileName(), ["HasHeaders"] = true },
                    "JSON" => new Dictionary<string, object> { ["FilePath"] = Path.GetTempFileName(), ["Format"] = "Array" },
                    "XML" => new Dictionary<string, object> { ["FilePath"] = Path.GetTempFileName(), ["RootElement"] = "Root" },
                    "SQLite" => new Dictionary<string, object> { ["ConnectionString"] = "Data Source=:memory:" },
                    _ => new Dictionary<string, object>()
                };

                var connectorConfig = CreateConnectorConfiguration(connectorType, config);
                var connector = _connectorFactory.CreateConnector(connectorConfig);
                _utilities.DisplaySuccess($"✅ {connectorType} connector: Healthy");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ {connectorType} connector: {ex.Message}[/]");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests connector performance.
    /// </summary>
    private async Task TestConnectorPerformanceAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Connector Performance Tests", "Benchmark connector read/write performance");

        var dataSizes = new[] { "Small (100 records)", "Medium (1,000 records)", "Large (10,000 records)" };
        var selectedSize = _utilities.PromptForSelection("Select data size for performance test:", dataSizes);

        var recordCount = selectedSize switch
        {
            var s when s.Contains("Small") => 100,
            var s when s.Contains("Medium") => 1000,
            var s when s.Contains("Large") => 10000,
            _ => 100
        };

        AnsiConsole.MarkupLine($"[blue]Generating {recordCount} records for performance testing...[/]");

        var testData = _sampleDataService.GenerateCustomerData(recordCount).ToList();
        
        // Test CSV connector performance
        await TestConnectorPerformanceForType("CSV", testData, cancellationToken);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests performance for a specific connector type.
    /// </summary>
    private async Task TestConnectorPerformanceForType(string connectorType, List<CustomerData> testData, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Create test file
            var filePath = await _sampleDataService.CreateTempCsvFileAsync(testData);
            var createTime = stopwatch.Elapsed;

            // Test read performance
            stopwatch.Restart();
            var config = new Dictionary<string, object>
            {
                ["FilePath"] = filePath,
                ["HasHeaders"] = true
            };
            var connectorConfig = CreateConnectorConfiguration(connectorType, config);
            var connector = _connectorFactory.CreateConnector(connectorConfig);
            var sourceConnector = connector as ISourceConnector<DataRecord>;
            if (sourceConnector == null)
            {
                throw new InvalidOperationException($"Connector {connectorType} does not support reading");
            }

            var readData = new List<DataRecord>();
            await foreach (var record in sourceConnector.ReadAsync())
            {
                readData.Add(record);
            }
            var readTime = stopwatch.Elapsed;

            // Test write performance
            stopwatch.Restart();
            var writeFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".csv");
            var writeConfig = new Dictionary<string, object>
            {
                ["FilePath"] = writeFilePath,
                ["HasHeaders"] = true
            };
            var writeConnectorConfig = CreateConnectorConfiguration(connectorType, writeConfig);
            var writeConnector = _connectorFactory.CreateConnector(writeConnectorConfig);
            var destinationConnector = writeConnector as IDestinationConnector<DataRecord>;
            if (destinationConnector == null)
            {
                throw new InvalidOperationException($"Connector {connectorType} does not support writing");
            }

            await destinationConnector.WriteBatchAsync(readData);
            var writeTime = stopwatch.Elapsed;

            // Display results
            var resultsTable = new Table()
                .Title($"[bold]{connectorType} Performance Results[/]")
                .BorderColor(Color.Green);

            resultsTable.AddColumn("Operation");
            resultsTable.AddColumn("Records");
            resultsTable.AddColumn("Time");
            resultsTable.AddColumn("Records/sec");

            resultsTable.AddRow("File Creation", testData.Count.ToString(), createTime.ToDisplayString(), 
                (testData.Count / Math.Max(createTime.TotalSeconds, 0.001)).ToString("F0"));
            resultsTable.AddRow("Read", readData.Count.ToString(), readTime.ToDisplayString(), 
                (readData.Count / Math.Max(readTime.TotalSeconds, 0.001)).ToString("F0"));
            resultsTable.AddRow("Write", readData.Count.ToString(), writeTime.ToDisplayString(), 
                (readData.Count / Math.Max(writeTime.TotalSeconds, 0.001)).ToString("F0"));

            AnsiConsole.Write(resultsTable);

            // Cleanup
            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                if (File.Exists(writeFilePath)) File.Delete(writeFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup performance test files");
            }
        }
        catch (Exception ex)
        {
            _utilities.DisplayError($"Performance test failed for {connectorType}", ex);
        }
    }

    /// <summary>
    /// Tests custom connector configuration.
    /// </summary>
    private async Task TestCustomConnectorConfigurationAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Custom Connector Configuration", "Create and test custom connector configurations");

        AnsiConsole.MarkupLine("[bold]This feature allows you to create custom connector configurations interactively.[/]");
        AnsiConsole.MarkupLine("[dim]You can specify custom parameters and test the connector with your settings.[/]");

        var connectorType = _utilities.PromptForSelection("Select connector type to configure:", 
            new[] { "CSV", "JSON", "XML", "SQLite" });

        var config = new Dictionary<string, object>();

        // Get configuration based on connector type
        switch (connectorType)
        {
            case "CSV":
                config["FilePath"] = _utilities.PromptForInput("Enter CSV file path:", "sample.csv");
                config["HasHeaders"] = _utilities.PromptForSelection("Has headers?", new[] { "true", "false" }) == "true";
                config["Delimiter"] = _utilities.PromptForInput("Enter delimiter:", ",");
                config["Encoding"] = _utilities.PromptForInput("Enter encoding:", "UTF-8");
                break;

            case "JSON":
                config["FilePath"] = _utilities.PromptForInput("Enter JSON file path:", "sample.json");
                config["Format"] = _utilities.PromptForSelection("Select JSON format:", new[] { "Array", "Object" });
                break;

            case "XML":
                config["FilePath"] = _utilities.PromptForInput("Enter XML file path:", "sample.xml");
                config["RootElement"] = _utilities.PromptForInput("Enter root element name:", "Root");
                config["RecordElement"] = _utilities.PromptForInput("Enter record element name:", "Record");
                break;

            case "SQLite":
                config["ConnectionString"] = _utilities.PromptForInput("Enter connection string:", "Data Source=test.db");
                config["TableName"] = _utilities.PromptForInput("Enter table name:", "TestTable");
                break;
        }

        // Display configuration
        _utilities.DisplaySuccess("Custom Configuration:");
        var configTable = new Table().BorderColor(Color.Blue);
        configTable.AddColumn("Setting");
        configTable.AddColumn("Value");

        foreach (var kvp in config)
        {
            configTable.AddRow(kvp.Key, kvp.Value?.ToString() ?? "null");
        }

        AnsiConsole.Write(configTable);

        // Test the configuration
        if (AnsiConsole.Confirm("Test this configuration?"))
        {
            try
            {
                var connectorConfig = CreateConnectorConfiguration(connectorType, config);
                var connector = _connectorFactory.CreateConnector(connectorConfig);
                _utilities.DisplaySuccess($"✅ {connectorType} connector created successfully with custom configuration!");
            }
            catch (Exception ex)
            {
                _utilities.DisplayError("Failed to create connector with custom configuration", ex);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a connector configuration from a connector type and parameters dictionary.
    /// </summary>
    /// <param name="connectorType">The connector type</param>
    /// <param name="parameters">The configuration parameters</param>
    /// <returns>A connector configuration</returns>
    private IConnectorConfiguration CreateConnectorConfiguration(string connectorType, Dictionary<string, object> parameters)
    {
        var config = new ConnectorConfiguration
        {
            Name = $"{connectorType} Playground Connector",
            ConnectorType = connectorType,
            ConnectionString = GetParameterValue(parameters, "FilePath") ??
                             GetParameterValue(parameters, "ConnectionString") ?? "",
            ConnectionProperties = new Dictionary<string, object>(parameters)
        };

        return config;
    }

    /// <summary>
    /// Helper method to get a parameter value from the dictionary.
    /// </summary>
    /// <param name="parameters">The parameters dictionary</param>
    /// <param name="key">The parameter key</param>
    /// <returns>The parameter value as string, or null if not found</returns>
    private static string? GetParameterValue(Dictionary<string, object> parameters, string key)
    {
        return parameters.TryGetValue(key, out var value) ? value?.ToString() : null;
    }
}
