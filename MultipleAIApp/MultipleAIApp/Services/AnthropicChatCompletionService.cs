using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MultipleAIApp.Models;

namespace MultipleAIApp.Services;

public sealed class AnthropicChatCompletionService : IChatCompletionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnthropicChatCompletionService> _logger;
    private readonly IConnectionService _connectionService;

    public AnthropicChatCompletionService(IHttpClientFactory httpClientFactory, ILogger<AnthropicChatCompletionService> logger, IConnectionService connectionService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _connectionService = connectionService;
    }

    public async Task<string> CompleteAsync(ModelDefinition model, IReadOnlyList<ChatMessage> messages, string apiKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        // Perform handshake if not connected
        if (!_connectionService.IsConnected("anthropic"))
        {
            _logger.LogInformation("Connection not established, performing handshake for Anthropic");
            var handshakeResult = await _connectionService.PerformHandshakeAsync("anthropic", apiKey, cancellationToken);
            if (!handshakeResult.IsSuccess)
            {
                throw new InvalidOperationException($"Handshake failed: {handshakeResult.Message}");
            }
        }

        var client = _httpClientFactory.CreateClient("anthropic");
        using var request = BuildRequest(model, messages, apiKey);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Anthropic call failed: {Status} {Content}", response.StatusCode, errorPayload);
            response.EnsureSuccessStatusCode();
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (document.RootElement.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array && content.GetArrayLength() > 0)
        {
            var firstContent = content[0];
            if (firstContent.TryGetProperty("text", out var text))
            {
                return text.GetString() ?? string.Empty;
            }
        }

        _logger.LogWarning("Unknown Anthropic response shape: {Json}", document.RootElement.GetRawText());
        return document.RootElement.GetRawText();
    }

    private static HttpRequestMessage BuildRequest(ModelDefinition model, IReadOnlyList<ChatMessage> messages, string apiKey)
    {
        // Convert messages, separating system messages
        var systemMessage = messages.FirstOrDefault(m => m.Role == ChatRole.System)?.Content ?? "";
        var conversationMessages = messages.Where(m => m.Role != ChatRole.System).Select(m => new
        {
            role = m.Role == ChatRole.Assistant ? "assistant" : "user",
            content = m.Content
        }).ToArray();

        var payload = new
        {
            model = model.ModelId,
            max_tokens = GetParameter(model.Parameters, "max_tokens", 1000),
            temperature = GetParameter(model.Parameters, "temperature", 0.7),
            system = !string.IsNullOrEmpty(systemMessage) ? systemMessage : (object?)null,
            messages = conversationMessages
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }), Encoding.UTF8, "application/json")
        };

        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        return request;
    }

    private static T GetParameter<T>(IReadOnlyList<ModelParameterDefinition> parameters, string name, T defaultValue)
    {
        var param = parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (param?.Default.HasValue == true)
        {
            try
            {
                if (typeof(T) == typeof(int) && param.Default.Value.TryGetInt32(out var intValue))
                    return (T)(object)intValue;
                if (typeof(T) == typeof(double) && param.Default.Value.TryGetDouble(out var doubleValue))
                    return (T)(object)doubleValue;
                if (typeof(T) == typeof(string) && param.Default.Value.ValueKind == JsonValueKind.String)
                    return (T)(object)param.Default.Value.GetString()!;
            }
            catch (Exception)
            {
                // Fall back to default value
            }
        }
        return defaultValue;
    }
}