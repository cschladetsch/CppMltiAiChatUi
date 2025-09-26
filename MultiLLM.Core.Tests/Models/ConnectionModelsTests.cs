using FluentAssertions;
using MultiLLM.Core.Models;

namespace MultiLLM.Core.Tests.Models;

public class ConnectionModelsTests
{
    public class HandshakeResultTests
    {
        [Fact]
        public void HandshakeResult_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            var result = new HandshakeResult();

            // Assert
            result.IsSuccess.Should().BeFalse(); // Default boolean value
            result.Message.Should().Be(string.Empty); // Default value
            result.HandshakeId.Should().Be(string.Empty); // Default value
        }

        [Fact]
        public void HandshakeResult_ShouldSetAllProperties()
        {
            // Arrange
            var handshakeId = Guid.NewGuid().ToString();

            // Act
            var result = new HandshakeResult
            {
                IsSuccess = true,
                Message = "Connection successful",
                HandshakeId = handshakeId
            };

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Connection successful");
            result.HandshakeId.Should().Be(handshakeId);
        }

        [Theory]
        [InlineData(true, "Success")]
        [InlineData(false, "Failed")]
        [InlineData(true, "")]
        [InlineData(false, "Error occurred")]
        public void HandshakeResult_ShouldSupportVariousStates(bool isSuccess, string message)
        {
            // Arrange & Act
            var result = new HandshakeResult
            {
                IsSuccess = isSuccess,
                Message = message
            };

            // Assert
            result.IsSuccess.Should().Be(isSuccess);
            result.Message.Should().Be(message);
        }
    }

    public class ConnectionStatusTests
    {
        [Fact]
        public void ConnectionStatus_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            var status = new ConnectionStatus();

            // Assert
            status.IsConnected.Should().BeFalse(); // Default boolean value
            status.LastHandshakeTime.Should().BeNull(); // Default nullable DateTime
            status.Message.Should().Be(string.Empty); // Default value
        }

        [Fact]
        public void ConnectionStatus_ShouldSetAllProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;

            // Act
            var status = new ConnectionStatus
            {
                IsConnected = true,
                LastHandshakeTime = timestamp,
                Message = "Connected successfully"
            };

            // Assert
            status.IsConnected.Should().BeTrue();
            status.LastHandshakeTime.Should().Be(timestamp);
            status.Message.Should().Be("Connected successfully");
        }

        [Fact]
        public void ConnectionStatus_ShouldHandleNullTimestamp()
        {
            // Arrange & Act
            var status = new ConnectionStatus
            {
                IsConnected = false,
                LastHandshakeTime = null,
                Message = "Never connected"
            };

            // Assert
            status.IsConnected.Should().BeFalse();
            status.LastHandshakeTime.Should().BeNull();
            status.Message.Should().Be("Never connected");
        }

        [Theory]
        [InlineData(true, "Connection established")]
        [InlineData(false, "Connection lost")]
        [InlineData(true, "")]
        [InlineData(false, "Timeout occurred")]
        public void ConnectionStatus_ShouldSupportVariousStates(bool isConnected, string message)
        {
            // Arrange & Act
            var status = new ConnectionStatus
            {
                IsConnected = isConnected,
                Message = message
            };

            // Assert
            status.IsConnected.Should().Be(isConnected);
            status.Message.Should().Be(message);
        }
    }

    public class ConnectionStatusEventArgsTests
    {
        [Fact]
        public void ConnectionStatusEventArgs_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            var args = new ConnectionStatusEventArgs();

            // Assert
            args.Provider.Should().Be(string.Empty); // Default value
            args.IsConnected.Should().BeFalse(); // Default boolean value
            args.Message.Should().Be(string.Empty); // Default value
            args.Timestamp.Should().Be(default(DateTime)); // Default DateTime
        }

        [Fact]
        public void ConnectionStatusEventArgs_ShouldSetAllProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;

            // Act
            var args = new ConnectionStatusEventArgs
            {
                Provider = "openai",
                IsConnected = true,
                Message = "Connection successful",
                Timestamp = timestamp
            };

            // Assert
            args.Provider.Should().Be("openai");
            args.IsConnected.Should().BeTrue();
            args.Message.Should().Be("Connection successful");
            args.Timestamp.Should().Be(timestamp);
        }

        [Theory]
        [InlineData("openai", true, "Connected")]
        [InlineData("anthropic", false, "Disconnected")]
        [InlineData("huggingface", true, "")]
        [InlineData("", false, "Unknown error")]
        public void ConnectionStatusEventArgs_ShouldSupportVariousProviders(
            string provider, bool isConnected, string message)
        {
            // Arrange & Act
            var args = new ConnectionStatusEventArgs
            {
                Provider = provider,
                IsConnected = isConnected,
                Message = message
            };

            // Assert
            args.Provider.Should().Be(provider);
            args.IsConnected.Should().Be(isConnected);
            args.Message.Should().Be(message);
        }

        [Fact]
        public void ConnectionStatusEventArgs_ShouldInheritFromEventArgs()
        {
            // Arrange & Act
            var args = new ConnectionStatusEventArgs();

            // Assert
            args.Should().BeAssignableTo<EventArgs>();
        }

        [Theory]
        [InlineData("openai")]
        [InlineData("anthropic")]
        [InlineData("huggingface")]
        [InlineData("google")]
        [InlineData("azure")]
        [InlineData("grok")]
        public void ConnectionStatusEventArgs_ShouldSupportAllProviders(string provider)
        {
            // Arrange & Act
            var args = new ConnectionStatusEventArgs
            {
                Provider = provider,
                IsConnected = true,
                Message = $"Connected to {provider}",
                Timestamp = DateTime.UtcNow
            };

            // Assert
            args.Provider.Should().Be(provider);
            args.IsConnected.Should().BeTrue();
            args.Message.Should().Contain(provider);
        }

        [Fact]
        public void ConnectionStatusEventArgs_ShouldHandleRecentTimestamp()
        {
            // Arrange
            var beforeTimestamp = DateTime.UtcNow;

            // Act
            var args = new ConnectionStatusEventArgs
            {
                Provider = "test",
                IsConnected = true,
                Message = "Test",
                Timestamp = DateTime.UtcNow
            };

            var afterTimestamp = DateTime.UtcNow;

            // Assert
            args.Timestamp.Should().BeOnOrAfter(beforeTimestamp);
            args.Timestamp.Should().BeOnOrBefore(afterTimestamp);
        }
    }
}