using Microsoft.Extensions.Configuration;
using MultipleAIApp.Models;

namespace MultipleAIApp.Services;

public interface IModelCatalog
{
    IReadOnlyList<ModelDefinition> Models { get; }

    SummaryConfiguration Summary { get; }
}

public sealed class ModelCatalog : IModelCatalog
{
    public ModelCatalog(IConfiguration configuration)
    {
        var config = configuration.Get<ModelCatalogConfiguration>() ?? new ModelCatalogConfiguration();
        Models = config.Models;
        Summary = config.Summary;
    }

    public IReadOnlyList<ModelDefinition> Models { get; }

    public SummaryConfiguration Summary { get; }
}
