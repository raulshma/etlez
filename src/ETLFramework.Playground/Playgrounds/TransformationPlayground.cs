using ETLFramework.Transformation.Interfaces;
using ETLFramework.Transformation.Transformations.FieldTransformations;
using ETLFramework.Transformation.Models;
using ETLFramework.Core.Models;
using ETLFramework.Playground.Services;
using ETLFramework.Playground.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ETLFramework.Playground.Playgrounds;

/// <summary>
/// Playground module for testing data transformations.
/// </summary>
public class TransformationPlayground : ITransformationPlayground
{
    private readonly ILogger<TransformationPlayground> _logger;
    private readonly IPlaygroundUtilities _utilities;
    private readonly ISampleDataService _sampleDataService;
    private readonly ITransformationProcessor _transformationProcessor;

    public TransformationPlayground(
        ILogger<TransformationPlayground> logger,
        IPlaygroundUtilities utilities,
        ISampleDataService sampleDataService,
        ITransformationProcessor transformationProcessor)
    {
        _logger = logger;
        _utilities = utilities;
        _sampleDataService = sampleDataService;
        _transformationProcessor = transformationProcessor;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _utilities.DisplayHeader("Transformation Playground", 
            "Test data transformations and field mappings");

        while (!cancellationToken.IsCancellationRequested)
        {
            var options = new[]
            {
                "🔤 String Transformations",
                "🔢 Numeric Transformations", 
                "📅 Date/Time Transformations",
                "🗺️ Field Mapping",
                "🔄 Complex Transformations",
                "🧪 Custom Transformation Builder",
                "🔙 Back to Main Menu"
            };

            var selection = _utilities.PromptForSelection("Select transformation category:", options);

            try
            {
                switch (selection)
                {
                    case var s when s.Contains("String"):
                        await TestStringTransformationsAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Numeric"):
                        await TestNumericTransformationsAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Date/Time"):
                        await TestDateTimeTransformationsAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Field Mapping"):
                        await TestFieldMappingAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Complex"):
                        await TestComplexTransformationsAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Custom"):
                        await TestCustomTransformationBuilderAsync(cancellationToken);
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
                _utilities.DisplayError("Error in transformation playground", ex);
                _utilities.WaitForKeyPress();
            }
        }
    }

    private async Task TestStringTransformationsAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("String Transformations", "Test various string transformation operations");

        var transformationTypes = new[]
        {
            "Uppercase Transformation",
            "Lowercase Transformation",
            "Trim Transformation",
            "Regex Replace Transformation",
            "Concatenate Fields",
            "Substring Transformation",
            "Test All String Transformations"
        };

        var selectedType = _utilities.PromptForSelection("Select string transformation to test:", transformationTypes);

        // Generate sample data with string fields
        var sampleData = _sampleDataService.GenerateCustomerData(5).Select(c => new DataRecord
        {
            Fields = new Dictionary<string, object?>
            {
                ["FirstName"] = c.FirstName,
                ["LastName"] = c.LastName,
                ["Email"] = c.Email,
                ["Address"] = c.Address,
                ["City"] = c.City,
                ["FullText"] = $"  {c.FirstName} {c.LastName}  " // With extra spaces for trim testing
            }
        }).ToList();

        _utilities.DisplayResults(sampleData.Select(r => new
        {
            FirstName = r.Fields["FirstName"],
            LastName = r.Fields["LastName"],
            Email = r.Fields["Email"],
            FullText = r.Fields["FullText"]
        }), "Original Sample Data");

        try
        {
            await _utilities.WithProgressAsync(async progress =>
            {
                progress.Report("Creating transformation...");

                ITransformation transformation = selectedType switch
                {
                    "Uppercase Transformation" => new UppercaseTransformation("FirstName"),
                    "Lowercase Transformation" => new LowercaseTransformation("Email"),
                    "Trim Transformation" => new TrimTransformation("FullText"),
                    "Regex Replace Transformation" => new RegexReplaceTransformation("Email", @"@.*", "@example.com"),
                    "Concatenate Fields" => new ConcatenateTransformation(new[] { "FirstName", "LastName" }, "FullName", " "),
                    "Substring Transformation" => new RegexReplaceTransformation("FirstName", @"^(.{3}).*", "$1"), // Take first 3 chars
                    _ => new UppercaseTransformation("FirstName")
                };

                progress.Report("Applying transformation...");

                var context = new TransformationContext("String Transformation Test", cancellationToken);
                var transformedData = new List<DataRecord>();

                foreach (var record in sampleData)
                {
                    var result = await transformation.TransformAsync(record, context, cancellationToken);
                    if (result.IsSuccessful && result.OutputRecord != null)
                    {
                        transformedData.Add(result.OutputRecord);
                    }
                }

                progress.Report("Displaying results...");

                // Display transformed data
                AnsiConsole.WriteLine();
                _utilities.DisplaySuccess($"Applied {transformation.Name} transformation:");

                if (selectedType == "Test All String Transformations")
                {
                    await TestAllStringTransformationsAsync(sampleData, context, cancellationToken);
                }
                else
                {
                    _utilities.DisplayResults(transformedData.Select(r => r.Fields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)),
                        $"Transformed Data - {transformation.Name}");
                }

                // Show transformation statistics
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[green]✅ Transformation completed successfully[/]");
                AnsiConsole.MarkupLine($"[blue]📊 Records processed: {transformedData.Count}[/]");
                AnsiConsole.MarkupLine($"[blue]📊 Transformation: {transformation.Description}[/]");

            }, "Testing String Transformations");

        }
        catch (Exception ex)
        {
            _utilities.DisplayError("Failed to test string transformations", ex);
        }
    }

    private async Task TestNumericTransformationsAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Numeric Transformations", "Test numeric transformation operations");

        var transformationTypes = new[]
        {
            "Round Transformation",
            "Add Transformation",
            "Multiply Transformation",
            "Format Number Transformation",
            "Calculate Transformation",
            "Test All Numeric Transformations"
        };

        var selectedType = _utilities.PromptForSelection("Select numeric transformation to test:", transformationTypes);

        // Generate sample data with numeric fields
        var sampleData = _sampleDataService.GenerateCustomerData(5).Select(c => new DataRecord
        {
            Fields = new Dictionary<string, object?>
            {
                ["CustomerId"] = c.CustomerId,
                ["CreditLimit"] = c.CreditLimit,
                ["Score"] = 85.7654m, // Sample score with decimals
                ["Age"] = 35,
                ["Salary"] = 75000.50m,
                ["Bonus"] = 5000.25m
            }
        }).ToList();

        _utilities.DisplayResults(sampleData.Select(r => new
        {
            CustomerId = r.Fields["CustomerId"],
            CreditLimit = r.Fields["CreditLimit"],
            Score = r.Fields["Score"],
            Age = r.Fields["Age"],
            Salary = r.Fields["Salary"],
            Bonus = r.Fields["Bonus"]
        }), "Original Sample Data");

        try
        {
            await _utilities.WithProgressAsync(async progress =>
            {
                progress.Report("Creating transformation...");

                ITransformation transformation = selectedType switch
                {
                    "Round Transformation" => new RoundTransformation("Score", 2),
                    "Add Transformation" => new AddTransformation("Age", 1, "AgeNextYear"),
                    "Multiply Transformation" => new MultiplyTransformation("Salary", 1.05m, "SalaryWithRaise"),
                    "Format Number Transformation" => new FormatNumberTransformation("CreditLimit", "C2"),
                    "Calculate Transformation" => new CalculateTransformation("Salary", "Bonus", MathOperation.Add, "TotalCompensation"),
                    _ => new RoundTransformation("Score", 2)
                };

                progress.Report("Applying transformation...");

                var context = new TransformationContext("Numeric Transformation Test", cancellationToken);
                var transformedData = new List<DataRecord>();

                foreach (var record in sampleData)
                {
                    var result = await transformation.TransformAsync(record, context, cancellationToken);
                    if (result.IsSuccessful && result.OutputRecord != null)
                    {
                        transformedData.Add(result.OutputRecord);
                    }
                }

                progress.Report("Displaying results...");

                // Display transformed data
                AnsiConsole.WriteLine();
                _utilities.DisplaySuccess($"Applied {transformation.Name} transformation:");

                if (selectedType == "Test All Numeric Transformations")
                {
                    await TestAllNumericTransformationsAsync(sampleData, context, cancellationToken);
                }
                else
                {
                    _utilities.DisplayResults(transformedData.Select(r => r.Fields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)),
                        $"Transformed Data - {transformation.Name}");
                }

                // Show transformation statistics
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[green]✅ Transformation completed successfully[/]");
                AnsiConsole.MarkupLine($"[blue]📊 Records processed: {transformedData.Count}[/]");
                AnsiConsole.MarkupLine($"[blue]📊 Transformation: {transformation.Description}[/]");

            }, "Testing Numeric Transformations");

        }
        catch (Exception ex)
        {
            _utilities.DisplayError("Failed to test numeric transformations", ex);
        }
    }

    private async Task TestDateTimeTransformationsAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Date/Time Transformations", "Test date and time transformation operations");

        // Generate sample data with date fields
        var sampleData = _sampleDataService.GenerateCustomerData(3).Select(c => new DataRecord
        {
            Fields = new Dictionary<string, object?>
            {
                ["CustomerId"] = c.CustomerId,
                ["Name"] = $"{c.FirstName} {c.LastName}",
                ["BirthDate"] = c.DateOfBirth.ToString("yyyy-MM-dd"),
                ["CreatedDate"] = c.CreatedDate,
                ["LastLogin"] = DateTime.Now.AddDays(-5)
            }
        }).ToList();

        _utilities.DisplayResults(sampleData.Select(r => new
        {
            CustomerId = r.Fields["CustomerId"],
            Name = r.Fields["Name"],
            BirthDate = r.Fields["BirthDate"],
            CreatedDate = r.Fields["CreatedDate"],
            LastLogin = r.Fields["LastLogin"]
        }), "Original Sample Data");

        AnsiConsole.MarkupLine("\n[yellow]Date/Time transformations would include:[/]");
        AnsiConsole.MarkupLine("[dim]• Parse date strings to DateTime objects[/]");
        AnsiConsole.MarkupLine("[dim]• Format dates to specific string formats[/]");
        AnsiConsole.MarkupLine("[dim]• Add/subtract days, months, years[/]");
        AnsiConsole.MarkupLine("[dim]• Convert between time zones[/]");
        AnsiConsole.MarkupLine("[dim]• Extract date parts (year, month, day)[/]");

        await Task.CompletedTask;
    }

    private async Task TestFieldMappingAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Field Mapping", "Test field mapping and data restructuring");
        
        AnsiConsole.MarkupLine("[yellow]Field mapping testing will be implemented here.[/]");
        AnsiConsole.MarkupLine("[dim]This will include: Rename fields, Combine fields, Split fields, etc.[/]");
        
        await Task.CompletedTask;
    }

    private async Task TestComplexTransformationsAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Complex Transformations", "Test complex multi-step transformations");
        
        AnsiConsole.MarkupLine("[yellow]Complex transformation testing will be implemented here.[/]");
        AnsiConsole.MarkupLine("[dim]This will include: Chained transformations, Conditional logic, etc.[/]");
        
        await Task.CompletedTask;
    }

    private async Task TestCustomTransformationBuilderAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Custom Transformation Builder", "Build and test custom transformations");
        
        AnsiConsole.MarkupLine("[yellow]Custom transformation builder will be implemented here.[/]");
        AnsiConsole.MarkupLine("[dim]This will allow users to create custom transformation logic.[/]");
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests all string transformations with sample data.
    /// </summary>
    private async Task TestAllStringTransformationsAsync(List<DataRecord> sampleData, TransformationContext context, CancellationToken cancellationToken)
    {
        var transformations = new List<ITransformation>
        {
            new UppercaseTransformation("FirstName"),
            new LowercaseTransformation("Email"),
            new TrimTransformation("FullText"),
            new ConcatenateTransformation(new[] { "FirstName", "LastName" }, "FullName", " ")
        };

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Testing all string transformations:[/]");

        foreach (var transformation in transformations)
        {
            AnsiConsole.MarkupLine($"\n[blue]🔧 {transformation.Name}:[/] {transformation.Description}");

            var transformedData = new List<DataRecord>();
            foreach (var record in sampleData.Take(2)) // Show only first 2 records for brevity
            {
                var result = await transformation.TransformAsync(record, context, cancellationToken);
                if (result.IsSuccessful && result.OutputRecord != null)
                {
                    transformedData.Add(result.OutputRecord);
                }
            }

            // Display before/after for first record
            if (transformedData.Any())
            {
                var original = sampleData.First();
                var transformed = transformedData.First();

                var table = new Table().BorderColor(Color.Green);
                table.AddColumn("Field");
                table.AddColumn("Before");
                table.AddColumn("After");

                foreach (var field in original.Fields.Keys)
                {
                    var beforeValue = original.Fields[field]?.ToString() ?? "null";
                    var afterValue = transformed.Fields.TryGetValue(field, out var value) ? value?.ToString() ?? "null" : beforeValue;

                    if (beforeValue != afterValue)
                    {
                        table.AddRow(field, $"[dim]{beforeValue}[/]", $"[green]{afterValue}[/]");
                    }
                }

                // Show new fields
                foreach (var field in transformed.Fields.Keys.Except(original.Fields.Keys))
                {
                    var newValue = transformed.Fields[field]?.ToString() ?? "null";
                    table.AddRow($"[yellow]{field}[/]", "[dim]<new>[/]", $"[green]{newValue}[/]");
                }

                if (table.Rows.Count > 0)
                {
                    AnsiConsole.Write(table);
                }
                else
                {
                    AnsiConsole.MarkupLine("[dim]No changes detected[/]");
                }
            }
        }
    }

    /// <summary>
    /// Tests all numeric transformations with sample data.
    /// </summary>
    private async Task TestAllNumericTransformationsAsync(List<DataRecord> sampleData, TransformationContext context, CancellationToken cancellationToken)
    {
        var transformations = new List<ITransformation>
        {
            new RoundTransformation("Score", 1),
            new AddTransformation("Age", 1, "AgeNextYear"),
            new MultiplyTransformation("Salary", 1.05m, "SalaryWithRaise"),
            new CalculateTransformation("Salary", "Bonus", MathOperation.Add, "TotalCompensation")
        };

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Testing all numeric transformations:[/]");

        foreach (var transformation in transformations)
        {
            AnsiConsole.MarkupLine($"\n[blue]🔧 {transformation.Name}:[/] {transformation.Description}");

            var transformedData = new List<DataRecord>();
            foreach (var record in sampleData.Take(2)) // Show only first 2 records for brevity
            {
                var result = await transformation.TransformAsync(record, context, cancellationToken);
                if (result.IsSuccessful && result.OutputRecord != null)
                {
                    transformedData.Add(result.OutputRecord);
                }
            }

            // Display before/after for first record
            if (transformedData.Any())
            {
                var original = sampleData.First();
                var transformed = transformedData.First();

                var table = new Table().BorderColor(Color.Blue);
                table.AddColumn("Field");
                table.AddColumn("Before");
                table.AddColumn("After");

                foreach (var field in original.Fields.Keys)
                {
                    var beforeValue = original.Fields[field]?.ToString() ?? "null";
                    var afterValue = transformed.Fields.TryGetValue(field, out var value) ? value?.ToString() ?? "null" : beforeValue;

                    if (beforeValue != afterValue)
                    {
                        table.AddRow(field, $"[dim]{beforeValue}[/]", $"[green]{afterValue}[/]");
                    }
                }

                // Show new fields
                foreach (var field in transformed.Fields.Keys.Except(original.Fields.Keys))
                {
                    var newValue = transformed.Fields[field]?.ToString() ?? "null";
                    table.AddRow($"[yellow]{field}[/]", "[dim]<new>[/]", $"[green]{newValue}[/]");
                }

                if (table.Rows.Count > 0)
                {
                    AnsiConsole.Write(table);
                }
                else
                {
                    AnsiConsole.MarkupLine("[dim]No changes detected[/]");
                }
            }
        }
    }
}
