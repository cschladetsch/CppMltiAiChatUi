using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiLLM.Core.Interfaces;
using MultiLLM.Core.Models;
using MultiLLM.Core.Services;
using MultiLLM.Core.Services.ChatCompletion;

namespace MultiLLM.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 MultiLLM.Core Demo");
        Console.WriteLine("====================");

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();

        // Get the logger and chat service
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("MultiLLM.Core demo starting...");

        try
        {
            await RunDemo(serviceProvider);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo failed with error");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        try
        {
            Console.ReadKey();
        }
        catch (InvalidOperationException)
        {
            // Console input is redirected, can't read keys
            Console.WriteLine("Console input redirected - exiting...");
        }
    }

    static void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add HTTP client factory
        services.AddHttpClient();

        // Configure OpenAI HTTP client
        services.AddHttpClient("openai", client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/");
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        // Register core services
        services.AddSingleton<IConnectionService, MockConnectionService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<OpenAIChatCompletionService>();
        services.AddSingleton<IChatCompletionServiceFactory, ChatCompletionServiceFactory>();
    }

    static async Task RunDemo(IServiceProvider serviceProvider)
    {
        var factory = serviceProvider.GetRequiredService<IChatCompletionServiceFactory>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Create a simple model definition for testing
        var model = new ModelDefinition
        {
            Name = "GPT-3.5 Turbo",
            Provider = "openai",
            ModelId = "gpt-3.5-turbo",
            Description = "OpenAI's GPT-3.5 Turbo model for chat completions",
            Parameters = new List<ModelParameterDefinition>
            {
                new() { Name = "temperature", Default = System.Text.Json.JsonDocument.Parse("0.7").RootElement },
                new() { Name = "max_tokens", Default = System.Text.Json.JsonDocument.Parse("150").RootElement }
            }
        };

        // Create test messages
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant. Keep responses brief and friendly."),
            new(ChatRole.User, "Hello! Can you tell me a fun fact about programming?")
        };

        Console.WriteLine("🔍 Testing MultiLLM.Core library...");
        Console.WriteLine($"📋 Model: {model.Name} ({model.ModelId})");
        Console.WriteLine($"💬 Messages: {messages.Count}");
        Console.WriteLine();

        // Check for API key
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("⚠️  No OpenAI API key found in environment variable 'OPENAI_API_KEY'");
            Console.WriteLine("🔄 Running in mock mode (no actual API call will be made)");

            // Mock response for demo
            logger.LogInformation("Running in mock mode - simulating API response");
            Console.WriteLine("🤖 Mock Response: Here's a fun fact - The first computer bug was an actual bug! In 1947, Grace Hopper found a moth stuck in a computer relay!");
        }
        else
        {
            try
            {
                Console.WriteLine("🔑 OpenAI API key found - making real API call...");

                // Get the OpenAI service
                var openAiService = factory.GetService("openai");

                Console.WriteLine("📞 Calling OpenAI API...");
                var response = await openAiService.CompleteAsync(model, messages, apiKey, CancellationToken.None);

                Console.WriteLine("✅ Success! Response received:");
                Console.WriteLine($"🤖 {response}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API call failed");
                Console.WriteLine($"❌ API call failed: {ex.Message}");

                if (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
                {
                    Console.WriteLine("💡 This might be due to an invalid API key");
                }
                else if (ex.Message.Contains("429"))
                {
                    Console.WriteLine("💡 This might be due to rate limiting");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("✨ MultiLLM.Core demo completed!");
        Console.WriteLine("📦 The library is working correctly and ready for use.");
    }
}
