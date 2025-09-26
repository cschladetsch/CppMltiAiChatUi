using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using MultipleAIApp.Services;

namespace MultipleAIApp.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IModelCatalog _catalog;
    private readonly Func<ModelDefinition, ChatSessionViewModel> _sessionFactory;
    private readonly ILogger<MainViewModel> _logger;
    private readonly ConnectionStatusViewModel _connectionStatusViewModel;
    private readonly IConfigurationService _configurationService;
    private readonly DispatcherQueue _dispatcher;

    private void TryEnqueue(Action action) => _dispatcher.TryEnqueue(() => action());

    private ModelDefinition? selectedModel;
    private ChatSessionViewModel? selectedSession;
    private string apiKey = string.Empty;
    private string userInput = string.Empty;
    private string summaryText = "Cross-model summary will appear here after initialization.";
    private string statusMessage = "Initializing connections...";
    private bool isBusy;
    private bool showConnectionPanel;

    public MainViewModel(IModelCatalog catalog, Func<ModelDefinition, ChatSessionViewModel> sessionFactory, ILogger<MainViewModel> logger, ConnectionStatusViewModel connectionStatusViewModel, IConfigurationService configurationService)
    {
        _catalog = catalog;
        _sessionFactory = sessionFactory;
        _logger = logger;
        _connectionStatusViewModel = connectionStatusViewModel;
        _configurationService = configurationService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        Models = new ObservableCollection<ModelDefinition>(_catalog.Models);
        Sessions = new ObservableCollection<ChatSessionViewModel>(Models.Select(_sessionFactory));

        SelectedModel = Models.FirstOrDefault();
        SelectedSession = Sessions.FirstOrDefault();

        SendCommand = new AsyncRelayCommand(BroadcastAsync, CanBroadcast);
        RefreshSummaryCommand = new AsyncRelayCommand(RefreshSummariesAsync, CanRefreshSummary);
        ToggleConnectionPanelCommand = new RelayCommand(ToggleConnectionPanel);
        InitializeConnectionsCommand = new AsyncRelayCommand(InitializeAllConnectionsAsync, CanInitializeConnections);

        // Initialize connection status for available providers
        var providers = Models.Select(m => m.Provider).Distinct().ToList();
        _connectionStatusViewModel.InitializeProviders(providers);

        // Auto-initialize connections on startup - one thread per session
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000); // Wait for UI to fully load
            await AutoInitializeConnectionsAsync();
        });
    }

    public ObservableCollection<ModelDefinition> Models { get; }

    public ObservableCollection<ChatSessionViewModel> Sessions { get; }

    public IReadOnlyList<ModelParameterDefinition>? SelectedModelParameters => SelectedModel?.Parameters;

    public ModelDefinition? SelectedModel
    {
        get => selectedModel;
        set
        {
            if (SetProperty(ref selectedModel, value))
            {
                OnSelectedModelChanged(value);
            }
        }
    }

    public ChatSessionViewModel? SelectedSession
    {
        get => selectedSession;
        set => SetProperty(ref selectedSession, value);
    }

    public string ApiKey
    {
        get => apiKey;
        set
        {
            if (SetProperty(ref apiKey, value))
            {
                SendCommand?.NotifyCanExecuteChanged();
                RefreshSummaryCommand?.NotifyCanExecuteChanged();
                InitializeConnectionsCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    public string UserInput
    {
        get => userInput;
        set
        {
            if (SetProperty(ref userInput, value))
            {
                SendCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    public string SummaryText
    {
        get => summaryText;
        set => SetProperty(ref summaryText, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        set
        {
            if (SetProperty(ref isBusy, value))
            {
                SendCommand?.NotifyCanExecuteChanged();
                RefreshSummaryCommand?.NotifyCanExecuteChanged();
                InitializeConnectionsCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    public IAsyncRelayCommand SendCommand { get; }

    public IAsyncRelayCommand RefreshSummaryCommand { get; }

    public IRelayCommand ToggleConnectionPanelCommand { get; }

    public IAsyncRelayCommand InitializeConnectionsCommand { get; }

    public ConnectionStatusViewModel ConnectionStatusViewModel => _connectionStatusViewModel;

    public bool ShowConnectionPanel
    {
        get => showConnectionPanel;
        set => SetProperty(ref showConnectionPanel, value);
    }

    private bool CanBroadcast() => !IsBusy && !string.IsNullOrWhiteSpace(UserInput);

    private bool CanRefreshSummary() => !IsBusy && !string.IsNullOrWhiteSpace(ApiKey) && Sessions.Count > 0;

    private bool CanInitializeConnections() => !IsBusy && !string.IsNullOrWhiteSpace(ApiKey) && Sessions.Count > 0;

    private async Task InitializeAllConnectionsAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "API key is required.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Initializing connections for all sessions...";

            var tasks = Sessions.Select(session => session.InitializeConnectionAsync(ApiKey));
            await Task.WhenAll(tasks).ConfigureAwait(false);

            StatusMessage = $"Initialized {Sessions.Count} sessions.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Warning: {ex.Message}";
            _logger.LogError(ex, "Failed to initialize connections");
        }
        finally
        {
            IsBusy = false;
            SendCommand.NotifyCanExecuteChanged();
            RefreshSummaryCommand.NotifyCanExecuteChanged();
            InitializeConnectionsCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task BroadcastAsync()
    {
        var message = UserInput.Trim();
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Sending message to all sessions...";
            var tasks = Sessions.Select(session => session.SendAsync(message, ApiKey));
            await Task.WhenAll(tasks).ConfigureAwait(false);
            UserInput = string.Empty;
            StatusMessage = $"Sent to {Sessions.Count} sessions.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Warning: {ex.Message}";
            _logger.LogError(ex, "Broadcast failed");
        }
        finally
        {
            IsBusy = false;
            SendCommand.NotifyCanExecuteChanged();
            RefreshSummaryCommand.NotifyCanExecuteChanged();
            InitializeConnectionsCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task RefreshSummariesAsync()
    {
        if (Sessions.Count == 0)
        {
            SummaryText = "No sessions to summarize.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Generating summaries...";
            var prompt = _catalog.Summary.SystemPrompt;
            var tasks = Sessions.Select(session => session.SummarizeAsync(ApiKey, prompt));
            var summaries = await Task.WhenAll(tasks).ConfigureAwait(false);
            SummaryText = string.Join(Environment.NewLine + Environment.NewLine, summaries);
            StatusMessage = "Summary updated.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Warning: {ex.Message}";
            _logger.LogError(ex, "Failed to refresh summaries");
        }
        finally
        {
            IsBusy = false;
            SendCommand.NotifyCanExecuteChanged();
            RefreshSummaryCommand.NotifyCanExecuteChanged();
            InitializeConnectionsCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnSelectedModelChanged(ModelDefinition? value) => OnPropertyChanged(nameof(SelectedModelParameters));

    private void ToggleConnectionPanel() => ShowConnectionPanel = !ShowConnectionPanel;

    private async Task AutoInitializeConnectionsAsync()
    {
        try
        {
            // Update status message on main thread
            TryEnqueue(() =>
            {
                StatusMessage = "Starting auto-initialization...";
            });

            var initTasks = new List<Task>();

            // For each session, start a background task
            foreach (var session in Sessions)
            {
                var provider = session.Model.Provider;
                var apiKey = _configurationService.GetApiKey(provider, !string.IsNullOrWhiteSpace(ApiKey) ? ApiKey : null);

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogInformation("Auto-initializing connection for {ModelName} ({Provider})", session.Model.Name, provider);

                    // Start each connection initialization on its own thread
                    initTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await session.InitializeConnectionAsync(apiKey);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to initialize {ModelName}", session.Model.Name);
                        }
                    }));
                }
                else
                {
                    _logger.LogWarning("No API key found for provider {Provider}. Model {ModelName} will not be initialized.", provider, session.Model.Name);

                    // Schedule UI update on main thread
                    TryEnqueue(() =>
                    {
                        var missingKeyMessage = new ChatMessageViewModel(ChatRole.System, $"❌ No API key configured for {provider}. Please set API key in config.json or environment variable.")
                        {
                            Provider = provider
                        };
                        session.Messages.Add(missingKeyMessage);
                    });
                }
            }

            // Update status on main thread
            if (initTasks.Count > 0)
            {
                TryEnqueue(() =>
                {
                    StatusMessage = $"Initializing {initTasks.Count} connections...";
                });

                // Wait for all initialization tasks to complete
                await Task.WhenAll(initTasks);

                TryEnqueue(() =>
                {
                    StatusMessage = $"Initialized {initTasks.Count} sessions successfully.";
                });
            }
            else
            {
                TryEnqueue(() =>
                {
                    StatusMessage = "No API keys found. Please configure API keys to enable chat functionality.";
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-initialize connections");
            TryEnqueue(() =>
            {
                StatusMessage = $"Auto-initialization failed: {ex.Message}";
            });
        }
    }
}




