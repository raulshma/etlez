using ETLFramework.Core.Interfaces;
using ETLFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace ETLFramework.Pipeline;

/// <summary>
/// Builder class for creating pipelines using a fluent API.
/// </summary>
public class PipelineBuilder
{
    private readonly ILogger<Pipeline> _logger;
    private readonly List<IPipelineStage> _stages;
    private Guid _id;
    private string _name;
    private string _description;

    /// <summary>
    /// Initializes a new instance of the PipelineBuilder class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public PipelineBuilder(ILogger<Pipeline> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stages = new List<IPipelineStage>();
        _id = Guid.NewGuid();
        _name = string.Empty;
        _description = string.Empty;
    }

    /// <summary>
    /// Sets the pipeline identifier.
    /// </summary>
    /// <param name="id">The pipeline identifier</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the pipeline name.
    /// </summary>
    /// <param name="name">The pipeline name</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder WithName(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Sets the pipeline description.
    /// </summary>
    /// <param name="description">The pipeline description</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder WithDescription(string description)
    {
        _description = description ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Adds a stage to the pipeline.
    /// </summary>
    /// <param name="stage">The stage to add</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder AddStage(IPipelineStage stage)
    {
        if (stage == null) throw new ArgumentNullException(nameof(stage));
        _stages.Add(stage);
        return this;
    }

    /// <summary>
    /// Adds a demo stage to the pipeline for testing purposes.
    /// </summary>
    /// <param name="name">The stage name</param>
    /// <param name="stageType">The stage type</param>
    /// <param name="order">The stage order</param>
    /// <param name="recordCount">Number of records to simulate</param>
    /// <param name="processingDelay">Processing delay per record</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder AddDemoStage(
        string name, 
        StageType stageType, 
        int order, 
        int recordCount = 100, 
        TimeSpan? processingDelay = null)
    {
        var stage = new DemoStage(
            Guid.NewGuid(),
            name,
            stageType,
            order,
            _logger,
            recordCount,
            processingDelay);

        return AddStage(stage);
    }

    /// <summary>
    /// Adds an extract stage to the pipeline.
    /// </summary>
    /// <param name="name">The stage name</param>
    /// <param name="order">The stage order</param>
    /// <param name="recordCount">Number of records to simulate</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder AddExtractStage(string name, int order, int recordCount = 100)
    {
        return AddDemoStage(name, StageType.Extract, order, recordCount);
    }

    /// <summary>
    /// Adds a transform stage to the pipeline.
    /// </summary>
    /// <param name="name">The stage name</param>
    /// <param name="order">The stage order</param>
    /// <param name="recordCount">Number of records to simulate</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder AddTransformStage(string name, int order, int recordCount = 100)
    {
        return AddDemoStage(name, StageType.Transform, order, recordCount);
    }

    /// <summary>
    /// Adds a load stage to the pipeline.
    /// </summary>
    /// <param name="name">The stage name</param>
    /// <param name="order">The stage order</param>
    /// <param name="recordCount">Number of records to simulate</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder AddLoadStage(string name, int order, int recordCount = 100)
    {
        return AddDemoStage(name, StageType.Load, order, recordCount);
    }

    /// <summary>
    /// Adds multiple stages in sequence.
    /// </summary>
    /// <param name="stages">The stages to add</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder AddStages(params IPipelineStage[] stages)
    {
        if (stages == null) throw new ArgumentNullException(nameof(stages));
        
        foreach (var stage in stages)
        {
            AddStage(stage);
        }
        return this;
    }

    /// <summary>
    /// Adds stages from a collection.
    /// </summary>
    /// <param name="stages">The stages to add</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder AddStages(IEnumerable<IPipelineStage> stages)
    {
        if (stages == null) throw new ArgumentNullException(nameof(stages));
        
        foreach (var stage in stages)
        {
            AddStage(stage);
        }
        return this;
    }

    /// <summary>
    /// Creates a simple ETL pipeline with extract, transform, and load stages.
    /// </summary>
    /// <param name="extractRecords">Number of records for extract stage</param>
    /// <param name="transformRecords">Number of records for transform stage</param>
    /// <param name="loadRecords">Number of records for load stage</param>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder CreateSimpleETL(int extractRecords = 100, int transformRecords = 100, int loadRecords = 100)
    {
        return AddExtractStage("Extract Data", 1, extractRecords)
               .AddTransformStage("Transform Data", 2, transformRecords)
               .AddLoadStage("Load Data", 3, loadRecords);
    }

    /// <summary>
    /// Builds the pipeline instance.
    /// </summary>
    /// <returns>The configured pipeline</returns>
    public IPipeline Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            throw new InvalidOperationException("Pipeline name is required. Use WithName() to set it.");
        }

        var pipeline = new Pipeline(_id, _name, _description, _logger);

        // Add stages in order
        var orderedStages = _stages.OrderBy(s => s.Order);
        foreach (var stage in orderedStages)
        {
            pipeline.AddStage(stage);
        }

        return pipeline;
    }

    /// <summary>
    /// Builds the pipeline and validates it.
    /// </summary>
    /// <returns>The configured and validated pipeline</returns>
    public async Task<IPipeline> BuildAndValidateAsync()
    {
        var pipeline = Build();
        
        var validationResult = await pipeline.ValidateAsync();
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"Pipeline validation failed: {errors}");
        }

        return pipeline;
    }

    /// <summary>
    /// Creates a new pipeline builder instance.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <returns>A new pipeline builder</returns>
    public static PipelineBuilder Create(ILogger<Pipeline> logger)
    {
        return new PipelineBuilder(logger);
    }

    /// <summary>
    /// Creates a new pipeline builder with a name.
    /// </summary>
    /// <param name="name">The pipeline name</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A new pipeline builder</returns>
    public static PipelineBuilder Create(string name, ILogger<Pipeline> logger)
    {
        return new PipelineBuilder(logger).WithName(name);
    }

    /// <summary>
    /// Creates a new pipeline builder with name and description.
    /// </summary>
    /// <param name="name">The pipeline name</param>
    /// <param name="description">The pipeline description</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A new pipeline builder</returns>
    public static PipelineBuilder Create(string name, string description, ILogger<Pipeline> logger)
    {
        return new PipelineBuilder(logger).WithName(name).WithDescription(description);
    }

    /// <summary>
    /// Gets the current number of stages in the builder.
    /// </summary>
    public int StageCount => _stages.Count;

    /// <summary>
    /// Gets the current pipeline name.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets the current pipeline description.
    /// </summary>
    public string Description => _description;

    /// <summary>
    /// Gets the current pipeline ID.
    /// </summary>
    public Guid Id => _id;

    /// <summary>
    /// Clears all stages from the builder.
    /// </summary>
    /// <returns>The builder instance for chaining</returns>
    public PipelineBuilder ClearStages()
    {
        _stages.Clear();
        return this;
    }

    /// <summary>
    /// Returns a string representation of the builder.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        return $"PipelineBuilder[Name={_name}, Stages={_stages.Count}]";
    }
}
