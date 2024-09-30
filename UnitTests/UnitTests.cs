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
        [TestCase(".unknown", null)] // Unknown extension should return null
        public void GetNewExtension_ValidAndInvalidExtensions_ReturnsCorrectNewExtension(string extension, string? expectedNewExtension)
        {
            var result = MimeTypeProvider.GetNewExtension(extension);

            result.Should().Be(expectedNewExtension);
        }

        [TestCase(".cs", "csharp")]
        [TestCase(".csproj", "msbuild")]
        [TestCase(".feature", "gherkin")]
        [TestCase(".unknown", null)] // Unknown extension should return null
        public void GetMdTag_ValidAndInvalidExtensions_ReturnsCorrectMdTag(string extension, string? expectedMdTag)
        {
            var result = MimeTypeProvider.GetMdTag(extension);

            result.Should().Be(expectedMdTag);
        }
    }
}