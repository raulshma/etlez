using ETLFramework.Core.Models;
using ETLFramework.Playground.Services;
using ETLFramework.Playground.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text.RegularExpressions;
using CoreValidationResult = ETLFramework.Core.Models.ValidationResult;

namespace ETLFramework.Playground.Playgrounds;

/// <summary>
/// Playground module for testing data validation.
/// </summary>
public class ValidationPlayground : IValidationPlayground
{
    private readonly ILogger<ValidationPlayground> _logger;
    private readonly IPlaygroundUtilities _utilities;
    private readonly ISampleDataService _sampleDataService;

    public ValidationPlayground(
        ILogger<ValidationPlayground> logger,
        IPlaygroundUtilities utilities,
        ISampleDataService sampleDataService)
    {
        _logger = logger;
        _utilities = utilities;
        _sampleDataService = sampleDataService;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _utilities.DisplayHeader("Validation Playground",
            "Test data validation rules and quality checks");

        while (!cancellationToken.IsCancellationRequested)
        {
            var options = new[]
            {
                "✅ Required Field Validation",
                "🔍 Regex Pattern Validation",
                "📊 Range Validation",
                "📧 Email Validation",
                "📞 Phone Number Validation",
                "🔢 Data Type Validation",
                "🧪 Custom Validation Rules",
                "📈 Data Quality Report",
                "🔙 Back to Main Menu"
            };

            var selection = _utilities.PromptForSelection("Select validation type:", options);

            try
            {
                switch (selection)
                {
                    case var s when s.Contains("Required Field"):
                        await TestRequiredFieldValidationAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Regex Pattern"):
                        await TestRegexPatternValidationAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Range"):
                        await TestRangeValidationAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Email"):
                        await TestEmailValidationAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Phone"):
                        await TestPhoneValidationAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Data Type"):
                        await TestDataTypeValidationAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Custom"):
                        await TestCustomValidationRulesAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Quality Report"):
                        await GenerateDataQualityReportAsync(cancellationToken);
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
                _utilities.DisplayError("Error in validation playground", ex);
                _utilities.WaitForKeyPress();
            }
        }
    }

    /// <summary>
    /// Tests required field validation.
    /// </summary>
    private async Task TestRequiredFieldValidationAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Required Field Validation", "Test validation of required fields");

        // Generate sample data with some missing values
        var sampleData = new List<DataRecord>
        {
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["CustomerId"] = 1,
                    ["FirstName"] = "John",
                    ["LastName"] = "Doe",
                    ["Email"] = "john.doe@example.com",
                    ["Phone"] = "555-1234"
                }
            },
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["CustomerId"] = 2,
                    ["FirstName"] = "Jane",
                    ["LastName"] = "", // Empty value
                    ["Email"] = "jane.smith@example.com",
                    ["Phone"] = null // Null value
                }
            },
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["CustomerId"] = 3,
                    ["FirstName"] = null, // Missing required field
                    ["LastName"] = "Johnson",
                    ["Email"] = "", // Empty email
                    ["Phone"] = "555-5678"
                }
            }
        };

        var requiredFields = new[] { "CustomerId", "FirstName", "LastName", "Email" };

        AnsiConsole.MarkupLine($"[blue]Testing {sampleData.Count} records against required fields:[/]");
        AnsiConsole.MarkupLine($"[dim]Required fields: {string.Join(", ", requiredFields)}[/]");

        var validationResults = new List<FieldValidationResult>();

        foreach (var record in sampleData)
        {
            var result = ValidateRequiredFields(record, requiredFields);
            validationResults.Add(result);
        }

        // Display results
        var resultsTable = new Table().BorderColor(Color.Yellow);
        resultsTable.AddColumn("Record");
        resultsTable.AddColumn("Status");
        resultsTable.AddColumn("Missing Fields");
        resultsTable.AddColumn("Empty Fields");

        for (int i = 0; i < sampleData.Count; i++)
        {
            var result = validationResults[i];
            var record = sampleData[i];

            var status = result.IsValid ? "[green]✅ Valid[/]" : "[red]❌ Invalid[/]";
            var missingFields = result.MissingFields.Any() ? string.Join(", ", result.MissingFields) : "-";
            var emptyFields = result.EmptyFields.Any() ? string.Join(", ", result.EmptyFields) : "-";

            resultsTable.AddRow(
                $"Record {i + 1}",
                status,
                missingFields,
                emptyFields
            );
        }

        AnsiConsole.Write(resultsTable);

        // Summary
        var validCount = validationResults.Count(r => r.IsValid);
        var invalidCount = validationResults.Count - validCount;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]✅ Valid records: {validCount}[/]");
        AnsiConsole.MarkupLine($"[red]❌ Invalid records: {invalidCount}[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests regex pattern validation.
    /// </summary>
    private async Task TestRegexPatternValidationAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Regex Pattern Validation", "Test validation using regular expressions");

        // Sample data with various formats
        var sampleData = new List<DataRecord>
        {
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["Email"] = "valid@example.com",
                    ["Phone"] = "555-123-4567",
                    ["ZipCode"] = "12345",
                    ["SSN"] = "123-45-6789"
                }
            },
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["Email"] = "invalid-email",
                    ["Phone"] = "555.123.4567",
                    ["ZipCode"] = "1234",
                    ["SSN"] = "123456789"
                }
            },
            new DataRecord
            {
                Fields = new Dictionary<string, object?>
                {
                    ["Email"] = "another@test.co.uk",
                    ["Phone"] = "(555) 123-4567",
                    ["ZipCode"] = "12345-6789",
                    ["SSN"] = "123-45-6789"
                }
            }
        };

        var patterns = new Dictionary<string, string>
        {
            ["Email"] = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            ["Phone"] = @"^(\(?\d{3}\)?[-.\s]?)?\d{3}[-.\s]?\d{4}$",
            ["ZipCode"] = @"^\d{5}(-\d{4})?$",
            ["SSN"] = @"^\d{3}-\d{2}-\d{4}$"
        };

        AnsiConsole.MarkupLine($"[blue]Testing {sampleData.Count} records against regex patterns:[/]");

        var resultsTable = new Table().BorderColor(Color.Blue);
        resultsTable.AddColumn("Record");
        resultsTable.AddColumn("Field");
        resultsTable.AddColumn("Value");
        resultsTable.AddColumn("Pattern");
        resultsTable.AddColumn("Valid");

        foreach (var (record, recordIndex) in sampleData.Select((r, i) => (r, i)))
        {
            foreach (var (field, pattern) in patterns)
            {
                var value = record.Fields.TryGetValue(field, out var fieldValue) ? fieldValue?.ToString() ?? "" : "";
                var isValid = !string.IsNullOrEmpty(value) && Regex.IsMatch(value, pattern);

                resultsTable.AddRow(
                    $"Record {recordIndex + 1}",
                    field,
                    value,
                    $"[dim]{pattern[..Math.Min(pattern.Length, 30)]}...[/]",
                    isValid ? "[green]✅[/]" : "[red]❌[/]"
                );
            }
        }

        AnsiConsole.Write(resultsTable);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates required fields in a data record.
    /// </summary>
    private static FieldValidationResult ValidateRequiredFields(DataRecord record, string[] requiredFields)
    {
        var missingFields = new List<string>();
        var emptyFields = new List<string>();

        foreach (var field in requiredFields)
        {
            if (!record.Fields.ContainsKey(field))
            {
                missingFields.Add(field);
            }
            else
            {
                var value = record.Fields[field];
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    emptyFields.Add(field);
                }
            }
        }

        return new FieldValidationResult
        {
            IsValid = missingFields.Count == 0 && emptyFields.Count == 0,
            MissingFields = missingFields,
            EmptyFields = emptyFields
        };
    }

    // Placeholder methods for other validation types
    private async Task TestRangeValidationAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Range Validation", "Test numeric and date range validation");
        AnsiConsole.MarkupLine("[yellow]Range validation features:[/]");
        AnsiConsole.MarkupLine("[dim]• Numeric range validation (min/max values)[/]");
        AnsiConsole.MarkupLine("[dim]• Date range validation[/]");
        AnsiConsole.MarkupLine("[dim]• String length validation[/]");
        await Task.CompletedTask;
    }

    private async Task TestEmailValidationAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Email Validation", "Test email address validation");
        AnsiConsole.MarkupLine("[yellow]Email validation features:[/]");
        AnsiConsole.MarkupLine("[dim]• RFC-compliant email validation[/]");
        AnsiConsole.MarkupLine("[dim]• Domain validation[/]");
        AnsiConsole.MarkupLine("[dim]• Disposable email detection[/]");
        await Task.CompletedTask;
    }

    private async Task TestPhoneValidationAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Phone Validation", "Test phone number validation");
        AnsiConsole.MarkupLine("[yellow]Phone validation features:[/]");
        AnsiConsole.MarkupLine("[dim]• International phone number formats[/]");
        AnsiConsole.MarkupLine("[dim]• US phone number validation[/]");
        AnsiConsole.MarkupLine("[dim]• Phone number formatting[/]");
        await Task.CompletedTask;
    }

    private async Task TestDataTypeValidationAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Data Type Validation", "Test data type validation and conversion");
        AnsiConsole.MarkupLine("[yellow]Data type validation features:[/]");
        AnsiConsole.MarkupLine("[dim]• Type checking (int, decimal, date, etc.)[/]");
        AnsiConsole.MarkupLine("[dim]• Safe type conversion[/]");
        AnsiConsole.MarkupLine("[dim]• Format validation[/]");
        await Task.CompletedTask;
    }

    private async Task TestCustomValidationRulesAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Custom Validation Rules", "Test custom business rule validation");
        AnsiConsole.MarkupLine("[yellow]Custom validation features:[/]");
        AnsiConsole.MarkupLine("[dim]• Business rule validation[/]");
        AnsiConsole.MarkupLine("[dim]• Cross-field validation[/]");
        AnsiConsole.MarkupLine("[dim]• Conditional validation rules[/]");
        await Task.CompletedTask;
    }

    private async Task GenerateDataQualityReportAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Data Quality Report", "Generate comprehensive data quality analysis");
        AnsiConsole.MarkupLine("[yellow]Data quality report features:[/]");
        AnsiConsole.MarkupLine("[dim]• Data completeness analysis[/]");
        AnsiConsole.MarkupLine("[dim]• Data accuracy metrics[/]");
        AnsiConsole.MarkupLine("[dim]• Data consistency checks[/]");
        AnsiConsole.MarkupLine("[dim]• Quality score calculation[/]");
        await Task.CompletedTask;
    }
}

/// <summary>
/// Simple validation result for field validation.
/// </summary>
public class FieldValidationResult
{
    public bool IsValid { get; set; }
    public List<string> MissingFields { get; set; } = new();
    public List<string> EmptyFields { get; set; } = new();
}
