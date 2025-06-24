using System.Text.Json;
using ETLFramework.Data.Models;
using ETLFramework.Data.Entities;

namespace ETLFramework.Data.Configuration;

/// <summary>
/// Helper class for JSON serialization and deserialization of entity properties.
/// </summary>
public static class JsonConverters
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a ConnectorConfigurationDto to JSON string.
    /// </summary>
    /// <param name="connector">The connector configuration</param>
    /// <returns>JSON string</returns>
    public static string SerializeConnector(ConnectorConfigurationDto connector)
    {
        return JsonSerializer.Serialize(connector, JsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to ConnectorConfigurationDto.
    /// </summary>
    /// <param name="json">The JSON string</param>
    /// <returns>ConnectorConfigurationDto</returns>
    public static ConnectorConfigurationDto DeserializeConnector(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ConnectorConfigurationDto();

        return JsonSerializer.Deserialize<ConnectorConfigurationDto>(json, JsonOptions) 
               ?? new ConnectorConfigurationDto();
    }

    /// <summary>
    /// Serializes a list of TransformationConfigurationDto to JSON string.
    /// </summary>
    /// <param name="transformations">The transformation configurations</param>
    /// <returns>JSON string</returns>
    public static string SerializeTransformations(List<TransformationConfigurationDto> transformations)
    {
        return JsonSerializer.Serialize(transformations, JsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to list of TransformationConfigurationDto.
    /// </summary>
    /// <param name="json">The JSON string</param>
    /// <returns>List of TransformationConfigurationDto</returns>
    public static List<TransformationConfigurationDto> DeserializeTransformations(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return new List<TransformationConfigurationDto>();

        return JsonSerializer.Deserialize<List<TransformationConfigurationDto>>(json, JsonOptions) 
               ?? new List<TransformationConfigurationDto>();
    }

    /// <summary>
    /// Serializes a dictionary to JSON string.
    /// </summary>
    /// <param name="dictionary">The dictionary</param>
    /// <returns>JSON string</returns>
    public static string SerializeDictionary(Dictionary<string, object> dictionary)
    {
        return JsonSerializer.Serialize(dictionary, JsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to dictionary.
    /// </summary>
    /// <param name="json">The JSON string</param>
    /// <returns>Dictionary</returns>
    public static Dictionary<string, object> DeserializeDictionary(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
            return new Dictionary<string, object>();

        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, JsonOptions) 
               ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Populates the non-mapped properties of a Pipeline entity from JSON columns.
    /// </summary>
    /// <param name="pipeline">The pipeline entity</param>
    public static void PopulatePipelineFromJson(Pipeline pipeline)
    {
        if (pipeline == null) return;

        pipeline.SourceConnector = DeserializeConnector(pipeline.SourceConnectorJson);
        pipeline.TargetConnector = DeserializeConnector(pipeline.TargetConnectorJson);
        pipeline.Transformations = DeserializeTransformations(pipeline.TransformationsJson);
        pipeline.Configuration = DeserializeDictionary(pipeline.ConfigurationJson);
    }

    /// <summary>
    /// Populates the JSON columns of a Pipeline entity from non-mapped properties.
    /// </summary>
    /// <param name="pipeline">The pipeline entity</param>
    public static void PopulatePipelineToJson(Pipeline pipeline)
    {
        if (pipeline == null) return;

        if (pipeline.SourceConnector != null)
            pipeline.SourceConnectorJson = SerializeConnector(pipeline.SourceConnector);

        if (pipeline.TargetConnector != null)
            pipeline.TargetConnectorJson = SerializeConnector(pipeline.TargetConnector);

        if (pipeline.Transformations != null)
            pipeline.TransformationsJson = SerializeTransformations(pipeline.Transformations);

        if (pipeline.Configuration != null)
            pipeline.ConfigurationJson = SerializeDictionary(pipeline.Configuration);
    }

    /// <summary>
    /// Populates the non-mapped properties of an Execution entity from JSON columns.
    /// </summary>
    /// <param name="execution">The execution entity</param>
    public static void PopulateExecutionFromJson(Execution execution)
    {
        if (execution == null) return;

        execution.Parameters = DeserializeDictionary(execution.ParametersJson);
    }

    /// <summary>
    /// Populates the JSON columns of an Execution entity from non-mapped properties.
    /// </summary>
    /// <param name="execution">The execution entity</param>
    public static void PopulateExecutionToJson(Execution execution)
    {
        if (execution == null) return;

        if (execution.Parameters != null)
            execution.ParametersJson = SerializeDictionary(execution.Parameters);
    }
}
