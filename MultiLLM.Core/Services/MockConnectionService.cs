using Microsoft.Extensions.Logging;
using MultiLLM.Core.Interfaces;
using MultiLLM.Core.Models;

namespace MultiLLM.Core.Services;

public sealed class MockConnectionService : IConnectionService
{
    private readonly ILogger<MockConnectionService> _logger;
    private readonly Dictionary<string, ConnectionStatus> _connections = new();

    public MockConnectionService(ILogger<MockConnectionService> logger)
    {
        _logger = logger;
    }

    public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

    public async Task<HandshakeResult> PerformHandshakeAsync(string provider, string apiKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing handshake for provider: {Provider}", provider);

        // Simulate handshake delay
        await Task.Delay(100, cancellationToken);

        // For demo purposes, assume all handshakes succeed if API key is provided
        var isSuccess = !string.IsNullOrWhiteSpace(apiKey);
        var message = isSuccess ? "Connection established successfully" : "Invalid or missing API key";

        UpdateConnectionStatus(provider, message, isSuccess);

        return new HandshakeResult
        {
            IsSuccess = isSuccess,
            Message = message,
            HandshakeId = Guid.NewGuid().ToString()
        };
    }

    public bool IsConnected(string provider)
    {
        return _connections.TryGetValue(provider.ToLowerInvariant(), out var status) && status.IsConnected;
    }

    public DateTime? GetLastHandshakeTime(string provider)
    {
        return _connections.TryGetValue(provider.ToLowerInvariant(), out var status)
            ? status.LastHandshakeTime
            : null;
    }

    public void UpdateConnectionStatus(string provider, string message, bool isConnected)
    {
        var key = provider.ToLowerInvariant();
        _connections[key] = new ConnectionStatus
        {
            IsConnected = isConnected,
            LastHandshakeTime = DateTime.UtcNow,
            Message = message
        };

        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs
        {
            Provider = provider,
            IsConnected = isConnected,
            Message = message,
            Timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Connection status updated for {Provider}: {Status} - {Message}",
            provider, isConnected ? "Connected" : "Disconnected", message);
    }
}