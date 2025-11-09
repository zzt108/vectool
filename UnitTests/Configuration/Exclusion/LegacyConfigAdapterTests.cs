namespace VecTool.UnitTests.Configuration.Exclusion;

using NUnit.Framework;
using Shouldly;
using VecTool.Configuration;
using VecTool.Configuration.Exclusion;

[TestFixture]
public class LegacyConfigAdapterTests:IgnoreAdapterTestBase
{
    private LegacyConfigAdapter _adapter = null!;
    private VectorStoreConfig _config = null!;

    [SetUp]
    public void Setup()
    {
        _config = new VectorStoreConfig();
    }

    [TearDown]
    public void Teardown()
    {
        _adapter?.Dispose();
    }

    protected override IIgnorePatternMatcher CreateAdapter()
    {
        return new LegacyConfigAdapter(_config);
    }

    protected override void SetupTestPatterns(IIgnorePatternMatcher adapter, string[] patterns)
    {
        // For LegacyConfigAdapter, populate the _config object
        _config.ExcludedFiles.Clear();
        _config.ExcludedFolders.Clear();

        foreach (var pattern in patterns)
        {
            if (pattern.StartsWith('.') && !pattern.Contains('/'))
            {
                // File extension pattern (e.g., ".log")
                _config.ExcludedFiles.Add(pattern);
            }
            else if (!pattern.Contains('.') || pattern.StartsWith('.'))
            {
                // Folder pattern or extension  
                _config.ExcludedFolders.Add(pattern);
            }
            else
            {
                // Default: treat as file extension if it's short
                if (pattern.Length <= 10)
                    _config.ExcludedFiles.Add(pattern);
                else
                    _config.ExcludedFolders.Add(pattern);
            }
        }
    }

    // ======== File Exclusion Tests ========

