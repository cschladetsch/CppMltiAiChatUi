using System.Linq;
using System.Threading;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using MultipleAIApp.Services;

namespace MultipleAIApp.ViewModels;

public sealed partial class ChatSessionViewModel : ObservableObject
{
    private readonly IChatCompletionServiceFactory _chatServiceFactory;
    private readonly IConnectionService _connectionService;
    private readonly ILogger<ChatSessionViewModel> _logger;
    private readonly DispatcherQueue _dispatcher;

    private void TryEnqueue(Action action) => _dispatcher.TryEnqueue(() => action());

    public ChatSessionViewModel(ModelDefinition model, IChatCompletionServiceFactory chatServiceFactory, IConnectionService connectionService, ILogger<ChatSessionViewModel> logger)
    {
        Model = model;
        _chatServiceFactory = chatServiceFactory;
        _connectionService = connectionService;
        _logger = logger;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    public ModelDefinition Model { get; }

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();

    private bool isBusy;

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    public string DisplayName => Model.Name;

    public async Task InitializeConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        ChatMessageViewModel? connectionVm = null;

        try
        {
            // Update UI on main thread
            TryEnqueue(() =>
            {
                IsBusy = true;

                // Add connection status message
                connectionVm = new ChatMessageViewModel(ChatRole.System, "Establishing connection...", isStreaming: true)
                {
                    Provider = Model.Provider
                };
                Messages.Add(connectionVm);
            });

            // Perform handshake on background thread
            var handshakeResult = await _connectionService.PerformHandshakeAsync(Model.Provider, apiKey, cancellationToken);

            if (handshakeResult.IsSuccess)
            {
                // Update UI on main thread
                TryEnqueue(() =>
                {
                    if (connectionVm != null)
                    {
                        connectionVm.Content = $"✅ Connected to {Model.Provider} - {handshakeResult.Message}";
                        connectionVm.IsStreaming = false;
                    }
                });

                // Send initial "hello" message
                await SendAsync("hello", apiKey, cancellationToken);
            }
            else
            {
                TryEnqueue(() =>
                {
                    if (connectionVm != null)
                    {
                        connectionVm.Content = $"❌ Connection failed: {handshakeResult.Message}";
                        connectionVm.IsStreaming = false;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            TryEnqueue(() =>
            {
                var errorVm = new ChatMessageViewModel(ChatRole.System, $"❌ Connection error: {ex.Message}")
                {
                    Provider = Model.Provider
                };
                Messages.Add(errorVm);
            });
            _logger.LogError(ex, "Failed to initialize connection for model {Model}", Model.Name);
        }
        finally
        {
            TryEnqueue(() =>
            {
                IsBusy = false;
            });
        }
    }

    public async Task SendAsync(string userMessage, string apiKey, CancellationToken cancellationToken = default)
    {
        var trimmed = userMessage.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return;
        }

        var history = BuildHistory();
        history.Add(new ChatMessage(ChatRole.User, trimmed));

        var userVm = new ChatMessageViewModel(ChatRole.User, trimmed)
        {
            Provider = Model.Provider
        };
        TryEnqueue(() => Messages.Add(userVm));

        var assistantVm = new ChatMessageViewModel(ChatRole.Assistant, "Thinking...", isStreaming: true)
        {
            Provider = Model.Provider
        };
        TryEnqueue(() => Messages.Add(assistantVm));

        try
        {
            TryEnqueue(() => IsBusy = true);
            var chatService = _chatServiceFactory.GetService(Model.Provider);
            var response = await chatService.CompleteAsync(Model, history, apiKey, cancellationToken).ConfigureAwait(false);
            TryEnqueue(() => assistantVm.Content = string.IsNullOrWhiteSpace(response) ? "(no response)" : response.Trim());
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            TryEnqueue(() => assistantVm.Content = $"Warning: {ex.Message}");
            _logger.LogError(ex, "Failed to complete chat for model {Model}", Model.Name);
        }
        finally
        {
            TryEnqueue(() =>
            {
                assistantVm.IsStreaming = false;
                IsBusy = false;
            });
        }
    }

    public async Task<string> SummarizeAsync(string apiKey, string systemPrompt, CancellationToken cancellationToken = default)
    {
        if (Messages.Count == 0)
        {
            return $"{Model.Name}: No conversation yet.";
        }

        var transcript = BuildTranscript();
        var summaryMessages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, $"Summarize the following conversation between a user and an assistant named {Model.Name}. Provide 2 short bullet points.\n\n{transcript}")
        };

        try
        {
            var chatService = _chatServiceFactory.GetService(Model.Provider);
            var summary = await chatService.CompleteAsync(Model, summaryMessages, apiKey, cancellationToken).ConfigureAwait(false);
            return $"{Model.Name}: {summary.Trim()}";
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Failed to summarize conversation for model {Model}", Model.Name);
            return $"{Model.Name}: Warning: {ex.Message}";
        }
    }

    private List<ChatMessage> BuildHistory() => Messages.Select(static m => m.ToMessage()).ToList();

    private string BuildTranscript()
    {
        var builder = new StringBuilder();
        foreach (var message in Messages)
        {
            var header = message.Role switch
            {
                ChatRole.User => "User",
                ChatRole.Assistant => Model.Name,
                ChatRole.System => "System",
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(header))
            {
                builder.Append(header);
                builder.Append(':');
                builder.Append(' ');
            }

            builder.AppendLine(message.Content);
        }

        return builder.ToString();
    }
}
