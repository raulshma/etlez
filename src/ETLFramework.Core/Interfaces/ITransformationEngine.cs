using ETLFramework.Core.Models;
using ETLFramework.Core.Interfaces;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Interface for the transformation engine that applies transformation rules to data records.
/// </summary>
public interface ITransformationEngine
{
    /// <summary>
    /// Transforms a single data record using the specified transformation rules.
    /// </summary>
    /// <param name="input">The input data record to transform</param>
    /// <param name="rules">The transformation rules to apply</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The transformation result</returns>
    Task<TransformationResult> TransformAsync(DataRecord input, IEnumerable<ITransformationRule> rules, ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transforms a batch of data records using the specified transformation rules.
    /// </summary>
    /// <param name="input">The input data records to transform</param>
    /// <param name="rules">The transformation rules to apply</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The transformation results</returns>
    Task<IEnumerable<TransformationResult>> TransformBatchAsync(IEnumerable<DataRecord> input, IEnumerable<ITransformationRule> rules, ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transforms data records as a stream using the specified transformation rules.
    /// </summary>
    /// <param name="input">The input data stream to transform</param>
    /// <param name="rules">The transformation rules to apply</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The transformed data stream</returns>
    IAsyncEnumerable<TransformationResult> TransformStreamAsync(IAsyncEnumerable<DataRecord> input, IEnumerable<ITransformationRule> rules, ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the transformation rules against the input schema.
    /// </summary>
    /// <param name="rules">The transformation rules to validate</param>
    /// <param name="inputSchema">The input data schema</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateRulesAsync(IEnumerable<ITransformationRule> rules, DataSchema inputSchema);

    /// <summary>
    /// Gets the output schema that would result from applying the transformation rules to the input schema.
    /// </summary>
    /// <param name="rules">The transformation rules</param>
    /// <param name="inputSchema">The input data schema</param>
    /// <returns>The predicted output schema</returns>
    Task<DataSchema> GetOutputSchemaAsync(IEnumerable<ITransformationRule> rules, DataSchema inputSchema);

    /// <summary>
    /// Registers a custom transformation rule type.
    /// </summary>
    /// <param name="ruleType">The transformation rule type</param>
    /// <param name="factory">Factory function to create instances of the rule</param>
    void RegisterTransformationRule(Type ruleType, Func<ITransformationRuleConfiguration, ITransformationRule> factory);

    /// <summary>
    /// Gets all registered transformation rule types.
    /// </summary>
    /// <returns>Collection of registered rule types</returns>
    IEnumerable<Type> GetRegisteredRuleTypes();
}

/// <summary>
/// Interface for transformation rules that can be applied to data records.
/// </summary>
public interface ITransformationRule
{
    /// <summary>
    /// Gets the unique identifier for this transformation rule.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of the transformation rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this rule does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the type of transformation rule.
    /// </summary>
    string RuleType { get; }

    /// <summary>
    /// Gets the order/priority of this rule when multiple rules are applied.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets whether this rule is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the configuration for this transformation rule.
    /// </summary>
    ITransformationRuleConfiguration Configuration { get; }

    /// <summary>
    /// Applies the transformation rule to a data record.
    /// </summary>
    /// <param name="input">The input data record</param>
    /// <param name="context">The transformation context</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The transformation result</returns>
    Task<TransformationResult> ApplyAsync(DataRecord input, ITransformationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the transformation rule configuration.
    /// </summary>
    /// <param name="inputSchema">The input data schema</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateAsync(DataSchema inputSchema);

    /// <summary>
    /// Gets the output schema that would result from applying this rule to the input schema.
    /// </summary>
    /// <param name="inputSchema">The input data schema</param>
    /// <returns>The predicted output schema</returns>
    Task<DataSchema> GetOutputSchemaAsync(DataSchema inputSchema);

    /// <summary>
    /// Determines if this rule can be applied to the given data record.
    /// </summary>
    /// <param name="input">The input data record</param>
    /// <param name="context">The transformation context</param>
    /// <returns>True if the rule can be applied, false otherwise</returns>
    bool CanApply(DataRecord input, ITransformationContext context);
}

/// <summary>
/// Interface for data mapping functionality.
/// </summary>
public interface IDataMapper
{
    /// <summary>
    /// Maps a data record from source schema to destination schema.
    /// </summary>
    /// <param name="source">The source data record</param>
    /// <param name="sourceSchema">The source data schema</param>
    /// <param name="destinationSchema">The destination data schema</param>
    /// <param name="mappingConfiguration">The mapping configuration</param>
    /// <returns>The mapped data record</returns>
    Task<DataRecord> MapAsync(DataRecord source, DataSchema sourceSchema, DataSchema destinationSchema, IMappingConfiguration mappingConfiguration);

    /// <summary>
    /// Creates a mapping configuration between two schemas.
    /// </summary>
    /// <param name="sourceSchema">The source data schema</param>
    /// <param name="destinationSchema">The destination data schema</param>
    /// <param name="autoMap">Whether to automatically map fields with matching names</param>
    /// <returns>The mapping configuration</returns>
    IMappingConfiguration CreateMapping(DataSchema sourceSchema, DataSchema destinationSchema, bool autoMap = true);

    /// <summary>
    /// Validates a mapping configuration.
    /// </summary>
    /// <param name="mappingConfiguration">The mapping configuration to validate</param>
    /// <param name="sourceSchema">The source data schema</param>
    /// <param name="destinationSchema">The destination data schema</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateMapping(IMappingConfiguration mappingConfiguration, DataSchema sourceSchema, DataSchema destinationSchema);
}
