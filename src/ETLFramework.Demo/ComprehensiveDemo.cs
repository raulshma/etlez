using ETLFramework.Configuration.Models;
using ETLFramework.Configuration.Providers;
using ETLFramework.Connectors;
using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Pipeline;
using ETLFramework.Transformation;
using ETLFramework.Transformation.Transformations.FieldTransformations;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Demo;

/// <summary>
/// Comprehensive demonstration of the ETL Framework capabilities.
/// </summary>
public class ComprehensiveDemo
{
    private readonly ILogger<ComprehensiveDemo> _logger;
    private readonly IConnectorFactory _connectorFactory;
    private readonly ITransformationEngine _transformationEngine;
    private readonly IPipelineOrchestrator _orchestrator;

    public ComprehensiveDemo(
        ILogger<ComprehensiveDemo> logger,
        IConnectorFactory connectorFactory,
        ITransformationEngine transformationEngine,
        IPipelineOrchestrator orchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectorFactory = connectorFactory ?? throw new ArgumentNullException(nameof(connectorFactory));
        _transformationEngine = transformationEngine ?? throw new ArgumentNullException(nameof(transformationEngine));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    /// <summary>
    /// Runs the comprehensive demo showcasing all framework features.
    /// </summary>
    public async Task RunComprehensiveDemoAsync()
    {
        _logger.LogInformation("🚀 Starting ETL Framework Comprehensive Demo");
        _logger.LogInformation("=" + new string('=', 60));

        try
        {
            // Demo 1: Basic CSV Processing
            await Demo1_BasicCsvProcessingAsync();

            // Demo 2: Advanced Transformations
            await Demo2_AdvancedTransformationsAsync();

            // Demo 3: Rule-Based Processing
            await Demo3_RuleBasedProcessingAsync();

            // Demo 4: Data Mapping and Validation
            await Demo4_DataMappingAndValidationAsync();

            // Demo 5: Performance Testing
            await Demo5_PerformanceTestingAsync();

            // Demo 6: Error Handling and Recovery
            await Demo6_ErrorHandlingAsync();

            // Demo 7: Pipeline Orchestration
            await Demo7_PipelineOrchestrationAsync();

            _logger.LogInformation("🎉 All demos completed successfully!");
            _logger.LogInformation("=" + new string('=', 60));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Demo failed with error: {Error}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Demo 1: Basic CSV Processing
    /// </summary>
    private async Task Demo1_BasicCsvProcessingAsync()
    {
        _logger.LogInformation("📊 Demo 1: Basic CSV Processing");
        _logger.LogInformation("-" + new string('-', 40));

        // Create sample CSV data
        var csvData = @"Name,Age,Email,Salary
John Doe,30,john.doe@example.com,50000
Jane Smith,25,jane.smith@example.com,60000
Bob Johnson,35,bob.johnson@example.com,75000
Alice Brown,28,alice.brown@example.com,55000
Charlie Wilson,42,charlie.wilson@example.com,80000";

        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, csvData);

        try
        {
            // Create CSV connector
            var csvConnector = await _connectorFactory.CreateConnectorAsync("CSV", new Dictionary<string, object>
            {
                ["FilePath"] = tempFile,
                ["HasHeaders"] = true
            });

            // Read data
            var data = await csvConnector.ReadAsync();
            _logger.LogInformation("✅ Read {Count} records from CSV", data.Count);

            // Apply basic transformations
            var transformations = new List<ITransformation>
            {
                new StringTransformations.ToUpperTransformation("Name"),
                new NumericTransformations.ToDecimalTransformation("Salary")
            };

            var transformedData = await _transformationEngine.TransformAsync(data, transformations);
            _logger.LogInformation("✅ Applied {Count} transformations", transformations.Count);

            // Display results
            foreach (var record in transformedData.Take(3))
            {
                _logger.LogInformation("Record: {Name}, Age: {Age}, Salary: {Salary:C}",
                    record.Fields["Name"],
                    record.Fields["Age"],
                    record.Fields["Salary"]);
            }

            _logger.LogInformation("✅ Demo 1 completed successfully\n");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Demo 2: Advanced Transformations
    /// </summary>
    private async Task Demo2_AdvancedTransformationsAsync()
    {
        _logger.LogInformation("🔧 Demo 2: Advanced Transformations");
        _logger.LogInformation("-" + new string('-', 40));

        // Create sample data with various data types
        var data = new List<DataRecord>
        {
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["CustomerName"] = "  john doe  ",
                    ["Email"] = "JOHN.DOE@EXAMPLE.COM",
                    ["Phone"] = "1234567890",
                    ["BirthDate"] = "1990-05-15",
                    ["Score"] = "85.5",
                    ["IsActive"] = "true"
                }
            },
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["CustomerName"] = "  jane smith  ",
                    ["Email"] = "JANE.SMITH@EXAMPLE.COM",
                    ["Phone"] = "9876543210",
                    ["BirthDate"] = "1985-12-03",
                    ["Score"] = "92.3",
                    ["IsActive"] = "false"
                }
            }
        };

        // Apply comprehensive transformations
        var transformations = new List<ITransformation>
        {
            new StringTransformations.TrimTransformation("CustomerName"),
            new StringTransformations.ToTitleCaseTransformation("CustomerName"),
            new StringTransformations.ToLowerTransformation("Email"),
            new DateTimeTransformations.ParseDateTransformation("BirthDate"),
            new NumericTransformations.ToDecimalTransformation("Score"),
            new StringTransformations.FormatPhoneTransformation("Phone")
        };

        var transformedData = await _transformationEngine.TransformAsync(data, transformations);
        _logger.LogInformation("✅ Applied {Count} advanced transformations", transformations.Count);

        // Display results
        foreach (var record in transformedData)
        {
            _logger.LogInformation("Customer: {Name}, Email: {Email}, Phone: {Phone}, Score: {Score}",
                record.Fields["CustomerName"],
                record.Fields["Email"],
                record.Fields["Phone"],
                record.Fields["Score"]);
        }

        _logger.LogInformation("✅ Demo 2 completed successfully\n");
    }

