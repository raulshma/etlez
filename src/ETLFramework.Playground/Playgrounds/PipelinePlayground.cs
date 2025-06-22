using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Pipeline;
using ETLFramework.Transformation.Transformations.FieldTransformations;
using ETLFramework.Transformation.Models;
using ETLFramework.Connectors;
using ETLFramework.Configuration.Models;
using ETLFramework.Playground.Services;
using ETLFramework.Playground.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ETLFramework.Playground.Playgrounds;

/// <summary>
/// Playground module for testing complete ETL pipelines.
/// </summary>
public class PipelinePlayground : IPipelinePlayground
{
    private readonly ILogger<PipelinePlayground> _logger;
    private readonly IPlaygroundUtilities _utilities;
    private readonly ISampleDataService _sampleDataService;
    private readonly IPipelineOrchestrator _pipelineOrchestrator;

    public PipelinePlayground(
        ILogger<PipelinePlayground> logger,
        IPlaygroundUtilities utilities,
        ISampleDataService sampleDataService,
        IPipelineOrchestrator pipelineOrchestrator)
    {
        _logger = logger;
        _utilities = utilities;
        _sampleDataService = sampleDataService;
        _pipelineOrchestrator = pipelineOrchestrator;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _utilities.DisplayHeader("Pipeline Playground",
            "Build and test complete ETL pipelines");

        while (!cancellationToken.IsCancellationRequested)
        {
            var options = new[]
            {
                "🏗️ Simple ETL Pipeline",
                "🔄 Multi-Stage Pipeline",
                "📊 Data Quality Pipeline",
                "⚡ Performance Pipeline",
                "🧪 Custom Pipeline Builder",
                "📈 Pipeline Monitoring",
                "🔙 Back to Main Menu"
            };

            var selection = _utilities.PromptForSelection("Select pipeline scenario:", options);

            try
            {
                switch (selection)
                {
                    case var s when s.Contains("Simple ETL"):
                        await RunSimpleETLPipelineAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Multi-Stage"):
                        await RunMultiStagePipelineAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Data Quality"):
                        await RunDataQualityPipelineAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Performance"):
                        await RunPerformancePipelineAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Custom"):
                        await RunCustomPipelineBuilderAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Monitoring"):
                        await RunPipelineMonitoringAsync(cancellationToken);
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
                _utilities.DisplayError("Error in pipeline playground", ex);
                _utilities.WaitForKeyPress();
            }
        }
    }

    /// <summary>
    /// Runs a simple ETL pipeline demonstration.
    /// </summary>
    private async Task RunSimpleETLPipelineAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Simple ETL Pipeline", "Extract → Transform → Load demonstration");

        try
        {
            await _utilities.WithProgressAsync(async progress =>
            {
                progress.Report("Setting up pipeline...");

                // Create sample data
                var customerData = _sampleDataService.GenerateCustomerData(10).ToList();

                progress.Report("Creating source data file...");
                var sourceFile = await _sampleDataService.CreateTempCsvFileAsync(customerData, "source_customers.csv");

                progress.Report("Creating pipeline configuration...");

                // Create pipeline configuration
                var pipelineConfig = new PipelineConfiguration
                {
                    Name = "Simple Customer ETL",
                    Description = "Extract customer data, transform names to uppercase, load to output file",
                    IsEnabled = true,
                    MaxDegreeOfParallelism = 1,
                    Timeout = TimeSpan.FromMinutes(5)
                };

                progress.Report("Executing pipeline...");

                // Simulate pipeline execution
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold blue]🔄 Pipeline Execution:[/]");

                // Stage 1: Extract
                AnsiConsole.MarkupLine("\n[green]📥 EXTRACT Stage:[/]");
                AnsiConsole.MarkupLine($"[dim]• Reading data from: {Path.GetFileName(sourceFile)}[/]");
                AnsiConsole.MarkupLine($"[dim]• Records found: {customerData.Count}[/]");

                // Stage 2: Transform
                AnsiConsole.MarkupLine("\n[yellow]🔧 TRANSFORM Stage:[/]");
                AnsiConsole.MarkupLine("[dim]• Applying transformations:[/]");
                AnsiConsole.MarkupLine("[dim]  - Convert FirstName to uppercase[/]");
                AnsiConsole.MarkupLine("[dim]  - Convert LastName to uppercase[/]");
                AnsiConsole.MarkupLine("[dim]  - Trim whitespace from Email[/]");

                // Simulate transformations
                var transformedData = customerData.Select(c => new
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName.ToUpper(),
                    LastName = c.LastName.ToUpper(),
                    Email = c.Email.Trim(),
                    City = c.City,
                    State = c.State,
                    IsActive = c.IsActive
                }).ToList();

                // Stage 3: Load
                AnsiConsole.MarkupLine("\n[blue]📤 LOAD Stage:[/]");
                var outputFile = Path.ChangeExtension(Path.GetTempFileName(), ".csv");
                AnsiConsole.MarkupLine($"[dim]• Writing data to: {Path.GetFileName(outputFile)}[/]");
                AnsiConsole.MarkupLine($"[dim]• Records written: {transformedData.Count}[/]");

                progress.Report("Pipeline completed successfully!");

                // Display results
                AnsiConsole.WriteLine();
                _utilities.DisplaySuccess("Pipeline execution completed!");

                // Show sample of transformed data
                AnsiConsole.MarkupLine("\n[bold]Sample of transformed data:[/]");
                var sampleTable = new Table().BorderColor(Color.Green);
                sampleTable.AddColumn("ID");
                sampleTable.AddColumn("First Name");
                sampleTable.AddColumn("Last Name");
                sampleTable.AddColumn("Email");
                sampleTable.AddColumn("Location");

                foreach (var record in transformedData.Take(5))
                {
                    sampleTable.AddRow(
                        record.CustomerId.ToString(),
                        record.FirstName,
                        record.LastName,
                        record.Email,
                        $"{record.City}, {record.State}"
                    );
                }

                AnsiConsole.Write(sampleTable);

                // Pipeline statistics
                AnsiConsole.WriteLine();
                var statsTable = new Table().BorderColor(Color.Blue);
                statsTable.AddColumn("Metric");
                statsTable.AddColumn("Value");

                statsTable.AddRow("Pipeline Name", pipelineConfig.Name);
                statsTable.AddRow("Records Processed", transformedData.Count.ToString());
                statsTable.AddRow("Transformations Applied", "3");
                statsTable.AddRow("Status", "[green]Success[/]");
                statsTable.AddRow("Duration", "< 1 second");

                AnsiConsole.Write(statsTable);

                // Cleanup
                try
                {
                    if (File.Exists(sourceFile)) File.Delete(sourceFile);
                    if (File.Exists(outputFile)) File.Delete(outputFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup pipeline test files");
                }

            }, "Running Simple ETL Pipeline");

        }
        catch (Exception ex)
        {
            _utilities.DisplayError("Failed to run simple ETL pipeline", ex);
        }
    }

    /// <summary>
    /// Runs a multi-stage pipeline demonstration.
    /// </summary>
    private async Task RunMultiStagePipelineAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Multi-Stage Pipeline", "Complex pipeline with multiple transformation stages");

        try
        {
            await _utilities.WithProgressAsync(async progress =>
            {
                progress.Report("Initializing multi-stage pipeline...");

                // Generate initial dataset
                var rawCustomers = _sampleDataService.GenerateCustomerData(15).ToList();
                var processedRecords = 0;
                var validRecords = 0;
                var enrichedRecords = 0;

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold blue]🔄 Multi-Stage Pipeline Execution:[/]");

                // Stage 1: Extract
                progress.Report("Stage 1: Extracting customer data...");
                AnsiConsole.MarkupLine("\n[green]📥 STAGE 1 - EXTRACT:[/]");
                AnsiConsole.MarkupLine($"[dim]• Extracted {rawCustomers.Count} customer records[/]");
                AnsiConsole.MarkupLine("[dim]• Source: Customer database[/]");
                AnsiConsole.MarkupLine("[dim]• Format: Structured data[/]");

                await Task.Delay(500, cancellationToken); // Simulate processing time

                // Stage 2: Data Validation and Cleansing
                progress.Report("Stage 2: Validating and cleansing data...");
                AnsiConsole.MarkupLine("\n[yellow]🧹 STAGE 2 - DATA VALIDATION & CLEANSING:[/]");

                var cleanedCustomers = new List<dynamic>();
                foreach (var customer in rawCustomers)
                {
                    // Simulate validation and cleansing
                    var isValid = !string.IsNullOrWhiteSpace(customer.FirstName) &&
                                 !string.IsNullOrWhiteSpace(customer.LastName) &&
                                 customer.Email.Contains("@");

                    if (isValid)
                    {
                        cleanedCustomers.Add(new
                        {
                            CustomerId = customer.CustomerId,
                            FirstName = customer.FirstName.Trim(),
                            LastName = customer.LastName.Trim(),
                            Email = customer.Email.ToLower().Trim(),
                            CreditLimit = customer.CreditLimit,
                            IsActive = customer.IsActive,
                            CreatedDate = customer.CreatedDate,
                            ValidationStatus = "Valid"
                        });
                        validRecords++;
                    }
                    processedRecords++;
                }

                AnsiConsole.MarkupLine($"[dim]• Records processed: {processedRecords}[/]");
                AnsiConsole.MarkupLine($"[dim]• Valid records: {validRecords}[/]");
                AnsiConsole.MarkupLine($"[dim]• Invalid records: {processedRecords - validRecords}[/]");
                AnsiConsole.MarkupLine("[dim]• Applied: Name trimming, email normalization[/]");

                await Task.Delay(500, cancellationToken);

                // Stage 3: Business Rule Transformations
                progress.Report("Stage 3: Applying business rules...");
                AnsiConsole.MarkupLine("\n[blue]⚙️ STAGE 3 - BUSINESS RULE TRANSFORMATIONS:[/]");

                var businessRuleResults = cleanedCustomers.Select(c => new
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    CreditLimit = c.CreditLimit,
                    IsActive = c.IsActive,
                    CreatedDate = c.CreatedDate,
                    CustomerTier = c.CreditLimit switch
                    {
                        > 50000 => "Platinum",
                        > 25000 => "Gold",
                        > 10000 => "Silver",
                        _ => "Bronze"
                    },
                    RiskCategory = CalculateRiskCategory(c.CreditLimit, c.IsActive),
                    EligibleForUpgrade = c.CreditLimit > 20000 && c.IsActive,
                    ValidationStatus = c.ValidationStatus
                }).ToList();

                AnsiConsole.MarkupLine("[dim]• Applied customer tier classification[/]");
                AnsiConsole.MarkupLine("[dim]• Calculated risk categories[/]");
                AnsiConsole.MarkupLine("[dim]• Determined upgrade eligibility[/]");
                AnsiConsole.MarkupLine($"[dim]• Platinum customers: {businessRuleResults.Count(c => c.CustomerTier == "Platinum")}[/]");
                AnsiConsole.MarkupLine($"[dim]• Eligible for upgrade: {businessRuleResults.Count(c => c.EligibleForUpgrade)}[/]");

                await Task.Delay(500, cancellationToken);

                // Stage 4: Data Enrichment
                progress.Report("Stage 4: Enriching data...");
                AnsiConsole.MarkupLine("\n[magenta]🔍 STAGE 4 - DATA ENRICHMENT:[/]");

                var enrichedResults = businessRuleResults.Select(c => new
                {
                    c.CustomerId,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.CreditLimit,
                    c.IsActive,
                    c.CreatedDate,
                    c.CustomerTier,
                    c.RiskCategory,
                    c.EligibleForUpgrade,
                    // Enriched fields
                    FullName = $"{c.FirstName} {c.LastName}",
                    EmailDomain = GetEmailDomain(c.Email),
                    AccountAge = (DateTime.Now - c.CreatedDate).Days,
                    LastContactDate = DateTime.Now.AddDays(-Random.Shared.Next(1, 30)),
                    PreferredChannel = c.CustomerTier == "Platinum" ? "Phone" : "Email",
                    NextReviewDate = CalculateNextReviewDate(c.CustomerTier),
                    c.ValidationStatus
                }).ToList();

                enrichedRecords = enrichedResults.Count;

                AnsiConsole.MarkupLine("[dim]• Added full name concatenation[/]");
                AnsiConsole.MarkupLine("[dim]• Extracted email domains[/]");
                AnsiConsole.MarkupLine("[dim]• Calculated account ages[/]");
                AnsiConsole.MarkupLine("[dim]• Assigned preferred contact channels[/]");
                AnsiConsole.MarkupLine($"[dim]• Enriched records: {enrichedRecords}[/]");

                await Task.Delay(500, cancellationToken);

                // Stage 5: Load to Multiple Destinations
                progress.Report("Stage 5: Loading to destinations...");
                AnsiConsole.MarkupLine("\n[cyan]📤 STAGE 5 - LOAD TO MULTIPLE DESTINATIONS:[/]");

                // Simulate loading to different destinations based on customer tier
                var platinumCustomers = enrichedResults.Where(c => c.CustomerTier == "Platinum").ToList();
                var goldCustomers = enrichedResults.Where(c => c.CustomerTier == "Gold").ToList();
                var standardCustomers = enrichedResults.Where(c => c.CustomerTier is "Silver" or "Bronze").ToList();

                AnsiConsole.MarkupLine($"[dim]• CRM System: {enrichedResults.Count} complete customer records[/]");
                AnsiConsole.MarkupLine($"[dim]• VIP Database: {platinumCustomers.Count} platinum customers[/]");
                AnsiConsole.MarkupLine($"[dim]• Marketing System: {goldCustomers.Count} gold customers[/]");
                AnsiConsole.MarkupLine($"[dim]• Standard Database: {standardCustomers.Count} standard customers[/]");
                AnsiConsole.MarkupLine($"[dim]• Data Warehouse: {enrichedResults.Count} analytical records[/]");

                progress.Report("Pipeline completed successfully!");

                // Display sample results
                AnsiConsole.WriteLine();
                _utilities.DisplaySuccess("Multi-stage pipeline execution completed!");

                // Show sample of final enriched data
                AnsiConsole.MarkupLine("\n[bold]Sample of final enriched data:[/]");
                var sampleTable = new Table().BorderColor(Color.Green);
                sampleTable.AddColumn("ID");
                sampleTable.AddColumn("Full Name");
                sampleTable.AddColumn("Tier");
                sampleTable.AddColumn("Risk");
                sampleTable.AddColumn("Account Age");
                sampleTable.AddColumn("Channel");

                foreach (var record in enrichedResults.Take(5))
                {
                    var values = new string[]
                    {
                        record.CustomerId.ToString(),
                        record.FullName?.ToString() ?? "",
                        record.CustomerTier?.ToString() ?? "",
                        record.RiskCategory?.ToString() ?? "",
                        $"{record.AccountAge} days",
                        record.PreferredChannel?.ToString() ?? ""
                    };
                    sampleTable.AddRow(values);
                }

                AnsiConsole.Write(sampleTable);

                // Pipeline statistics
                AnsiConsole.WriteLine();
                var statsTable = new Table().BorderColor(Color.Blue);
                statsTable.AddColumn("Stage");
                statsTable.AddColumn("Records In");
                statsTable.AddColumn("Records Out");
                statsTable.AddColumn("Success Rate");

                statsTable.AddRow("Extract", rawCustomers.Count.ToString(), rawCustomers.Count.ToString(), "100%");
                statsTable.AddRow("Validation", rawCustomers.Count.ToString(), validRecords.ToString(), $"{(validRecords * 100 / rawCustomers.Count)}%");
                statsTable.AddRow("Business Rules", validRecords.ToString(), validRecords.ToString(), "100%");
                statsTable.AddRow("Enrichment", validRecords.ToString(), enrichedRecords.ToString(), "100%");
                statsTable.AddRow("Load", enrichedRecords.ToString(), enrichedRecords.ToString(), "100%");

                AnsiConsole.Write(statsTable);

            }, "Running Multi-Stage Pipeline");

        }
        catch (Exception ex)
        {
            _utilities.DisplayError("Failed to run multi-stage pipeline", ex);
        }
    }

    /// <summary>
    /// Runs a data quality pipeline demonstration.
    /// </summary>
    private async Task RunDataQualityPipelineAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Data Quality Pipeline", "Pipeline focused on data validation and quality checks");

        try
        {
            await _utilities.WithProgressAsync(async progress =>
            {
                progress.Report("Initializing data quality pipeline...");

                // Generate sample data with intentional quality issues
                var rawData = GenerateDataWithQualityIssues();

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold blue]🔍 Data Quality Pipeline Execution:[/]");

                // Stage 1: Data Profiling
                progress.Report("Stage 1: Profiling data...");
                AnsiConsole.MarkupLine("\n[green]📊 STAGE 1 - DATA PROFILING:[/]");

                var profileResults = ProfileData(rawData);
                AnsiConsole.MarkupLine($"[dim]• Total records: {profileResults.TotalRecords}[/]");
                AnsiConsole.MarkupLine($"[dim]• Complete records: {profileResults.CompleteRecords}[/]");
                AnsiConsole.MarkupLine($"[dim]• Records with missing data: {profileResults.RecordsWithMissingData}[/]");
                AnsiConsole.MarkupLine($"[dim]• Duplicate records: {profileResults.DuplicateRecords}[/]");
                AnsiConsole.MarkupLine($"[dim]• Invalid email formats: {profileResults.InvalidEmails}[/]");

                await Task.Delay(500, cancellationToken);

                // Stage 2: Validation Rules
                progress.Report("Stage 2: Applying validation rules...");
                AnsiConsole.MarkupLine("\n[yellow]✅ STAGE 2 - VALIDATION RULES:[/]");

                var validationResults = ApplyValidationRules(rawData);
                AnsiConsole.MarkupLine("[dim]• Rule 1: Required fields validation[/]");
                AnsiConsole.MarkupLine("[dim]• Rule 2: Email format validation[/]");
                AnsiConsole.MarkupLine("[dim]• Rule 3: Credit limit range validation[/]");
                AnsiConsole.MarkupLine("[dim]• Rule 4: Date consistency validation[/]");
                AnsiConsole.MarkupLine($"[dim]• Validation failures: {validationResults.Count(r => !r.IsValid)}[/]");

                await Task.Delay(500, cancellationToken);

                // Stage 3: Data Quality Scoring
                progress.Report("Stage 3: Calculating quality scores...");
                AnsiConsole.MarkupLine("\n[blue]📈 STAGE 3 - QUALITY SCORING:[/]");

                var scoredData = CalculateQualityScores(validationResults);
                var avgScore = scoredData.Average(d => d.QualityScore);
                var highQualityCount = scoredData.Count(d => d.QualityScore >= 80);
                var lowQualityCount = scoredData.Count(d => d.QualityScore < 50);

                AnsiConsole.MarkupLine($"[dim]• Average quality score: {avgScore:F1}%[/]");
                AnsiConsole.MarkupLine($"[dim]• High quality records (≥80%): {highQualityCount}[/]");
                AnsiConsole.MarkupLine($"[dim]• Low quality records (<50%): {lowQualityCount}[/]");

                await Task.Delay(500, cancellationToken);

                // Stage 4: Data Cleansing
                progress.Report("Stage 4: Cleansing data...");
                AnsiConsole.MarkupLine("\n[magenta]🧹 STAGE 4 - DATA CLEANSING:[/]");

                var cleansedData = CleanseData(scoredData);
                var cleansedCount = cleansedData.Count(d => d.WasCleansed);

                AnsiConsole.MarkupLine($"[dim]• Records cleansed: {cleansedCount}[/]");
                AnsiConsole.MarkupLine("[dim]• Applied: Name standardization[/]");
                AnsiConsole.MarkupLine("[dim]• Applied: Email normalization[/]");
                AnsiConsole.MarkupLine("[dim]• Applied: Data type corrections[/]");

                await Task.Delay(500, cancellationToken);

                // Stage 5: Quality Report & Quarantine
                progress.Report("Stage 5: Generating quality report...");
                AnsiConsole.MarkupLine("\n[cyan]📋 STAGE 5 - QUALITY REPORT & QUARANTINE:[/]");

                var quarantinedRecords = cleansedData.Where(d => d.QualityScore < 50).ToList();
                var approvedRecords = cleansedData.Where(d => d.QualityScore >= 50).ToList();

                AnsiConsole.MarkupLine($"[dim]• Approved records: {approvedRecords.Count}[/]");
                AnsiConsole.MarkupLine($"[dim]• Quarantined records: {quarantinedRecords.Count}[/]");
                AnsiConsole.MarkupLine("[dim]• Quality report generated[/]");
                AnsiConsole.MarkupLine("[dim]• Alerts sent for low-quality data[/]");

                progress.Report("Quality pipeline completed!");

                // Display quality report
                AnsiConsole.WriteLine();
                _utilities.DisplaySuccess("Data Quality Pipeline completed!");

                // Quality summary table
                AnsiConsole.MarkupLine("\n[bold]Data Quality Summary:[/]");
                var qualityTable = new Table().BorderColor(Color.Green);
                qualityTable.AddColumn("Metric");
                qualityTable.AddColumn("Count");
                qualityTable.AddColumn("Percentage");

                var totalRecords = rawData.Count;
                qualityTable.AddRow("Total Records", totalRecords.ToString(), "100%");
                qualityTable.AddRow("High Quality (≥80%)", highQualityCount.ToString(), $"{(highQualityCount * 100.0 / totalRecords):F1}%");
                qualityTable.AddRow("Medium Quality (50-79%)", (scoredData.Count(d => d.QualityScore >= 50 && d.QualityScore < 80)).ToString(), $"{(scoredData.Count(d => d.QualityScore >= 50 && d.QualityScore < 80) * 100.0 / totalRecords):F1}%");
                qualityTable.AddRow("Low Quality (<50%)", lowQualityCount.ToString(), $"{(lowQualityCount * 100.0 / totalRecords):F1}%");
                qualityTable.AddRow("Records Cleansed", cleansedCount.ToString(), $"{(cleansedCount * 100.0 / totalRecords):F1}%");
                qualityTable.AddRow("Quarantined", quarantinedRecords.Count.ToString(), $"{(quarantinedRecords.Count * 100.0 / totalRecords):F1}%");

                AnsiConsole.Write(qualityTable);

                // Sample quality issues
                if (quarantinedRecords.Any())
                {
                    AnsiConsole.MarkupLine("\n[bold]Sample Quality Issues:[/]");
                    var issuesTable = new Table().BorderColor(Color.Red);
                    issuesTable.AddColumn("Record ID");
                    issuesTable.AddColumn("Quality Score");
                    issuesTable.AddColumn("Issues");

                    foreach (var record in quarantinedRecords.Take(3))
                    {
                        issuesTable.AddRow(
                            record.CustomerId.ToString(),
                            $"{record.QualityScore:F0}%",
                            string.Join(", ", record.QualityIssues)
                        );
                    }

                    AnsiConsole.Write(issuesTable);
                }

            }, "Running Data Quality Pipeline");

        }
        catch (Exception ex)
        {
            _utilities.DisplayError("Failed to run data quality pipeline", ex);
        }
    }

    /// <summary>
    /// Runs a performance pipeline demonstration.
    /// </summary>
    private async Task RunPerformancePipelineAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Performance Pipeline", "High-throughput pipeline with performance optimization");

        AnsiConsole.MarkupLine("[yellow]Performance pipeline features:[/]");
        AnsiConsole.MarkupLine("[dim]• Parallel processing[/]");
        AnsiConsole.MarkupLine("[dim]• Batch optimization[/]");
        AnsiConsole.MarkupLine("[dim]• Memory management[/]");
        AnsiConsole.MarkupLine("[dim]• Throughput monitoring[/]");
        AnsiConsole.MarkupLine("[dim]• Resource utilization tracking[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs a custom pipeline builder demonstration.
    /// </summary>
    private async Task RunCustomPipelineBuilderAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Custom Pipeline Builder", "Interactive pipeline construction");

        AnsiConsole.MarkupLine("[yellow]Custom pipeline builder features:[/]");
        AnsiConsole.MarkupLine("[dim]• Interactive stage configuration[/]");
        AnsiConsole.MarkupLine("[dim]• Connector selection and setup[/]");
        AnsiConsole.MarkupLine("[dim]• Transformation chain building[/]");
        AnsiConsole.MarkupLine("[dim]• Pipeline validation[/]");
        AnsiConsole.MarkupLine("[dim]• Configuration export[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs a pipeline monitoring demonstration.
    /// </summary>
    private async Task RunPipelineMonitoringAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Pipeline Monitoring", "Real-time pipeline execution monitoring");

        AnsiConsole.MarkupLine("[yellow]Pipeline monitoring features:[/]");
        AnsiConsole.MarkupLine("[dim]• Real-time execution status[/]");
        AnsiConsole.MarkupLine("[dim]• Performance metrics[/]");
        AnsiConsole.MarkupLine("[dim]• Error tracking and alerts[/]");
        AnsiConsole.MarkupLine("[dim]• Resource utilization[/]");
        AnsiConsole.MarkupLine("[dim]• Execution history[/]");

        await Task.CompletedTask;
    }

    // Helper methods for pipeline implementations
    private static string GetEmailDomain(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            return "";

        var parts = email.Split('@');
        return parts.Length > 1 ? parts[1] : "";
    }

    private static string CalculateRiskCategory(decimal creditLimit, bool isActive)
    {
        if (!isActive) return "High Risk - Inactive";
        return creditLimit switch
        {
            > 75000 => "Low Risk",
            > 25000 => "Medium Risk",
            > 10000 => "Medium-High Risk",
            _ => "High Risk"
        };
    }

    private static DateTime CalculateNextReviewDate(string customerTier)
    {
        var months = customerTier switch
        {
            "Platinum" => 12,
            "Gold" => 6,
            "Silver" => 3,
            _ => 1
        };
        return DateTime.Now.AddMonths(months);
    }

    private List<dynamic> GenerateDataWithQualityIssues()
    {
        var customers = _sampleDataService.GenerateCustomerData(20).ToList();
        var dataWithIssues = new List<dynamic>();

        for (int i = 0; i < customers.Count; i++)
        {
            var customer = customers[i];

            // Introduce quality issues intentionally
            var firstName = i % 5 == 0 ? "" : customer.FirstName; // 20% missing first names
            var lastName = i % 7 == 0 ? "" : customer.LastName; // ~14% missing last names
            var email = i % 4 == 0 ? customer.Email.Replace("@", "") : customer.Email; // 25% invalid emails
            var creditLimit = i % 6 == 0 ? -1000 : customer.CreditLimit; // ~17% invalid credit limits

            dataWithIssues.Add(new
            {
                CustomerId = customer.CustomerId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                CreditLimit = creditLimit,
                IsActive = customer.IsActive,
                CreatedDate = customer.CreatedDate
            });
        }

        // Add some duplicate records
        if (dataWithIssues.Count > 2)
        {
            dataWithIssues.Add(dataWithIssues[0]); // Duplicate first record
            dataWithIssues.Add(dataWithIssues[1]); // Duplicate second record
        }

        return dataWithIssues;
    }

    private static DataProfileResult ProfileData(List<dynamic> data)
    {
        var totalRecords = data.Count;
        var completeRecords = 0;
        var recordsWithMissingData = 0;
        var invalidEmails = 0;
        var duplicateRecords = 0;

        var seenIds = new HashSet<int>();

        foreach (var record in data)
        {
            var hasAllFields = !string.IsNullOrWhiteSpace(record.FirstName) &&
                              !string.IsNullOrWhiteSpace(record.LastName) &&
                              !string.IsNullOrWhiteSpace(record.Email) &&
                              record.CreditLimit > 0;

            if (hasAllFields) completeRecords++;
            else recordsWithMissingData++;

            if (!record.Email.Contains("@")) invalidEmails++;

            if (seenIds.Contains(record.CustomerId)) duplicateRecords++;
            else seenIds.Add(record.CustomerId);
        }

        return new DataProfileResult
        {
            TotalRecords = totalRecords,
            CompleteRecords = completeRecords,
            RecordsWithMissingData = recordsWithMissingData,
            DuplicateRecords = duplicateRecords,
            InvalidEmails = invalidEmails
        };
    }

    private static List<ValidationResult> ApplyValidationRules(List<dynamic> data)
    {
        return data.Select(record => new ValidationResult
        {
            CustomerId = record.CustomerId,
            FirstName = record.FirstName,
            LastName = record.LastName,
            Email = record.Email,
            CreditLimit = record.CreditLimit,
            IsActive = record.IsActive,
            CreatedDate = record.CreatedDate,
            IsValid = !string.IsNullOrWhiteSpace(record.FirstName) &&
                     !string.IsNullOrWhiteSpace(record.LastName) &&
                     record.Email.Contains("@") &&
                     record.CreditLimit > 0
        }).ToList();
    }

    private static List<QualityResult> CalculateQualityScores(List<ValidationResult> data)
    {
        return data.Select(record =>
        {
            var score = 0;
            var issues = new List<string>();

            // Check each quality dimension
            if (!string.IsNullOrWhiteSpace(record.FirstName)) score += 20;
            else issues.Add("Missing first name");

            if (!string.IsNullOrWhiteSpace(record.LastName)) score += 20;
            else issues.Add("Missing last name");

            if (record.Email.Contains("@") && record.Email.Contains(".")) score += 30;
            else issues.Add("Invalid email format");

            if (record.CreditLimit > 0) score += 20;
            else issues.Add("Invalid credit limit");

            if (record.IsActive) score += 10;
            else issues.Add("Inactive account");

            return new QualityResult
            {
                CustomerId = record.CustomerId,
                FirstName = record.FirstName,
                LastName = record.LastName,
                Email = record.Email,
                CreditLimit = record.CreditLimit,
                IsActive = record.IsActive,
                CreatedDate = record.CreatedDate,
                QualityScore = score,
                QualityIssues = issues,
                WasCleansed = false
            };
        }).ToList();
    }

    private static List<QualityResult> CleanseData(List<QualityResult> data)
    {
        return data.Select(record =>
        {
            var cleansed = false;
            var firstName = record.FirstName;
            var lastName = record.LastName;
            var email = record.Email;

            // Apply cleansing rules
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                firstName = firstName.Trim().ToTitleCase();
                cleansed = true;
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                lastName = lastName.Trim().ToTitleCase();
                cleansed = true;
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim().ToLower();
                cleansed = true;
            }

            return new QualityResult
            {
                CustomerId = record.CustomerId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                CreditLimit = record.CreditLimit,
                IsActive = record.IsActive,
                CreatedDate = record.CreatedDate,
                QualityScore = record.QualityScore,
                QualityIssues = record.QualityIssues,
                WasCleansed = cleansed
            };
        }).ToList();
    }
}

// Helper classes for pipeline data
public class DataProfileResult
{
    public int TotalRecords { get; set; }
    public int CompleteRecords { get; set; }
    public int RecordsWithMissingData { get; set; }
    public int DuplicateRecords { get; set; }
    public int InvalidEmails { get; set; }
}

public class ValidationResult
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsValid { get; set; }
}

public class QualityResult
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int QualityScore { get; set; }
    public List<string> QualityIssues { get; set; } = new();
    public bool WasCleansed { get; set; }
}

// Note: ToTitleCase extension method is defined in TransformationPlayground.cs
