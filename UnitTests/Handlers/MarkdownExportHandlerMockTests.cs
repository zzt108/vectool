// ✅ FULL FILE VERSION
using LogCtxShared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

namespace UnitTests.Handlers
{
    /// <summary>
    /// Consolidated tests for MDHandler (Markdown export) with mock dependencies.
    /// Combines mock-based and integration tests for minimal token footprint.
    /// </summary>
    [TestFixture]
    public class MarkdownExportHandlerTests
    {
        private IFileSystemTraverser _mockTraverser = null!;
        private IRecentFilesManager _mockRecentFiles = null!;
        private IUserInterface _mockUi = null!;
        private VectorStoreConfig _config = null!;
        private string _testDir = null!;
        private MDHandler _handler = null!;
        private readonly ILogger logger = TestLogger.For<MarkdownExportHandlerTests>();

        [SetUp]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"MDHandlerTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);

            _mockTraverser = Substitute.For<IFileSystemTraverser>();
            _mockRecentFiles = Substitute.For<IRecentFilesManager>();
            _mockUi = Substitute.For<IUserInterface>();
            _config = new VectorStoreConfig();

            _handler = new MDHandler(logger, _mockUi, _mockRecentFiles, _mockTraverser);
        }

        [TearDown]
        public void Cleanup()
        {
            try { if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true); }
            catch { /* Best-effort cleanup */ }
        }

        // ✅ Helper for file creation (DRY)
        private string CreateTestFile(string relativePath, string content)
        {
            var fullPath = Path.Combine(_testDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
            return fullPath;
        }

        #region Dependency Injection Tests

        [Test]
        public void Constructor_AcceptsDependencies()
        {
            _handler.ShouldNotBeNull();
            _mockTraverser.Received(0).EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>());
        }

        #endregion Dependency Injection Tests

        #region ARCH-002: Traverser Exclusivity Tests

        [Test]
        public void ExportSelectedFolders_UsesTraverserNotDirectEnumeration()
        {
            // Arrange
            var file1 = CreateTestFile("code.cs", "namespace Demo { }");
            var file2 = CreateTestFile("readme.md", "# Project");

            _mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1, file2 });

            var outputPath = Path.Combine(_testDir, "output.md");

            // Act
            _handler.ExportSelectedFolders(outputPath, _config);

            // Assert - CRITICAL: Traverser called exactly once per folder
            _mockTraverser.Received(1).EnumerateFilesRespectingExclusions(
                Arg.Any<string>(),
                Arg.Any<VectorStoreConfig>());
        }

        [Test]
        public void Handler_NeverCallsIsFileExcludedDirectly()
        {
            // Arrange
            var allowedFile = CreateTestFile("code.cs", "code");
            var excludedFile = CreateTestFile("debug.log", "logs");

            _config.ExcludedFiles.Add("*.log");

            // Mock: Traverser already applied exclusions
            _mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { allowedFile }); // Only allowed file

            var outputPath = Path.Combine(_testDir, "output.md");

            // Act
            _handler.ExportSelectedFolders(outputPath, _config);

            // Assert - Handler delegates all exclusion logic
            _mockTraverser.Received(1).EnumerateFilesRespectingExclusions(
                Arg.Any<string>(),
                Arg.Any<VectorStoreConfig>());

            // Verify handler has no IsFileExcluded method
            var methodNames = _handler.GetType()
                .GetMethods(System.Reflection.BindingFlags.Instance |
                           System.Reflection.BindingFlags.Public |
                           System.Reflection.BindingFlags.NonPublic)
                .Select(m => m.Name);

            methodNames.ShouldNotContain("IsFileExcluded");
        }

        #endregion ARCH-002: Traverser Exclusivity Tests

        #region Output Generation Tests

        [Test]
        public void ExportSelectedFolders_CreatesValidMarkdownFile()
        {
            // Arrange
            var sourceFile = CreateTestFile("code.cs", "namespace Demo { }");

            _mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { sourceFile });

            var outputPath = Path.Combine(_testDir, "output.md");

            // Act
            _handler.ExportSelectedFolders(outputPath, _config);

            // Assert
            File.Exists(outputPath).ShouldBeTrue();
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("code.cs", Case.Insensitive);
            content.ShouldContain("namespace Demo", Case.Insensitive);
        }

        [Test]
        public void ExportSelectedFolders_RegistersOutputWithRecentFiles()
        {
            // Arrange
            var sourceFile = CreateTestFile("code.cs", "code");

            _mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { sourceFile });

            _mockRecentFiles.ClearReceivedCalls();
            var outputPath = Path.Combine(_testDir, "output.md");

            // Act
            _handler.ExportSelectedFolders(outputPath, _config);

            // Assert
            _mockRecentFiles.Received(1).RegisterGeneratedFile(
                outputPath,
                RecentFileType.Codebase_Md,
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<long>());
        }

        [Test]
        public void ExportSelectedFolders_MultipleFolders_IncludesAllFiles()
        {
            // Arrange
            var file1 = CreateTestFile("folder1/code.cs", "class A { }");
            var file2 = CreateTestFile("folder2/readme.md", "# Docs");

            _mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1 }, new[] { file2 }); // Sequential calls

            var outputPath = Path.Combine(_testDir, "output.md");

            // Act
            _handler.ExportSelectedFolders(
                // new List<string> { Path.Combine(_testDir, "folder1"), Path.Combine(_testDir, "folder2") },
                outputPath,
                _config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("code.cs");
            content.ShouldContain("readme.md");
            content.ShouldContain("class A");
            content.ShouldContain("# Docs");
        }

        #endregion Output Generation Tests

        #region Input Validation Tests

        [TestCase(null, "output.md", "Folder list cannot be null")]
        [TestCase("[]", "output.md", "Folder list cannot be empty")]
        [TestCase("[\"folder\"]", null, "Output path cannot be null")]
        [TestCase("[\"folder\"]", "", "Output path cannot be null")]
        public void ExportSelectedFolders_InvalidInput_ThrowsArgumentException(
            string? foldersJson,
            string? outputPath,
            string expectedMessage)
        {
            // Arrange
            var folders = foldersJson == null ? null :
                          foldersJson == "[]" ? new List<string>() :
                          new List<string> { "folder" };

            // Act & Assert
            var ex = Should.Throw<ArgumentException>(() =>
                _handler.ExportSelectedFolders(
                    // folders!,
                    outputPath!, _config));

            ex.Message.ShouldContain(expectedMessage, Case.Insensitive);
        }

        #endregion Input Validation Tests

        #region UI Integration Tests

        [Test]
        public void ExportSelectedFolders_UpdatesUiProgress()
        {
            // Arrange
            var sourceFile = CreateTestFile("code.cs", "code");

            _mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { sourceFile });

            var outputPath = Path.Combine(_testDir, "output.md");

            // Act
            _handler.ExportSelectedFolders(outputPath, _config);

            // Assert
            _mockUi.Received().WorkStart(Arg.Any<string>(), Arg.Any<IEnumerable<string>>());
            _mockUi.Received().UpdateStatus(Arg.Is<string>(s => s.Contains("Enumerating")));
            _mockUi.Received().WorkFinish();
        }

        #endregion UI Integration Tests
    }
}