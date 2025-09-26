using System.Text.Json.Serialization;

namespace MultiLLM.Core.Models;

public sealed class ModelCatalogConfiguration
{
    [JsonPropertyName("summary")]
    public SummaryConfiguration Summary { get; init; } = new();

    [JsonPropertyName("models")]
    public IReadOnlyList<ModelDefinition> Models { get; init; } = Array.Empty<ModelDefinition>();
}

public sealed class SummaryConfiguration
{
    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; init; } = "Summarize the following assistant conversations in concise bullet points.";

    [JsonPropertyName("modelId")]
    public string? ModelId { get; init; }
}