using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using ETLFramework.Transformation.Helpers;
using ETLFramework.Transformation.Interfaces;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Transformation.Rules;

/// <summary>
/// Base class for rule actions.
/// </summary>
public abstract class BaseRuleAction : IRuleAction
{
    /// <summary>
    /// Initializes a new instance of the BaseRuleAction class.
    /// </summary>
    /// <param name="id">The action ID</param>
    /// <param name="name">The action name</param>
    /// <param name="actionType">The action type</param>
    protected BaseRuleAction(string id, string name, ActionType actionType)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ActionType = actionType;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ActionType ActionType { get; }

    /// <inheritdoc />
    public abstract Task<Core.Models.TransformationResult> ExecuteAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Action to set a field value.
/// </summary>
public class SetFieldAction : BaseRuleAction
{
    private readonly string _fieldName;
    private readonly object? _value;

    /// <summary>
    /// Initializes a new instance of the SetFieldAction class.
    /// </summary>
    /// <param name="id">The action ID</param>
    /// <param name="fieldName">The field name to set</param>
    /// <param name="value">The value to set</param>
    public SetFieldAction(string id, string fieldName, object? value)
        : base(id, $"Set {fieldName}", ActionType.SetField)
    {
        _fieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        _value = value;
    }

    /// <inheritdoc />
    public override Task<Core.Models.TransformationResult> ExecuteAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var outputRecord = record.Clone();
            outputRecord.SetField(_fieldName, _value);
            return Task.FromResult(TransformationResultHelper.Success(outputRecord));
        }
        catch (Exception ex)
        {
            return Task.FromResult(TransformationResultHelper.Failure($"Failed to set field {_fieldName}: {ex.Message}", ex));
        }
    }
}

/// <summary>
/// Action to remove a field.
/// </summary>
public class RemoveFieldAction : BaseRuleAction
{
    private readonly string _fieldName;

    /// <summary>
    /// Initializes a new instance of the RemoveFieldAction class.
    /// </summary>
    /// <param name="id">The action ID</param>
    /// <param name="fieldName">The field name to remove</param>
    public RemoveFieldAction(string id, string fieldName)
        : base(id, $"Remove {fieldName}", ActionType.RemoveField)
    {
        _fieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
    }

    /// <inheritdoc />
    public override Task<Core.Models.TransformationResult> ExecuteAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var outputRecord = record.Clone();
            outputRecord.RemoveField(_fieldName);
            return Task.FromResult(TransformationResultHelper.Success(outputRecord));
        }
        catch (Exception ex)
        {
            return Task.FromResult(TransformationResultHelper.Failure($"Failed to remove field {_fieldName}: {ex.Message}", ex));
        }
    }
}

/// <summary>
/// Action to copy a field to another field.
/// </summary>
public class CopyFieldAction : BaseRuleAction
{
    private readonly string _sourceField;
    private readonly string _targetField;

    /// <summary>
    /// Initializes a new instance of the CopyFieldAction class.
    /// </summary>
    /// <param name="id">The action ID</param>
    /// <param name="sourceField">The source field name</param>
    /// <param name="targetField">The target field name</param>
    public CopyFieldAction(string id, string sourceField, string targetField)
        : base(id, $"Copy {sourceField} to {targetField}", ActionType.CopyField)
    {
        _sourceField = sourceField ?? throw new ArgumentNullException(nameof(sourceField));
        _targetField = targetField ?? throw new ArgumentNullException(nameof(targetField));
    }

    /// <inheritdoc />
    public override Task<Core.Models.TransformationResult> ExecuteAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var outputRecord = record.Clone();
            var value = outputRecord.GetField<object>(_sourceField);
            outputRecord.SetField(_targetField, value);
            return Task.FromResult(TransformationResultHelper.Success(outputRecord));
        }
        catch (Exception ex)
        {
            return Task.FromResult(TransformationResultHelper.Failure($"Failed to copy field {_sourceField} to {_targetField}: {ex.Message}", ex));
        }
    }
}

/// <summary>
/// Action to transform a field using a transformation.
/// </summary>
public class TransformFieldAction : BaseRuleAction
{
    private readonly ITransformation _transformation;

    /// <summary>
    /// Initializes a new instance of the TransformFieldAction class.
    /// </summary>
    /// <param name="id">The action ID</param>
    /// <param name="transformation">The transformation to apply</param>
    public TransformFieldAction(string id, ITransformation transformation)
        : base(id, $"Transform using {transformation.Name}", ActionType.TransformField)
    {
        _transformation = transformation ?? throw new ArgumentNullException(nameof(transformation));
    }

