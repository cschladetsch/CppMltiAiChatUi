using System.Text.Json;
using System.Text.Json.Serialization;

namespace MultiLLM.Core.Models;

public sealed class ModelParameterDefinition
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("default")]
    public JsonElement? Default { get; init; }

    [JsonIgnore]
    public string DefaultDisplay => Default.HasValue
        ? Default.Value.ValueKind switch
        {
            JsonValueKind.String => Default.Value.GetString() ?? string.Empty,
            JsonValueKind.Null => "—",
            _ => Default.Value.GetRawText()
        }
        : "—";
}