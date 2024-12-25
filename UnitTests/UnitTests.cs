using FluentAssertions;
using NUnit.Framework;
using oaiVectorStore;
using DocXHandler;
using System.Collections.Generic;

namespace oaiVectorStoreTests
{
    [TestFixture]
    public class MimeTypeProviderTests
    {
        [TestCase(".cs", "text/x-csharp")]
        [TestCase(".cpp", "text/x-c++")]
        [TestCase(".doc", "application/msword")]
        [TestCase(".json", "application/json")]
        [TestCase(".unknown", "application/octet-stream")] // Unknown extension should return default
        public void GetMimeType_ValidExtensions_ReturnsCorrectMimeType(string extension, string expectedMimeType)
        {
            var result = MimeTypeProvider.GetMimeType(extension);

            result.Should().Be(expectedMimeType);
        }

        [TestCase(".cs", ".cs.md")]
        [TestCase(".csproj", ".csproj.md")]
        [TestCase(".feature", ".feature.md")]
        [TestCase(".unknown", null)] // Unknown extension should return unchanged
        public void GetNewExtension_ValidAndInvalidExtensions_ReturnsCorrectNewExtension(string extension, string? expectedNewExtension)
        {
            var result = MimeTypeProvider.GetNewExtension(extension);

            result.Should().Be(expectedNewExtension);
        }

        [TestCase(".cs", "csharp")]
        [TestCase(".csproj", "msbuild")]
        [TestCase(".feature", "gherkin")]
        [TestCase(".unknown", "unknown")] // Unknown extension should return null
        [TestCase(".txt", "txt")] // Unspecified extension should return null
        public void GetMdTag_ValidAndInvalidExtensions_ReturnsCorrectMdTag(string extension, string? expectedMdTag)
        {
            var result = MimeTypeProvider.GetMdTag(extension);

            result.Should().Be(expectedMdTag);
        }
        [TestCase("", "application/octet-stream")] // Empty string
        [TestCase(null, "application/octet-stream")] // Null input
        [TestCase(".verylongextensionthatshouldbehandledproperly", "application/octet-stream")] // Long extension
        public void GetMimeType_InvalidOrEdgeCases_ReturnsDefaultMimeType(string? extension, string expectedMimeType)
        {
            var result = MimeTypeProvider.GetMimeType(extension);
            result.Should().Be(expectedMimeType);
        }

        [TestCase(".cs", ".cs.md")] // changed extension
        [TestCase(".txt", null)] // Valid extension
        [TestCase(".unknown", null)] // Unknown extension
        public void GetNewExtension_EdgeCases_ReturnsExpected(string extension, string? expectedNewExtension)
        {
            var result = MimeTypeProvider.GetNewExtension(extension);
            result.Should().Be(expectedNewExtension);
        }

        [TestCase(".md", "md")] // Valid extension
        [TestCase(".unknown", "unknown")] // Unknown extension
        public void GetMdTag_EdgeCases_ReturnsExpected(string extension, string? expectedMdTag)
        {
            var result = MimeTypeProvider.GetMdTag(extension);
            result.Should().Be(expectedMdTag);
        }

        [TestCase(".md", false)] // Valid extension
        [TestCase(".cs", false)] // Valid extension
        [TestCase(".json", false)] // Valid extension
        [TestCase(".unknown", false)] // unknown extension regarded as text
        [TestCase(".doc", true)] // Unknown extension
        [TestCase(".docx", true)] // Unknown extension
        [TestCase(".pdf", true)] // Unknown extension
        [TestCase(".pptx", true)] // Unknown extension
        public void IsBinary(string extension, bool expected)
        {
            var result = MimeTypeProvider.IsBinary(extension);
            result.Should().Be(expected);
        }
    }

    public class TestFileHandler : FileHandlerBase
    {
        public static bool TestIsFileExcluded(string fileName, List<string> excludedFiles)
        {
            return IsFileExcluded(fileName, excludedFiles);
        }
    }

    [TestFixture]
    public class FileHandlerBaseTests
    {
        [Test]
        public void IsFileExcluded_ExactMatch_ReturnsTrue()
        {
            var excludedFiles = new List<string> { "test.txt", "example.doc" };
            var result = TestFileHandler.TestIsFileExcluded("test.txt", excludedFiles);
            result.Should().BeTrue();
        }

        [Test]
        public void IsFileExcluded_WildcardAtEnd_ReturnsTrue()
        {
            var excludedFiles = new List<string> { "test.*", "example.doc" };
            var result = TestFileHandler.TestIsFileExcluded("test.txt", excludedFiles);
            result.Should().BeTrue();
        }

        [Test]
        public void IsFileExcluded_WildcardAtStart_ReturnsTrue()
        {
            var excludedFiles = new List<string> { "*.txt", "example.doc" };
            var result = TestFileHandler.TestIsFileExcluded("test.txt", excludedFiles);
            result.Should().BeTrue();
        }

        [Test]
        public void IsFileExcluded_WildcardInMiddle_ReturnsTrue()
        {
            var excludedFiles = new List<string> { "te*t.txt", "example.doc" };
            var result = TestFileHandler.TestIsFileExcluded("test.txt", excludedFiles);
            result.Should().BeTrue();
        }

        [Test]
        public void IsFileExcluded_MultipleWildcards_ReturnsTrue()
        {
            var excludedFiles = new List<string> { "t*t.t*t", "example.doc" };
            var result = TestFileHandler.TestIsFileExcluded("test.txt", excludedFiles);
            result.Should().BeTrue();
        }

        [Test]
        public void IsFileExcluded_NoMatch_ReturnsFalse()
        {
            var excludedFiles = new List<string> { "test.*", "example.doc" };
            var result = TestFileHandler.TestIsFileExcluded("other.txt", excludedFiles);
            result.Should().BeFalse();
        }

        [Test]
        public void IsFileExcluded_CaseInsensitive_ReturnsTrue()
        {
            var excludedFiles = new List<string> { "TEST.*", "example.doc" };
            var result = TestFileHandler.TestIsFileExcluded("test.txt", excludedFiles);
            result.Should().BeTrue();
        }
    }
}