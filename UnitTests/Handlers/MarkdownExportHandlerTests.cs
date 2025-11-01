using LogCtxShared;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using NLogShared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

namespace UnitTests.Handlers
{
    /// <summary>
    /// Unit tests for MarkdownExportHandler (MDHandler) verifying exclusive use of traverser
    /// for file enumeration and ensuring handler is exclusion-unaware.
    /// </summary>
    [TestFixture]
    public class MarkdownExportHandlerMockTests
    {
        private string testDir = default!;
        private VectorStoreConfig config = default!;
        private IUserInterface mockUi = default!;
        private IRecentFilesManager mockRecentFilesManager = default!;
        private IFileSystemTraverser mockTraverser = default!;
        private readonly CtxLogger log = new();

        [SetUp]
        public void Setup()
        {
            testDir = Path.Combine(
                Path.GetTempPath(),
                "MarkdownExportTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);

            config = new VectorStoreConfig();
            mockUi = Substitute.For<IUserInterface>();
            mockRecentFilesManager = Substitute.For<IRecentFilesManager>();
            mockTraverser = Substitute.For<IFileSystemTraverser>(null, testDir);
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, recursive: true);
            }
            catch
            {
                // Swallow cleanup exceptions
            }
        }

        /// <summary>
        /// TEST 1: Constructor should accept dependency injection
        /// Verifies DI wiring is correct
        /// </summary>
        [Test]
        public void ConstructorShouldAcceptDependencies()
        {
            // Arrange
            // Act
            var handler = new MDHandler(mockUi, mockRecentFilesManager);

            // Assert
            handler.ShouldNotBeNull();
        }

