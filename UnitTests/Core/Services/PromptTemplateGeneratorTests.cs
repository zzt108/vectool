using NUnit.Framework;
using Shouldly;
using VecTool.Core.Services;

namespace UnitTests.Core.Services;

[TestFixture]
public sealed class PromptTemplateGeneratorTests
{
    private PromptTemplateGenerator _generator;

    [SetUp]
    public void Setup()
    {
        _generator = new PromptTemplateGenerator();
    }

    [Test]
    public void ApplyTemplateVariables_CustomVars_Substitutes()
    {
        // Arrange
        var content = "Project: {{PROJECT}}, Version: {{VERSION}}";
        var vars = new Dictionary<string, string>
        {
            { "PROJECT", "VecTool" },
            { "VERSION", "4.6" }
        };

        // Act
        var result = _generator.ApplyTemplateVariables(content, vars);

        // Assert
        result.ShouldBe("Project: VecTool, Version: 4.6");
    }

    [Test]
    public void ApplyTemplateVariables_AutoProvided_SubstitutesTimestamp()
    {
        // Arrange
        var content = "Generated: {{TIMESTAMP}}";

        // Act
        var result = _generator.ApplyTemplateVariables(content);

        // Assert
        result.ShouldContain("Generated:");
        result.ShouldNotContain("{{TIMESTAMP}}");
    }

    [Test]
    public void ApplyTemplateVariables_AutoProvided_SubstitutesAuthor()
    {
        // Arrange
        var content = "Author: {{AUTHOR}}";

        // Act
        var result = _generator.ApplyTemplateVariables(content);

        // Assert
        result.ShouldContain("Author:");
        result.ShouldBe($"Author: {Environment.UserName}");
    }

    [Test]
    public void ApplyTemplateVariables_UnresolvedVars_LeavesPlaceholder()
    {
        // Arrange
        var content = "Missing: {{UNKNOWN_VAR}}";

        // Act
        var result = _generator.ApplyTemplateVariables(content);

        // Assert
        result.ShouldBe("Missing: {{UNKNOWN_VAR}}");
    }

    [Test]
    public void ApplyTemplateVariables_NoVars_ReturnsOriginal()
    {
        // Arrange
        var content = "Plain text without variables";

        // Act
        var result = _generator.ApplyTemplateVariables(content);

        // Assert
        result.ShouldBe(content);
    }
}
