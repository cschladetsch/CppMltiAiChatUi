namespace MultipleAIApp.Models;

public sealed class AppConfiguration
{
    public ApiKeysConfiguration ApiKeys { get; set; } = new();
    public DefaultSettingsConfiguration DefaultSettings { get; set; } = new();
    public LoggingConfiguration Logging { get; set; } = new();
    public UiConfiguration Ui { get; set; } = new();
    public ConnectionConfiguration Connection { get; set; } = new();
    public ModelsConfiguration Models { get; set; } = new();
}

public sealed class ApiKeysConfiguration
{
    public string? HuggingFace { get; set; }
    public string? OpenAi { get; set; }
    public string? Anthropic { get; set; }
    public string? Google { get; set; }
    public string? Grok { get; set; }
    public AzureConfiguration? Azure { get; set; }
}

public sealed class AzureConfiguration
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
}

public sealed class DefaultSettingsConfiguration
{
    public string DefaultProvider { get; set; } = "huggingface";
    public int MaxTokens { get; set; } = 512;
    public double Temperature { get; set; } = 0.7;
    public int Timeout { get; set; } = 30000;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelay { get; set; } = 1000;
}

public sealed class LoggingConfiguration
{
    public string Level { get; set; } = "Information";
    public bool EnableFileLogging { get; set; } = false;
    public string? LogFilePath { get; set; }
}

public sealed class UiConfiguration
{
    public string Theme { get; set; } = "light";
    public int MaxChatHistory { get; set; } = 100;
    public int AutoSaveInterval { get; set; } = 60000;
}

public sealed class ConnectionConfiguration
{
    public bool EnableHandshake { get; set; } = true;
    public int HandshakeTimeout { get; set; } = 10000;
    public bool AutoHandshakeOnStartup { get; set; } = true;
    public bool ShowConnectionStatus { get; set; } = true;
    public int HandshakeRetryAttempts { get; set; } = 3;
    public int HandshakeRetryDelay { get; set; } = 2000;
}

public sealed class ModelsConfiguration
{
    public SummaryConfiguration Summary { get; set; } = new();
    public List<ModelDefinition> AvailableModels { get; set; } = new();
}