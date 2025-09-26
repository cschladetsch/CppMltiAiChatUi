using MultiLLM.Core.Models;

namespace MultiLLM.Core.Interfaces;

public interface IModelCatalog
{
    IReadOnlyList<ModelDefinition> Models { get; }
    SummaryConfiguration Summary { get; }
}