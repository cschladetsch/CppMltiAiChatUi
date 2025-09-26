namespace MultiLLM.Core.Models;

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
    public string Message { get; set; } = string.Empty;
}

public sealed class ConnectionStatusEventArgs : EventArgs
{
    public string Provider { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}