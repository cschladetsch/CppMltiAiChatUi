using Microsoft.Extensions.Logging;
using MultipleAIApp.Models;
using System.Text.Json;
using System.Text;

namespace MultipleAIApp.Services;

public interface IConnectionService
{
    Task<HandshakeResult> PerformHandshakeAsync(string provider, string apiKey, CancellationToken cancellationToken = default);
    bool IsConnected(string provider);
    DateTime? GetLastHandshakeTime(string provider);
    void UpdateConnectionStatus(string provider, string message, bool isConnected);
    event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
}

public sealed class ConnectionService : IConnectionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConnectionService> _logger;
    private readonly Dictionary<string, ConnectionStatus> _connections = new();
    private readonly object _lock = new();

    public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

    public ConnectionService(IHttpClientFactory httpClientFactory, ILogger<ConnectionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HandshakeResult> PerformHandshakeAsync(string provider, string apiKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing handshake for provider: {Provider}", provider);

        try
        {
            var handshakeMessage = GenerateHandshakeMessage(provider);
            var result = await SendHandshakeAsync(provider, apiKey, handshakeMessage, cancellationToken);

            lock (_lock)
            {
                _connections[provider] = new ConnectionStatus
                {
                    IsConnected = result.IsSuccess,
                    LastHandshakeTime = DateTime.UtcNow,
                    LastMessage = result.Message,
                    HandshakeId = result.HandshakeId
                };
            }

            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs
            {
                Provider = provider,
                IsConnected = result.IsSuccess,
                Message = result.Message,
                Timestamp = DateTime.UtcNow
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handshake failed for provider: {Provider}", provider);
            var errorResult = new HandshakeResult
            {
                IsSuccess = false,
                Message = $"Handshake failed: {ex.Message}",
                HandshakeId = string.Empty
            };

            UpdateConnectionStatus(provider, errorResult.Message, false);
            return errorResult;
        }
    }

    private async Task<HandshakeResult> SendHandshakeAsync(string provider, string apiKey, string handshakeMessage, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(provider.ToLowerInvariant());

        return provider.ToLowerInvariant() switch
        {
            "huggingface" => await SendHuggingFaceHandshakeAsync(client, apiKey, handshakeMessage, cancellationToken),
            "openai" => await SendOpenAIHandshakeAsync(client, apiKey, handshakeMessage, cancellationToken),
            "anthropic" => await SendAnthropicHandshakeAsync(client, apiKey, handshakeMessage, cancellationToken),
            _ => await SendGenericHandshakeAsync(client, apiKey, handshakeMessage, cancellationToken)
        };
    }

    private async Task<HandshakeResult> SendHuggingFaceHandshakeAsync(HttpClient client, string apiKey, string handshakeMessage, CancellationToken cancellationToken)
    {
        var payload = new
        {
            inputs = handshakeMessage,
            parameters = new { max_new_tokens = 50, temperature = 0.1 }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "models/microsoft/DialoGPT-medium")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var handshakeId = $"hf_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
            return new HandshakeResult
            {
                IsSuccess = true,
                Message = $"HuggingFace connection established. Response: {content}",
                HandshakeId = handshakeId
            };
        }

        return new HandshakeResult
        {
            IsSuccess = false,
            Message = $"HuggingFace handshake failed: {response.StatusCode} - {content}",
            HandshakeId = string.Empty
        };
    }

    private async Task<HandshakeResult> SendOpenAIHandshakeAsync(HttpClient client, string apiKey, string handshakeMessage, CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = "gpt-3.5-turbo",
            messages = new[] { new { role = "user", content = handshakeMessage } },
            max_tokens = 50
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var handshakeId = $"oai_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
            return new HandshakeResult
            {
                IsSuccess = true,
                Message = $"OpenAI connection established. Response: {content}",
                HandshakeId = handshakeId
            };
        }

        return new HandshakeResult
        {
            IsSuccess = false,
            Message = $"OpenAI handshake failed: {response.StatusCode} - {content}",
            HandshakeId = string.Empty
        };
    }

    private async Task<HandshakeResult> SendAnthropicHandshakeAsync(HttpClient client, string apiKey, string handshakeMessage, CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = "claude-3-haiku-20240307",
            max_tokens = 50,
            messages = new[] { new { role = "user", content = handshakeMessage } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var handshakeId = $"ant_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
            return new HandshakeResult
            {
                IsSuccess = true,
                Message = $"Anthropic connection established. Response: {content}",
                HandshakeId = handshakeId
            };
        }

        return new HandshakeResult
        {
            IsSuccess = false,
            Message = $"Anthropic handshake failed: {response.StatusCode} - {content}",
            HandshakeId = string.Empty
        };
    }

    private async Task<HandshakeResult> SendGenericHandshakeAsync(HttpClient client, string apiKey, string handshakeMessage, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken); // Simulate network delay
        var handshakeId = $"gen_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";

        return new HandshakeResult
        {
            IsSuccess = true,
            Message = $"Generic connection established for handshake: {handshakeMessage}",
            HandshakeId = handshakeId
        };
    }

    private string GenerateHandshakeMessage(string provider)
    {
        return "hello";
    }

    public bool IsConnected(string provider)
    {
        lock (_lock)
        {
            return _connections.TryGetValue(provider, out var status) && status.IsConnected;
        }
    }

    public DateTime? GetLastHandshakeTime(string provider)
    {
        lock (_lock)
        {
            return _connections.TryGetValue(provider, out var status) ? status.LastHandshakeTime : null;
        }
    }

    public void UpdateConnectionStatus(string provider, string message, bool isConnected)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(provider, out var status))
            {
                status = new ConnectionStatus();
                _connections[provider] = status;
            }

            status.IsConnected = isConnected;
            status.LastMessage = message;
            if (isConnected)
            {
                status.LastHandshakeTime = DateTime.UtcNow;
            }
        }

        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs
        {
            Provider = provider,
            IsConnected = isConnected,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }
}

public sealed class HandshakeResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string HandshakeId { get; set; } = string.Empty;
}

public sealed class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public DateTime? LastHandshakeTime { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public string HandshakeId { get; set; } = string.Empty;
}

public sealed class ConnectionStatusEventArgs : EventArgs
{
    public string Provider { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}