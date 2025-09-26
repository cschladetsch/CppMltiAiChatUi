using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MultipleAIApp.Services;

public interface IConfigurationService
{
    AppConfiguration GetConfiguration();
    Task<AppConfiguration> LoadConfigurationAsync();
    Task SaveConfigurationAsync(AppConfiguration configuration);
    string? GetApiKey(string provider, string? commandLineOverride = null);
}

public sealed class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configPath;
    private readonly string _exampleConfigPath;
    private AppConfiguration? _configuration;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        _exampleConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.example.json");
    }

    public AppConfiguration GetConfiguration()
    {
        if (_configuration == null)
        {
            _configuration = LoadConfigurationAsync().GetAwaiter().GetResult();
        }
        return _configuration;
    }

    public async Task<AppConfiguration> LoadConfigurationAsync()
    {
        try
        {
            // Check if config.json exists
            if (!File.Exists(_configPath))
            {
                _logger.LogWarning("config.json not found. Please copy config.example.json to config.json and update with your API keys.");

                // Try to create from example if it exists
                if (File.Exists(_exampleConfigPath))
                {
                    _logger.LogInformation("Creating config.json from config.example.json");
                    File.Copy(_exampleConfigPath, _configPath, overwrite: false);
                }
                else
                {
                    _logger.LogError("config.example.json not found. Using default configuration.");
                    _configuration = new AppConfiguration();
                    return _configuration;
                }
            }

            // Load configuration from file
            var jsonString = await File.ReadAllTextAsync(_configPath);
            _configuration = JsonSerializer.Deserialize<AppConfiguration>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AppConfiguration();

            _logger.LogInformation("Configuration loaded successfully from config.json");

            // Merge with existing models.json if needed
            await MergeWithModelsJsonAsync();

            return _configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            _configuration = new AppConfiguration();
            return _configuration;
        }
    }

    private async Task MergeWithModelsJsonAsync()
    {
        try
        {
            var modelsJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "models.json");
            if (File.Exists(modelsJsonPath))
            {
                var modelsJson = await File.ReadAllTextAsync(modelsJsonPath);
                var modelsConfig = JsonSerializer.Deserialize<ModelCatalogConfiguration>(modelsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (modelsConfig != null)
                {
                    // If config.json doesn't have models, use models.json
                    if (_configuration!.Models.AvailableModels.Count == 0)
                    {
                        _configuration.Models.AvailableModels = modelsConfig.Models.ToList();
                        _configuration.Models.Summary = modelsConfig.Summary;
                        _logger.LogInformation("Merged model definitions from models.json");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not merge models.json configuration");
        }
    }

    public async Task SaveConfigurationAsync(AppConfiguration configuration)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            var jsonString = JsonSerializer.Serialize(configuration, options);
            await File.WriteAllTextAsync(_configPath, jsonString);

            _configuration = configuration;
            _logger.LogInformation("Configuration saved successfully to config.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            throw;
        }
    }

    public string? GetApiKey(string provider, string? commandLineOverride = null)
    {
        // Priority order:
        // 1. Command-line argument / UI override
        // 2. ~/.KEY_NAME file
        // 3. config.json
        // 4. Environment variable

        if (!string.IsNullOrWhiteSpace(commandLineOverride))
        {
            _logger.LogDebug("Using command-line/UI override for {Provider} API key", provider);
            return commandLineOverride;
        }

        var config = GetConfiguration();
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return provider.ToLowerInvariant() switch
        {
            "huggingface" =>
                ReadKeyFile(Path.Combine(homeDir, ".HUGGINGFACE_API_KEY")) ??
                config.ApiKeys.HuggingFace ??
                Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY"),

            "openai" =>
                ReadKeyFile(Path.Combine(homeDir, ".OPENAI_API_KEY")) ??
                config.ApiKeys.OpenAi ??
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"),

            "anthropic" =>
                ReadKeyFile(Path.Combine(homeDir, ".CLAUDE_API_KEY")) ??
                ReadKeyFile(Path.Combine(homeDir, ".ANTHROPIC_API_KEY")) ??
                config.ApiKeys.Anthropic ??
                Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ??
                Environment.GetEnvironmentVariable("CLAUDE_API_KEY"),

            "google" =>
                ReadKeyFile(Path.Combine(homeDir, ".GOOGLE_API_KEY")) ??
                config.ApiKeys.Google ??
                Environment.GetEnvironmentVariable("GOOGLE_API_KEY"),

            "azure" =>
                ReadKeyFile(Path.Combine(homeDir, ".AZURE_API_KEY")) ??
                config.ApiKeys.Azure?.ApiKey ??
                Environment.GetEnvironmentVariable("AZURE_API_KEY"),

            "grok" =>
                ReadKeyFile(Path.Combine(homeDir, ".GROK_API_KEY")) ??
                config.ApiKeys.Grok ??
                Environment.GetEnvironmentVariable("GROK_API_KEY"),

            _ => null
        };
    }

    private string? ReadKeyFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var key = File.ReadAllText(filePath).Trim();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogDebug("Loaded API key from {FilePath}", filePath);
                    return key;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read key file {FilePath}", filePath);
        }
        return null;
    }
}