    [Test]
    public void ShouldExcludeFileByExtension()
    {
        // Arrange
        _config.ExcludedFiles.Add("*.log");
        _adapter = new LegacyConfigAdapter(_config);

        // Act
        var result = _adapter.IsIgnored("App.log", isDirectory: false);

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void ShouldNotExcludeNonMatchingFile()
    {
        // Arrange
        _config.ExcludedFiles.Add("*.log");
        _adapter = new LegacyConfigAdapter(_config);

        // Act
        var result = _adapter.IsIgnored("Program.cs", isDirectory: false);

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void ShouldExcludeMultipleFileExtensions()
    {
        // Arrange
        _config.ExcludedFiles.AddRange(new[] { "*.log", "*.tmp", "*.bak" });
        _adapter = new LegacyConfigAdapter(_config);

        // Act & Assert
        _adapter.IsIgnored("debug.log", isDirectory: false).ShouldBeTrue();
        _adapter.IsIgnored("temp.tmp", isDirectory: false).ShouldBeTrue();
        _adapter.IsIgnored("backup.bak", isDirectory: false).ShouldBeTrue();
        _adapter.IsIgnored("source.cs", isDirectory: false).ShouldBeFalse();
    }

    [Test]
    public void ShouldHandleWildcardFilePatterns()
    {
        // Arrange
        _config.ExcludedFiles.Add("*.log");
        _adapter = new LegacyConfigAdapter(_config);

        // Act & Assert (pattern like "*.log" uses regex internally)
        _adapter.IsIgnored("app.log", isDirectory: false).ShouldBeTrue();
        _adapter.IsIgnored("debug.log", isDirectory: false).ShouldBeTrue();
    }

    // ======== Folder Exclusion Tests ========

    [Test]
    public void ShouldExcludeFolderByName()
    {
        // Arrange
        _config.ExcludedFolders.Add("bin");
        _adapter = new LegacyConfigAdapter(_config);

        // Act
        var result = _adapter.IsIgnored("bin", isDirectory: true);

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void ShouldNotExcludeNonMatchingFolder()
    {
        // Arrange
        _config.ExcludedFolders.Add("bin");
        _adapter = new LegacyConfigAdapter(_config);

        // Act
        var result = _adapter.IsIgnored("src", isDirectory: true);

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void ShouldExcludeMultipleFolders()
    {
        // Arrange
        _config.ExcludedFolders.AddRange(new[] { "bin", "obj", ".git" });
        _adapter = new LegacyConfigAdapter(_config);

        // Act & Assert
        _adapter.IsIgnored("bin", isDirectory: true).ShouldBeTrue();
        _adapter.IsIgnored("obj", isDirectory: true).ShouldBeTrue();
        _adapter.IsIgnored(".git", isDirectory: true).ShouldBeTrue();
        _adapter.IsIgnored("src", isDirectory: true).ShouldBeFalse();
    }

    // ======== Config State Tests ========

    [Test]
    public void ShouldReturnFalseWhenConfigNotSet()
    {
        // Arrange - don't call SetConfig

        // Act
        var fileResult = _adapter.IsIgnored("anything.log", isDirectory: false);
        var folderResult = _adapter.IsIgnored("bin", isDirectory: true);

        // Assert
        fileResult.ShouldBeFalse("No config set means nothing is excluded");
        folderResult.ShouldBeFalse("No config set means nothing is excluded");
    }

    [Test]
    public void ShouldHandleNullOrEmptyPath()
    {
        // Arrange
        _config.ExcludedFiles.Add("*.log");
        _adapter = new LegacyConfigAdapter(_config);

        // Act & Assert
        _adapter.IsIgnored(null!, isDirectory: false).ShouldBeFalse();
        _adapter.IsIgnored("", isDirectory: false).ShouldBeFalse();
        _adapter.IsIgnored("   ", isDirectory: false).ShouldBeFalse();
    }

    // ======== Path Extraction Tests ========

    [Test]
    public void ShouldExtractFileNameFromFullPath()
    {
        // Arrange
        _config.ExcludedFiles.Add("*.log");
        _adapter = new LegacyConfigAdapter(_config);

        // Act
        var result = _adapter.IsIgnored("C:\\Temp\\logs\\app.log", isDirectory: false);

        // Assert
        result.ShouldBeTrue("Should extract 'app.log' from full path");
    }

    [Test]
    public void ShouldExtractFolderNameFromPath()
    {
        // Arrange
        _config.ExcludedFolders.Add("bin");
        _adapter = new LegacyConfigAdapter(_config);

        // Act
        var result = _adapter.IsIgnored("C:\\Project\\bin\\Debug", isDirectory: true);

        // Assert
        result.ShouldBeTrue("Should extract 'Debug' from path (last segment)");
    }

    // ======== Load/Set Semantics Tests ========

    [Test]
    public void ShouldSupportLoadFromRootMethod()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            _config.ExcludedFiles.Add("*.log");
            _config.ExcludedFolders.Add("bin");

            // Act
            _adapter.LoadFromRoot(tempDir);  // Uses AppConfig under the hood

            // Assert - if LoadFromRoot succeeds, adapter should be initialized
            // (behavior depends on whether app.config has settings)
            _adapter.IsIgnored("app.log", isDirectory: false);  // Should not throw
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void ShouldThrowWhenSettingNullConfig()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new LegacyConfigAdapter(null!));
    }

    [Test]
    public void DisposeShouldNotThrow()
    {
        // Arrange
        _config.ExcludedFiles.Add("*.log");
        _adapter = new LegacyConfigAdapter(_config);
        // Act & Assert - should be idempotent
        _adapter.Dispose();
        _adapter.Dispose();  // Second dispose should be safe
    }

    // ======== Integration: FileSystemTraverser Chain Tests ========

    [Test]
    public void ShouldWorkAsPartOfAdapterChain()
    {
        // Arrange - simulate adapter chain pattern
        var config = new VectorStoreConfig
        {
            ExcludedFiles = new List<string> { "*.log" },
            ExcludedFolders = new List<string> { "bin" }
        };
        var primaryAdapter = new LegacyConfigAdapter(config);

        // Act - Simulate chain: primary.IsIgnored() || fallback.IsIgnored()
        var isFileExcluded = primaryAdapter.IsIgnored("app.log", isDirectory: false);
        var isFolderExcluded = primaryAdapter.IsIgnored("bin", isDirectory: true);
        var isNormalFileIncluded = !primaryAdapter.IsIgnored("program.cs", isDirectory: false);

        // Assert
        isFileExcluded.ShouldBeTrue();
        isFolderExcluded.ShouldBeTrue();
        isNormalFileIncluded.ShouldBeTrue();
    }
}