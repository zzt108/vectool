// ✅ FULL FILE VERSION
using NUnit.Framework;
using Shouldly;
using VecTool.Core.Services;
using VecTool.Core.AI;
using VecTool.Core.Models;
using NSubstitute;

namespace UnitTests.Core.Services;

[TestFixture]
public sealed class GitCommitMessageGeneratorTests
{
    private ILlmProvider _mockProvider;
    private GitCommitMessageGenerator _generator;

    [SetUp]
    public void Setup()
    {
        _mockProvider = Substitute.For<ILlmProvider>();
        _generator = new GitCommitMessageGenerator(_mockProvider);
    }

    [Test]
    public async Task GenerateAsync_ValidDiff_ReturnsCommitMessage()
    {
        // Arrange
        var diff = "+++ PromptCategorizer.cs\n+public class PromptCategorizer";
        var context = new CommitContext { Repo = "VecTool", Phase = "4.6.1.5" };
        _mockProvider
            .RequestAsync(Arg.Any<string>())
            .Returns("Add PromptCategorizer service");

        // Act
        var result = await _generator.GenerateAsync(diff, context);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("Add PromptCategorizer service");
    }

    [Test]
    public async Task GenerateAsync_MessageTooLong_Truncates()
    {
        // Arrange
        var diff = "+++ file.cs";
        var context = new CommitContext { Repo = "VecTool" };
        var longMessage = new string('X', 100);
        _mockProvider
            .RequestAsync(Arg.Any<string>())
            .Returns(longMessage);

        // Act
        var result = await _generator.GenerateAsync(diff, context);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeLessThanOrEqualTo(72);
    }

    [Test]
    public async Task GenerateAsync_EmptyDiff_ReturnsNull()
    {
        // Arrange
        var context = new CommitContext { Repo = "VecTool" };

        // Act
        var result = await _generator.GenerateAsync("", context);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task GenerateAsync_ProviderThrows_ReturnsNull()
    {
        // Arrange
        var diff = "+++ file.cs";
        var context = new CommitContext { Repo = "VecTool" };
        _mockProvider
            .RequestAsync(Arg.Any<string>())
            .Returns<string>(_ => throw new TimeoutException());

        // Act
        var result = await _generator.GenerateAsync(diff, context);

        // Assert
        result.ShouldBeNull();
    }
}
