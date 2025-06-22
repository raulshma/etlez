using System.Text.RegularExpressions;
using ETLFramework.Core.Models;
using ETLFramework.Transformation.Helpers;

namespace ETLFramework.Transformation.Rules;

/// <summary>
/// Default implementation of a transformation rule.
/// </summary>
public class TransformationRule : ITransformationRule
{
    private readonly List<IRuleCondition> _conditions;
    private readonly List<IRuleAction> _actions;

    /// <summary>
    /// Initializes a new instance of the TransformationRule class.
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="name">The rule name</param>
    /// <param name="description">The rule description</param>
    /// <param name="priority">The rule priority</param>
    public TransformationRule(string id, string name, string description, int priority = 0)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Priority = priority;
        IsEnabled = true;
        _conditions = new List<IRuleCondition>();
        _actions = new List<IRuleAction>();
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public int Priority { get; }

    /// <inheritdoc />
    public bool IsEnabled { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IRuleCondition> Conditions => _conditions.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<IRuleAction> Actions => _actions.AsReadOnly();

    /// <summary>
    /// Adds a condition to this rule.
    /// </summary>
    /// <param name="condition">The condition to add</param>
    public void AddCondition(IRuleCondition condition)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        _conditions.Add(condition);
    }

    /// <summary>
    /// Adds an action to this rule.
    /// </summary>
    /// <param name="action">The action to add</param>
    public void AddAction(IRuleAction action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _actions.Add(action);
    }

