using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultipleAIApp.Models;
using MultipleAIApp.Services;
using MultipleAIApp.ViewModels;
using Uno.Resizetizer;
using Serilog;
using Serilog.Events;

namespace MultipleAIApp;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        InitializeComponent();
        InitializeLogging();
        _host = BuildHost();
    }

    public static new App Current => (App)Application.Current;

    public IServiceProvider Services => _host.Services;

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Log.Information("Application starting...");

            MainWindow = new Window();
#if DEBUG
            MainWindow.UseStudio();
#endif

            if (MainWindow.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                MainWindow.Content = rootFrame;
                rootFrame.NavigationFailed += OnNavigationFailed;
            }

            if (rootFrame.Content is null)
            {
                rootFrame.Navigate(typeof(MainPage), args.Arguments);
            }

            MainWindow.SetWindowIcon();
            MainWindow.Activate();

            Log.Information("Application started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            throw;
        }
    }


    private static IHost BuildHost()
    {
        // Configure Serilog early
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("logs/multipleai-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Services.AddSerilog();

        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile("config.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddJsonFile("Configs/models.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
        builder.Services.AddSingleton<IModelCatalog, ModelCatalog>();
        builder.Services.AddSingleton<IConnectionService, ConnectionService>();

        // Register individual chat completion services
        builder.Services.AddSingleton<HuggingFaceChatCompletionService>();
        builder.Services.AddSingleton<OpenAIChatCompletionService>();
        builder.Services.AddSingleton<AnthropicChatCompletionService>();

        // Register the factory
        builder.Services.AddSingleton<IChatCompletionServiceFactory, ChatCompletionServiceFactory>();

        // Configure HTTP clients for different providers
        builder.Services.AddHttpClient("huggingface", client =>
        {
            client.BaseAddress = new Uri("https://api-inference.huggingface.co/");
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        builder.Services.AddHttpClient("openai", client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/");
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        builder.Services.AddHttpClient("anthropic", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        builder.Services.AddSingleton<ConnectionStatusViewModel>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<ChatSessionViewModel>();
        builder.Services.AddTransient<Func<ModelDefinition, ChatSessionViewModel>>(sp => model =>
            ActivatorUtilities.CreateInstance<ChatSessionViewModel>(sp, model,
                sp.GetRequiredService<IChatCompletionServiceFactory>(),
                sp.GetRequiredService<IConnectionService>(),
                sp.GetRequiredService<ILogger<ChatSessionViewModel>>()));

        return builder.Build();
    }

    private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    public static void InitializeLogging()
    {
#if DEBUG
        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
            builder.AddConsole();
#else
            builder.AddConsole();
#endif
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
