using NUnit.Framework;
using Shouldly;
using VecTool.Configuration.Exclusion;

namespace VecTool.UnitTests.Configuration.Exclusion;

[TestFixture]
public class MabDotIgnoreAdapterTests
{
    private string _testRepoPath = null!;
    private MabDotIgnoreAdapter _adapter = null!;

    [SetUp]
    public void Setup()
    {
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"test-repo-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);
        _adapter = new MabDotIgnoreAdapter();
    }

    [TearDown]
    public void Teardown()
    {
        _adapter?.Dispose();

        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, recursive: true);
        }
    }

    [Test]
    public void Should_Exclude_Dll_Files()
    {
        // Arrange
        var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
        File.WriteAllLines(vtignorePath, new[] { "*.dll" });

        // Act
        _adapter.LoadFromRoot(_testRepoPath);

        // Assert
        _adapter.IsIgnored("MyLibrary.dll", false).ShouldBeTrue();
        _adapter.IsIgnored("MyLibrary.cs", false).ShouldBeFalse();
    }

    [Test]
    public void Should_Exclude_Bin_Folder()
    {
        // Arrange
        var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
        File.WriteAllLines(vtignorePath, new[] { "bin/" });

        // Act
        _adapter.LoadFromRoot(_testRepoPath);

        // Assert
        _adapter.IsIgnored("bin", true).ShouldBeTrue();
        _adapter.IsIgnored("src", true).ShouldBeFalse();
    }

    [Test]
    public void Should_Handle_Wildcard_Patterns()
    {
        // Arrange
        var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
        File.WriteAllLines(vtignorePath, new[] { "**/*.pdb" });

        // Act
        _adapter.LoadFromRoot(_testRepoPath);

        // Assert
        _adapter.IsIgnored("Debug/MyApp.pdb", false).ShouldBeTrue();
        _adapter.IsIgnored("Release/Temp/Test.pdb", false).ShouldBeTrue();
        _adapter.IsIgnored("Program.cs", false).ShouldBeFalse();
    }

    [Test]
    public void Should_Throw_When_No_Patterns_Loaded()
    {
        // Act - don't load any patterns
        Action loadFromRoot = () => _adapter.LoadFromRoot(_testRepoPath);
        loadFromRoot.ShouldThrow<InvalidOperationException>().Message.ShouldContain("No ignore patterns found in .gitignore or .vtignore");
    }

    [Test]
    public void Should_Load_Both_Gitignore_And_VtIgnore()
    {
        // Arrange
        var gitignorePath = Path.Combine(_testRepoPath, ".gitignore");
        File.WriteAllLines(gitignorePath, new[] { "*.dll" });

        var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
        File.WriteAllLines(vtignorePath, new[] { "*.exe" });

        // Act
        _adapter.LoadFromRoot(_testRepoPath);

        // Assert
        _adapter.IsIgnored("app.dll", false).ShouldBeTrue();
        _adapter.IsIgnored("app.exe", false).ShouldBeTrue();
        _adapter.IsIgnored("app.cs", false).ShouldBeFalse();
    }
}