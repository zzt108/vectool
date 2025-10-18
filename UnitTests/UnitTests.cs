using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.Handlers.Analysis; // For IUserInterface
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles; // For IRecentFilesManager
using VecTool.Utils;

namespace UnitTests
{
    [TestFixture]
    public class MimeTypeProviderTests
    {

        [TestCase(".cs", "csharp")]
        [TestCase(".csproj", "msbuild")]
        [TestCase(".feature", "gherkin")]
        [TestCase(".unknown", "unknown")]
        [TestCase(".txt", "text")]
        [TestCase(null, "")]
        public void GetMdTag_ValidAndInvalidExtensions_ReturnsCorrectMdTag(string extension, string expectedMdTag)
        {
            var result = MimeTypeProvider.GetMdTag(extension);
            result.ShouldBe(expectedMdTag);
        }

        [TestCase("", "application/octet-stream")]
        [TestCase(null, "application/octet-stream")]
        [TestCase(".verylongextensionthatshouldbehandledproperly", "application/octet-stream")]
        [TestCase(".json", "application/json")]
        public void GetMimeType_InvalidOrEdgeCases_ReturnsCorrectMimeType(string? extension, string expectedMimeType)
        {
            var result = MimeTypeProvider.GetMimeType(extension);
            result.ShouldBe(expectedMimeType);
        }

        [TestCase(".md", false)]
        [TestCase(".dll", true)]
        public void IsBinaryExtension_ForVariousExtensions_ReturnsCorrectResult(string extension, bool expected)
        {
            var result = FileValidator.IsBinaryExtension(extension);
            result.ShouldBe(expected);
        }
    }

    public class TestFileHandler : FileHandlerBase
    {
        public TestFileHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager = null)
            : base(ui, recentFilesManager)
        {
        }

        public static bool TestIsFileExcluded(string fileName, VectorStoreConfig config)
        {
            var handler = new TestFileHandler(null);
            // This method is now in FileValidator
            return FileValidator.IsFileExcluded(fileName, config);
        }
    }

    [TestFixture]
    public class FileHandlerBaseTests
    {
        [Test]
        public void IsFileExcluded_ExactMatch_ReturnsTrue()
        {
            // Arrange
            var config = new VectorStoreConfig();
            config.ExcludedFiles.AddRange(new List<string> { "test.txt", "example.doc" });

            // Act
            var result = TestFileHandler.TestIsFileExcluded("test.txt", config);

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void IsFileExcluded_WildcardAtEnd_ReturnsTrue()
        {
            // Arrange
            var config = new VectorStoreConfig();
            config.ExcludedFiles.AddRange(new List<string> { "test.*", "example.doc" });

            // Act
            var result = TestFileHandler.TestIsFileExcluded("test.txt", config);

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void IsFileExcluded_WildcardAtStart_ReturnsTrue()
        {
            // Arrange
            var config = new VectorStoreConfig();
            config.ExcludedFiles.AddRange(new List<string> { "*.txt", "example.doc" });

            // Act
            var result = TestFileHandler.TestIsFileExcluded("test.txt", config);

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void IsFileExcluded_NoMatch_ReturnsFalse()
        {
            // Arrange
            var config = new VectorStoreConfig();
            config.ExcludedFiles.AddRange(new List<string> { "another.file", "example.doc" });

            // Act
            var result = TestFileHandler.TestIsFileExcluded("test.txt", config);

            // Assert
            result.ShouldBeFalse();
        }
    }
}
