using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MultiLLM.Core.Interfaces;
using MultiLLM.Core.Models;
using MultiLLM.Core.Services.ChatCompletion;
using RichardSzalay.MockHttp;

namespace MultiLLM.Core.Tests.Services;

public class OpenAIChatCompletionServiceTests : IDisposable
{
    private readonly Mock<ILogger<OpenAIChatCompletionService>> _loggerMock;
    private readonly Mock<IConnectionService> _connectionServiceMock;
    private readonly MockHttpMessageHandler _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly OpenAIChatCompletionService _service;

    public OpenAIChatCompletionServiceTests()
    {
        _loggerMock = new Mock<ILogger<OpenAIChatCompletionService>>();
        _connectionServiceMock = new Mock<IConnectionService>();
        _mockHttpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpHandler) { BaseAddress = new Uri("https://api.openai.com/") };

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient("openai")).Returns(_httpClient);

        _service = new OpenAIChatCompletionService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _connectionServiceMock.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttpHandler?.Dispose();
    }

    [Fact]
    public async Task CompleteAsync_ShouldReturnResponse_WhenApiCallSucceeds()
    {
        // Arrange
        var model = CreateTestModel();
        var messages = CreateTestMessages();
        var apiKey = "test-api-key";

        _connectionServiceMock.Setup(x => x.IsConnected("openai")).Returns(true);

        var expectedResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Hello! This is a test response from OpenAI."
                    }
                }
            }
        };

        _mockHttpHandler
            .When("https://api.openai.com/v1/chat/completions")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse));

        // Act
        var result = await _service.CompleteAsync(model, messages, apiKey, CancellationToken.None);

        // Assert
        result.Should().Be("Hello! This is a test response from OpenAI.");
    }

    [Fact]
    public async Task CompleteAsync_ShouldPerformHandshake_WhenNotConnected()
    {
        // Arrange
        var model = CreateTestModel();
        var messages = CreateTestMessages();
        var apiKey = "test-api-key";

        _connectionServiceMock.Setup(x => x.IsConnected("openai")).Returns(false);
        _connectionServiceMock
            .Setup(x => x.PerformHandshakeAsync("openai", apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HandshakeResult { IsSuccess = true, Message = "Connected" });

        var expectedResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Response after handshake"
                    }
                }
            }
        };

        _mockHttpHandler
            .When("https://api.openai.com/v1/chat/completions")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse));

        // Act
        var result = await _service.CompleteAsync(model, messages, apiKey, CancellationToken.None);

        // Assert
        result.Should().Be("Response after handshake");
        _connectionServiceMock.Verify(x => x.PerformHandshakeAsync("openai", apiKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_ShouldThrowException_WhenHandshakeFails()
    {
        // Arrange
        var model = CreateTestModel();
        var messages = CreateTestMessages();
        var apiKey = "invalid-key";

        _connectionServiceMock.Setup(x => x.IsConnected("openai")).Returns(false);
        _connectionServiceMock
            .Setup(x => x.PerformHandshakeAsync("openai", apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HandshakeResult { IsSuccess = false, Message = "Invalid API key" });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CompleteAsync(model, messages, apiKey, CancellationToken.None));

        exception.Message.Should().Contain("Handshake failed");
        exception.Message.Should().Contain("Invalid API key");
    }

    [Fact]
    public async Task CompleteAsync_ShouldThrowException_WhenApiKeyIsEmpty()
    {
        // Arrange
        var model = CreateTestModel();
        var messages = CreateTestMessages();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CompleteAsync(model, messages, string.Empty, CancellationToken.None));

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CompleteAsync(model, messages, "   ", CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAsync_ShouldHandleHttpError_WithErrorResponse()
    {
        // Arrange
        var model = CreateTestModel();
        var messages = CreateTestMessages();
        var apiKey = "test-api-key";

        _connectionServiceMock.Setup(x => x.IsConnected("openai")).Returns(true);

        _mockHttpHandler
            .When("https://api.openai.com/v1/chat/completions")
            .Respond(HttpStatusCode.Unauthorized, "application/json", """{"error": {"message": "Invalid API key"}}""");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _service.CompleteAsync(model, messages, apiKey, CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAsync_ShouldIncludeModelParameters_InRequest()
    {
        // Arrange
        var model = new ModelDefinition
        {
            Name = "Test Model",
            Provider = "openai",
            ModelId = "gpt-3.5-turbo",
            Parameters = new List<ModelParameterDefinition>
            {
                new() { Name = "temperature", Default = JsonDocument.Parse("0.8").RootElement },
                new() { Name = "max_tokens", Default = JsonDocument.Parse("500").RootElement }
            }
        };

        var messages = CreateTestMessages();
        var apiKey = "test-api-key";

        _connectionServiceMock.Setup(x => x.IsConnected("openai")).Returns(true);

        var expectedResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Test response"
                    }
                }
            }
        };

        string? capturedRequestBody = null;
        _mockHttpHandler
            .When("https://api.openai.com/v1/chat/completions")
            .With(message =>
            {
                capturedRequestBody = message.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse));

        // Act
        await _service.CompleteAsync(model, messages, apiKey, CancellationToken.None);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody.Should().Contain("\"temperature\":0.8");
        capturedRequestBody.Should().Contain("\"max_tokens\":500");
        capturedRequestBody.Should().Contain("\"model\":\"gpt-3.5-turbo\"");
    }

    [Fact]
    public async Task CompleteAsync_ShouldHandleUnexpectedResponseFormat()
    {
        // Arrange
        var model = CreateTestModel();
        var messages = CreateTestMessages();
        var apiKey = "test-api-key";

        _connectionServiceMock.Setup(x => x.IsConnected("openai")).Returns(true);

        var unexpectedResponse = new { unexpected = "format" };

        _mockHttpHandler
            .When("https://api.openai.com/v1/chat/completions")
            .Respond("application/json", JsonSerializer.Serialize(unexpectedResponse));

        // Act
        var result = await _service.CompleteAsync(model, messages, apiKey, CancellationToken.None);

        // Assert
        result.Should().Contain("unexpected");
        result.Should().Contain("format");
    }

    [Fact]
    public async Task CompleteAsync_ShouldSetCorrectHeaders()
    {
        // Arrange
        var model = CreateTestModel();
        var messages = CreateTestMessages();
        var apiKey = "test-api-key";

        _connectionServiceMock.Setup(x => x.IsConnected("openai")).Returns(true);

        var expectedResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Test response"
                    }
                }
            }
        };

        HttpRequestMessage? capturedRequest = null;
        _mockHttpHandler
            .When("https://api.openai.com/v1/chat/completions")
            .With(message =>
            {
                capturedRequest = message;
                return true;
            })
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse));

        // Act
        await _service.CompleteAsync(model, messages, apiKey, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization.Parameter.Should().Be(apiKey);
        capturedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Theory]
    [InlineData(ChatRole.System, "system")]
    [InlineData(ChatRole.User, "user")]
    [InlineData(ChatRole.Assistant, "assistant")]
    public async Task CompleteAsync_ShouldFormatRolesCorrectly(ChatRole role, string expectedRole)
    {
        // Arrange
        var model = CreateTestModel();
        var messages = new List<ChatMessage>
        {
            new(role, "Test message")
        };
        var apiKey = "test-api-key";

        _connectionServiceMock.Setup(x => x.IsConnected("openai")).Returns(true);

        var expectedResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Test response"
                    }
                }
            }
        };

        string? capturedRequestBody = null;
        _mockHttpHandler
            .When("https://api.openai.com/v1/chat/completions")
            .With(message =>
            {
                capturedRequestBody = message.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse));

        // Act
        await _service.CompleteAsync(model, messages, apiKey, CancellationToken.None);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody.Should().Contain($"\"role\":\"{expectedRole}\"");
    }

    [Fact]
    public async Task CompleteAsync_ShouldLogWarning_WhenResponseIsUnexpected()
    {
        // Arrange
        var model = CreateTestModel();
        var messages = CreateTestMessages();
        var apiKey = "test-api-key";

        _connectionServiceMock.Setup(x => x.IsConnected("openai")).Returns(true);

        var unexpectedResponse = new { no_choices = "field" };

        _mockHttpHandler
            .When("https://api.openai.com/v1/chat/completions")
            .Respond("application/json", JsonSerializer.Serialize(unexpectedResponse));

        // Act
        await _service.CompleteAsync(model, messages, apiKey, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown OpenAI response shape")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static ModelDefinition CreateTestModel()
    {
        return new ModelDefinition
        {
            Name = "Test GPT",
            Provider = "openai",
            ModelId = "gpt-3.5-turbo",
            Parameters = new List<ModelParameterDefinition>
            {
                new() { Name = "temperature", Default = JsonDocument.Parse("0.7").RootElement },
                new() { Name = "max_tokens", Default = JsonDocument.Parse("1000").RootElement }
            }
        };
    }

    private static List<ChatMessage> CreateTestMessages()
    {
        return new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Hello, how are you?")
        };
    }
}