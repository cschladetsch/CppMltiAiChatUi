using MultiLLM.Core.Models;

namespace MultiLLM.Core.Interfaces;

public interface IConnectionService
{
    Task<HandshakeResult> PerformHandshakeAsync(string provider, string apiKey, CancellationToken cancellationToken = default);
    bool IsConnected(string provider);
    DateTime? GetLastHandshakeTime(string provider);
    void UpdateConnectionStatus(string provider, string message, bool isConnected);
    event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
}