using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MultipleAIApp.Models;

namespace MultipleAIApp.Services;

public interface IChatCompletionService
{
    Task<string> CompleteAsync(ModelDefinition model, IReadOnlyList<ChatMessage> messages, string apiKey, CancellationToken cancellationToken);
}

public sealed class HuggingFaceChatCompletionService : IChatCompletionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HuggingFaceChatCompletionService> _logger;
    private readonly IConnectionService _connectionService;

    public HuggingFaceChatCompletionService(IHttpClientFactory httpClientFactory, ILogger<HuggingFaceChatCompletionService> logger, IConnectionService connectionService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _connectionService = connectionService;
    }

    public async Task<string> CompleteAsync(ModelDefinition model, IReadOnlyList<ChatMessage> messages, string apiKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        // Perform handshake if not connected
        if (!_connectionService.IsConnected("huggingface"))
        {
            _logger.LogInformation("Connection not established, performing handshake for HuggingFace");
            var handshakeResult = await _connectionService.PerformHandshakeAsync("huggingface", apiKey, cancellationToken);
            if (!handshakeResult.IsSuccess)
            {
                throw new InvalidOperationException($"Handshake failed: {handshakeResult.Message}");
            }
        }

        var client = _httpClientFactory.CreateClient("huggingface");
        using var request = BuildRequest(model, messages, apiKey);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("HuggingFace call failed: {Status} {Content}", response.StatusCode, errorPayload);
            response.EnsureSuccessStatusCode();
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0)
        {
            var first = document.RootElement[0];
            if (first.TryGetProperty("generated_text", out var generated))
            {
                return generated.GetString() ?? string.Empty;
            }

            if (first.TryGetProperty("generated_texts", out var generatedTexts) && generatedTexts.ValueKind == JsonValueKind.Array && generatedTexts.GetArrayLength() > 0)
            {
                return generatedTexts[0].GetString() ?? string.Empty;
            }
        }
        else if (document.RootElement.TryGetProperty("generated_text", out var generatedText))
        {
            return generatedText.GetString() ?? string.Empty;
        }

        _logger.LogWarning("Unknown HuggingFace response shape: {Json}", document.RootElement.GetRawText());
        return document.RootElement.GetRawText();
    }

    private static HttpRequestMessage BuildRequest(ModelDefinition model, IReadOnlyList<ChatMessage> messages, string apiKey)
    {
        var endpoint = string.IsNullOrWhiteSpace(model.Endpoint)
            ? new Uri($"models/{model.ModelId}", UriKind.Relative)
            : new Uri(model.Endpoint, UriKind.RelativeOrAbsolute);

        var promptBuilder = new StringBuilder();
        foreach (var message in messages)
        {
            var prefix = message.Role switch
            {
                ChatRole.System => "System:",
                ChatRole.User => "User:",
                ChatRole.Assistant => "Assistant:",
                _ => string.Empty
            };
            promptBuilder.Append(prefix);
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                promptBuilder.Append(' ');
            }
            promptBuilder.AppendLine(message.Content.Trim());
        }

        promptBuilder.Append("Assistant:");

        var parameters = CreateParameterPayload(model.Parameters);

        var payload = new HuggingFaceRequest
        {
            Inputs = promptBuilder.ToString(),
            Parameters = parameters
        };

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return request;
    }

    private static Dictionary<string, object> CreateParameterPayload(IReadOnlyList<ModelParameterDefinition> parameters)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
        {
            if (!parameter.Default.HasValue)
            {
                continue;
            }

            var element = parameter.Default.Value;
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var intValue))
                    {
                        result[parameter.Name] = intValue;
                    }
                    else if (element.TryGetDouble(out var doubleValue))
                    {
                        result[parameter.Name] = doubleValue;
                    }
                    break;
                case JsonValueKind.String:
                    var stringValue = element.GetString();
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        result[parameter.Name] = stringValue;
                    }
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    result[parameter.Name] = element.GetBoolean();
                    break;
            }
        }

        return result;
    }

    private sealed record HuggingFaceRequest
    {
        public string Inputs { get; init; } = string.Empty;

        public Dictionary<string, object> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
