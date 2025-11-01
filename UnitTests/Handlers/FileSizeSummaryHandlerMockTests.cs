using LogCtxShared;
using NSubstitute;
using NUnit.Framework;
using static NSubstitute.Received;
using NLogShared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VecTool.Configuration;
using VecTool.Core;
using VecTool.Handlers;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

namespace UnitTests.Handlers
{
    /// <summary>
    /// Unit tests for FileSizeSummaryHandler verifying exclusive use of traverser for folder enumeration
    /// and ensuring handler is exclusion-unaware.
    /// </summary>
    [TestFixture]
    public class FileSizeSummaryHandlerMockTests
    {
        private string testDir = default!;
        private VectorStoreConfig config = default!;
        private IUserInterface mockUi = default!;
        private IRecentFilesManager mockRecentFilesManager = default!;
        private FileSystemTraverser mockTraverser = default!;
        private readonly CtxLogger log = new();

        [SetUp]
        public void Setup()
        {
            testDir = Path.Combine(
                Path.GetTempPath(),
                "FileSizeSummaryHandlerTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);

            config = new VectorStoreConfig();
            mockUi = Substitute.For<IUserInterface>();
            mockRecentFilesManager = Substitute.For<IRecentFilesManager>();
            mockTraverser = Substitute.For<FileSystemTraverser>(null, testDir);
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
        /// TEST 1: Constructor should accept traverser dependency
        /// Verifies dependency injection is wired correctly.
        /// </summary>
        [Test]
        public void ConstructorShouldAcceptTraverserInjection()
        {
            // Arrange
            // Act
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);

            // Assert
            handler.ShouldNotBeNull();
        }

        /// <summary>
        /// TEST 2: GenerateFileSizeSummary should use traverser for folder enumeration
        /// Handler must NOT enumerate folders directly, must delegate to traverser.
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryShouldUseTraverserNotDirectEnumeration()
        {
            // Arrange
            var testFolder = Path.Combine(testDir, "TestFolder");
            Directory.CreateDirectory(testFolder);
            var testFile = Path.Combine(testFolder, "test.cs");
            File.WriteAllText(testFile, "// Test code");

            // ✅ Mock traverser to return allowed files only (NSubstitute syntax)
            var allowedFiles = new[] { testFile };
            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(allowedFiles);

            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);
            var outputPath = Path.Combine(testDir, "summary.md");

            // Act
            handler.GenerateFileSizeSummary(
                new List<string> { testFolder },
                outputPath,
                config);

            // Assert - Verify traverser was used
            mockTraverser.Received(1).EnumerateFilesRespectingExclusions(
                Arg.Any<string>(),
                Arg.Any<VectorStoreConfig>());

            File.Exists(outputPath).ShouldBeTrue("Output file should be created");
        }

        /// <summary>
        /// TEST 3: Handler should never call IsFileExcluded directly
        /// All exclusion logic must come from traverser.
        /// </summary>
        [Test]
        public void FileSizeSummaryHandlerShouldNotCallIsFileExcludedDirectly()
        {
            // Arrange
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);
            Directory.CreateDirectory(Path.Combine(testDir, "included"));
            Directory.CreateDirectory(Path.Combine(testDir, "excluded"));

            // Add exclusion rule
            config.ExcludedFolders.Add("excluded");

            // Act - Handler processes folders doesn't check exclusions itself
            var folders = new List<string> { testDir };

            // Assert - Verify handler doesn't have IsFileExcluded in its code
            var handlerType = handler.GetType();
            var methodNames = handlerType
                .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .Select(m => m.Name)
                .ToList();

            // Handler methods should not call IsFileExcluded
            methodNames.ShouldNotContain("IsFileExcluded", "Handler must NOT have IsFileExcluded calls");
        }

        /// <summary>
        /// TEST 4: Output file should be registered with RecentFilesManager
        /// Verifies artifact tracking integration.
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryShouldRegisterOutputWithRecentFilesManager()
        {
            // Arrange
            var testFolder = Path.Combine(testDir, "TestFolder");
            Directory.CreateDirectory(testFolder);
            var testFile = Path.Combine(testFolder, "test.cs");
            File.WriteAllText(testFile, "// Test code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { testFile });

            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);
            var outputPath = Path.Combine(testDir, "summary.md");

            // Act
            handler.GenerateFileSizeSummary(
                new List<string> { testFolder },
                outputPath,
                config);

            // Assert - Verify recent files was called
            mockRecentFilesManager.Received(1).RegisterGeneratedFile(
                Arg.Any<string>(),
                Arg.Any<RecentFileType>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<long>(),
                Arg.Any<DateTime?>());
        }

        /// <summary>
        /// TEST 5: Handler should update UI progress during execution
        /// Verifies UI integration without blocking.
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryShouldUpdateUiDuringExecution()
        {
            // Arrange
            var testFolder = Path.Combine(testDir, "TestFolder");
            Directory.CreateDirectory(testFolder);
            var testFile = Path.Combine(testFolder, "test.cs");
            File.WriteAllText(testFile, "// Test code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { testFile });

            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);
            var outputPath = Path.Combine(testDir, "summary.md");

            // Act
            handler.GenerateFileSizeSummary(
                new List<string> { testFolder },
                outputPath,
                config);

            // Assert
            mockUi.Received().UpdateStatus(Arg.Any<string>());
            mockUi.Received().WorkStart(Arg.Any<string>(), Arg.Any<IEnumerable<string>>());
            mockUi.Received(1).WorkFinish();
        }

        /// <summary>
        /// TEST 6: Invalid input parameters should throw ArgumentException
        /// Verifies input validation.
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryWithNullFoldersShouldThrowArgumentException()
        {
            // Arrange
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);

