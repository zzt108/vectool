using NUnit.Framework;
using Shouldly;
using VecTool.Core.Services;
using VecTool.Core.AI;
using VecTool.Core.Models;
using NSubstitute;

namespace UnitTests.Core.Services;

[TestFixture]
public sealed class PromptCategorizerTests
{
    private ILlmProvider _mockProvider;
    private PromptCategorizer _categorizer;

    [SetUp]
    public void Setup()
    {
        _mockProvider = Substitute.For<ILlmProvider>();
        _categorizer = new PromptCategorizer(_mockProvider);
    }

    [Test]
    public async Task SuggestCategoryAsync_ValidResponse_ParsesCorrectly()
    {
        // Arrange
        var content = "This is a prompt about VecTool UI development";
        _mockProvider
            .RequestAsync(Arg.Any<string>())
            .Returns("work/VecTool/Spaces");

        // Act
        var result = await _categorizer.SuggestCategoryAsync(content);

        // Assert
        result.ShouldNotBeNull();
        result.Area.ShouldBe("work");
        result.Project.ShouldBe("VecTool");
        result.Category.ShouldBe("Spaces");
        result.SuggestedPath.ShouldBe("work/VecTool/Spaces");
    }

    [Test]
    public async Task SuggestCategoryAsync_InvalidFormat_ReturnsNull()
    {
        // Arrange
        var content = "Test content";
        _mockProvider
            .RequestAsync(Arg.Any<string>())
            .Returns("invalid response");

        // Act
        var result = await _categorizer.SuggestCategoryAsync(content);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task SuggestCategoryAsync_LongContent_TruncatesTo1000Chars()
    {
        // Arrange
        var content = new string('X', 2000);
        _mockProvider
            .RequestAsync(Arg.Is<string>(s => s.Contains(new string('X', 1000))))
            .Returns("work/VecTool/Guides");

        // Act
        var result = await _categorizer.SuggestCategoryAsync(content);

        // Assert
        result.ShouldNotBeNull();
        await _mockProvider.Received(1).RequestAsync(Arg.Any<string>());
    }

    [Test]
    public async Task SuggestCategoryAsync_ProviderThrows_ReturnsNull()
    {
        // Arrange
        var content = "Test content";
        _mockProvider
            .RequestAsync(Arg.Any<string>())
            .Returns<string>(_ => throw new HttpRequestException("Network error"));

        // Act
        var result = await _categorizer.SuggestCategoryAsync(content);

        // Assert
        result.ShouldBeNull();
    }
}
