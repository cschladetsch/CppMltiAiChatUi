namespace MultipleAIApp.ViewModels;

public sealed partial class ChatMessageViewModel : ObservableObject
{
    private string debugInfo = string.Empty;
    private bool showDebugInfo = false;

    public ChatMessageViewModel(ChatRole role, string content, bool isStreaming = false)
    {
        Role = role;
        this.content = content;
        this.isStreaming = isStreaming;
        MessageId = Guid.NewGuid().ToString("N")[..8];
        Timestamp = DateTime.UtcNow;

        UpdateDebugInfo();
        ToggleDebugCommand = new RelayCommand(ToggleDebugInfo);
    }

    public ChatRole Role { get; }
    public string MessageId { get; }
    public DateTime Timestamp { get; }

    private string content;
    private string provider = string.Empty;
    private string connectionId = string.Empty;

    public string Content
    {
        get => content;
        set
        {
            if (SetProperty(ref content, value))
                UpdateDebugInfo();
        }
    }

    public string Provider
    {
        get => provider;
        set
        {
            if (SetProperty(ref provider, value))
                UpdateDebugInfo();
        }
    }

    public string ConnectionId
    {
        get => connectionId;
        set
        {
            if (SetProperty(ref connectionId, value))
                UpdateDebugInfo();
        }
    }

    private bool isStreaming;

    public bool IsStreaming
    {
        get => isStreaming;
        set => SetProperty(ref isStreaming, value);
    }

    public bool ShowDebugInfo
    {
        get => showDebugInfo;
        set => SetProperty(ref showDebugInfo, value);
    }

    public string DebugInfo
    {
        get => debugInfo;
        private set => SetProperty(ref debugInfo, value);
    }

    public string TimestampDisplay => Timestamp.ToString("HH:mm:ss");
    public string EntireString => $"[{TimestampDisplay}] {Provider} ({MessageId}): {Content}";

    public IRelayCommand ToggleDebugCommand { get; }

    private void UpdateDebugInfo()
    {
        var neString = Content?.Replace("\n", "\\n").Replace("\r", "\\r") ?? string.Empty;
        DebugInfo = $"This: {MessageId}\nNe_String: {neString}\nEntire_String: {EntireString}";
    }

    private void ToggleDebugInfo()
    {
        ShowDebugInfo = !ShowDebugInfo;
    }

    public ChatMessage ToMessage() => new(Role, Content);

    public EnhancedChatMessage ToEnhancedMessage() => new(Role, Content, MessageId, Timestamp)
    {
        Provider = Provider,
        ConnectionId = ConnectionId,
        ProcessedString = Content?.Replace("\n", "\\n").Replace("\r", "\\r") ?? string.Empty
    };
}
