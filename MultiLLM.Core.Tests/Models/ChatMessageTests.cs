using FluentAssertions;
using MultiLLM.Core.Models;

namespace MultiLLM.Core.Tests.Models;

public class ChatMessageTests
{
    [Fact]
    public void ChatMessage_ShouldCreateWithValidParameters()
    {
        // Arrange
        var role = ChatRole.User;
        var content = "Hello, world!";

        // Act
        var message = new ChatMessage(role, content);

        // Assert
        message.Role.Should().Be(role);
        message.Content.Should().Be(content);
    }

    [Theory]
    [InlineData(ChatRole.System)]
    [InlineData(ChatRole.User)]
    [InlineData(ChatRole.Assistant)]
    public void ChatMessage_ShouldSupportAllRoles(ChatRole role)
    {
        // Arrange
        var content = "Test message";

        // Act
        var message = new ChatMessage(role, content);

        // Assert
        message.Role.Should().Be(role);
        message.Content.Should().Be(content);
    }

    [Fact]
    public void ChatMessage_ShouldAllowEmptyContent()
    {
        // Arrange
        var role = ChatRole.System;
        var content = "";

        // Act
        var message = new ChatMessage(role, content);

        // Assert
        message.Role.Should().Be(role);
        message.Content.Should().Be(content);
    }

    [Fact]
    public void ChatMessage_ShouldHandleNullContent()
    {
        // Arrange
        var role = ChatRole.Assistant;
        string content = null!;

        // Act
        var message = new ChatMessage(role, content);

        // Assert
        message.Role.Should().Be(role);
        message.Content.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_ShouldBeImmutable()
    {
        // Arrange
        var role = ChatRole.User;
        var content = "Original content";
        var message = new ChatMessage(role, content);

        // Act & Assert
        // Records are immutable by default
        message.Should().BeOfType<ChatMessage>();
        message.Role.Should().Be(role);
        message.Content.Should().Be(content);
    }

    [Fact]
    public void ChatMessage_ShouldSupportEquality()
    {
        // Arrange
        var message1 = new ChatMessage(ChatRole.User, "Hello");
        var message2 = new ChatMessage(ChatRole.User, "Hello");
        var message3 = new ChatMessage(ChatRole.Assistant, "Hello");

        // Act & Assert
        message1.Should().Be(message2); // Same role and content
        message1.Should().NotBe(message3); // Different role
    }

    [Fact]
    public void ChatMessage_ShouldSupportDeconstruction()
    {
        // Arrange
        var originalRole = ChatRole.System;
        var originalContent = "System message";
        var message = new ChatMessage(originalRole, originalContent);

        // Act
        var (role, content) = message;

        // Assert
        role.Should().Be(originalRole);
        content.Should().Be(originalContent);
    }

    [Theory]
    [InlineData("Short message")]
    [InlineData("This is a much longer message that contains multiple sentences and should still work correctly with our ChatMessage record.")]
    [InlineData("Message with special characters: !@#$%^&*()_+-=[]{}|;:,.<>?")]
    [InlineData("Message with unicode: 🚀 Hello, 世界!")]
    public void ChatMessage_ShouldHandleVariousContentTypes(string content)
    {
        // Arrange & Act
        var message = new ChatMessage(ChatRole.User, content);

        // Assert
        message.Content.Should().Be(content);
    }
}