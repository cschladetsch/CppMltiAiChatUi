using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MultiLLM.Core.Models;
using MultiLLM.Core.Services;

namespace MultiLLM.Core.Tests.Services;

public class MockConnectionServiceTests
{
    private readonly Mock<ILogger<MockConnectionService>> _loggerMock;
    private readonly MockConnectionService _service;

    public MockConnectionServiceTests()
    {
        _loggerMock = new Mock<ILogger<MockConnectionService>>();
        _service = new MockConnectionService(_loggerMock.Object);
    }

    [Fact]
    public async Task PerformHandshakeAsync_ShouldReturnSuccess_WithValidApiKey()
    {
        // Arrange
        var provider = "openai";
        var apiKey = "valid-api-key";

        // Act
        var result = await _service.PerformHandshakeAsync(provider, apiKey);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Connection established successfully");
        result.HandshakeId.Should().NotBeEmpty();
        result.HandshakeId.Should().MatchRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$"); // GUID format
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task PerformHandshakeAsync_ShouldReturnFailure_WithInvalidApiKey(string? apiKey)
    {
        // Arrange
        var provider = "openai";

        // Act
        var result = await _service.PerformHandshakeAsync(provider, apiKey!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Invalid or missing API key");
        result.HandshakeId.Should().NotBeEmpty();
    }

    [Fact]
    public void IsConnected_ShouldReturnFalse_InitiallyForAllProviders()
    {
        // Act & Assert
        _service.IsConnected("openai").Should().BeFalse();
        _service.IsConnected("anthropic").Should().BeFalse();
        _service.IsConnected("huggingface").Should().BeFalse();
    }

    [Fact]
    public async Task IsConnected_ShouldReturnTrue_AfterSuccessfulHandshake()
    {
        // Arrange
        var provider = "openai";
        var apiKey = "valid-key";

        // Act
        await _service.PerformHandshakeAsync(provider, apiKey);

        // Assert
        _service.IsConnected(provider).Should().BeTrue();
    }

    [Fact]
    public async Task IsConnected_ShouldReturnFalse_AfterFailedHandshake()
    {
        // Arrange
        var provider = "openai";
        var apiKey = "";

        // Act
        await _service.PerformHandshakeAsync(provider, apiKey);

        // Assert
        _service.IsConnected(provider).Should().BeFalse();
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("OPENAI")]
    [InlineData("OpenAI")]
    public async Task IsConnected_ShouldBeCaseInsensitive(string provider)
    {
        // Arrange
        var apiKey = "valid-key";

        // Act
        await _service.PerformHandshakeAsync("openai", apiKey);

        // Assert
        _service.IsConnected(provider).Should().BeTrue();
    }

    [Fact]
    public void GetLastHandshakeTime_ShouldReturnNull_WhenNeverConnected()
    {
        // Act
        var lastTime = _service.GetLastHandshakeTime("openai");

        // Assert
        lastTime.Should().BeNull();
    }

    [Fact]
    public async Task GetLastHandshakeTime_ShouldReturnTime_AfterHandshake()
    {
        // Arrange
        var provider = "openai";
        var apiKey = "valid-key";
        var beforeHandshake = DateTime.UtcNow;

        // Act
        await _service.PerformHandshakeAsync(provider, apiKey);
        var afterHandshake = DateTime.UtcNow;

        // Assert
        var lastTime = _service.GetLastHandshakeTime(provider);
        lastTime.Should().NotBeNull();
        lastTime.Should().BeOnOrAfter(beforeHandshake);
        lastTime.Should().BeOnOrBefore(afterHandshake);
    }

    [Fact]
    public void UpdateConnectionStatus_ShouldUpdateStatus()
    {
        // Arrange
        var provider = "openai";
        var message = "Custom status message";

        // Act
        _service.UpdateConnectionStatus(provider, message, true);

        // Assert
        _service.IsConnected(provider).Should().BeTrue();
        var lastTime = _service.GetLastHandshakeTime(provider);
        lastTime.Should().NotBeNull();
    }

    [Fact]
    public void UpdateConnectionStatus_ShouldTriggerEvent()
    {
        // Arrange
        var provider = "anthropic";
        var message = "Test message";
        var isConnected = true;
        ConnectionStatusEventArgs? capturedArgs = null;

        _service.ConnectionStatusChanged += (sender, args) => capturedArgs = args;

        // Act
        _service.UpdateConnectionStatus(provider, message, isConnected);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Provider.Should().Be(provider);
        capturedArgs.IsConnected.Should().Be(isConnected);
        capturedArgs.Message.Should().Be(message);
        capturedArgs.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task PerformHandshakeAsync_ShouldTriggerEvent()
    {
        // Arrange
        var provider = "openai";
        var apiKey = "test-key";
        ConnectionStatusEventArgs? capturedArgs = null;

        _service.ConnectionStatusChanged += (sender, args) => capturedArgs = args;

        // Act
        await _service.PerformHandshakeAsync(provider, apiKey);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Provider.Should().Be(provider);
        capturedArgs.IsConnected.Should().BeTrue();
        capturedArgs.Message.Should().Be("Connection established successfully");
    }

    [Fact]
    public async Task PerformHandshakeAsync_ShouldHaveSimulatedDelay()
    {
        // Arrange
        var provider = "openai";
        var apiKey = "test-key";
        var startTime = DateTime.UtcNow;

        // Act
        await _service.PerformHandshakeAsync(provider, apiKey);
        var endTime = DateTime.UtcNow;

        // Assert
        var duration = endTime - startTime;
        duration.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task PerformHandshakeAsync_ShouldSupportCancellation()
    {
        // Arrange
        var provider = "openai";
        var apiKey = "test-key";
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.PerformHandshakeAsync(provider, apiKey, cts.Token));
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("anthropic")]
    [InlineData("huggingface")]
    [InlineData("google")]
    [InlineData("azure")]
    [InlineData("grok")]
    public async Task PerformHandshakeAsync_ShouldSupportAllProviders(string provider)
    {
        // Arrange
        var apiKey = "test-key";

        // Act
        var result = await _service.PerformHandshakeAsync(provider, apiKey);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _service.IsConnected(provider).Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WithValidLogger()
    {
        // Arrange & Act
        var service = () => new MockConnectionService(_loggerMock.Object);

        // Assert
        service.Should().NotThrow();
    }

    [Fact]
    public void UpdateConnectionStatus_ShouldHandleMultipleProviders()
    {
        // Arrange
        var providers = new[] { "openai", "anthropic", "huggingface" };

        // Act
        foreach (var provider in providers)
        {
            _service.UpdateConnectionStatus(provider, $"Connected to {provider}", true);
        }

        // Assert
        foreach (var provider in providers)
        {
            _service.IsConnected(provider).Should().BeTrue();
        }
    }

    [Fact]
    public void UpdateConnectionStatus_ShouldOverwritePreviousStatus()
    {
        // Arrange
        var provider = "openai";

        // Act
        _service.UpdateConnectionStatus(provider, "First connection", true);
        var firstTime = _service.GetLastHandshakeTime(provider);

        Thread.Sleep(10); // Small delay to ensure different timestamps

        _service.UpdateConnectionStatus(provider, "Second connection", false);
        var secondTime = _service.GetLastHandshakeTime(provider);

        // Assert
        _service.IsConnected(provider).Should().BeFalse(); // Latest status
        secondTime.Should().BeAfter(firstTime!.Value);
    }

    [Fact]
    public async Task PerformHandshakeAsync_ShouldLogInformation()
    {
        // Arrange
        var provider = "openai";
        var apiKey = "test-key";

        // Act
        await _service.PerformHandshakeAsync(provider, apiKey);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Performing handshake for provider: {provider}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}