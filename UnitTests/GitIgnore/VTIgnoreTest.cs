using NUnit.Framework;
using Shouldly;
using GitIgnore.Models;
using GitIgnore.Services;
using System.IO;

namespace UnitTests.GitIgnore
{
    [TestFixture]
    public class VTIgnoreCaseSensitivityTests
    {
        private string _testRootDirectory;
        private HierarchicalIgnoreManager _manager;

        [SetUp]
        public void Setup()
        {
            _testRootDirectory = Path.Combine(Path.GetTempPath(), $"GitIgnoreCaseTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testRootDirectory);

            CreateTestIgnoreFile();
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

        private void CreateTestIgnoreFile()
        {
            var vtIgnoreFile = Path.Combine(_testRootDirectory, ".vtignore");
            File.WriteAllLines(vtIgnoreFile, new[]
            {
                "license-2.0.txt",
                "*.log",
                "temp/"
            });
        }

        [TestCase("license-2.0.txt", true)]
        [TestCase("LICENSE-2.0.txt", true)]
        [TestCase("License-2.0.txt", true)]
        [TestCase("LICENSE-2.0.TXT", true)]
        [TestCase("other-file.txt", false)]
        public void ShouldIgnore_CaseInsensitive_ShouldMatchCorrectly(string fileName, bool expectedIgnored)
        {
            // Arrange
            var filePath = Path.Combine(_testRootDirectory, fileName);
            CreateTestFile(filePath);

            // Act
            var isIgnored = _manager.ShouldIgnore(filePath, false);

            // Assert
            isIgnored.ShouldBe(expectedIgnored, $"File '{fileName}' should {(expectedIgnored ? "" : "not ")}be ignored");
        }

        private void CreateTestFile(string filePath)
        {
            File.WriteAllText(filePath, "test content");
        }
    }
}
