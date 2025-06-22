using ETLFramework.Core.Models;
using ETLFramework.Playground.Services;
using ETLFramework.Playground.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ETLFramework.Playground.Playgrounds;

/// <summary>
/// Playground module for testing rule-based processing.
/// </summary>
public class RuleEnginePlayground : IRuleEnginePlayground
{
    private readonly ILogger<RuleEnginePlayground> _logger;
    private readonly IPlaygroundUtilities _utilities;
    private readonly ISampleDataService _sampleDataService;

    public RuleEnginePlayground(
        ILogger<RuleEnginePlayground> logger,
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
        _utilities.DisplayHeader("Rule Engine Playground",
            "Test rule-based processing and conditional logic");

        while (!cancellationToken.IsCancellationRequested)
        {
            var options = new[]
            {
                "🔀 Simple Conditional Rules",
                "📊 Priority-Based Rules",
                "🏢 Business Logic Rules",
                "🔄 Chained Rule Execution",
                "🎯 Rule Matching Engine",
                "📋 Rule Configuration Builder",
                " Back to Main Menu"
            };

            var selection = _utilities.PromptForSelection("Select rule engine scenario:", options);

            try
            {
                switch (selection)
                {
                    case var s when s.Contains("Simple Conditional"):
                        await TestSimpleConditionalRulesAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Priority-Based"):
                        await TestPriorityBasedRulesAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Business Logic"):
                        await TestBusinessLogicRulesAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Chained"):
                        await TestChainedRuleExecutionAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Matching"):
                        await TestRuleMatchingEngineAsync(cancellationToken);
                        break;
                    case var s when s.Contains("Configuration"):
                        await TestRuleConfigurationBuilderAsync(cancellationToken);
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
                _utilities.DisplayError("Error in rule engine playground", ex);
                _utilities.WaitForKeyPress();
            }
        }
    }

    /// <summary>
    /// Tests simple conditional rules.
    /// </summary>
    private async Task TestSimpleConditionalRulesAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Simple Conditional Rules", "Test basic if-then rule logic");

        // Generate sample customer data
        var customers = _sampleDataService.GenerateCustomerData(8).ToList();

        AnsiConsole.MarkupLine("[blue]Testing conditional rules on customer data:[/]");

        // Define simple rules
        var rules = new List<BusinessRule>
        {
            new BusinessRule("HighValueCustomer", "Credit limit > 10000",
                customer => customer.CreditLimit > 10000m, "VIP"),
            new BusinessRule("ActiveCustomer", "Customer is active",
                customer => customer.IsActive, "Active"),
            new BusinessRule("NewCustomer", "Created within last 30 days",
                customer => customer.CreatedDate > DateTime.Now.AddDays(-30), "New"),
            new BusinessRule("SeniorCustomer", "Age > 65",
                customer => customer.DateOfBirth < DateTime.Now.AddYears(-65), "Senior")
        };

        var resultsTable = new Table().BorderColor(Color.Green);
        resultsTable.AddColumn("Customer");
        resultsTable.AddColumn("Credit Limit");
        resultsTable.AddColumn("Active");
        resultsTable.AddColumn("Age");
        resultsTable.AddColumn("Applied Rules");

        foreach (var customer in customers)
        {
            var appliedRules = new List<string>();

            foreach (var rule in rules)
            {
                if (rule.Condition(customer))
                {
                    appliedRules.Add(rule.Action);
                }
            }

            var age = DateTime.Now.Year - customer.DateOfBirth.Year;
            resultsTable.AddRow(
                $"{customer.FirstName} {customer.LastName}",
                customer.CreditLimit.ToString("C"),
                customer.IsActive ? "✅" : "❌",
                age.ToString(),
                appliedRules.Any() ? string.Join(", ", appliedRules) : "None"
            );
        }

        AnsiConsole.Write(resultsTable);

        // Summary
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]✅ Processed {customers.Count} customers with {rules.Count} rules[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests priority-based rules.
    /// </summary>
    private async Task TestPriorityBasedRulesAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Priority-Based Rules", "Test rules with priority ordering");

        AnsiConsole.MarkupLine("[yellow]Priority-based rule features:[/]");
        AnsiConsole.MarkupLine("[dim]• Rules executed in priority order[/]");
        AnsiConsole.MarkupLine("[dim]• Higher priority rules override lower ones[/]");
        AnsiConsole.MarkupLine("[dim]• Early termination on match[/]");
        AnsiConsole.MarkupLine("[dim]• Rule conflict resolution[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests business logic rules.
    /// </summary>
    private async Task TestBusinessLogicRulesAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Business Logic Rules", "Test complex business rule scenarios");

        AnsiConsole.MarkupLine("[yellow]Business logic rule features:[/]");
        AnsiConsole.MarkupLine("[dim]• Multi-condition rules[/]");
        AnsiConsole.MarkupLine("[dim]• Cross-entity validation[/]");
        AnsiConsole.MarkupLine("[dim]• Business process automation[/]");
        AnsiConsole.MarkupLine("[dim]• Compliance rule enforcement[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests chained rule execution.
    /// </summary>
    private async Task TestChainedRuleExecutionAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Chained Rule Execution", "Test sequential rule processing");

        AnsiConsole.MarkupLine("[yellow]Chained rule execution features:[/]");
        AnsiConsole.MarkupLine("[dim]• Sequential rule processing[/]");
        AnsiConsole.MarkupLine("[dim]• Rule output as input to next rule[/]");
        AnsiConsole.MarkupLine("[dim]• Conditional branching[/]");
        AnsiConsole.MarkupLine("[dim]• Loop detection and prevention[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests rule matching engine.
    /// </summary>
    private async Task TestRuleMatchingEngineAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Rule Matching Engine", "Test pattern matching and rule selection");

        AnsiConsole.MarkupLine("[yellow]Rule matching engine features:[/]");
        AnsiConsole.MarkupLine("[dim]• Pattern-based rule matching[/]");
        AnsiConsole.MarkupLine("[dim]• Dynamic rule selection[/]");
        AnsiConsole.MarkupLine("[dim]• Rule performance optimization[/]");
        AnsiConsole.MarkupLine("[dim]• Match confidence scoring[/]");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests rule configuration builder.
    /// </summary>
    private async Task TestRuleConfigurationBuilderAsync(CancellationToken cancellationToken)
    {
        _utilities.DisplayHeader("Rule Configuration Builder", "Interactive rule creation and management");

        AnsiConsole.MarkupLine("[yellow]Rule configuration builder features:[/]");
        AnsiConsole.MarkupLine("[dim]• Visual rule builder interface[/]");
        AnsiConsole.MarkupLine("[dim]• Rule template library[/]");
        AnsiConsole.MarkupLine("[dim]• Rule validation and testing[/]");
        AnsiConsole.MarkupLine("[dim]• Export/import rule configurations[/]");

        await Task.CompletedTask;
    }
}

/// <summary>
/// Simple business rule for demonstration.
/// </summary>
public class BusinessRule
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Func<CustomerData, bool> Condition { get; set; }
    public string Action { get; set; }

    public BusinessRule(string name, string description, Func<CustomerData, bool> condition, string action)
    {
        Name = name;
        Description = description;
        Condition = condition;
        Action = action;
    }
}
