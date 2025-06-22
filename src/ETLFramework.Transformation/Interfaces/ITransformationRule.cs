using ETLFramework.Core.Models;

namespace ETLFramework.Transformation.Interfaces;

/// <summary>
/// Interface for transformation rules that define how transformations should be applied.
/// </summary>
public interface ITransformationRule
{
    /// <summary>
    /// Gets the unique identifier for this rule.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of this rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this rule.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the rule priority (higher values have higher priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets whether this rule is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the rule conditions.
    /// </summary>
    IList<IRuleCondition> Conditions { get; }

    /// <summary>
    /// Gets the transformations to apply when conditions are met.
    /// </summary>
    IList<ITransformation> Transformations { get; }

    /// <summary>
    /// Gets the rule configuration.
    /// </summary>
    IDictionary<string, object> Configuration { get; }

    /// <summary>
    /// Evaluates whether this rule should be applied to the given record.
    /// </summary>
    /// <param name="record">The input record</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the rule should be applied</returns>
    Task<bool> ShouldApplyAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the rule transformations to the given record.
    /// </summary>
    /// <param name="record">The input record</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation results</returns>
    Task<IEnumerable<Core.Models.TransformationResult>> ApplyAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the rule configuration.
    /// </summary>
    /// <returns>A validation result</returns>
    ValidationResult Validate();
}

/// <summary>
/// Interface for rule conditions.
/// </summary>
public interface IRuleCondition
{
    /// <summary>
    /// Gets the condition type.
    /// </summary>
    ConditionType Type { get; }

    /// <summary>
    /// Gets the field name for field-based conditions.
    /// </summary>
    string? FieldName { get; }

    /// <summary>
    /// Gets the condition operator.
    /// </summary>
    ConditionOperator Operator { get; }

    /// <summary>
    /// Gets the condition value.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Gets additional condition parameters.
    /// </summary>
    IDictionary<string, object> Parameters { get; }

    /// <summary>
    /// Evaluates the condition against a data record.
    /// </summary>
    /// <param name="record">The input record</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the condition is met</returns>
    Task<bool> EvaluateAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the type of condition.
/// </summary>
public enum ConditionType
{
    /// <summary>
    /// Field value condition.
    /// </summary>
    FieldValue,

    /// <summary>
    /// Field existence condition.
    /// </summary>
    FieldExists,

    /// <summary>
    /// Field type condition.
    /// </summary>
    FieldType,

    /// <summary>
    /// Record count condition.
    /// </summary>
    RecordCount,

    /// <summary>
    /// Custom expression condition.
    /// </summary>
    Expression,

    /// <summary>
    /// Always true condition.
    /// </summary>
    Always,

    /// <summary>
    /// Never true condition.
    /// </summary>
    Never
}

/// <summary>
/// Represents condition operators.
/// </summary>
public enum ConditionOperator
{
    /// <summary>
    /// Equal to.
    /// </summary>
    Equals,

    /// <summary>
    /// Not equal to.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Greater than.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal to.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than.
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal to.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Contains.
    /// </summary>
    Contains,

    /// <summary>
    /// Does not contain.
    /// </summary>
    NotContains,

    /// <summary>
    /// Starts with.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Matches regular expression.
    /// </summary>
    Matches,

    /// <summary>
    /// Is null.
    /// </summary>
    IsNull,

    /// <summary>
    /// Is not null.
    /// </summary>
    IsNotNull,

    /// <summary>
    /// Is empty.
    /// </summary>
    IsEmpty,

    /// <summary>
    /// Is not empty.
    /// </summary>
    IsNotEmpty,

    /// <summary>
    /// In list.
    /// </summary>
    In,

    /// <summary>
    /// Not in list.
    /// </summary>
    NotIn
}

/// <summary>
/// Interface for transformation rule sets.
/// </summary>
public interface ITransformationRuleSet
{
    /// <summary>
    /// Gets the unique identifier for this rule set.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of this rule set.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this rule set.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the rules in this set.
    /// </summary>
    IList<ITransformationRule> Rules { get; }

    /// <summary>
    /// Gets the rule execution strategy.
    /// </summary>
    RuleExecutionStrategy ExecutionStrategy { get; }

    /// <summary>
    /// Gets whether to stop on first rule match.
    /// </summary>
    bool StopOnFirstMatch { get; }

    /// <summary>
    /// Applies the rule set to a data record.
    /// </summary>
    /// <param name="record">The input record</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transformation results</returns>
    Task<IEnumerable<Core.Models.TransformationResult>> ApplyAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the rule set.
    /// </summary>
    /// <returns>A validation result</returns>
    ValidationResult Validate();
}

/// <summary>
/// Represents rule execution strategies.
/// </summary>
public enum RuleExecutionStrategy
{
    /// <summary>
    /// Execute rules sequentially in priority order.
    /// </summary>
    Sequential,

    /// <summary>
    /// Execute rules in parallel where possible.
    /// </summary>
    Parallel,

    /// <summary>
    /// Execute only the first matching rule.
    /// </summary>
    FirstMatch,

    /// <summary>
    /// Execute all matching rules.
    /// </summary>
    AllMatches
}