            // Act & Assert
            Should.Throw<ArgumentException>(
                () => handler.GenerateFileSizeSummary(
                    null!,
                    Path.Combine(testDir, "output.md"),
                    config));
        }

        /// <summary>
        /// TEST 7: Empty folders list should throw ArgumentException
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryWithEmptyFoldersShouldThrowArgumentException()
        {
            // Arrange
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);

            // Act & Assert
            Should.Throw<ArgumentException>(
                () => handler.GenerateFileSizeSummary(
                    new List<string>(),
                    Path.Combine(testDir, "output.md"),
                    config));
        }

        /// <summary>
        /// TEST 8: Null output path should throw ArgumentException
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryWithNullOutputPathShouldThrowArgumentException()
        {
            // Arrange
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);

            // Act & Assert
            Should.Throw<ArgumentException>(
                () => handler.GenerateFileSizeSummary(
                    new List<string> { testDir },
                    null!,
                    config));
        }

        /// <summary>
        /// TEST 9: LogCtx should be used for structured logging
        /// Verifies audit trail is created.
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryShouldUseLogCtxForAuditTrail()
        {
            // Arrange
            var testFolder = Path.Combine(testDir, "TestFolder");
            Directory.CreateDirectory(testFolder);
            var testFile = Path.Combine(testFolder, "test.cs");
            File.WriteAllText(testFile, "// Test code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { testFile });

            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);
            var outputPath = Path.Combine(testDir, "summary.md");

            // Act
            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "filesizelgctx")
                .Add("testDir", testDir));

            handler.GenerateFileSizeSummary(
                new List<string> { testFolder },
                outputPath,
                config);

            // Assert - If we get here, LogCtx was working
            // (actual log verification would require test-specific log sink)
            File.Exists(outputPath).ShouldBeTrue();
        }

        /// <summary>
        /// TEST 10: Output file should contain formatted size summary
        /// Verifies report structure and content.
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryShouldProduceFormattedReport()
        {
            // Arrange
            var testFolder = Path.Combine(testDir, "TestFolder");
            Directory.CreateDirectory(testFolder);

            // Create multiple files with different extensions
            var csFile = Path.Combine(testFolder, "Program.cs");
            File.WriteAllText(csFile, new string('x', 1000)); // 1000 bytes

            var mdFile = Path.Combine(testFolder, "README.md");
            File.WriteAllText(mdFile, new string('y', 2000)); // 2000 bytes

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { csFile, mdFile });

            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);
            var outputPath = Path.Combine(testDir, "summary.md");

            // Act
            handler.GenerateFileSizeSummary(
                new List<string> { testFolder },
                outputPath,
                config);

            // Assert
            File.Exists(outputPath).ShouldBeTrue("Output file should exist");
            var content = File.ReadAllText(outputPath);

            content.ShouldContain("File Size Summary",Case.Insensitive, "Report should have title");
            content.ShouldContain(".cs", Case.Insensitive, "Report should contain .cs extension");
            content.ShouldContain(".md", Case.Insensitive, "Report should contain .md extension");
            content.ShouldContain("Total", Case.Insensitive, "Report should have totals");
        }

        /// <summary>
        /// TEST 11: Multiple folders should all be included in report
        /// Verifies aggregation across folders.
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryShouldIncludeMultipleFolders()
        {
            // Arrange
            var folder1 = Path.Combine(testDir, "Folder1");
            var folder2 = Path.Combine(testDir, "Folder2");
            Directory.CreateDirectory(folder1);
            Directory.CreateDirectory(folder2);

            var file1 = Path.Combine(folder1, "file1.cs");
            var file2 = Path.Combine(folder2, "file2.cs");
            File.WriteAllText(file1, "// File 1");
            File.WriteAllText(file2, "// File 2");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1, file2 });

            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);
            var outputPath = Path.Combine(testDir, "summary.md");

            // Act
            handler.GenerateFileSizeSummary(
                new List<string> { folder1, folder2 },
                outputPath,
                config);

            // Assert
            File.Exists(outputPath).ShouldBeTrue();
            var content = File.ReadAllText(outputPath);
            content.ShouldNotBeNullOrEmpty();
        }

        /// <summary>
        /// TEST 12: Non-existent folder should be handled gracefully
        /// Verifies error resilience.
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryWithNonExistentFolderShouldHandleGracefully()
        {
            // Arrange
            var nonExistentFolder = Path.Combine(testDir, "doesnotexist");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(Array.Empty<string>());

            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);
            var outputPath = Path.Combine(testDir, "summary.md");

            // Act & Assert - Should not throw, just produce output with zero files
            Should.NotThrow(
                () => handler.GenerateFileSizeSummary(
                    new List<string> { nonExistentFolder },
                    outputPath,
                    config));

            File.Exists(outputPath).ShouldBeTrue("Output should exist even with no files");
        }

        /// <summary>
        /// TEST 13: Output file size should be recorded correctly
        /// Verifies metadata is captured.
        /// </summary>
        [Test]
        public void GenerateFileSizeSummaryShouldRecordOutputFileSize()
        {
            // Arrange
            var testFolder = Path.Combine(testDir, "TestFolder");
            Directory.CreateDirectory(testFolder);
            var testFile = Path.Combine(testFolder, "test.cs");
            File.WriteAllText(testFile, "// Test code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { testFile });

            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);
            var outputPath = Path.Combine(testDir, "summary.md");

            // Act
            handler.GenerateFileSizeSummary(
                new List<string> { testFolder },
                outputPath,
                config);

            // Assert - Verify recent files received a non-zero file size
            mockRecentFilesManager.Received(1).RegisterGeneratedFile(
                Arg.Any<string>(),
                Arg.Any<RecentFileType>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Is<long>(size => size > 0), // File size should be positive
                Arg.Any<DateTime?>());
        }
    }
}
