using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MultiLLM.Core.Models;
using MultiLLM.Core.Interfaces;

namespace MultiLLM.Core.Services.ChatCompletion;

public sealed class OpenAIChatCompletionService : IChatCompletionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAIChatCompletionService> _logger;
    private readonly IConnectionService _connectionService;

    public OpenAIChatCompletionService(IHttpClientFactory httpClientFactory, ILogger<OpenAIChatCompletionService> logger, IConnectionService connectionService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _connectionService = connectionService;
    }

    public async Task<string> CompleteAsync(ModelDefinition model, IReadOnlyList<ChatMessage> messages, string apiKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        // Perform handshake if not connected
        if (!_connectionService.IsConnected("openai"))
        {
            _logger.LogInformation("Connection not established, performing handshake for OpenAI");
            var handshakeResult = await _connectionService.PerformHandshakeAsync("openai", apiKey, cancellationToken);
            if (!handshakeResult.IsSuccess)
            {
                throw new InvalidOperationException($"Handshake failed: {handshakeResult.Message}");
            }
        }

        var client = _httpClientFactory.CreateClient("openai");
        using var request = BuildRequest(model, messages, apiKey);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("OpenAI call failed: {Status} {Content}", response.StatusCode, errorPayload);
            response.EnsureSuccessStatusCode();
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (document.RootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
        {
            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }
        }

        _logger.LogWarning("Unknown OpenAI response shape: {Json}", document.RootElement.GetRawText());
        return document.RootElement.GetRawText();
    }

    private static HttpRequestMessage BuildRequest(ModelDefinition model, IReadOnlyList<ChatMessage> messages, string apiKey)
    {
        var openAiMessages = messages.Select(m => new
        {
            role = m.Role.ToString().ToLower(),
            content = m.Content
        }).ToArray();

        var payload = new
        {
            model = model.ModelId,
            messages = openAiMessages,
            max_tokens = GetParameter(model.Parameters, "max_tokens", 1000),
            temperature = GetParameter(model.Parameters, "temperature", 0.7)
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
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