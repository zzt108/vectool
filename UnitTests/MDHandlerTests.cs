
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Collections.Generic;
using DocXHandler;

namespace UnitTests
{
    [TestFixture]
    public class MDHandlerTests
    {
        private string _testFolderPath;
        private string _outputDir;
        private string _outputMDPath;
        private MDHandler _mdHandler;
        private VectorStoreConfig _vectorStoreConfig;

        [SetUp]
        public void Setup()
        {
            _testFolderPath = Path.Combine(Path.GetTempPath(), "MDHandlerTests");
            Directory.CreateDirectory(_testFolderPath);
            _outputDir = Path.Combine(Path.GetTempPath(), "output");
            Directory.CreateDirectory(_outputDir);
            _outputMDPath = Path.Combine(_outputDir, "output.md");
                        _mdHandler = new MDHandler(null);
            _vectorStoreConfig = new VectorStoreConfig(new List<string>() { _testFolderPath });
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_testFolderPath))
            {
                Directory.Delete(_testFolderPath, true);
            }
            if (Directory.Exists(_outputDir))
            {
                Directory.Delete(_outputDir, true);
            }
        }

        [Test]
        public void ExportSelectedFolders_WithTextFiles_ShouldCreateMDFileWithContent()
        {
            // Arrange
            var textFilePath1 = Path.Combine(_testFolderPath, "test1.txt");
            var textFilePath2 = Path.Combine(_testFolderPath, "test2.txt");
            File.WriteAllText(textFilePath1, "Hello");
            File.WriteAllText(textFilePath2, "World");

            var folders = new List<string> { _testFolderPath };

            // Act
            _mdHandler.ExportSelectedFolders(folders, _outputMDPath, _vectorStoreConfig);

            // Assert
            File.Exists(_outputMDPath).ShouldBeTrue();
            var content = File.ReadAllText(_outputMDPath);
            content.ShouldContain("# Folder: MDHandlerTests");
            content.ShouldContain("## File: MDHandlerTests/test1.txt");
            content.ShouldContain("```txt\r\nHello\r\n```");
            content.ShouldContain("## File: MDHandlerTests/test2.txt");
            content.ShouldContain("```txt\r\nWorld\r\n```");
        }

        [Test]
        public void ExportSelectedFolders_EmptyFolder_ShouldCreateEmptyMDFile()
        {
            // Arrange
            var folders = new List<string> { _testFolderPath };

            // Act
            _mdHandler.ExportSelectedFolders(folders, _outputMDPath, _vectorStoreConfig);

            // Assert
            File.Exists(_outputMDPath).ShouldBeTrue();
            var content = File.ReadAllText(_outputMDPath);
            content.ShouldContain("# Folder: MDHandlerTests");
            content.ShouldNotContain("## File:");
        }

        [Test]
        public void ExportSelectedFolders_WithNonTextFile_ShouldNotBeIncluded()
        {
            // Arrange
            var nonTextFilePath = Path.Combine(_testFolderPath, "image.png");
            File.WriteAllBytes(nonTextFilePath, new byte[] { 1, 2, 3 });
            var folders = new List<string> { _testFolderPath };

            // Act
            _mdHandler.ExportSelectedFolders(folders, _outputMDPath, _vectorStoreConfig);

            // Assert
            File.Exists(_outputMDPath).ShouldBeTrue();
            var content = File.ReadAllText(_outputMDPath);
            content.ShouldContain("# Folder: MDHandlerTests");
            content.ShouldNotContain("## File: image.png");
        }
    }
}
