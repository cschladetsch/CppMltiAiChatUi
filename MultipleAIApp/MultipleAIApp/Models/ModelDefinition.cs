using System.Text.Json.Serialization;

namespace MultipleAIApp.Models;

public sealed class ModelDefinition
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("provider")]
    public required string Provider { get; init; }

    [JsonPropertyName("modelId")]
    public required string ModelId { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; init; }

    [JsonPropertyName("parameters")]
    public IReadOnlyList<ModelParameterDefinition> Parameters { get; init; } = Array.Empty<ModelParameterDefinition>();

    public override string ToString() => Name;
}
