using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiLLM.Core.Interfaces;
using MultiLLM.Core.Services.ChatCompletion;

namespace MultiLLM.Core.Services;

public sealed class ChatCompletionServiceFactory : IChatCompletionServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatCompletionServiceFactory> _logger;

    public ChatCompletionServiceFactory(IServiceProvider serviceProvider, ILogger<ChatCompletionServiceFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IChatCompletionService GetService(string provider)
    {
        if (provider == null)
        {
            throw new NotSupportedException($"Provider '{provider}' is not supported");
        }

        return provider.ToLowerInvariant() switch
        {
            "openai" => _serviceProvider.GetService<OpenAIChatCompletionService>() ?? throw new InvalidOperationException("OpenAI service not registered"),
            // Additional providers can be added here as they are implemented
            _ => throw new NotSupportedException($"Provider '{provider}' is not supported")
        };
    }
}