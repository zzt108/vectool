// File: UnitTests/MimeTypeProviderTests.cs

using LogCtxShared;        
using NLogShared;          
using NUnit.Framework;
using Shouldly;
using System;
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
        private static NLogShared.CtxLogger _log => field = new();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            using var ctx = LogCtx.Set();
            ctx.Add("Suite", nameof(MimeTypeProviderTests));
            _log.Info("Test suite starting");
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
            LogCtx.Set(props);
            _log.Info("Test start");
        }

        [TearDown]
        public void TearDown()
        {
            var outcome = TestContext.CurrentContext.Result.Outcome.Status.ToString();
            var props = new LogCtxShared.Props("Status", outcome);
            LogCtx.Set(props);
            _log.Info("Test end");
        }

        [TestCase(".cs", "csharp")]
        [TestCase(".csproj", "msbuild")]
        [TestCase(".feature", "gherkin")]
        [TestCase(".unknown", "unknown")]
        [TestCase(".txt", "text")]
        [TestCase(null, null)]
        public void GetMdTagValidAndInvalidExtensionsReturnsCorrectMdTag(string extension, string expectedMdTag)
        {
            LogCtx.Set(new LogCtxShared.Props("Extension", extension ?? "(null)", "Expected", expectedMdTag));
            _log.Debug($"Arrange: setting up test for extension {extension}");
            _log.Debug($"Arrange: setting up test for extension {extension}");

            var result = MimeTypeProvider.GetMdTag(extension);

            LogCtx.Set(new LogCtxShared.Props("Actual", result??"(null)"));
            _log.Info($"Assert: comparing expected vs actual for {extension}");
            result.ShouldBe(expectedMdTag);
        }

        [TestCase("", null)]
        [TestCase(null, null)]
        [TestCase(".verylongextensionthatshouldbehandledproperly", null)]
        [TestCase(".json", "application/json")]
        public void GetMimeTypeInvalidOrEdgeCasesReturnsCorrectMimeType(string? extension, string expectedMimeType)
        {
            LogCtx.Set(new LogCtxShared.Props("Extension", extension ?? "(null)", "Expected", expectedMimeType));
            _log.Debug($"Arrange: setting up test for extension {extension}");
            _log.Debug($"Arrange: setting up test for extension {extension}");

            var result = MimeTypeProvider.GetMimeType(extension);

            LogCtx.Set(new LogCtxShared.Props("Actual", result??"(null)"));
            _log.Info($"Assert: comparing expected vs actual for {extension}");
            result.ShouldBe(expectedMimeType);
        }

        // 🔄 MODIFY: instrument parameterized test for IsBinaryExtension (FileValidator)
        [TestCase(".md", false)]
        [TestCase(".dll", true)]
        public void IsBinaryExtensionForVariousExtensionsReturnsCorrectResult(string extension, bool expected)
        {
            LogCtx.Set(new LogCtxShared.Props("Extension", extension, "IsBinaryExpected", expected));
            _log.Debug($"Arrange: setting up test for extension {extension}");
            _log.Debug($"Arrange: setting up test for extension {extension}");

            var result = FileValidator.IsBinary(extension, null);

            LogCtx.Set(new LogCtxShared.Props("Actual", result));
            _log.Info($"Assert: comparing expected vs actual for {extension}");
            result.ShouldBe(expected);
        }
    }

    // Helper test class remains unchanged
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
}
