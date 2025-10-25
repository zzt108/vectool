// File: UnitTests/MimeTypeProviderTests.cs

using LogCtxShared;        // ✅ NEW
using NLogShared;          // ✅ NEW
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
            using var ctx = _log.Ctx.Set();
            ctx.Add("Suite", nameof(MimeTypeProviderTests));
            _log.Info("Test suite starting");
        }

        // ✅ NEW: per-test scoped logger/context
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
            _log.Ctx.Set(props);
            _log.Info("Test start");
        }

        [TearDown]
        public void TearDown()
        {
            var outcome = TestContext.CurrentContext.Result.Outcome.Status.ToString();
            var props = new LogCtxShared.Props("Status", outcome);
            _log.Ctx.Set(props);
            _log.Info("Test end");
        }

        // 🔄 MODIFY: instrument parameterized test for GetMdTag
        [TestCase(".cs", "csharp")]
        [TestCase(".csproj", "msbuild")]
        [TestCase(".feature", "gherkin")]
        [TestCase(".unknown", "unknown")]
        [TestCase(".txt", "text")]
        [TestCase(null, "")]
        public void GetMdTagValidAndInvalidExtensionsReturnsCorrectMdTag(string extension, string expectedMdTag)
        {
            _log.Ctx.Set(new LogCtxShared.Props("Extension", extension ?? "(null)", "Expected", expectedMdTag));
            _log.Debug($"Arrange: setting up test for extension {extension}");
            _log.Debug($"Arrange: setting up test for extension {extension}");

            var result = MimeTypeProvider.GetMdTag(extension);

            _log.Ctx.Set(new LogCtxShared.Props("Actual", result));
            _log.Info($"Assert: comparing expected vs actual for {extension}");
            result.ShouldBe(expectedMdTag);
        }

        [TestCase("", "application/binary")]
        [TestCase(null, "application/binary")]
        [TestCase(".verylongextensionthatshouldbehandledproperly", "application/binary")]
        [TestCase(".json", "application/json")]
        public void GetMimeTypeInvalidOrEdgeCasesReturnsCorrectMimeType(string? extension, string expectedMimeType)
        {
            _log.Ctx.Set(new LogCtxShared.Props("Extension", extension ?? "(null)", "Expected", expectedMimeType));
            _log.Debug($"Arrange: setting up test for extension {extension}");
            _log.Debug($"Arrange: setting up test for extension {extension}");

            var result = MimeTypeProvider.GetMimeType(extension);

            _log.Ctx.Set(new LogCtxShared.Props("Actual", result));
            _log.Info($"Assert: comparing expected vs actual for {extension}");
            result.ShouldBe(expectedMimeType);
        }

        // 🔄 MODIFY: instrument parameterized test for IsBinaryExtension (FileValidator)
        [TestCase(".md", false)]
        [TestCase(".dll", true)]
        public void IsBinaryExtensionForVariousExtensionsReturnsCorrectResult(string extension, bool expected)
        {
            _log.Ctx.Set(new LogCtxShared.Props("Extension", extension, "IsBinaryExpected", expected));
            _log.Debug($"Arrange: setting up test for extension {extension}");
            _log.Debug($"Arrange: setting up test for extension {extension}");

            var result = FileValidator.IsBinary(extension, null);

            _log.Ctx.Set(new LogCtxShared.Props("Actual", result));
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
