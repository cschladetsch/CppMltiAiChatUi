using System.Text.Json;
using FluentAssertions;
using MultiLLM.Core.Models;

namespace MultiLLM.Core.Tests.Models;

public class ModelDefinitionTests
{
    [Fact]
    public void ModelDefinition_ShouldCreateWithRequiredProperties()
    {
        // Arrange & Act
        var model = new ModelDefinition
        {
            Name = "Test Model",
            Provider = "test-provider",
            ModelId = "test-model-id"
        };

        // Assert
        model.Name.Should().Be("Test Model");
        model.Provider.Should().Be("test-provider");
        model.ModelId.Should().Be("test-model-id");
        model.Description.Should().Be(string.Empty); // Default value
        model.Endpoint.Should().BeNull(); // Default value
        model.Parameters.Should().BeEmpty(); // Default value
    }

    [Fact]
    public void ModelDefinition_ShouldSetAllProperties()
    {
        // Arrange
        var parameters = new List<ModelParameterDefinition>
        {
            new() { Name = "temperature", Description = "Controls randomness" },
            new() { Name = "max_tokens", Description = "Maximum tokens to generate" }
        };

        // Act
        var model = new ModelDefinition
        {
            Name = "GPT-4",
            Provider = "openai",
            ModelId = "gpt-4",
            Description = "OpenAI's most capable model",
            Endpoint = "https://api.openai.com/v1/chat/completions",
            Parameters = parameters
        };

        // Assert
        model.Name.Should().Be("GPT-4");
        model.Provider.Should().Be("openai");
        model.ModelId.Should().Be("gpt-4");
        model.Description.Should().Be("OpenAI's most capable model");
        model.Endpoint.Should().Be("https://api.openai.com/v1/chat/completions");
        model.Parameters.Should().HaveCount(2);
        model.Parameters.Should().BeEquivalentTo(parameters);
    }

    [Fact]
    public void ModelDefinition_ToStringShouldReturnName()
    {
        // Arrange
        var model = new ModelDefinition
        {
            Name = "Test Model",
            Provider = "test",
            ModelId = "test-id"
        };

        // Act
        var result = model.ToString();

        // Assert
        result.Should().Be("Test Model");
    }

    [Fact]
    public void ModelDefinition_ShouldSerializeToJson()
    {
        // Arrange
        var model = new ModelDefinition
        {
            Name = "Test Model",
            Provider = "test-provider",
            ModelId = "test-model",
            Description = "A test model",
            Parameters = new List<ModelParameterDefinition>
            {
                new()
                {
                    Name = "temperature",
                    Description = "Controls randomness",
                    Default = JsonDocument.Parse("0.7").RootElement
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(model);
        var deserialized = JsonSerializer.Deserialize<ModelDefinition>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be(model.Name);
        deserialized.Provider.Should().Be(model.Provider);
        deserialized.ModelId.Should().Be(model.ModelId);
        deserialized.Description.Should().Be(model.Description);
        deserialized.Parameters.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ModelDefinition_ShouldHandleEmptyDescription(string? description)
    {
        // Arrange & Act
        var model = new ModelDefinition
        {
            Name = "Test",
            Provider = "test",
            ModelId = "test",
            Description = description ?? string.Empty
        };

        // Assert
        model.Description.Should().Be(description ?? string.Empty);
    }

    [Fact]
    public void ModelDefinition_ShouldHandleNullEndpoint()
    {
        // Arrange & Act
        var model = new ModelDefinition
        {
            Name = "Test",
            Provider = "test",
            ModelId = "test",
            Endpoint = null
        };

        // Assert
        model.Endpoint.Should().BeNull();
    }

    [Fact]
    public void ModelDefinition_ShouldHandleEmptyParameters()
    {
        // Arrange & Act
        var model = new ModelDefinition
        {
            Name = "Test",
            Provider = "test",
            ModelId = "test",
            Parameters = new List<ModelParameterDefinition>()
        };

        // Assert
        model.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void ModelDefinition_ShouldPreserveParameterOrder()
    {
        // Arrange
        var parameters = new List<ModelParameterDefinition>
        {
            new() { Name = "temperature" },
            new() { Name = "max_tokens" },
            new() { Name = "top_p" }
        };

        // Act
        var model = new ModelDefinition
        {
            Name = "Test",
            Provider = "test",
            ModelId = "test",
            Parameters = parameters
        };

        // Assert
        model.Parameters.Should().ContainInOrder(parameters);
    }
}