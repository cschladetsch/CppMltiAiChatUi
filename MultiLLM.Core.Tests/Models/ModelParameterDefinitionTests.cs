using System.Text.Json;
using FluentAssertions;
using MultiLLM.Core.Models;

namespace MultiLLM.Core.Tests.Models;

public class ModelParameterDefinitionTests
{
    [Fact]
    public void ModelParameterDefinition_ShouldCreateWithRequiredProperties()
    {
        // Arrange & Act
        var parameter = new ModelParameterDefinition
        {
            Name = "temperature"
        };

        // Assert
        parameter.Name.Should().Be("temperature");
        parameter.Description.Should().Be(string.Empty); // Default value
        parameter.Default.Should().BeNull(); // Default value
        parameter.DefaultDisplay.Should().Be("—"); // Default display for null
    }

    [Fact]
    public void ModelParameterDefinition_ShouldSetAllProperties()
    {
        // Arrange
        var defaultValue = JsonDocument.Parse("0.7").RootElement;

        // Act
        var parameter = new ModelParameterDefinition
        {
            Name = "temperature",
            Description = "Controls the randomness of responses",
            Default = defaultValue
        };

        // Assert
        parameter.Name.Should().Be("temperature");
        parameter.Description.Should().Be("Controls the randomness of responses");
        parameter.Default.Should().NotBeNull();
        parameter.Default!.Value.GetDouble().Should().Be(0.7);
    }

    [Theory]
    [InlineData("0.7", "0.7")]
    [InlineData("42", "42")]
    [InlineData("true", "true")]
    [InlineData("\"text\"", "text")]
    [InlineData("null", "—")]
    public void ModelParameterDefinition_DefaultDisplayShouldFormatCorrectly(string jsonValue, string expectedDisplay)
    {
        // Arrange
        var defaultValue = JsonDocument.Parse(jsonValue).RootElement;
        var parameter = new ModelParameterDefinition
        {
            Name = "test_param",
            Default = defaultValue
        };

        // Act
        var display = parameter.DefaultDisplay;

        // Assert
        display.Should().Be(expectedDisplay);
    }

    [Fact]
    public void ModelParameterDefinition_DefaultDisplayShouldHandleNullDefault()
    {
        // Arrange
        var parameter = new ModelParameterDefinition
        {
            Name = "test_param",
            Default = null
        };

        // Act
        var display = parameter.DefaultDisplay;

        // Assert
        display.Should().Be("—");
    }

    [Fact]
    public void ModelParameterDefinition_ShouldSerializeToJson()
    {
        // Arrange
        var parameter = new ModelParameterDefinition
        {
            Name = "temperature",
            Description = "Controls randomness",
            Default = JsonDocument.Parse("0.7").RootElement
        };

        // Act
        var json = JsonSerializer.Serialize(parameter);
        var deserialized = JsonSerializer.Deserialize<ModelParameterDefinition>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be(parameter.Name);
        deserialized.Description.Should().Be(parameter.Description);
        deserialized.Default.Should().NotBeNull();
        deserialized.Default!.Value.GetDouble().Should().Be(0.7);
    }

    [Fact]
    public void ModelParameterDefinition_ShouldHandleComplexJsonDefaults()
    {
        // Arrange
        var complexJson = JsonDocument.Parse("""{"key": "value", "number": 42}""").RootElement;
        var parameter = new ModelParameterDefinition
        {
            Name = "complex_param",
            Default = complexJson
        };

        // Act
        var display = parameter.DefaultDisplay;

        // Assert
        display.Should().Contain("key");
        display.Should().Contain("value");
        display.Should().Contain("42");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ModelParameterDefinition_ShouldHandleEmptyDescription(string? description)
    {
        // Arrange & Act
        var parameter = new ModelParameterDefinition
        {
            Name = "test_param",
            Description = description ?? string.Empty
        };

        // Assert
        parameter.Description.Should().Be(description ?? string.Empty);
    }

    [Fact]
    public void ModelParameterDefinition_ShouldHandleArrayDefaults()
    {
        // Arrange
        var arrayJson = JsonDocument.Parse("""[1, 2, 3]""").RootElement;
        var parameter = new ModelParameterDefinition
        {
            Name = "array_param",
            Default = arrayJson
        };

        // Act
        var display = parameter.DefaultDisplay;

        // Assert
        display.Should().Be("[1, 2, 3]");
    }

    [Theory]
    [InlineData("temperature")]
    [InlineData("max_tokens")]
    [InlineData("top_p")]
    [InlineData("frequency_penalty")]
    [InlineData("presence_penalty")]
    public void ModelParameterDefinition_ShouldSupportCommonParameterNames(string paramName)
    {
        // Arrange & Act
        var parameter = new ModelParameterDefinition
        {
            Name = paramName
        };

        // Assert
        parameter.Name.Should().Be(paramName);
    }

    [Fact]
    public void ModelParameterDefinition_ShouldHandleBooleanDefaults()
    {
        // Arrange
        var booleanJson = JsonDocument.Parse("true").RootElement;
        var parameter = new ModelParameterDefinition
        {
            Name = "boolean_param",
            Default = booleanJson
        };

        // Act
        var display = parameter.DefaultDisplay;

        // Assert
        display.Should().Be("true");
    }

    [Fact]
    public void ModelParameterDefinition_ShouldHandleNumberDefaults()
    {
        // Arrange
        var numberJson = JsonDocument.Parse("123.456").RootElement;
        var parameter = new ModelParameterDefinition
        {
            Name = "number_param",
            Default = numberJson
        };

        // Act
        var display = parameter.DefaultDisplay;

        // Assert
        display.Should().Be("123.456");
    }
}