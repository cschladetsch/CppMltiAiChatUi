using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MultiLLM.Core.Interfaces;
using MultiLLM.Core.Services;
using MultiLLM.Core.Services.ChatCompletion;

namespace MultiLLM.Core.Tests.Services;

public class ChatCompletionServiceFactoryTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<ChatCompletionServiceFactory>> _loggerMock;
    private readonly ChatCompletionServiceFactory _factory;

    public ChatCompletionServiceFactoryTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<ChatCompletionServiceFactory>>();
        _factory = new ChatCompletionServiceFactory(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GetService_ShouldReturnOpenAIService_ForOpenAIProvider()
    {
        // Arrange
        var realService = new OpenAIChatCompletionService(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<OpenAIChatCompletionService>>(),
            Mock.Of<IConnectionService>());

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(OpenAIChatCompletionService)))
            .Returns(realService);

        // Act
        var service = _factory.GetService("openai");

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<OpenAIChatCompletionService>();
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("OPENAI")]
    [InlineData("OpenAI")]
    public void GetService_ShouldBeCaseInsensitive(string provider)
    {
        // Arrange
        var realService = new OpenAIChatCompletionService(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<OpenAIChatCompletionService>>(),
            Mock.Of<IConnectionService>());

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(OpenAIChatCompletionService)))
            .Returns(realService);

        // Act
        var service = _factory.GetService(provider);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<OpenAIChatCompletionService>();
    }

    [Fact]
    public void GetService_ShouldThrowInvalidOperationException_WhenServiceNotRegistered()
    {
        // Arrange
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(OpenAIChatCompletionService)))
            .Returns((object?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _factory.GetService("openai"));
        exception.Message.Should().Contain("OpenAI service not registered");
    }

    [Theory]
    [InlineData("anthropic")]
    [InlineData("huggingface")]
    [InlineData("google")]
    [InlineData("azure")]
    [InlineData("unknown")]
    public void GetService_ShouldThrowNotSupportedException_ForUnsupportedProvider(string provider)
    {
        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _factory.GetService(provider));
        exception.Message.Should().Contain($"Provider '{provider}' is not supported");
    }

    [Fact]
    public void GetService_ShouldHandleNullProvider()
    {
        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _factory.GetService(null!));
        exception.Message.Should().Contain("not supported");
    }

    [Fact]
    public void GetService_ShouldHandleEmptyProvider()
    {
        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _factory.GetService(""));
        exception.Message.Should().Contain("not supported");
    }

    [Fact]
    public void GetService_ShouldHandleWhitespaceProvider()
    {
        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _factory.GetService("   "));
        exception.Message.Should().Contain("not supported");
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WithValidParameters()
    {
        // Arrange & Act
        var factory = () => new ChatCompletionServiceFactory(_serviceProviderMock.Object, _loggerMock.Object);

        // Assert
        factory.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenServiceProviderIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ChatCompletionServiceFactory(null!, _loggerMock.Object));

        exception.ParamName.Should().Be("serviceProvider");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ChatCompletionServiceFactory(_serviceProviderMock.Object, null!));

        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public void GetService_ShouldCallServiceProviderCorrectly()
    {
        // Arrange
        var realService = new OpenAIChatCompletionService(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<OpenAIChatCompletionService>>(),
            Mock.Of<IConnectionService>());

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(OpenAIChatCompletionService)))
            .Returns(realService);

        // Act
        _factory.GetService("openai");

        // Assert
        _serviceProviderMock.Verify(x => x.GetService(typeof(OpenAIChatCompletionService)), Times.Once);
    }

    [Fact]
    public void GetService_ShouldReturnSameTypeForMultipleCalls()
    {
        // Arrange
        var realService1 = new OpenAIChatCompletionService(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<OpenAIChatCompletionService>>(),
            Mock.Of<IConnectionService>());

        var realService2 = new OpenAIChatCompletionService(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<OpenAIChatCompletionService>>(),
            Mock.Of<IConnectionService>());

        _serviceProviderMock.SetupSequence(x => x.GetService(typeof(OpenAIChatCompletionService)))
            .Returns(realService1)
            .Returns(realService2);

        // Act
        var service1 = _factory.GetService("openai");
        var service2 = _factory.GetService("openai");

        // Assert
        service1.Should().BeOfType<OpenAIChatCompletionService>();
        service2.Should().BeOfType<OpenAIChatCompletionService>();
        // Note: They may or may not be the same instance depending on DI registration
    }
}