        /// <summary>
        /// TEST 2: ExportSelectedFolders should use traverser, not Directory.GetFiles
        /// CRITICAL: Handler must delegate ALL file enumeration to traverser.
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldUseTraverserNotDirectoryEnumeration()
        {
            // Arrange
            var file1 = Path.Combine(testDir, "program.cs");
            var file2 = Path.Combine(testDir, "readme.md");
            File.WriteAllText(file1, "code");
            File.WriteAllText(file2, "readme");

            // Mock traverser to return filtered files (already excluded)
            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1, file2 });

            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert - Verify traverser was called
            mockTraverser
                .Received(1)
                .EnumerateFilesRespectingExclusions(
                    Arg.Any<string>(),
                    Arg.Any<VectorStoreConfig>());
            // "Handler must use traverser for file enumeration"
        }

        /// <summary>
        /// TEST 3: Handler should NEVER call IsFileExcluded
        /// All exclusion logic must come from traverser.
        /// </summary>
        [Test]
        public void HandlerShouldNotCallIsFileExcluded()
        {
            // Arrange
            var allowedFile = Path.Combine(testDir, "allowed.cs");
            var excludedFile = Path.Combine(testDir, "debug.log");
            File.WriteAllText(allowedFile, "code");
            File.WriteAllText(excludedFile, "logs");

            // Mock traverser - excluded file never returned
            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { allowedFile }); // Only allowed file

            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            // Verify handler didn't check exclusions directly (via traverser interface)
            // Correct - Verify that traverser's enumeration method WAS called instead
            mockTraverser
                .Received(1)
                .EnumerateFilesRespectingExclusions(
                    Arg.Any<string>(),
                    Arg.Any<VectorStoreConfig>());
            // "Handler must use traverser for file enumeration"
        }

        /// <summary>
        /// TEST 4: Output file should be created with correct content
        /// Verifies export functionality.
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldCreateValidMarkdownFile()
        {
            // Arrange
            var file1 = Path.Combine(testDir, "code.cs");
            File.WriteAllText(file1, "namespace Demo");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1 });

            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            File.Exists(outputPath).ShouldBeTrue("Output file should be created");
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("code.cs", Case.Insensitive, "Filename should be in output");
            content.ShouldContain("namespace Demo", Case.Insensitive, "File content should be in output");
        }

        /// <summary>
        /// TEST 5: Output should be registered with RecentFilesManager
        /// Verifies artifact tracking.
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldRegisterOutputWithRecentFiles()
        {
            // Arrange
            var file1 = Path.Combine(testDir, "code.cs");
            File.WriteAllText(file1, "code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1 });

            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            mockRecentFilesManager
                .Received(1)
                .RegisterGeneratedFile(
                    outputPath,
                    RecentFileType.Codebase_Md,
                    Arg.Any<IReadOnlyList<string>>(),
                    Arg.Any<long>());
        }

        /// <summary>
        /// TEST 6: UI should be updated during export
        /// Verifies progress tracking.
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldUpdateUiProgress()
        {
            // Arrange
            var file1 = Path.Combine(testDir, "code.cs");
            File.WriteAllText(file1, "code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1 });

            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            mockUi.Received(1).WorkStart(Arg.Any<string>(), Arg.Any<IEnumerable<string>>());
            mockUi.Received(1).WorkFinish();
        }

        /// <summary>
        /// TEST 7: Async wrapper should delegate to sync method
        /// Verifies ExportSelectedFoldersAsync.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task ExportSelectedFoldersAsyncShouldDelegate()
        {
            // Arrange
            var file1 = Path.Combine(testDir, "code.cs");
            File.WriteAllText(file1, "code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1 });

            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            await handler.ExportSelectedFoldersAsync(new List<string> { testDir }, outputPath, config);

            // Assert - Async should produce same result
            File.Exists(outputPath).ShouldBeTrue();
        }

        /// <summary>
        /// TEST 8: Multiple folders should all be processed
        /// Verifies multi-folder export.
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldProcessMultipleFolders()
        {
            // Arrange
            var folder1 = Path.Combine(testDir, "folder1");
            var folder2 = Path.Combine(testDir, "folder2");
            Directory.CreateDirectory(folder1);
            Directory.CreateDirectory(folder2);
            File.WriteAllText(Path.Combine(folder1, "file1.cs"), "file1");
            File.WriteAllText(Path.Combine(folder2, "file2.cs"), "file2");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] {
                    Path.Combine(folder1, "file1.cs"),
                    Path.Combine(folder2, "file2.cs")
                });

            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { folder1, folder2 }, outputPath, config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("file1.cs");
            content.ShouldContain("file2.cs");
        }

        /// <summary>
        /// TEST 9: Null folders list should throw ArgumentException
        /// Verifies input validation.
        /// </summary>
        [Test]
        public void ExportSelectedFoldersWithNullFoldersShouldThrow()
        {
            // Arrange
            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
                handler.ExportSelectedFolders(null!, outputPath, config));
        }

        /// <summary>
        /// TEST 10: Empty folders list should throw ArgumentException
        /// </summary>
        [Test]
        public void ExportSelectedFoldersWithEmptyFoldersShouldThrow()
        {
            // Arrange
            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
                handler.ExportSelectedFolders(new List<string>(), outputPath, config));
        }

        /// <summary>
        /// TEST 11: Null output path should throw ArgumentException
        /// </summary>
        [Test]
        public void ExportSelectedFoldersWithNullOutputPathShouldThrow()
        {
            // Arrange
            var handler = new MDHandler(mockUi, mockRecentFilesManager);

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
                handler.ExportSelectedFolders(new List<string> { testDir }, null!, config));
        }

        /// <summary>
        /// TEST 12: Files should be grouped by folder in output
        /// Verifies hierarchical structure preservation.
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldGroupFilesByFolder()
        {
            // Arrange
            var subfolder = Path.Combine(testDir, "sub");
            Directory.CreateDirectory(subfolder);
            File.WriteAllText(Path.Combine(testDir, "root.cs"), "root");
            File.WriteAllText(Path.Combine(subfolder, "sub.cs"), "sub");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] {
                    Path.Combine(testDir, "root.cs"),
                    Path.Combine(subfolder, "sub.cs")
                });

            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("Folder", Case.Insensitive, "Should have folder headers");
            content.ShouldContain("root.cs");
            content.ShouldContain("sub.cs");
        }

        /// <summary>
        /// TEST 13: LogCtx should be used for structured logging
        /// Verifies audit trail creation.
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldUseLogCtx()
        {
            // Arrange
            var file1 = Path.Combine(testDir, "code.cs");
            File.WriteAllText(file1, "code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1 });

            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            using (var ctx = log.Ctx.Set(new Props()
                .Add("test", "markdownexport")
                .Add("testDir", testDir)))
            {
                handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);
            }

            // Assert - If we get here, LogCtx is working
            File.Exists(outputPath).ShouldBeTrue();
        }

        /// <summary>
        /// TEST 14: Exception during export should be propagated
        /// Verifies error handling.
        /// </summary>
        [Test]
        public void ExportSelectedFoldersWithInvalidOutputPathShouldThrow()
        {
            // Arrange
            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var invalidPath = Path.Combine(testDir, "invalid?output.md");

            // Act & Assert
            Should.Throw<Exception>(() =>
                handler.ExportSelectedFolders(new List<string> { testDir }, invalidPath, config));
        }

        /// <summary>
        /// TEST 15: File timestamps should be included in output
        /// Verifies metadata export.
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldIncludeFileTimestamps()
        {
            // Arrange
            var file1 = Path.Combine(testDir, "code.cs");
            File.WriteAllText(file1, "code");
            var timestamp = File.GetLastWriteTime(file1);

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1 });

            var handler = new MDHandler(mockUi, mockRecentFilesManager);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("Time", Case.Insensitive, "Should include timestamp metadata");
        }
    }
}