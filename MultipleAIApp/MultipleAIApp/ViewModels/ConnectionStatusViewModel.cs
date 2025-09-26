using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using MultipleAIApp.Services;

namespace MultipleAIApp.ViewModels;

public sealed partial class ConnectionStatusViewModel : ObservableObject
{
    private readonly IConnectionService _connectionService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ConnectionStatusViewModel> _logger;
    private readonly DispatcherQueue _dispatcher;

    private void TryEnqueue(Action action) => _dispatcher.TryEnqueue(() => action());

    public ConnectionStatusViewModel(IConnectionService connectionService, IConfigurationService configurationService, ILogger<ConnectionStatusViewModel> logger)
    {
        _connectionService = connectionService;
        _configurationService = configurationService;
        _logger = logger;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;
        TestConnectionCommand = new AsyncRelayCommand<string>(TestConnectionAsync, CanTestConnection);
    }

    public ObservableCollection<ConnectionStatusItem> ConnectionStatuses { get; } = new();

    public IAsyncRelayCommand<string> TestConnectionCommand { get; }

    private bool CanTestConnection(string? provider) => !string.IsNullOrEmpty(provider);

    private async Task TestConnectionAsync(string? provider)
    {
        if (string.IsNullOrEmpty(provider))
            return;

        var apiKey = _configurationService.GetApiKey(provider);
        if (string.IsNullOrEmpty(apiKey))
        {
            UpdateConnectionStatusItem(provider, false, "No API key configured");
            return;
        }

        try
        {
            var result = await _connectionService.PerformHandshakeAsync(provider, apiKey);
            UpdateConnectionStatusItem(provider, result.IsSuccess, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test connection for provider: {Provider}", provider);
            UpdateConnectionStatusItem(provider, false, $"Test failed: {ex.Message}");
        }
    }

    private void OnConnectionStatusChanged(object? sender, ConnectionStatusEventArgs e)
    {
        UpdateConnectionStatusItem(e.Provider, e.IsConnected, e.Message);
    }

    private void UpdateConnectionStatusItem(string provider, bool isConnected, string message)
    {
        var existing = ConnectionStatuses.FirstOrDefault(s => s.Provider == provider);
        if (existing == null)
        {
            TryEnqueue(() => ConnectionStatuses.Add(new ConnectionStatusItem
            {
                Provider = provider,
                IsConnected = isConnected,
                Status = isConnected ? "Connected" : "Disconnected",
                Message = message,
                LastUpdated = DateTime.UtcNow
            }));
        }
        else
        {
            TryEnqueue(() =>
            {
                existing.IsConnected = isConnected;
                existing.Status = isConnected ? "Connected" : "Disconnected";
                existing.Message = message;
                existing.LastUpdated = DateTime.UtcNow;
            });
        }
    }

    public void InitializeProviders(IEnumerable<string> providers)
    {
        foreach (var provider in providers)
        {
            if (!ConnectionStatuses.Any(s => s.Provider == provider))
            {
                TryEnqueue(() => ConnectionStatuses.Add(new ConnectionStatusItem
                {
                    Provider = provider,
                    IsConnected = false,
                    Status = "Not Connected",
                    Message = "Connection not tested",
                    LastUpdated = DateTime.UtcNow
                }));
            }
        }
    }
}

public sealed partial class ConnectionStatusItem : ObservableObject
{
    private string _provider = string.Empty;
    public string Provider
    {
        get => _provider;
        set => SetProperty(ref _provider, value);
    }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private string _message = string.Empty;
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private DateTime _lastUpdated;
    public DateTime LastUpdated
    {
        get => _lastUpdated;
        set => SetProperty(ref _lastUpdated, value);
    }

    public string DisplayName => Provider.ToUpperInvariant();
    public string StatusColor => IsConnected ? "Green" : "Red";
    public string LastUpdatedDisplay => $"Updated: {LastUpdated:HH:mm:ss}";
}