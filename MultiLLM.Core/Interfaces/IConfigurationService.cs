using MultiLLM.Core.Models;

namespace MultiLLM.Core.Interfaces;

public interface IConfigurationService
{
    AppConfiguration GetConfiguration();
    Task<AppConfiguration> LoadConfigurationAsync();
    Task SaveConfigurationAsync(AppConfiguration configuration);
    string? GetApiKey(string provider, string? commandLineOverride = null);
}