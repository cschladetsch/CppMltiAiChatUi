using System.Text.Json;
using MultiLLM.Core.Models;

namespace MultiLLM.Core.Tests.Fixtures;

/// <summary>
/// Provides common test data and fixtures for unit tests
/// </summary>
public static class TestFixtures
{
    public static class Models
    {
        public static ModelDefinition CreateOpenAIModel(string? name = null, string? modelId = null)
        {
            return new ModelDefinition
            {
                Name = name ?? "Test GPT-3.5",
                Provider = "openai",
                ModelId = modelId ?? "gpt-3.5-turbo",
                Description = "Test OpenAI model for unit tests",
                Parameters = new List<ModelParameterDefinition>
                {
                    new()
                    {
                        Name = "temperature",
                        Description = "Controls randomness",
                        Default = JsonDocument.Parse("0.7").RootElement
                    },
                    new()
                    {
                        Name = "max_tokens",
                        Description = "Maximum tokens to generate",
                        Default = JsonDocument.Parse("1000").RootElement
                    },
                    new()
                    {
                        Name = "top_p",
                        Description = "Nucleus sampling parameter",
                        Default = JsonDocument.Parse("1.0").RootElement
                    }
                }
            };
        }

        public static ModelDefinition CreateAnthropicModel(string? name = null, string? modelId = null)
        {
            return new ModelDefinition
            {
                Name = name ?? "Test Claude",
                Provider = "anthropic",
                ModelId = modelId ?? "claude-3-haiku-20240307",
                Description = "Test Anthropic model for unit tests",
                Parameters = new List<ModelParameterDefinition>
                {
                    new()
                    {
                        Name = "temperature",
                        Description = "Controls randomness",
                        Default = JsonDocument.Parse("0.7").RootElement
                    },
                    new()
                    {
                        Name = "max_tokens",
                        Description = "Maximum tokens to generate",
                        Default = JsonDocument.Parse("1000").RootElement
                    }
                }
            };
        }

        public static ModelDefinition CreateHuggingFaceModel(string? name = null, string? modelId = null)
        {
            return new ModelDefinition
            {
                Name = name ?? "Test Llama",
                Provider = "huggingface",
                ModelId = modelId ?? "meta-llama/Llama-2-7b-chat-hf",
                Description = "Test HuggingFace model for unit tests",
                Parameters = new List<ModelParameterDefinition>
                {
                    new()
                    {
                        Name = "temperature",
                        Description = "Controls randomness",
                        Default = JsonDocument.Parse("0.6").RootElement
                    },
                    new()
                    {
                        Name = "max_new_tokens",
                        Description = "Maximum new tokens to generate",
                        Default = JsonDocument.Parse("512").RootElement
                    },
                    new()
                    {
                        Name = "top_p",
                        Description = "Nucleus sampling parameter",
                        Default = JsonDocument.Parse("0.9").RootElement
                    }
                }
            };
        }

        public static ModelDefinition CreateMinimalModel(string provider = "test")
        {
            return new ModelDefinition
            {
                Name = "Minimal Test Model",
                Provider = provider,
                ModelId = "test-model"
            };
        }

        public static List<ModelDefinition> CreateSampleModels()
        {
            return new List<ModelDefinition>
            {
                CreateOpenAIModel(),
                CreateAnthropicModel(),
                CreateHuggingFaceModel()
            };
        }
    }

    public static class Messages
    {
        public static List<ChatMessage> CreateBasicConversation()
        {
            return new List<ChatMessage>
            {
                new(ChatRole.System, "You are a helpful assistant."),
                new(ChatRole.User, "Hello, how are you?"),
                new(ChatRole.Assistant, "I'm doing well, thank you for asking! How can I help you today?"),
                new(ChatRole.User, "Can you explain what machine learning is?")
            };
        }

        public static List<ChatMessage> CreateSystemOnlyMessages()
        {
            return new List<ChatMessage>
            {
                new(ChatRole.System, "You are a specialized coding assistant.")
            };
        }

