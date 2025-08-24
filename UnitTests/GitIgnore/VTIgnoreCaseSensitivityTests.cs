using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Linq;
using DocXHandler;                  // for VectorStoreConfig

namespace UnitTests.GitIgnore
{
    [TestFixture]
    public class VTIgnoreCaseSensitivityTests
    {
        private string _testRootDirectory;
        private VectorStoreConfig _config;

        [SetUp]
        public void Setup()
        {
            _testRootDirectory = Path.Combine(Path.GetTempPath(), $"VTIgnoreCaseTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testRootDirectory);

            // Create a .vtignore to skip license-2.0.txt and *.log, skip temp/
            File.WriteAllLines(Path.Combine(_testRootDirectory, ".vtignore"), new[]
            {
                "license-2.0.txt",
                "*.log",
                "temp/"
            });

            _config = new VectorStoreConfig(new List<string> { _testRootDirectory });
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testRootDirectory))
                Directory.Delete(_testRootDirectory, true);
        }

        private void CreateTestFile(string relativePath)
        {
            var full = Path.Combine(_testRootDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, "content");
        }

        [TestCase("license-2.0.txt", false)]
        [TestCase("LICENSE-2.0.TXT", false)]
        [TestCase("other-file.txt", true)]
        [TestCase("app.log", false)]
        [TestCase("INFO.LOG", false)]
        [TestCase("notes.txt", true)]
        [TestCase("temp/data.txt", false)]
        public void EnumerateFilesRespectingGitIgnore_ShouldRespectVtignore(
            string relativePath,
            bool shouldBeReturned)
        {
            // Arrange
            CreateTestFile(relativePath);

            // Act
            var files = _testRootDirectory
                .EnumerateFilesRespectingGitIgnore(_config)
                .Select(Path.GetFileName)
                .ToList();

            // Assert
            if (shouldBeReturned)
                files.ShouldContain(Path.GetFileName(relativePath));
            else
                files.ShouldNotContain(Path.GetFileName(relativePath));
        }
    }
}
