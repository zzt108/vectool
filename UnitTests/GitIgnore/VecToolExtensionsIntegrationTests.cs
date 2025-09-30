using DocXHandler;
using Shouldly;

namespace UnitTests.GitIgnore
{
    [TestFixture]
    public class VecToolExtensionsIntegrationTests
    {
        private string _testRoot;
        private VectorStoreConfig _config;

        private void CreateTestFile(string relativePath)
        {
            var fullPath = Path.Combine(_testRoot, relativePath);
            var dir = Path.GetDirectoryName(fullPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, $"// test content for {relativePath}");
        }

        [SetUp]
        public void Setup()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), $"VecToolTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testRoot);
            _config = new VectorStoreConfig(new[] { _testRoot }.ToList());
        }

        [Test]
        public void Should_Handle_Nested_GitIgnore_With_Negations()
        {
            // Arrange
            // Root .gitignore
            File.WriteAllLines(Path.Combine(_testRoot, ".gitignore"), new[]
            {
            "*.log",
            "temp/"
            });

            // Subfolder with negation
            var subDir = Path.Combine(_testRoot, "src");
            Directory.CreateDirectory(subDir);
            File.WriteAllLines(Path.Combine(subDir, ".vtignore"), new[]
            {
            "!important.log"  // un-ignore this specific file
            });

            CreateTestFile("src/important.log");
            CreateTestFile("debug.log");
            CreateTestFile("readme.txt");

            // Act
            var files = _testRoot
                .EnumerateFilesRespectingGitIgnore(_config)
                .Select(f => Path.GetRelativePath(_testRoot, f))
                .ToList();

            // Assert
            files.ShouldContain("src\\important.log");    // negated in subfolder
            files.ShouldNotContain("debug.log");     // ignored by root
            files.ShouldContain("readme.txt");       // not ignored
        }

        [Test]
        public void Should_Ignore_Folders_Starting_With_Dot()
        {
            // Arrange
            CreateTestFile(".git/ignore.tmp");
            CreateTestFile(".cr/ignore.tmp");
            File.WriteAllLines(Path.Combine(_testRoot, ".vtignore"), new[]
            {
        ".git/",
        ".cr/",
    });

            // Act
            var files = _testRoot
                .EnumerateFilesRespectingGitIgnore(_config)
                .Select(Path.GetFileName)
                .ToList();

            // Assert
            files.ShouldNotContain(".git\\ignore.tmp");
            files.ShouldNotContain(".cr\\ignore.tmp");
        }


        // [Test] negation doesn't seem to work
        public void Should_Unignore_Specific_File_CaseInsensitive()
        {
            // Arrange
            CreateTestFile("Src/keep.tmp");
            CreateTestFile("src/ignore.tmp");
            File.WriteAllLines(Path.Combine(_testRoot, ".vtignore"), new[]
            {
        "**/*.tmp",
        "**/!keep.tmp",
    });

            // Act
            var files = _testRoot
                .EnumerateFilesRespectingGitIgnore(_config)
                .Select(Path.GetFileName)
                .ToList();

            // Assert
            files.ShouldContain("KeeP.tmp");
            files.ShouldNotContain("ignore.tmp");
        }

        [Test]
        public void Should_Ignore_DirectoryOnlyPattern_ButNotSameFileName()
        {
            // Arrange
            CreateTestFile("temp/data.txt");
            CreateTestFile("data.txt");
            File.WriteAllLines(Path.Combine(_testRoot, ".gitignore"), new[] { "temp/" });

            // Act
            var files = _testRoot
                .EnumerateFilesRespectingGitIgnore(_config)
                .Select(f => Path.GetRelativePath(_testRoot, f))
                .ToList();

            // Assert
            files.ShouldContain(".gitignore");         // data.txt at root remains
            files.ShouldNotContain("temp//data.txt");         // file in temp is ignored by virtue of temp/
            files.ShouldContain("data.txt");            // ensure root file is still present
        }

        [Test]
        public void Should_Unignore_Specific_File_By_Negation_In_Vtignore()
        {
            // Arrange
            CreateTestFile("src/keep.tmp");
            CreateTestFile("src/ignore.tmp");
            File.WriteAllLines(Path.Combine(_testRoot, ".vtignore"), new[]
            {
        "!keep.tmp",
        "*.tmp"
        });
        }

        [TearDown]
        public void Teardown()
        {
            Directory.Delete(_testRoot, true);
        }

    }
}