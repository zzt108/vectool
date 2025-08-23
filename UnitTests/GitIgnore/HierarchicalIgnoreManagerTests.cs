using Shouldly;
using GitIgnore.Services;

namespace UnitTests.GitIgnore
{
    [TestFixture]
    public class HierarchicalIgnoreManagerTests
    {
        private string _testRootDirectory;
        private HierarchicalIgnoreManager _manager;

        [SetUp]
        public void Setup()
        {
            // Create temporary test directory structure
            _testRootDirectory = Path.Combine(Path.GetTempPath(), $"GitIgnoreTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testRootDirectory);

            CreateTestDirectoryStructure();
            _manager = new HierarchicalIgnoreManager(_testRootDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            _manager?.Dispose();
            
            if (Directory.Exists(_testRootDirectory))
            {
                Directory.Delete(_testRootDirectory, true);
            }
        }

        private void CreateTestDirectoryStructure()
        {
            // Root .gitignore
            var rootGitIgnore = Path.Combine(_testRootDirectory, ".gitignore");
            File.WriteAllLines(rootGitIgnore, new[]
            {
                "*.log",
                "bin/",
                "obj/",
                "temp/"
            });

            // Create subdirectories
            var srcDir = Path.Combine(_testRootDirectory, "src");
            Directory.CreateDirectory(srcDir);

            var configDir = Path.Combine(_testRootDirectory, "config");  
            Directory.CreateDirectory(configDir);

            var testsDir = Path.Combine(srcDir, "tests");
            Directory.CreateDirectory(testsDir);

            // Config .gitignore (overrides some root patterns)
            var configGitIgnore = Path.Combine(configDir, ".gitignore");
            File.WriteAllLines(configGitIgnore, new[]
            {
                "!important.log",  // Negation - don't ignore important.log in config
                "*.secret",
                "local.*"
            });

            // Tests .gitignore 
            var testsGitIgnore = Path.Combine(testsDir, ".gitignore");
            File.WriteAllLines(testsGitIgnore, new[]
            {
                "!*.log",  // Allow all logs in tests directory
                "coverage/"
            });

            // Create some test files
            CreateTestFile(_testRootDirectory, "app.log");
            CreateTestFile(_testRootDirectory, "readme.txt");
            CreateTestFile(configDir, "important.log");
            CreateTestFile(configDir, "config.secret");
            CreateTestFile(testsDir, "test.log");
            CreateTestFile(testsDir, "results.txt");

            // Create directories
            Directory.CreateDirectory(Path.Combine(_testRootDirectory, "bin"));
            Directory.CreateDirectory(Path.Combine(_testRootDirectory, "temp"));
            Directory.CreateDirectory(Path.Combine(testsDir, "coverage"));
        }

        private void CreateTestFile(string directory, string fileName)
        {
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, $"Test content for {fileName}");
        }

        [Test]
        public void Constructor_WithValidDirectory_ShouldInitialize()
        {
            // Act & Assert
            _manager.ShouldNotBeNull();
            
            var stats = _manager.GetStatistics();
            stats.GitIgnoreFileCount.ShouldBe(3); // Root, config, tests
            stats.TotalPatterns.ShouldBeGreaterThan(0);
        }

        [Test]
        public void Constructor_WithInvalidDirectory_ShouldThrowException()
        {
            // Arrange
            var invalidDirectory = @"C:\NonExistentDirectory\Invalid";

            // Act & Assert
            Should.Throw<DirectoryNotFoundException>(() => new HierarchicalIgnoreManager(invalidDirectory));
        }

        [Test]
        public void ShouldIgnore_RootPatterns_ShouldIgnoreCorrectly()
        {
            // Arrange
            var logFile = Path.Combine(_testRootDirectory, "app.log");
            var txtFile = Path.Combine(_testRootDirectory, "readme.txt");
            var binDir = Path.Combine(_testRootDirectory, "bin");

            // Act & Assert
            _manager.ShouldIgnore(logFile, false).ShouldBe(true);   // *.log ignored
            _manager.ShouldIgnore(txtFile, false).ShouldBe(false);  // .txt not ignored
            _manager.ShouldIgnore(binDir, true).ShouldBe(true);     // bin/ ignored
        }

        [Test]
        public void ShouldIgnore_NegationPatterns_ShouldOverrideParent()
        {
            // Arrange
            var importantLog = Path.Combine(_testRootDirectory, "config", "important.log");
            var regularLog = Path.Combine(_testRootDirectory, "config", "app.log");

            // Act & Assert
            _manager.ShouldIgnore(importantLog, false).ShouldBe(false); // !important.log negation
            _manager.ShouldIgnore(regularLog, false).ShouldBe(true);    // Still ignored by root *.log
        }

        [Test]
        public void ShouldIgnore_NestedNegationPatterns_ShouldWork()
        {
            // Arrange
            var testLog = Path.Combine(_testRootDirectory, "src", "tests", "test.log");
            var rootLog = Path.Combine(_testRootDirectory, "app.log");

            // Act & Assert  
            _manager.ShouldIgnore(testLog, false).ShouldBe(false); // !*.log in tests overrides root
            _manager.ShouldIgnore(rootLog, false).ShouldBe(true);  // Still ignored at root level
        }

        [Test]
        public void ShouldIgnore_LocalPatterns_ShouldApplyLocally()
        {
            // Arrange
            var secretFile = Path.Combine(_testRootDirectory, "config", "app.secret");
            var rootSecretFile = Path.Combine(_testRootDirectory, "app.secret");

            // Act & Assert
            _manager.ShouldIgnore(secretFile, false).ShouldBe(true);     // *.secret in config
            _manager.ShouldIgnore(rootSecretFile, false).ShouldBe(false); // Not ignored at root
        }

        [Test]
        public void ShouldIgnore_DirectoryPatterns_ShouldOnlyMatchDirectories()
        {
            // Arrange
            var coverageDir = Path.Combine(_testRootDirectory, "src", "tests", "coverage");
            var coverageFile = Path.Combine(_testRootDirectory, "src", "tests", "coverage.txt");
            CreateTestFile(Path.Combine(_testRootDirectory, "src", "tests"), "coverage.txt");

            // Act & Assert
            _manager.ShouldIgnore(coverageDir, true).ShouldBe(true);   // coverage/ directory ignored
            _manager.ShouldIgnore(coverageFile, false).ShouldBe(false); // coverage.txt file not ignored
        }

        [Test]
        public void GetNonIgnoredPaths_ShouldReturnCorrectPaths()
        {
            // Act
            var nonIgnoredPaths = _manager.GetNonIgnoredPaths(_testRootDirectory, false).ToList();

            // Assert
            nonIgnoredPaths.ShouldNotBeEmpty();
            
            // Should include readme.txt but not app.log
            nonIgnoredPaths.Any(p => Path.GetFileName(p) == "readme.txt").ShouldBe(true);
            nonIgnoredPaths.Any(p => Path.GetFileName(p) == "app.log").ShouldBe(false);
        }

        [Test]
        public void GetStatistics_ShouldReturnCorrectStats()
        {
            // Act
            var stats = _manager.GetStatistics();

            // Assert
            stats.ShouldNotBeNull();
            stats.GitIgnoreFileCount.ShouldBe(3);
            stats.TotalPatterns.ShouldBeGreaterThan(5);
            stats.NegationPatterns.ShouldBe(2); // !important.log and !*.log
            stats.DirectoryOnlyPatterns.ShouldBe(4); // bin/, obj/, temp/, coverage/
            stats.RootDirectory.ShouldBe(_testRootDirectory);
        }

        [Test]
        public void RefreshCache_ShouldUpdatePatternsFromModifiedFiles()
        {
            // Arrange
            var rootGitIgnore = Path.Combine(_testRootDirectory, ".gitignore");
            var testFile = Path.Combine(_testRootDirectory, "new.tmp");
            CreateTestFile(_testRootDirectory, "new.tmp");

            // Initially should not be ignored
            _manager.ShouldIgnore(testFile, false).ShouldBe(false);

            // Modify .gitignore to add new pattern
            File.AppendAllLines(rootGitIgnore, new[] { "*.tmp" });

            // Act
            _manager.RefreshCache();

            // Assert
            _manager.ShouldIgnore(testFile, false).ShouldBe(true);
        }

        [Test]
        public void ShouldIgnore_PathOutsideRoot_ShouldReturnFalse()
        {
            // Arrange
            var outsidePath = Path.Combine(Path.GetTempPath(), "outside.log");

            // Act & Assert
            _manager.ShouldIgnore(outsidePath, false).ShouldBe(false);
        }

        [Test]
        public void ShouldIgnore_EmptyOrNullPath_ShouldReturnFalse()
        {
            // Act & Assert
            _manager.ShouldIgnore(null, false).ShouldBe(false);
            _manager.ShouldIgnore("", false).ShouldBe(false);
            _manager.ShouldIgnore("   ", false).ShouldBe(false);
        }
    }
}