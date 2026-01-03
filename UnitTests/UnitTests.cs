// File: UnitTests/MimeTypeProviderTests.cs

using LogCtxShared;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using UnitTests.Handlers;
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
        private readonly ILogger logger = TestLogger.For<MimeTypeProviderTests>();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            using var ctx = logger.SetContext(new Props()
            .Add("Suite", nameof(MimeTypeProviderTests)));
            logger.LogInformation("Test suite starting");
        }

        private string _corrId = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _corrId = Guid.NewGuid().ToString("N");
            var props = new LogCtxShared.Props(
                "Operation", "UnitTest",
                "Suite", nameof(MimeTypeProviderTests),
                "Test", TestContext.CurrentContext.Test.Name,
                "CorrelationId", _corrId);
            logger.SetContext(props);
            logger.LogInformation("Test start");
        }

        [TearDown]
        public void TearDown()
        {
            var outcome = TestContext.CurrentContext.Result.Outcome.Status.ToString();
            var props = new LogCtxShared.Props("Status", outcome);
            logger.SetContext(props);
            logger.LogInformation("Test end");
        }

        [TestCase(".cs", "csharp")]
        [TestCase(".csproj", "msbuild")]
        [TestCase(".feature", "gherkin")]
        [TestCase(".unknown", "unknown")]
        [TestCase(".txt", "text")]
        [TestCase(null, null)]
        public void GetMdTagValidAndInvalidExtensionsReturnsCorrectMdTag(string extension, string expectedMdTag)
        {
            logger.SetContext(new LogCtxShared.Props("Extension", extension ?? "(null)", "Expected", expectedMdTag));
            logger.LogDebug($"Arrange: setting up test for extension {extension}");
            logger.LogDebug($"Arrange: setting up test for extension {extension}");

            var result = MimeTypeProvider.GetMdTag(extension);

            logger.SetContext(new LogCtxShared.Props("Actual", result ?? "(null)"));
            logger.LogInformation($"Assert: comparing expected vs actual for {extension}");
            result.ShouldBe(expectedMdTag);
        }

        [TestCase("", null)]
        [TestCase(null, null)]
        [TestCase(".verylongextensionthatshouldbehandledproperly", null)]
        [TestCase(".json", "application/json")]
        public void GetMimeTypeInvalidOrEdgeCasesReturnsCorrectMimeType(string? extension, string expectedMimeType)
        {
            logger.SetContext(new LogCtxShared.Props("Extension", extension ?? "(null)", "Expected", expectedMimeType));
            logger.LogDebug($"Arrange: setting up test for extension {extension}");
            logger.LogDebug($"Arrange: setting up test for extension {extension}");

            var result = MimeTypeProvider.GetMimeType(extension);

            logger.SetContext(new LogCtxShared.Props("Actual", result ?? "(null)"));
            logger.LogInformation($"Assert: comparing expected vs actual for {extension}");
            result.ShouldBe(expectedMimeType);
        }

        // 🔄 MODIFY: instrument parameterized test for IsBinaryExtension (FileValidator)
        [TestCase(".md", false)]
        [TestCase(".dll", true)]
        public void IsBinaryExtensionForVariousExtensionsReturnsCorrectResult(string extension, bool expected)
        {
            logger.SetContext(new LogCtxShared.Props("Extension", extension, "IsBinaryExpected", expected));
            logger.LogDebug($"Arrange: setting up test for extension {extension}");
            logger.LogDebug($"Arrange: setting up test for extension {extension}");

            var result = FileValidator.IsBinary(extension, null);

            logger.SetContext(new LogCtxShared.Props("Actual", result));
            logger.LogInformation($"Assert: comparing expected vs actual for {extension}");
            result.ShouldBe(expected);
        }
    }

    // Helper test class remains unchanged
    public class TestFileHandler : FileHandlerBase
    {
        public TestFileHandler(ILogger? logger, IUserInterface? ui, IRecentFilesManager? recentFilesManager = null)
            : base(logger!, ui, recentFilesManager)
        {
        }

        public bool TestIsFileExcluded(string fileName, VectorStoreConfig config)
        {
            var handler = new TestFileHandler(logger, null);
            // This method is now in FileValidator
            return FileValidator.IsFileExcluded(fileName, config);
        }
    }
}