﻿using FluentAssertions;
using NUnit.Framework;
using oaiVectorStore;

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
        [TestCase(".unknown", null)] // Unknown extension should return null
        [TestCase(".txt", null)] // Unspecified extension should return null
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

        [TestCase(".md", null)] // Valid extension
        [TestCase(".unknown", null)] // Unknown extension
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
}