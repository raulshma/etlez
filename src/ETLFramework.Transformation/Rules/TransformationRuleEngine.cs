using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Transformation.Helpers;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Transformation.Rules;

/// <summary>
/// Engine for executing transformation rules against data records.
/// </summary>
public class TransformationRuleEngine
{
    private readonly ILogger<TransformationRuleEngine> _logger;
    private readonly List<ITransformationRule> _rules;

    /// <summary>
    /// Initializes a new instance of the TransformationRuleEngine class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public TransformationRuleEngine(ILogger<TransformationRuleEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rules = new List<ITransformationRule>();
    }

    /// <summary>
    /// Gets the collection of rules in this engine.
    /// </summary>
    public IReadOnlyList<ITransformationRule> Rules => _rules.AsReadOnly();

    /// <summary>
    /// Adds a rule to the engine.
    /// </summary>
    /// <param name="rule">The rule to add</param>
    public void AddRule(ITransformationRule rule)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));
        
        _rules.Add(rule);
        _rules.Sort((x, y) => y.Priority.CompareTo(x.Priority)); // Sort by priority descending
        
        _logger.LogDebug("Added rule {RuleId} ({RuleName}) with priority {Priority}", 
            rule.Id, rule.Name, rule.Priority);
    }

    /// <summary>
    /// Removes a rule from the engine.
    /// </summary>
    /// <param name="ruleId">The ID of the rule to remove</param>
    /// <returns>True if the rule was removed</returns>
    public bool RemoveRule(string ruleId)
    {
        var rule = _rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule != null)
        {
            _rules.Remove(rule);
            _logger.LogDebug("Removed rule {RuleId} ({RuleName})", rule.Id, rule.Name);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all rules from the engine.
    /// </summary>
    public void ClearRules()
    {
        var count = _rules.Count;
        _rules.Clear();
        _logger.LogDebug("Cleared {RuleCount} rules from engine", count);
    }

    /// <summary>
    /// Validates all rules in the engine.
    /// </summary>
    /// <param name="context">The transformation context</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateRules(ITransformationContext context)
    {
        var errors = new List<string>();

        foreach (var rule in _rules)
        {
            try
            {
                var validationResult = rule.Validate(context);
                if (!validationResult.IsValid)
                {
                    errors.AddRange(validationResult.Errors.Select(e => $"Rule {rule.Id}: {e.Message}"));
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Rule {rule.Id}: Validation failed - {ex.Message}");
            }
        }

        var result = new ValidationResult
        {
            IsValid = errors.Count == 0
        };

        foreach (var error in errors)
        {
            result.AddError(error);
        }

        return result;
    }

    /// <summary>
    /// Applies all matching rules to a data record.
    /// </summary>
    /// <param name="record">The data record to process</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation result</returns>
    public async Task<Core.Models.TransformationResult> ApplyRulesAsync(
        DataRecord record,
        ITransformationContext context,
        CancellationToken cancellationToken = default)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var startTime = DateTimeOffset.UtcNow;
        var currentRecord = record.Clone();
        var appliedRules = new List<string>();
        var allErrors = new List<ExecutionError>();

        try
        {
            _logger.LogDebug("Applying {RuleCount} rules to record", _rules.Count);

            foreach (var rule in _rules.Where(r => r.IsEnabled))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Evaluate rule conditions
                    var shouldApply = await rule.EvaluateAsync(currentRecord, context, cancellationToken);
                    
                    if (shouldApply)
                    {
                        _logger.LogDebug("Applying rule {RuleId} ({RuleName})", rule.Id, rule.Name);
                        
                        var ruleResult = await rule.ApplyAsync(currentRecord, context, cancellationToken);
                        
                        if (ruleResult.IsSuccessful && ruleResult.OutputRecord != null)
                        {
                            currentRecord = ruleResult.OutputRecord;
                            appliedRules.Add(rule.Id);
                            _logger.LogDebug("Successfully applied rule {RuleId}", rule.Id);
                        }
                        else
                        {
                            allErrors.AddRange(ruleResult.Errors);
                            _logger.LogWarning("Rule {RuleId} failed: {Errors}", 
                                rule.Id, string.Join(", ", ruleResult.Errors.Select(e => e.Message)));
                        }

                        // Check if we should stop processing
                        if (ShouldStopProcessing(rule, ruleResult))
                        {
                            _logger.LogDebug("Stopping rule processing after rule {RuleId}", rule.Id);
                            break;
                        }
                    }
                    else
                    {
                        _logger.LogTrace("Rule {RuleId} conditions not met, skipping", rule.Id);
                    }
                }
                catch (Exception ex)
                {
                    var error = new TransformationError($"Rule {rule.Id} execution failed: {ex.Message}", ex)
                    {
                        TransformationId = rule.Id
                    };
                    allErrors.Add(error);

                    _logger.LogError(ex, "Error applying rule {RuleId} ({RuleName})", rule.Id, rule.Name);
                }
            }

            // Update context metadata
            context.SetMetadata("AppliedRules", appliedRules);
            context.SetMetadata("RuleProcessingTime", DateTimeOffset.UtcNow - startTime);

            var result = new Core.Models.TransformationResult
            {
                IsSuccessful = allErrors.Count == 0,
                OutputRecord = currentRecord,
                Errors = allErrors
            };

            _logger.LogDebug("Rule processing completed. Applied {AppliedCount}/{TotalCount} rules with {ErrorCount} errors",
                appliedRules.Count, _rules.Count(r => r.IsEnabled), allErrors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during rule processing");
            return TransformationResultHelper.Failure($"Rule processing failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Applies rules to multiple records in batch.
    /// </summary>
    /// <param name="records">The records to process</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation results</returns>
    public async Task<IEnumerable<Core.Models.TransformationResult>> ApplyRulesBatchAsync(
        IEnumerable<DataRecord> records,
        ITransformationContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<Core.Models.TransformationResult>();
        
        foreach (var record in records)
        {
            var result = await ApplyRulesAsync(record, context, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Gets statistics about rule execution.
    /// </summary>
    /// <returns>Rule engine statistics</returns>
    public RuleEngineStatistics GetStatistics()
    {
        return new RuleEngineStatistics
        {
            TotalRules = _rules.Count,
            EnabledRules = _rules.Count(r => r.IsEnabled),
            DisabledRules = _rules.Count(r => !r.IsEnabled),
            RulesByPriority = _rules.GroupBy(r => r.Priority)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// Determines if rule processing should stop based on the rule and its result.
    /// </summary>
    /// <param name="rule">The rule that was executed</param>
    /// <param name="result">The result of the rule execution</param>
    /// <returns>True if processing should stop</returns>
    private static bool ShouldStopProcessing(ITransformationRule rule, Core.Models.TransformationResult result)
    {
        // Check if any action in the rule indicates we should stop processing
        return rule.Actions.Any(a => a.ActionType == ActionType.StopProcessing);
    }
}

/// <summary>
/// Statistics about rule engine execution.
/// </summary>
public class RuleEngineStatistics
{
    /// <summary>
    /// Gets or sets the total number of rules.
    /// </summary>
    public int TotalRules { get; set; }

    /// <summary>
    /// Gets or sets the number of enabled rules.
    /// </summary>
    public int EnabledRules { get; set; }

    /// <summary>
    /// Gets or sets the number of disabled rules.
    /// </summary>
    public int DisabledRules { get; set; }

    /// <summary>
    /// Gets or sets the distribution of rules by priority.
    /// </summary>
    public Dictionary<int, int> RulesByPriority { get; set; } = new();
}
