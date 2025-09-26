using MultiLLM.Core.Models;

namespace MultiLLM.Core.Interfaces;

public interface IChatCompletionService
{
    Task<string> CompleteAsync(ModelDefinition model, IReadOnlyList<ChatMessage> messages, string apiKey, CancellationToken cancellationToken);
}