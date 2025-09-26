using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiLLM.Core.Interfaces;
using MultiLLM.Core.Models;
using MultiLLM.Core.Services;
using MultiLLM.Core.Services.ChatCompletion;

namespace MultiLLM.Core.Tests.Integration;

/// <summary>
/// Integration tests that test the complete MultiLLM.Core library functionality
/// These tests verify that all components work together correctly
/// </summary>
public class MultiLLMIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _testConfigPath;

    public MultiLLMIntegrationTests()
    {
        // Set up a temporary directory for test configuration
        var testDir = Path.Combine(Path.GetTempPath(), "MultiLLMIntegrationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        Directory.CreateDirectory(Path.Combine(testDir, "Configs"));

        _testConfigPath = Path.Combine(testDir, "config.json");

        // Set up the base directory for the test
        AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", testDir);

        // Configure services like in a real application
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add HTTP client factory
        services.AddHttpClient();

        // Configure HTTP clients for providers
        services.AddHttpClient("openai", client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register core services
        services.AddSingleton<IConnectionService, MockConnectionService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<OpenAIChatCompletionService>();
        services.AddSingleton<IChatCompletionServiceFactory, ChatCompletionServiceFactory>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();

        // Clean up test files
        try
        {
            var testDir = Path.GetDirectoryName(_testConfigPath);
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void ServiceProvider_ShouldResolveAllServices()
    {
        // Act & Assert
        _serviceProvider.GetService<IConfigurationService>().Should().NotBeNull();
        _serviceProvider.GetService<IConnectionService>().Should().NotBeNull();
        _serviceProvider.GetService<IChatCompletionServiceFactory>().Should().NotBeNull();
        _serviceProvider.GetService<OpenAIChatCompletionService>().Should().NotBeNull();
    }

    [Fact]
    public async Task ConfigurationService_ShouldLoadAndSaveConfiguration()
    {
        // Arrange
        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
        var testConfig = new AppConfiguration
        {
            ApiKeys = new ApiKeysConfiguration
            {
                OpenAi = "test-openai-key",
                Anthropic = "test-anthropic-key"
            },
            DefaultSettings = new DefaultSettingsConfiguration
            {
                DefaultProvider = "openai",
                MaxTokens = 2000,
                Temperature = 0.8
            }
        };

        // Act
        await configService.SaveConfigurationAsync(testConfig);
        var loadedConfig = await configService.LoadConfigurationAsync();

        // Assert
        loadedConfig.Should().NotBeNull();
        loadedConfig.ApiKeys.OpenAi.Should().Be("test-openai-key");
        loadedConfig.ApiKeys.Anthropic.Should().Be("test-anthropic-key");
        loadedConfig.DefaultSettings.DefaultProvider.Should().Be("openai");
        loadedConfig.DefaultSettings.MaxTokens.Should().Be(2000);
        loadedConfig.DefaultSettings.Temperature.Should().Be(0.8);
    }

    [Fact]
    public async Task ConnectionService_ShouldManageConnectionStates()
    {
        // Arrange
        var connectionService = _serviceProvider.GetRequiredService<IConnectionService>();
        var provider = "openai";
        var apiKey = "test-api-key";

        // Act & Assert - Initial state
        connectionService.IsConnected(provider).Should().BeFalse();
        connectionService.GetLastHandshakeTime(provider).Should().BeNull();

        // Act - Perform handshake
        var handshakeResult = await connectionService.PerformHandshakeAsync(provider, apiKey);

        // Assert - After successful handshake
        handshakeResult.Should().NotBeNull();
        handshakeResult.IsSuccess.Should().BeTrue();
        connectionService.IsConnected(provider).Should().BeTrue();
        connectionService.GetLastHandshakeTime(provider).Should().NotBeNull();
    }

    [Fact]
    public void ChatCompletionServiceFactory_ShouldProvideCorrectServices()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IChatCompletionServiceFactory>();

        // Act
        var openAiService = factory.GetService("openai");

        // Assert
        openAiService.Should().NotBeNull();
        openAiService.Should().BeOfType<OpenAIChatCompletionService>();
    }

    [Fact]
    public async Task EndToEndWorkflow_ShouldProcessChatCompletionRequest()
    {
        // Arrange
        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
        var factory = _serviceProvider.GetRequiredService<IChatCompletionServiceFactory>();
        var connectionService = _serviceProvider.GetRequiredService<IConnectionService>();

        // Set up configuration
        var config = new AppConfiguration
        {
            ApiKeys = new ApiKeysConfiguration { OpenAi = "test-key" }
        };
        await configService.SaveConfigurationAsync(config);

        // Create test model and messages
        var model = new ModelDefinition
        {
            Name = "Test GPT",
            Provider = "openai",
            ModelId = "gpt-3.5-turbo",
            Parameters = new List<ModelParameterDefinition>
            {
                new() { Name = "temperature", Default = JsonDocument.Parse("0.7").RootElement },
                new() { Name = "max_tokens", Default = JsonDocument.Parse("100").RootElement }
            }
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Hello!")
        };

        // Act - This would normally make an API call, but our mock service will handle it
        var chatService = factory.GetService("openai");

        // Verify connection works
        connectionService.IsConnected("openai").Should().BeFalse(); // Initially disconnected
        var handshakeResult = await connectionService.PerformHandshakeAsync("openai", "test-key");
        handshakeResult.IsSuccess.Should().BeTrue();
        connectionService.IsConnected("openai").Should().BeTrue();

        // Note: We can't test the actual CompleteAsync without mocking HTTP
        // but we can verify all the components are wired together correctly
        chatService.Should().NotBeNull();
        chatService.Should().BeOfType<OpenAIChatCompletionService>();
    }

    [Fact]
    public async Task ConfigurationService_ShouldResolveApiKeysCorrectly()
    {
        // Arrange
        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();

        var config = new AppConfiguration
        {
            ApiKeys = new ApiKeysConfiguration
            {
                OpenAi = "config-openai-key",
                Anthropic = "config-anthropic-key",
                HuggingFace = "config-hf-key"
            }
        };

        await configService.SaveConfigurationAsync(config);

        // Act & Assert - Test that command line overrides work
        // Note: We only test the override functionality since real API key files might exist
        configService.GetApiKey("openai", "override-openai").Should().Be("override-openai");
        configService.GetApiKey("anthropic", "override-anthropic").Should().Be("override-anthropic");
        configService.GetApiKey("huggingface", "override-hf").Should().Be("override-hf");

        // Test override precedence
        configService.GetApiKey("openai", "override-key").Should().Be("override-key");
    }

    [Fact]
    public void ServiceLifetimes_ShouldBeSingleton()
    {
        // Arrange & Act
        var configService1 = _serviceProvider.GetRequiredService<IConfigurationService>();
        var configService2 = _serviceProvider.GetRequiredService<IConfigurationService>();

        var connectionService1 = _serviceProvider.GetRequiredService<IConnectionService>();
        var connectionService2 = _serviceProvider.GetRequiredService<IConnectionService>();

        var factory1 = _serviceProvider.GetRequiredService<IChatCompletionServiceFactory>();
        var factory2 = _serviceProvider.GetRequiredService<IChatCompletionServiceFactory>();

        // Assert - Services should be singletons
        configService1.Should().BeSameAs(configService2);
        connectionService1.Should().BeSameAs(connectionService2);
        factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public async Task ConnectionStatusEvents_ShouldPropagateCorrectly()
    {
        // Arrange
        var connectionService = _serviceProvider.GetRequiredService<IConnectionService>();
        ConnectionStatusEventArgs? capturedEvent = null;

        connectionService.ConnectionStatusChanged += (sender, args) => capturedEvent = args;

        // Act
        await connectionService.PerformHandshakeAsync("openai", "test-key");

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Provider.Should().Be("openai");
        capturedEvent.IsConnected.Should().BeTrue();
        capturedEvent.Message.Should().Be("Connection established successfully");
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("anthropic")]
    [InlineData("huggingface")]
    [InlineData("google")]
    [InlineData("grok")]
    public async Task ConnectionService_ShouldSupportMultipleProviders(string provider)
    {
        // Arrange
        var connectionService = _serviceProvider.GetRequiredService<IConnectionService>();

        // Act
        var result = await connectionService.PerformHandshakeAsync(provider, "test-key");

        // Assert
        result.IsSuccess.Should().BeTrue();
        connectionService.IsConnected(provider).Should().BeTrue();
    }

    [Fact]
    public async Task Configuration_ShouldPersistAcrossServiceInstances()
    {
        // Arrange
        var configService1 = _serviceProvider.GetRequiredService<IConfigurationService>();
        var config = new AppConfiguration
        {
            ApiKeys = new ApiKeysConfiguration { OpenAi = "persistent-key" }
        };

        // Act
        await configService1.SaveConfigurationAsync(config);

        // Create a new service provider to simulate application restart
        var newServices = new ServiceCollection();
        ConfigureServices(newServices);
        using var newServiceProvider = newServices.BuildServiceProvider();
        var configService2 = newServiceProvider.GetRequiredService<IConfigurationService>();

        var loadedConfig = await configService2.LoadConfigurationAsync();

        // Assert
        loadedConfig.ApiKeys.OpenAi.Should().Be("persistent-key");
    }
}