# MultiLLM.Core

A reusable .NET 8 library for multi-provider Large Language Model (LLM) chat completions.

## Features

- **Multi-provider support**: OpenAI, Anthropic, HuggingFace, Google, Grok, Azure
- **Unified interface**: Single `IChatCompletionService` interface for all providers
- **Configuration management**: Flexible API key and model configuration
- **Connection management**: Automatic connection handshaking and status tracking
- **Model catalog**: JSON-configurable model definitions with parameters
- **Dependency injection**: Built with Microsoft.Extensions.DependencyInjection

## Project Structure

```
MultiLLM.Core/
├── Interfaces/              # Core service interfaces
│   ├── IChatCompletionService.cs
│   ├── IChatCompletionServiceFactory.cs
│   ├── IConfigurationService.cs
│   ├── IConnectionService.cs
│   └── IModelCatalog.cs
├── Models/                  # Data models and DTOs
│   ├── AppConfiguration.cs
│   ├── ChatMessage.cs
│   ├── ConnectionModels.cs
│   ├── ModelDefinition.cs
│   └── ModelParameterDefinition.cs
└── Services/               # Service implementations
    └── ChatCompletion/     # Provider-specific chat services
        └── OpenAIChatCompletionService.cs
```

## Usage

### Basic Setup

```csharp
// Add to your DI container
services.AddSingleton<IChatCompletionServiceFactory, ChatCompletionServiceFactory>();
services.AddSingleton<OpenAIChatCompletionService>();

// Configure HTTP clients
services.AddHttpClient("openai", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(120);
});
```

### Using the Service

```csharp
var factory = serviceProvider.GetRequiredService<IChatCompletionServiceFactory>();
var openaiService = factory.GetService("openai");

var messages = new List<ChatMessage>
{
    new(ChatRole.User, "Hello, world!")
};

var response = await openaiService.CompleteAsync(model, messages, apiKey, cancellationToken);
```

## Extracted from MultipleAIApp

This library was extracted from a larger WPF/Uno application to provide reusable LLM functionality.

## Status

- ✅ Core models and interfaces extracted
- ✅ OpenAI chat completion service extracted
- ⏳ Other provider services (in progress)
- ⏳ Configuration system (pending)
- ⏳ Connection management (pending)