        public static List<ChatMessage> CreateLongConversation(int messageCount = 10)
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "You are a helpful assistant.")
            };

            for (int i = 0; i < messageCount; i++)
            {
                var role = i % 2 == 0 ? ChatRole.User : ChatRole.Assistant;
                var content = role == ChatRole.User
                    ? $"User message number {i + 1}"
                    : $"Assistant response number {i + 1}";

                messages.Add(new ChatMessage(role, content));
            }

            return messages;
        }

        public static List<ChatMessage> CreateEmptyMessages()
        {
            return new List<ChatMessage>();
        }

        public static List<ChatMessage> CreateMessagesWithSpecialCharacters()
        {
            return new List<ChatMessage>
            {
                new(ChatRole.System, "You are a helpful assistant that handles special characters."),
                new(ChatRole.User, "Hello! Can you help me with these symbols: @#$%^&*()_+-=[]{}|;:,.<>?"),
                new(ChatRole.User, "And some unicode: 🚀 Hello, 世界! 🌟"),
                new(ChatRole.User, "JSON-like content: {\"key\": \"value\", \"number\": 42}")
            };
        }
    }

    public static class Configurations
    {
        public static AppConfiguration CreateBasicConfiguration()
        {
            return new AppConfiguration
            {
                ApiKeys = new ApiKeysConfiguration
                {
                    OpenAi = "test-openai-key",
                    Anthropic = "test-anthropic-key",
                    HuggingFace = "test-hf-key",
                    Google = "test-google-key",
                    Grok = "test-grok-key",
                    Azure = new AzureConfiguration
                    {
                        Endpoint = "https://test.openai.azure.com/",
                        ApiKey = "test-azure-key"
                    }
                },
                DefaultSettings = new DefaultSettingsConfiguration
                {
                    DefaultProvider = "openai",
                    MaxTokens = 1000,
                    Temperature = 0.7,
                    Timeout = 30000,
                    RetryAttempts = 3,
                    RetryDelay = 1000
                },
                Logging = new LoggingConfiguration
                {
                    Level = "Information",
                    EnableFileLogging = false,
                    LogFilePath = "logs/test.log"
                },
                Ui = new UiConfiguration
                {
                    Theme = "light",
                    MaxChatHistory = 100,
                    AutoSaveInterval = 60000
                },
                Connection = new ConnectionConfiguration
                {
                    EnableHandshake = true,
                    HandshakeTimeout = 10000,
                    AutoHandshakeOnStartup = true,
                    ShowConnectionStatus = true,
                    HandshakeRetryAttempts = 3,
                    HandshakeRetryDelay = 2000
                },
                Models = new ModelsConfiguration
                {
                    Summary = new SummaryConfiguration
                    {
                        SystemPrompt = "Summarize the conversation in bullet points.",
                        ModelId = "gpt-3.5-turbo"
                    },
                    AvailableModels = Models.CreateSampleModels()
                }
            };
        }

        public static AppConfiguration CreateEmptyConfiguration()
        {
            return new AppConfiguration();
        }

        public static AppConfiguration CreateConfigurationWithOnlyApiKeys()
        {
            return new AppConfiguration
            {
                ApiKeys = new ApiKeysConfiguration
                {
                    OpenAi = "only-openai-key"
                }
            };
        }

        public static ModelCatalogConfiguration CreateModelCatalog()
        {
            return new ModelCatalogConfiguration
            {
                Summary = new SummaryConfiguration
                {
                    SystemPrompt = "Test summary prompt",
                    ModelId = "gpt-3.5-turbo"
                },
                Models = Models.CreateSampleModels()
            };
        }
    }

    public static class ConnectionEvents
    {
        public static ConnectionStatusEventArgs CreateSuccessEvent(string provider = "openai")
        {
            return new ConnectionStatusEventArgs
            {
                Provider = provider,
                IsConnected = true,
                Message = "Connection established successfully",
                Timestamp = DateTime.UtcNow
            };
        }

        public static ConnectionStatusEventArgs CreateFailureEvent(string provider = "openai", string? message = null)
        {
            return new ConnectionStatusEventArgs
            {
                Provider = provider,
                IsConnected = false,
                Message = message ?? "Connection failed",
                Timestamp = DateTime.UtcNow
            };
        }

        public static HandshakeResult CreateSuccessHandshake()
        {
            return new HandshakeResult
            {
                IsSuccess = true,
                Message = "Handshake successful",
                HandshakeId = Guid.NewGuid().ToString()
            };
        }

        public static HandshakeResult CreateFailedHandshake(string? message = null)
        {
            return new HandshakeResult
            {
                IsSuccess = false,
                Message = message ?? "Handshake failed",
                HandshakeId = Guid.NewGuid().ToString()
            };
        }
    }

    public static class ApiResponses
    {
        public static string CreateOpenAISuccessResponse(string content = "Test response from OpenAI")
        {
            var response = new
            {
                id = "chatcmpl-test",
                @object = "chat.completion",
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                model = "gpt-3.5-turbo",
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new
                        {
                            role = "assistant",
                            content = content
                        },
                        finish_reason = "stop"
                    }
                },
                usage = new
                {
                    prompt_tokens = 10,
                    completion_tokens = 20,
                    total_tokens = 30
                }
            };

            return JsonSerializer.Serialize(response);
        }

        public static string CreateOpenAIErrorResponse(string error = "Invalid API key")
        {
            var response = new
            {
                error = new
                {
                    message = error,
                    type = "invalid_request_error",
                    code = "invalid_api_key"
                }
            };

            return JsonSerializer.Serialize(response);
        }

        public static string CreateAnthropicSuccessResponse(string content = "Test response from Claude")
        {
            var response = new
            {
                id = "msg_test",
                type = "message",
                role = "assistant",
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = content
                    }
                },
                model = "claude-3-haiku-20240307",
                stop_reason = "end_turn",
                stop_sequence = null as string,
                usage = new
                {
                    input_tokens = 10,
                    output_tokens = 20
                }
            };

            return JsonSerializer.Serialize(response);
        }
    }

    public static class FileSystem
    {
        public static string CreateTempDirectory(string prefix = "MultiLLMTest")
        {
            var tempDir = Path.Combine(Path.GetTempPath(), prefix, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        public static void CleanupTempDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        public static async Task CreateConfigFile(string directory, AppConfiguration config)
        {
            var configPath = Path.Combine(directory, "config.json");
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(configPath, json);
        }

        public static async Task CreateModelsFile(string directory, ModelCatalogConfiguration catalog)
        {
            var configsDir = Path.Combine(directory, "Configs");
            Directory.CreateDirectory(configsDir);
            var modelsPath = Path.Combine(configsDir, "models.json");
            var json = JsonSerializer.Serialize(catalog, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(modelsPath, json);
        }
    }
}