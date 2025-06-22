namespace ETLFramework.Playground.Playgrounds;

/// <summary>
/// Base interface for all playground modules.
/// </summary>
public interface IPlaygroundModule
{
    /// <summary>
    /// Runs the playground module.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the connector playground module.
/// </summary>
public interface IConnectorPlayground : IPlaygroundModule
{
}

/// <summary>
/// Interface for the transformation playground module.
/// </summary>
public interface ITransformationPlayground : IPlaygroundModule
{
}

/// <summary>
/// Interface for the pipeline playground module.
/// </summary>
public interface IPipelinePlayground : IPlaygroundModule
{
}

/// <summary>
/// Interface for the validation playground module.
/// </summary>
public interface IValidationPlayground : IPlaygroundModule
{
}

/// <summary>
/// Interface for the rule engine playground module.
/// </summary>
public interface IRuleEnginePlayground : IPlaygroundModule
{
}

/// <summary>
/// Interface for the performance playground module.
/// </summary>
public interface IPerformancePlayground : IPlaygroundModule
{
}

/// <summary>
/// Interface for the error handling playground module.
/// </summary>
public interface IErrorHandlingPlayground : IPlaygroundModule
{
}
