namespace MultipleAIApp.Models;

public sealed class EnhancedChatMessage
{
    public EnhancedChatMessage(ChatRole role, string content)
    {
        Role = role;
        Content = content;
        Timestamp = DateTime.UtcNow;
        MessageId = Guid.NewGuid().ToString("N")[..8];
    }

    public EnhancedChatMessage(ChatRole role, string content, string messageId, DateTime timestamp)
    {
        Role = role;
        Content = content;
        MessageId = messageId;
        Timestamp = timestamp;
    }

    public ChatRole Role { get; init; }
    public string Content { get; init; } = string.Empty;
    public string MessageId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Provider { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public string OriginalString { get; set; } = string.Empty;
    public string ProcessedString { get; set; } = string.Empty;
    public string EntireString => $"[{Timestamp:HH:mm:ss}] {Provider} ({MessageId}): {Content}";

    public ChatMessage ToChatMessage() => new(Role, Content);

    public MessageDebugInfo GetDebugInfo() => new()
    {
        ThisMessage = this,
        NeString = ProcessedString,
        EntireString = EntireString
    };
}

public sealed class MessageDebugInfo
{
    public EnhancedChatMessage? ThisMessage { get; set; }
    public string NeString { get; set; } = string.Empty;
    public string EntireString { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"Debug Info:\n- This: {ThisMessage?.MessageId ?? "null"}\n- Ne_String: {NeString}\n- Entire_String: {EntireString}";
    }
}