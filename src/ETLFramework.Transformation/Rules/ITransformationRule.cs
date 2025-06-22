using ETLFramework.Core.Models;

namespace ETLFramework.Transformation.Rules;

/// <summary>
/// Represents a transformation rule that can be applied to data records.
/// </summary>
public interface ITransformationRule
{
    /// <summary>
    /// Gets the unique identifier for this rule.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of the rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this rule does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the priority of this rule (higher numbers execute first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets whether this rule is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the conditions that must be met for this rule to execute.
    /// </summary>
    IReadOnlyList<IRuleCondition> Conditions { get; }

    /// <summary>
    /// Gets the actions to perform when this rule matches.
    /// </summary>
    IReadOnlyList<IRuleAction> Actions { get; }

    /// <summary>
    /// Evaluates whether this rule should be applied to the given record.
    /// </summary>
    /// <param name="record">The data record to evaluate</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the rule should be applied</returns>
    Task<bool> EvaluateAsync(DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies this rule to the given record.
    /// </summary>
    /// <param name="record">The data record to transform</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation result</returns>
    Task<Core.Models.TransformationResult> ApplyAsync(DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates this rule configuration.
    /// </summary>
    /// <param name="context">The transformation context</param>
    /// <returns>Validation result</returns>
    ValidationResult Validate(ETLFramework.Transformation.Interfaces.ITransformationContext context);
}

/// <summary>
/// Represents a condition that must be met for a rule to execute.
/// </summary>
public interface IRuleCondition
{
    /// <summary>
    /// Gets the unique identifier for this condition.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of the condition.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the field name this condition applies to.
    /// </summary>
    string FieldName { get; }

    /// <summary>
    /// Gets the operator for this condition.
    /// </summary>
    ConditionOperator Operator { get; }

    /// <summary>
    /// Gets the value to compare against.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Evaluates this condition against the given record.
    /// </summary>
    /// <param name="record">The data record to evaluate</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the condition is met</returns>
    Task<bool> EvaluateAsync(DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an action to perform when a rule matches.
/// </summary>
public interface IRuleAction
{
    /// <summary>
    /// Gets the unique identifier for this action.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of the action.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type of action to perform.
    /// </summary>
    ActionType ActionType { get; }

    /// <summary>
    /// Executes this action on the given record.
    /// </summary>
    /// <param name="record">The data record to modify</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation result</returns>
    Task<Core.Models.TransformationResult> ExecuteAsync(DataRecord record, ETLFramework.Transformation.Interfaces.ITransformationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Operators for rule conditions.
/// </summary>
public enum ConditionOperator
{
    /// <summary>
    /// Equals comparison.
    /// </summary>
    Equals,

    /// <summary>
    /// Not equals comparison.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Greater than comparison.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal comparison.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than comparison.
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal comparison.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Contains text comparison.
    /// </summary>
    Contains,

    /// <summary>
    /// Starts with text comparison.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with text comparison.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Regular expression match.
    /// </summary>
    Regex,

    /// <summary>
    /// Field is null or empty.
    /// </summary>
    IsNullOrEmpty,

    /// <summary>
    /// Field is not null or empty.
    /// </summary>
    IsNotNullOrEmpty,

    /// <summary>
    /// Value is in a list of values.
    /// </summary>
    In,

    /// <summary>
    /// Value is not in a list of values.
    /// </summary>
    NotIn
}

/// <summary>
/// Types of actions that can be performed.
/// </summary>
public enum ActionType
{
    /// <summary>
    /// Set a field value.
    /// </summary>
    SetField,

    /// <summary>
    /// Remove a field.
    /// </summary>
    RemoveField,

    /// <summary>
    /// Transform a field using a transformation.
    /// </summary>
    TransformField,

    /// <summary>
    /// Copy a field to another field.
    /// </summary>
    CopyField,

    /// <summary>
    /// Skip the record.
    /// </summary>
    SkipRecord,

    /// <summary>
    /// Stop processing further rules.
    /// </summary>
    StopProcessing,

    /// <summary>
    /// Log a message.
    /// </summary>
    LogMessage,

    /// <summary>
    /// Execute a custom action.
    /// </summary>
    Custom
}