    /// <inheritdoc />
    public override async Task<Core.Models.TransformationResult> ExecuteAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _transformation.TransformAsync(record, context, cancellationToken);
        }
        catch (Exception ex)
        {
            return TransformationResultHelper.Failure($"Failed to apply transformation {_transformation.Name}: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Action to skip the current record.
/// </summary>
public class SkipRecordAction : BaseRuleAction
{
    private readonly string _reason;

    /// <summary>
    /// Initializes a new instance of the SkipRecordAction class.
    /// </summary>
    /// <param name="id">The action ID</param>
    /// <param name="reason">The reason for skipping</param>
    public SkipRecordAction(string id, string reason = "Record skipped by rule")
        : base(id, "Skip Record", ActionType.SkipRecord)
    {
        _reason = reason ?? "Record skipped by rule";
    }

    /// <inheritdoc />
    public override Task<Core.Models.TransformationResult> ExecuteAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        context.SkipRecord();
        return Task.FromResult(TransformationResultHelper.Skipped(record, _reason));
    }
}

/// <summary>
/// Action to stop processing further rules.
/// </summary>
public class StopProcessingAction : BaseRuleAction
{
    /// <summary>
    /// Initializes a new instance of the StopProcessingAction class.
    /// </summary>
    /// <param name="id">The action ID</param>
    public StopProcessingAction(string id)
        : base(id, "Stop Processing", ActionType.StopProcessing)
    {
    }

    /// <inheritdoc />
    public override Task<Core.Models.TransformationResult> ExecuteAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        // This action doesn't modify the record, it just signals to stop processing
        return Task.FromResult(TransformationResultHelper.Success(record));
    }
}

/// <summary>
/// Action to log a message.
/// </summary>
public class LogMessageAction : BaseRuleAction
{
    private readonly string _message;
    private readonly LogLevel _logLevel;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the LogMessageAction class.
    /// </summary>
    /// <param name="id">The action ID</param>
    /// <param name="message">The message to log</param>
    /// <param name="logLevel">The log level</param>
    /// <param name="logger">The logger instance</param>
    public LogMessageAction(string id, string message, LogLevel logLevel = LogLevel.Information, ILogger? logger = null)
        : base(id, "Log Message", ActionType.LogMessage)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
        _logLevel = logLevel;
        _logger = logger;
    }

    /// <inheritdoc />
    public override Task<Core.Models.TransformationResult> ExecuteAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Replace placeholders in message with field values
            var formattedMessage = FormatMessage(_message, record);
            
            if (_logger != null)
            {
                _logger.Log(_logLevel, formattedMessage);
            }
            else
            {
                // Fallback to context logging if available
                context.AddError(formattedMessage, null);
            }

            return Task.FromResult(TransformationResultHelper.Success(record));
        }
        catch (Exception ex)
        {
            return Task.FromResult(TransformationResultHelper.Failure($"Failed to log message: {ex.Message}", ex));
        }
    }

    /// <summary>
    /// Formats a message by replacing field placeholders with actual values.
    /// </summary>
    /// <param name="message">The message template</param>
    /// <param name="record">The data record</param>
    /// <returns>The formatted message</returns>
    private static string FormatMessage(string message, DataRecord record)
    {
        var result = message;
        
        // Replace {fieldName} placeholders with actual field values
        foreach (var field in record.Fields)
        {
            var placeholder = $"{{{field.Key}}}";
            if (result.Contains(placeholder))
            {
                result = result.Replace(placeholder, field.Value?.ToString() ?? "null");
            }
        }

        return result;
    }
}

/// <summary>
/// Action for custom logic.
/// </summary>
public class CustomAction : BaseRuleAction
{
    private readonly Func<DataRecord, ITransformationContext, CancellationToken, Task<Core.Models.TransformationResult>> _action;

    /// <summary>
    /// Initializes a new instance of the CustomAction class.
    /// </summary>
    /// <param name="id">The action ID</param>
    /// <param name="name">The action name</param>
    /// <param name="action">The custom action function</param>
    public CustomAction(string id, string name, Func<DataRecord, ITransformationContext, CancellationToken, Task<Core.Models.TransformationResult>> action)
        : base(id, name, ActionType.Custom)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <inheritdoc />
    public override async Task<Core.Models.TransformationResult> ExecuteAsync(DataRecord record, ITransformationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _action(record, context, cancellationToken);
        }
        catch (Exception ex)
        {
            return TransformationResultHelper.Failure($"Custom action failed: {ex.Message}", ex);
        }
    }
}
