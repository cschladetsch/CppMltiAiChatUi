namespace MultiLLM.Core.Interfaces;

public interface IChatCompletionServiceFactory
{
    IChatCompletionService GetService(string provider);
}