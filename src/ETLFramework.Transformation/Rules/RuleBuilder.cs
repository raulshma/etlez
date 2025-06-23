using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Transformation.Interfaces;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Transformation.Rules;

/// <summary>
/// Builder for creating transformation rules with a fluent API.
/// </summary>
public class RuleBuilder
{
    private readonly TransformationRule _rule;
    private int _conditionCounter = 0;
    private int _actionCounter = 0;

    /// <summary>
    /// Initializes a new instance of the RuleBuilder class.
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="name">The rule name</param>
    /// <param name="description">The rule description</param>
    /// <param name="priority">The rule priority</param>
    public RuleBuilder(string id, string name, string description, int priority = 0)
    {
        _rule = new TransformationRule(id, name, description, priority);
    }

    /// <summary>
    /// Creates a new rule builder.
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="name">The rule name</param>
    /// <param name="description">The rule description</param>
    /// <param name="priority">The rule priority</param>
    /// <returns>A new rule builder</returns>
    public static RuleBuilder Create(string id, string name, string description, int priority = 0)
    {
        return new RuleBuilder(id, name, description, priority);
    }

    /// <summary>
    /// Sets whether the rule is enabled.
    /// </summary>
    /// <param name="enabled">True to enable the rule</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder Enabled(bool enabled = true)
    {
        _rule.IsEnabled = enabled;
        return this;
    }

    /// <summary>
    /// Adds a condition that a field equals a value.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <param name="value">The value to compare</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder WhenFieldEquals(string fieldName, object? value)
    {
        return AddCondition(fieldName, ConditionOperator.Equals, value);
    }

    /// <summary>
    /// Adds a condition that a field does not equal a value.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <param name="value">The value to compare</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder WhenFieldNotEquals(string fieldName, object? value)
    {
        return AddCondition(fieldName, ConditionOperator.NotEquals, value);
    }

    /// <summary>
    /// Adds a condition that a field contains a value.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <param name="value">The value to search for</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder WhenFieldContains(string fieldName, string value)
    {
        return AddCondition(fieldName, ConditionOperator.Contains, value);
    }

    /// <summary>
    /// Adds a condition that a field is null or empty.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder WhenFieldIsEmpty(string fieldName)
    {
        return AddCondition(fieldName, ConditionOperator.IsNullOrEmpty, null);
    }

    /// <summary>
    /// Adds a condition that a field is not null or empty.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder WhenFieldIsNotEmpty(string fieldName)
    {
        return AddCondition(fieldName, ConditionOperator.IsNotNullOrEmpty, null);
    }

    /// <summary>
    /// Adds a condition that a field matches a regular expression.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <param name="pattern">The regex pattern</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder WhenFieldMatches(string fieldName, string pattern)
    {
        return AddCondition(fieldName, ConditionOperator.Regex, pattern);
    }

    /// <summary>
    /// Adds a condition that a field is greater than a value.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <param name="value">The value to compare</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder WhenFieldGreaterThan(string fieldName, object value)
    {
        return AddCondition(fieldName, ConditionOperator.GreaterThan, value);
    }

    /// <summary>
    /// Adds a condition that a field is in a list of values.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <param name="values">The list of values</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder WhenFieldIn(string fieldName, params object[] values)
    {
        return AddCondition(fieldName, ConditionOperator.In, values);
    }

    /// <summary>
    /// Adds a custom condition.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <param name="operator">The operator</param>
    /// <param name="value">The value</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder When(string fieldName, ConditionOperator @operator, object? value)
    {
        return AddCondition(fieldName, @operator, value);
    }

    /// <summary>
    /// Adds an action to set a field value.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <param name="value">The value to set</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder ThenSetField(string fieldName, object? value)
    {
        var action = new SetFieldAction($"action_{++_actionCounter}", fieldName, value);
        _rule.AddAction(action);
        return this;
    }

    /// <summary>
    /// Adds an action to remove a field.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder ThenRemoveField(string fieldName)
    {
        var action = new RemoveFieldAction($"action_{++_actionCounter}", fieldName);
        _rule.AddAction(action);
        return this;
    }

    /// <summary>
    /// Adds an action to copy a field.
    /// </summary>
    /// <param name="sourceField">The source field name</param>
    /// <param name="targetField">The target field name</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder ThenCopyField(string sourceField, string targetField)
    {
        var action = new CopyFieldAction($"action_{++_actionCounter}", sourceField, targetField);
        _rule.AddAction(action);
        return this;
    }

    /// <summary>
    /// Adds an action to apply a transformation.
    /// </summary>
    /// <param name="transformation">The transformation to apply</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder ThenTransform(ITransformation transformation)
    {
        var action = new TransformFieldAction($"action_{++_actionCounter}", transformation);
        _rule.AddAction(action);
        return this;
    }

    /// <summary>
    /// Adds an action to skip the record.
    /// </summary>
    /// <param name="reason">The reason for skipping</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder ThenSkipRecord(string reason = "Record skipped by rule")
    {
        var action = new SkipRecordAction($"action_{++_actionCounter}", reason);
        _rule.AddAction(action);
        return this;
    }

    /// <summary>
    /// Adds an action to stop processing further rules.
    /// </summary>
    /// <returns>This builder instance</returns>
    public RuleBuilder ThenStopProcessing()
    {
        var action = new StopProcessingAction($"action_{++_actionCounter}");
        _rule.AddAction(action);
        return this;
    }

    /// <summary>
    /// Adds an action to log a message.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="logLevel">The log level</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder ThenLogMessage(string message, LogLevel logLevel = LogLevel.Information, ILogger? logger = null)
    {
        var action = new LogMessageAction($"action_{++_actionCounter}", message, logLevel, logger);
        _rule.AddAction(action);
        return this;
    }

    /// <summary>
    /// Adds a custom action.
    /// </summary>
    /// <param name="name">The action name</param>
    /// <param name="action">The custom action function</param>
    /// <returns>This builder instance</returns>
    public RuleBuilder ThenExecute(string name, Func<DataRecord, ITransformationContext, CancellationToken, Task<TransformationResult>> action)
    {
        var customAction = new CustomAction($"action_{++_actionCounter}", name, action);
        _rule.AddAction(customAction);
        return this;
    }

    /// <summary>
    /// Builds the transformation rule.
    /// </summary>
    /// <returns>The constructed rule</returns>
    public ITransformationRule Build()
    {
        return _rule;
    }

    /// <summary>
    /// Adds a condition to the rule.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <param name="operator">The operator</param>
    /// <param name="value">The value</param>
    /// <returns>This builder instance</returns>
    private RuleBuilder AddCondition(string fieldName, ConditionOperator @operator, object? value)
    {
        var condition = new RuleCondition(
            $"condition_{++_conditionCounter}",
            $"{fieldName} {@operator} {value}",
            fieldName,
            @operator,
            value);
        
        _rule.AddCondition(condition);
        return this;
    }
}
