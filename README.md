# MultiLLM.Core

A reusable .NET library for multi-provider Large Language Model (LLM) chat completions supporting OpenAI, Anthropic, HuggingFace, Google, Grok, and Azure.

## 🏗️ Architecture Overview

```mermaid
graph TB
    Demo[MultiLLM.Demo]
    Core[MultiLLM.Core]
    Tests[MultiLLM.Core.Tests]

    Demo --> Core
    Tests --> Core

    subgraph "Core Components"
        Core --> Interfaces[Interfaces]
        Core --> Services[Services]
        Core --> Models[Models]
    end

    subgraph "External Dependencies"
        OpenAI[OpenAI API]
        Future[Future Providers]
    end

    Services --> OpenAI
    Services --> Future
```

## 📦 Solution Structure

```mermaid
graph LR
    Solution[MultiLLM.sln]

    Solution --> CoreProject[MultiLLM.Core]
    Solution --> DemoProject[MultiLLM.Demo]
    Solution --> TestProject[MultiLLM.Core.Tests]

    CoreProject --> CoreLib[📚 Core Library]
    DemoProject --> DemoApp[🚀 Demo Console App]
    TestProject --> TestSuite[🧪 Unit Tests]
```

## 🔧 Core Components

### Interfaces

```mermaid
classDiagram
    class IChatCompletionService {
        +CompleteAsync(model, messages, apiKey, token) string
    }

    class IChatCompletionServiceFactory {
        +GetService(provider) IChatCompletionService
    }

    class IConfigurationService {
        +LoadConfiguration()
    }

    class IModelCatalog {
        +GetModels()
    }

    class IConnectionService {
        +TestConnection()
    }
```

### Models

```mermaid
classDiagram
    class ModelDefinition {
        +string Name
        +string Provider
        +string ModelId
        +string Description
        +List~ModelParameterDefinition~ Parameters
    }

    class ChatMessage {
        +ChatRole Role
        +string Content
    }

    class ChatRole {
        <<enumeration>>
        System
        User
        Assistant
    }

    class ModelParameterDefinition {
        +string Name
        +JsonElement Default
    }

    class AppConfiguration {
        +ModelCatalogConfiguration ModelCatalog
    }

    ModelDefinition --> ModelParameterDefinition
    ChatMessage --> ChatRole
    AppConfiguration --> ModelCatalogConfiguration
```

## 🚀 Service Flow

```mermaid
sequenceDiagram
    participant Demo as Demo App
    participant Factory as ServiceFactory
    participant OpenAI as OpenAIService
    participant API as OpenAI API

    Demo->>Factory: GetService("openai")
    Factory->>OpenAI: Create/Return Service
    Factory-->>Demo: IChatCompletionService

    Demo->>OpenAI: CompleteAsync(model, messages, apiKey)
    OpenAI->>API: HTTP POST /chat/completions
    API-->>OpenAI: Response JSON
    OpenAI-->>Demo: Processed Response
```

## 🔌 Provider Support

```mermaid
graph LR
    Factory[ChatCompletionServiceFactory]

    Factory --> OpenAI[OpenAI ✅]
    Factory --> Anthropic[Anthropic 🚧]
    Factory --> HuggingFace[HuggingFace 🚧]
    Factory --> Google[Google 🚧]
    Factory --> Grok[Grok 🚧]
    Factory --> Azure[Azure 🚧]

    subgraph Legend
        Implemented[✅ Implemented]
        Planned[🚧 Planned]
    end
```

## 🧪 Testing Strategy

```mermaid
graph TB
    TestSuite[MultiLLM.Core.Tests]

    TestSuite --> UnitTests[Unit Tests]
    TestSuite --> IntegrationTests[Integration Tests]
    TestSuite --> MockTests[Mock Tests]

    UnitTests --> ServiceTests[Service Tests]
    UnitTests --> ModelTests[Model Tests]
    UnitTests --> FactoryTests[Factory Tests]

    IntegrationTests --> APITests[API Tests]
    IntegrationTests --> ConfigTests[Config Tests]

    MockTests --> MockService[MockConnectionService]
    MockTests --> MockResponses[Mock API Responses]
```

## 🛠️ Getting Started

### Installation

```bash
# Clone the repository
git clone <repository-url>

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

### Running the Demo

```bash
# Set OpenAI API key (optional - demo works without it)
set OPENAI_API_KEY=your-api-key-here

# Run the demo
dotnet run --project MultiLLM.Demo
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📋 Configuration Flow

```mermaid
graph TB
    Start[Application Start]

    Start --> DI[Setup Dependency Injection]
    DI --> HttpClients[Configure HTTP Clients]
    HttpClients --> Services[Register Core Services]

    Services --> ConfigSvc[ConfigurationService]
    Services --> ConnSvc[ConnectionService]
    Services --> ChatSvc[ChatCompletionService]
    Services --> Factory[ServiceFactory]

    Factory --> Ready[Ready for Use]
```

## 🔑 Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `OPENAI_API_KEY` | OpenAI API key for live testing | Optional |
| *Future providers* | Additional API keys as providers are added | Optional |

## 📚 Usage Example

```csharp
// Setup dependency injection
var services = new ServiceCollection();
services.AddHttpClient();
services.AddSingleton<OpenAIChatCompletionService>();
services.AddSingleton<IChatCompletionServiceFactory, ChatCompletionServiceFactory>();

var serviceProvider = services.BuildServiceProvider();

// Get service and make a chat completion
var factory = serviceProvider.GetRequiredService<IChatCompletionServiceFactory>();
var openAiService = factory.GetService("openai");

var model = new ModelDefinition { Provider = "openai", ModelId = "gpt-3.5-turbo" };
var messages = new[] { new ChatMessage(ChatRole.User, "Hello!") };

var response = await openAiService.CompleteAsync(model, messages, apiKey, CancellationToken.None);
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## 📄 License

MIT License - see LICENSE file for details.