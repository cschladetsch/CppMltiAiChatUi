using Microsoft.Extensions.Logging;
using MultipleAIApp.Models;

namespace MultipleAIApp.Services;

public interface IChatCompletionServiceFactory
{
    IChatCompletionService GetService(string provider);
}

public sealed class ChatCompletionServiceFactory : IChatCompletionServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatCompletionServiceFactory> _logger;

    public ChatCompletionServiceFactory(IServiceProvider serviceProvider, ILogger<ChatCompletionServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IChatCompletionService GetService(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "huggingface" => _serviceProvider.GetService<HuggingFaceChatCompletionService>() ?? throw new InvalidOperationException("HuggingFace service not registered"),
            "openai" => _serviceProvider.GetService<OpenAIChatCompletionService>() ?? throw new InvalidOperationException("OpenAI service not registered"),
            "anthropic" => _serviceProvider.GetService<AnthropicChatCompletionService>() ?? throw new InvalidOperationException("Anthropic service not registered"),
            _ => throw new NotSupportedException($"Provider '{provider}' is not supported")
        };
    }
}