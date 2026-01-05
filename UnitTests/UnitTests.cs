// File: UnitTests/MimeTypeProviderTests.cs

using DocumentFormat.OpenXml.CustomProperties;
using LogCtxShared;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using VecTool.Configuration;
using VecTool.Handlers;
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
            using var ctx = logger.SetContext()
            .Add("Suite", nameof(MimeTypeProviderTests));
            logger.LogInformation("Test suite starting");
        }

        private string _corrId = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _corrId = Guid.NewGuid().ToString("N");
            logger.SetContext()
                .Add("Operation", "UnitTest")
                .Add("Suite", nameof(MimeTypeProviderTests))
                .Add("Test", TestContext.CurrentContext.Test.Name)
                .Add("CorrelationId", _corrId)
                ;
            logger.LogInformation("Test start");
        }

        [TearDown]
        public void TearDown()
        {
            var outcome = TestContext.CurrentContext.Result.Outcome.Status.ToString();
            logger.SetContext().Add("Status", outcome);
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
            logger.SetContext().Add("Extension", extension ?? "(null)").Add("Expected", expectedMdTag);
            logger.LogDebug($"Arrange: setting up test for extension {extension}");
            logger.LogDebug($"Arrange: setting up test for extension {extension}");

            var result = MimeTypeProvider.GetMdTag(extension);

            logger.SetContext().Add("Actual", result ?? "(null)");
            logger.LogInformation($"Assert: comparing expected vs actual for {extension}");
            result.ShouldBe(expectedMdTag);
        }

        [TestCase("", null)]
        [TestCase(null, null)]
        [TestCase(".verylongextensionthatshouldbehandledproperly", null)]
        [TestCase(".json", "application/json")]
        public void GetMimeTypeInvalidOrEdgeCasesReturnsCorrectMimeType(string? extension, string expectedMimeType)
        {
            logger.SetContext().Add("Extension", extension ?? "(null)").Add("Expected", expectedMimeType);
            logger.LogDebug($"Arrange: setting up test for extension {extension}");
            logger.LogDebug($"Arrange: setting up test for extension {extension}");

            var result = MimeTypeProvider.GetMimeType(extension);

            logger.SetContext().Add("Actual", result ?? "(null)");
            logger.LogInformation($"Assert: comparing expected vs actual for {extension}");
            result.ShouldBe(expectedMimeType);
        }

        // 🔄 MODIFY: instrument parameterized test for IsBinaryExtension (FileValidator)
        [TestCase(".md", false)]
        [TestCase(".dll", true)]
        public void IsBinaryExtensionForVariousExtensionsReturnsCorrectResult(string extension, bool expected)
        {
            logger.SetContext().Add("Extension", extension).Add("IsBinaryExpected", expected);
            logger.LogDebug($"Arrange: setting up test for extension {extension}");
            logger.LogDebug($"Arrange: setting up test for extension {extension}");

            var result = FileValidator.IsBinary(extension, null);

            logger.SetContext().Add("Actual", result);
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