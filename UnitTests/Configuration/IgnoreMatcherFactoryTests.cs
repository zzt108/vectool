// ✅ FULL FILE VERSION

using NUnit.Framework;
using Shouldly;
using VecTool.Configuration.Exclusion;

namespace VecTool.UnitTests.Configuration.Exclusion;

[TestFixture]
public class IgnoreMatcherFactoryTests
{
    [Test]
    public void Should_Create_GitignoreParserNet_Adapter()
    {
        // Act
        var matcher = IgnoreMatcherFactory.Create(IgnoreLibraryType.GitignoreParserNet);

        // Assert
        matcher.ShouldNotBeNull();
        matcher.ShouldBeOfType<GitignoreParserNetAdapter>();
    }

    [Test]
    public void Should_Create_MabDotIgnore_Adapter()
    {
        // Act
        var matcher = IgnoreMatcherFactory.Create(IgnoreLibraryType.MabDotIgnore);

        // Assert
        matcher.ShouldNotBeNull();
        matcher.ShouldBeOfType<MabDotIgnoreAdapter>();
    }

    [Test]
    public void Should_Throw_On_Invalid_Library_Type()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            IgnoreMatcherFactory.Create((IgnoreLibraryType)999));
    }

    [Test]
    public void Should_Load_Patterns_When_RootPath_Provided()
    {
        // Arrange
        var testRepoPath = Path.Combine(Path.GetTempPath(), $"test-repo-{Guid.NewGuid()}");
        Directory.CreateDirectory(testRepoPath);

        try
        {
            var vtignorePath = Path.Combine(testRepoPath, ".vtignore");
            File.WriteAllLines(vtignorePath, new[] { "*.dll" });

            // Act
            var matcher = IgnoreMatcherFactory.Create(
                IgnoreLibraryType.MabDotIgnore,
                testRepoPath);

            // Assert
            matcher.IsIgnored("test.dll", false).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(testRepoPath))
            {
                Directory.Delete(testRepoPath, recursive: true);
            }
        }
    }
}