    /// <summary>
    /// Demo 3: Rule-Based Processing
    /// </summary>
    private async Task Demo3_RuleBasedProcessingAsync()
    {
        _logger.LogInformation("📋 Demo 3: Rule-Based Processing");
        _logger.LogInformation("-" + new string('-', 40));

        // Create sample customer data
        var data = new List<DataRecord>
        {
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["CustomerType"] = "Premium",
                    ["OrderAmount"] = 1500m,
                    ["YearsActive"] = 5,
                    ["Region"] = "North America"
                }
            },
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["CustomerType"] = "Standard",
                    ["OrderAmount"] = 500m,
                    ["YearsActive"] = 2,
                    ["Region"] = "Europe"
                }
            }
        };

        // Create rule engine and add business rules
        var ruleEngine = new RuleEngine(_logger);

        // Rule 1: Premium customers get 10% discount
        ruleEngine.AddRule(new Rule("PremiumDiscount", 1,
            record => record.Fields.ContainsKey("CustomerType") && 
                     record.Fields["CustomerType"]?.ToString() == "Premium",
            record =>
            {
                if (record.Fields.ContainsKey("OrderAmount") && 
                    decimal.TryParse(record.Fields["OrderAmount"]?.ToString(), out var amount))
                {
                    record.Fields["Discount"] = amount * 0.10m;
                    record.Fields["FinalAmount"] = amount * 0.90m;
                }
                return record;
            }));

        // Rule 2: Long-term customers get loyalty bonus
        ruleEngine.AddRule(new Rule("LoyaltyBonus", 2,
            record => record.Fields.ContainsKey("YearsActive") && 
                     int.TryParse(record.Fields["YearsActive"]?.ToString(), out var years) && years >= 3,
            record =>
            {
                record.Fields["LoyaltyBonus"] = 100m;
                return record;
            }));

        // Apply rules
        var processedData = await ruleEngine.ProcessAsync(data);
        _logger.LogInformation("✅ Applied business rules to {Count} records", data.Count);

        // Display results
        foreach (var record in processedData)
        {
            _logger.LogInformation("Customer: {Type}, Amount: {Amount:C}, Discount: {Discount:C}, Final: {Final:C}, Bonus: {Bonus:C}",
                record.Fields["CustomerType"],
                record.Fields["OrderAmount"],
                record.Fields.GetValueOrDefault("Discount", 0m),
                record.Fields.GetValueOrDefault("FinalAmount", record.Fields["OrderAmount"]),
                record.Fields.GetValueOrDefault("LoyaltyBonus", 0m));
        }

        _logger.LogInformation("✅ Demo 3 completed successfully\n");
    }

    /// <summary>
    /// Demo 4: Data Mapping and Validation
    /// </summary>
    private async Task Demo4_DataMappingAndValidationAsync()
    {
        _logger.LogInformation("🗺️ Demo 4: Data Mapping and Validation");
        _logger.LogInformation("-" + new string('-', 40));

        // Create legacy system data
        var legacyData = new List<DataRecord>
        {
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["cust_name"] = "John Doe",
                    ["cust_email"] = "john@example.com",
                    ["cust_phone"] = "123-456-7890",
                    ["order_total"] = "1500.00",
                    ["order_date"] = "2024-01-15"
                }
            }
        };

        // Create data mapper
        var mapper = new DataMapper(_logger);

        // Configure field mappings
        mapper.AddMapping("cust_name", "CustomerName", value => value?.ToString()?.ToUpper())
              .AddMapping("cust_email", "Email", value => value?.ToString()?.ToLower())
              .AddMapping("cust_phone", "PhoneNumber")
              .AddMapping("order_total", "OrderAmount", value => decimal.Parse(value?.ToString() ?? "0"))
              .AddMapping("order_date", "OrderDate", value => DateTime.Parse(value?.ToString() ?? DateTime.Now.ToString()))
              .AddConstantMapping("Source", "LegacySystem")
              .AddConditionalMapping("Priority", 
                  record => decimal.Parse(record.Fields["order_total"]?.ToString() ?? "0") > 1000 ? "High" : "Normal");

        // Apply mappings
        var mappedData = await mapper.MapAsync(legacyData);
        _logger.LogInformation("✅ Applied data mapping with {Count} field mappings", mapper.MappingCount);

        // Validate data
        var validator = new DataValidator(_logger);
        validator.AddRequiredField("CustomerName")
                .AddRequiredField("Email")
                .AddEmailValidation("Email")
                .AddRangeValidation("OrderAmount", 0m, 10000m);

        var validationResults = await validator.ValidateAsync(mappedData);
        _logger.LogInformation("✅ Validated {Count} records with {ErrorCount} errors",
            mappedData.Count, validationResults.Sum(r => r.Errors.Count));

        // Display results
        foreach (var record in mappedData)
        {
            _logger.LogInformation("Mapped Record: {Name}, {Email}, Amount: {Amount:C}, Priority: {Priority}",
                record.Fields["CustomerName"],
                record.Fields["Email"],
                record.Fields["OrderAmount"],
                record.Fields["Priority"]);
        }

        _logger.LogInformation("✅ Demo 4 completed successfully\n");
    }

    /// <summary>
    /// Demo 5: Performance Testing
    /// </summary>
    private async Task Demo5_PerformanceTestingAsync()
    {
        _logger.LogInformation("⚡ Demo 5: Performance Testing");
        _logger.LogInformation("-" + new string('-', 40));

        // Generate large dataset
        var largeDataset = new List<DataRecord>();
        for (int i = 1; i <= 10000; i++)
        {
            largeDataset.Add(new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["Id"] = i,
                    ["Name"] = $"User{i}",
                    ["Email"] = $"user{i}@example.com",
                    ["Score"] = Random.Shared.Next(0, 100),
                    ["CreatedDate"] = DateTime.Now.AddDays(-Random.Shared.Next(0, 365)).ToString()
                }
            });
        }

        _logger.LogInformation("📊 Generated {Count} records for performance testing", largeDataset.Count);

        // Performance test: Transformations
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var transformations = new List<ITransformation>
        {
            new NumericTransformations.ToIntTransformation("Id"),
            new StringTransformations.ToUpperTransformation("Name"),
            new StringTransformations.ToLowerTransformation("Email"),
            new DateTimeTransformations.ParseDateTransformation("CreatedDate")
        };

        var transformedData = await _transformationEngine.TransformAsync(largeDataset, transformations);
        stopwatch.Stop();

        var throughput = largeDataset.Count / stopwatch.Elapsed.TotalSeconds;
        _logger.LogInformation("⚡ Transformation Performance:");
        _logger.LogInformation("   - Records: {Count:N0}", largeDataset.Count);
        _logger.LogInformation("   - Time: {Time:F2} seconds", stopwatch.Elapsed.TotalSeconds);
        _logger.LogInformation("   - Throughput: {Throughput:N0} records/second", throughput);

        // Get performance metrics
        var metrics = await _transformationEngine.GetTransformationMetricsAsync();
        _logger.LogInformation("📈 Transformation Metrics:");
        _logger.LogInformation("   - Total Transformations: {Count}", metrics.TotalTransformations);
        _logger.LogInformation("   - Average Time: {Time:F2}ms", metrics.AverageTransformationTime.TotalMilliseconds);
        _logger.LogInformation("   - Success Rate: {Rate:P2}", metrics.SuccessRate);

        _logger.LogInformation("✅ Demo 5 completed successfully\n");
    }

    /// <summary>
    /// Demo 6: Error Handling and Recovery
    /// </summary>
    private async Task Demo6_ErrorHandlingAsync()
    {
        _logger.LogInformation("🛡️ Demo 6: Error Handling and Recovery");
        _logger.LogInformation("-" + new string('-', 40));

        // Create data with intentional errors
        var problematicData = new List<DataRecord>
        {
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["Name"] = "Valid User",
                    ["Age"] = "25",
                    ["Email"] = "valid@example.com"
                }
            },
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["Name"] = null, // Missing required field
                    ["Age"] = "invalid_age", // Invalid number
                    ["Email"] = "invalid-email" // Invalid email format
                }
            }
        };

        try
        {
            // Apply transformations with error handling
            var transformations = new List<ITransformation>
            {
                new NumericTransformations.ToIntTransformation("Age"),
                new StringTransformations.ToUpperTransformation("Name")
            };

            var result = await _transformationEngine.TransformAsync(problematicData, transformations);
            _logger.LogInformation("✅ Processed {Count} records with error handling", result.Count);

            // Validate and collect errors
            var validator = new DataValidator(_logger);
            validator.AddRequiredField("Name")
                    .AddEmailValidation("Email")
                    .AddRangeValidation("Age", 0, 120);

            var validationResults = await validator.ValidateAsync(result);
            
            var validRecords = validationResults.Where(r => r.IsValid).ToList();
            var invalidRecords = validationResults.Where(r => !r.IsValid).ToList();

            _logger.LogInformation("📊 Validation Results:");
            _logger.LogInformation("   - Valid Records: {Count}", validRecords.Count);
            _logger.LogInformation("   - Invalid Records: {Count}", invalidRecords.Count);

            foreach (var invalid in invalidRecords)
            {
                _logger.LogWarning("❌ Invalid Record Errors: {Errors}", 
                    string.Join(", ", invalid.Errors));
            }

            _logger.LogInformation("✅ Demo 6 completed successfully\n");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Demo 6: {Message}", ex.Message);
            _logger.LogInformation("🔄 Error handling demonstrated\n");
        }
    }

    /// <summary>
    /// Demo 7: Complete Pipeline Orchestration
    /// </summary>
    private async Task Demo7_PipelineOrchestrationAsync()
    {
        _logger.LogInformation("🎭 Demo 7: Complete Pipeline Orchestration");
        _logger.LogInformation("-" + new string('-', 40));

        // Create a complete ETL pipeline configuration
        var pipelineConfig = new PipelineConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Complete ETL Demo Pipeline",
            Description = "Demonstrates full ETL pipeline capabilities",
            SourceConnector = new ConnectorConfiguration
            {
                Type = "Memory",
                Configuration = new Dictionary<string, object>()
            },
            TargetConnector = new ConnectorConfiguration
            {
                Type = "Memory", 
                Configuration = new Dictionary<string, object>()
            }
        };

        // Create pipeline
        var pipeline = new Pipeline.Pipeline(
            pipelineConfig.Id,
            pipelineConfig.Name,
            pipelineConfig.Description,
            _logger);

        // Add Extract stage
        pipeline.AddStage(new PipelineStage("Extract", async (context, ct) =>
        {
            var sampleData = new List<DataRecord>
            {
                new DataRecord
                {
                    Fields = new Dictionary<string, object?>
                    {
                        ["CustomerID"] = 1,
                        ["Name"] = "john doe",
                        ["Email"] = "JOHN@EXAMPLE.COM",
                        ["OrderAmount"] = "1500.50",
                        ["OrderDate"] = "2024-01-15"
                    }
                }
            };
            
            context.SetData("extracted_data", sampleData);
            _logger.LogInformation("📥 Extracted {Count} records", sampleData.Count);
            return new StageResult { IsSuccess = true, RecordsProcessed = sampleData.Count };
        }));

        // Add Transform stage
        pipeline.AddStage(new PipelineStage("Transform", async (context, ct) =>
        {
            var data = context.GetData<List<DataRecord>>("extracted_data") ?? new List<DataRecord>();
            
            var transformations = new List<ITransformation>
            {
                new StringTransformations.ToTitleCaseTransformation("Name"),
                new StringTransformations.ToLowerTransformation("Email"),
                new NumericTransformations.ToDecimalTransformation("OrderAmount"),
                new DateTimeTransformations.ParseDateTransformation("OrderDate")
            };

            var transformedData = await _transformationEngine.TransformAsync(data, transformations, ct);
            context.SetData("transformed_data", transformedData);
            _logger.LogInformation("🔧 Transformed {Count} records", transformedData.Count);
            return new StageResult { IsSuccess = true, RecordsProcessed = transformedData.Count };
        }));

        // Add Load stage
        pipeline.AddStage(new PipelineStage("Load", async (context, ct) =>
        {
            var data = context.GetData<List<DataRecord>>("transformed_data") ?? new List<DataRecord>();
            
            // Simulate loading to target system
            await Task.Delay(100, ct);
            
            _logger.LogInformation("📤 Loaded {Count} records to target system", data.Count);
            return new StageResult { IsSuccess = true, RecordsProcessed = data.Count };
        }));

        // Execute pipeline
        var pipelineContext = _orchestrator.CreateContext(pipelineConfig, _logger);
        var result = await _orchestrator.ExecutePipelineAsync(pipeline, pipelineContext);

        _logger.LogInformation("🎯 Pipeline Execution Results:");
        _logger.LogInformation("   - Success: {Success}", result.IsSuccess);
        _logger.LogInformation("   - Records Processed: {Count}", result.RecordsProcessed);
        _logger.LogInformation("   - Duration: {Duration:F2} seconds", result.Duration?.TotalSeconds ?? 0);
        _logger.LogInformation("   - Errors: {ErrorCount}", result.Errors.Count);

        _logger.LogInformation("✅ Demo 7 completed successfully\n");
    }
}