    /// <inheritdoc />
    public async Task<bool> EvaluateAsync(DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        if (_conditions.Count == 0) return true; // No conditions means always apply

        // All conditions must be true (AND logic)
        foreach (var condition in _conditions)
        {
            var result = await condition.EvaluateAsync(record, context, cancellationToken);
            if (!result) return false;
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<Core.Models.TransformationResult> ApplyAsync(DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var currentRecord = record.Clone();
        var allErrors = new List<ExecutionError>();

        foreach (var action in _actions)
        {
            try
            {
                var actionResult = await action.ExecuteAsync(currentRecord, context, cancellationToken);
                
                if (actionResult.IsSuccessful && actionResult.OutputRecord != null)
                {
                    currentRecord = actionResult.OutputRecord;
                }
                else
                {
                    allErrors.AddRange(actionResult.Errors);
                }

                // Check if we should stop processing actions
                if (action.ActionType == ActionType.StopProcessing)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                allErrors.Add(new ExecutionError
                {
                    Message = $"Action {action.Id} failed: {ex.Message}",
                    Exception = ex,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }
        }

        return new Core.Models.TransformationResult
        {
            IsSuccessful = allErrors.Count == 0,
            OutputRecord = currentRecord,
            Errors = allErrors
        };
    }

    /// <inheritdoc />
    public ValidationResult Validate(ETLFramework.Transformation.Interfaces.ITransformationContext context)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("Rule ID cannot be empty");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Rule name cannot be empty");

        if (_actions.Count == 0)
            errors.Add("Rule must have at least one action");

        // Validate conditions
        foreach (var condition in _conditions)
        {
            if (string.IsNullOrWhiteSpace(condition.FieldName))
                errors.Add($"Condition {condition.Id} must specify a field name");
        }

        // Validate actions
        foreach (var action in _actions)
        {
            if (string.IsNullOrWhiteSpace(action.Id))
                errors.Add("Action ID cannot be empty");
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
}

/// <summary>
/// Default implementation of a rule condition.
/// </summary>
public class RuleCondition : IRuleCondition
{
    /// <summary>
    /// Initializes a new instance of the RuleCondition class.
    /// </summary>
    /// <param name="id">The condition ID</param>
    /// <param name="name">The condition name</param>
    /// <param name="fieldName">The field name to evaluate</param>
    /// <param name="operator">The comparison operator</param>
    /// <param name="value">The value to compare against</param>
    public RuleCondition(string id, string name, string fieldName, ConditionOperator @operator, object? value)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        Operator = @operator;
        Value = value;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string FieldName { get; }

    /// <inheritdoc />
    public ConditionOperator Operator { get; }

    /// <inheritdoc />
    public object? Value { get; }

    /// <inheritdoc />
    public Task<bool> EvaluateAsync(DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken = default)
    {
        var fieldValue = record.GetField<object>(FieldName);
        var result = EvaluateCondition(fieldValue, Operator, Value);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Evaluates a condition against field and comparison values.
    /// </summary>
    /// <param name="fieldValue">The field value</param>
    /// <param name="operator">The operator</param>
    /// <param name="compareValue">The value to compare against</param>
    /// <returns>True if the condition is met</returns>
    private static bool EvaluateCondition(object? fieldValue, ConditionOperator @operator, object? compareValue)
    {
        return @operator switch
        {
            ConditionOperator.Equals => Equals(fieldValue, compareValue),
            ConditionOperator.NotEquals => !Equals(fieldValue, compareValue),
            ConditionOperator.GreaterThan => CompareValues(fieldValue, compareValue) > 0,
            ConditionOperator.GreaterThanOrEqual => CompareValues(fieldValue, compareValue) >= 0,
            ConditionOperator.LessThan => CompareValues(fieldValue, compareValue) < 0,
            ConditionOperator.LessThanOrEqual => CompareValues(fieldValue, compareValue) <= 0,
            ConditionOperator.Contains => fieldValue?.ToString()?.Contains(compareValue?.ToString() ?? "") == true,
            ConditionOperator.StartsWith => fieldValue?.ToString()?.StartsWith(compareValue?.ToString() ?? "") == true,
            ConditionOperator.EndsWith => fieldValue?.ToString()?.EndsWith(compareValue?.ToString() ?? "") == true,
            ConditionOperator.Regex => compareValue != null && fieldValue != null && 
                                      Regex.IsMatch(fieldValue.ToString() ?? "", compareValue.ToString() ?? ""),
            ConditionOperator.IsNullOrEmpty => string.IsNullOrEmpty(fieldValue?.ToString()),
            ConditionOperator.IsNotNullOrEmpty => !string.IsNullOrEmpty(fieldValue?.ToString()),
            ConditionOperator.In => IsInList(fieldValue, compareValue),
            ConditionOperator.NotIn => !IsInList(fieldValue, compareValue),
            _ => false
        };
    }

    /// <summary>
    /// Compares two values for ordering.
    /// </summary>
    /// <param name="value1">First value</param>
    /// <param name="value2">Second value</param>
    /// <returns>Comparison result</returns>
    private static int CompareValues(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return 0;
        if (value1 == null) return -1;
        if (value2 == null) return 1;

        // Try to convert both values to decimal for numeric comparison
        if (TryConvertToDecimal(value1, out var decimal1) && TryConvertToDecimal(value2, out var decimal2))
        {
            return decimal1.CompareTo(decimal2);
        }

        // Try to convert both values to DateTime for date comparison
        if (TryConvertToDateTime(value1, out var date1) && TryConvertToDateTime(value2, out var date2))
        {
            return date1.CompareTo(date2);
        }

        // Fall back to string comparison
        return string.Compare(value1.ToString(), value2.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Tries to convert a value to decimal.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="result">The converted decimal value</param>
    /// <returns>True if conversion succeeded</returns>
    private static bool TryConvertToDecimal(object? value, out decimal result)
    {
        result = 0;
        if (value == null) return false;

        if (value is decimal d)
        {
            result = d;
            return true;
        }

        return decimal.TryParse(value.ToString(), out result);
    }

    /// <summary>
    /// Tries to convert a value to DateTime.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="result">The converted DateTime value</param>
    /// <returns>True if conversion succeeded</returns>
    private static bool TryConvertToDateTime(object? value, out DateTime result)
    {
        result = default;
        if (value == null) return false;

        if (value is DateTime dt)
        {
            result = dt;
            return true;
        }

        return DateTime.TryParse(value.ToString(), out result);
    }

    /// <summary>
    /// Checks if a value is in a list of values.
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <param name="list">The list to check against</param>
    /// <returns>True if the value is in the list</returns>
    private static bool IsInList(object? value, object? list)
    {
        if (list is IEnumerable<object> enumerable)
        {
            return enumerable.Any(item => Equals(value, item));
        }

        if (list is string listString)
        {
            var items = listString.Split(',').Select(s => s.Trim());
            return items.Any(item => Equals(value?.ToString(), item));
        }

        return false;
    }
}
