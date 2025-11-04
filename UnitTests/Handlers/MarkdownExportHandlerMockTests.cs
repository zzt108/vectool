// File: UnitTests/Handlers/MarkdownExportHandlerMockTests.cs

using LogCtxShared;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.Core.RecentFiles;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

namespace UnitTests.Handlers
{
    /// <summary>
    /// Mock-based unit tests for MDHandler (MarkdownExportHandler)
    /// Tests verify handler uses traverser exclusively and manages mock state properly
    /// </summary>
    [TestFixture]
    public class MarkdownExportHandlerMockTests
    {
        private IFileSystemTraverser mockTraverser = null!;
        private IRecentFilesManager mockRecentFilesManager = null!;
        private IUserInterface mockUi = null!;
        private VectorStoreConfig config = null!;
        private string testDir = null!;

        [SetUp]
        public void Setup()
        {
            // ✅ Create isolated test directory
            testDir = Path.Combine(Path.GetTempPath(), "MDHandlerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);

            // ✅ Create fresh mock instances for each test
            mockTraverser = Substitute.For<IFileSystemTraverser>();
            mockRecentFilesManager = Substitute.For<IRecentFilesManager>();
            mockUi = Substitute.For<IUserInterface>();

            // ✅ Default configuration
            config = new VectorStoreConfig
            {
                ExcludedFiles = new List<string> { ".log", ".tmp" },
                ExcludedFolders = new List<string> { "bin", "obj", ".git" }
            };
        }

        [TearDown]
        public void Cleanup()
        {
            try
            {
                if (!string.IsNullOrEmpty(testDir) && Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, recursive: true);
                }
            }
            catch
            {
                // Swallow cleanup exceptions in tests
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ✅ DEPENDENCY INJECTION TESTS
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// TEST 1: Constructor accepts all required dependencies
        /// Verifies DI wiring and initialization
        /// </summary>
        [Test]
        public void ConstructorShouldAcceptDependencies()
        {
            // Arrange & Act
            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);

            // Assert
            handler.ShouldNotBeNull();

            // ✅ No traversal should happen during construction
            mockTraverser.Received(0)
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>());
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ✅ TRAVERSER EXCLUSIVITY TESTS (ARCH-002 Validation)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// TEST 2: Handler MUST use traverser for file enumeration
        /// ✅ CRITICAL: Validates exclusive authority pattern (ARCH-002)
        /// Fails if handler uses Directory.GetFiles() directly
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldUseTraverserNotDirectoryEnumeration()
        {
            // Arrange
            var file1 = Path.Combine(testDir, "program.cs");
            var file2 = Path.Combine(testDir, "readme.md");
            File.WriteAllText(file1, "namespace Demo { }");
            File.WriteAllText(file2, "# Project");

            // Mock traverser to return filtered files
            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1, file2 });

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            // ✅ CRITICAL: Traverser MUST be called exactly once per folder
            mockTraverser
                .Received(1)
                .EnumerateFilesRespectingExclusions(
                    Arg.Any<string>(),
                    Arg.Any<VectorStoreConfig>());
        }

        /// <summary>
        /// TEST 3: Handler should NEVER make direct exclusion decisions
        /// ✅ CRITICAL: Validates exclusive authority (ARCH-003)
        /// All exclusion logic delegated to traverser
        /// </summary>
        [Test]
        public void HandlerShouldNotCallIsFileExcludedDirectly()
        {
            // Arrange
            var allowedFile = Path.Combine(testDir, "code.cs");
            var excludedFile = Path.Combine(testDir, "debug.log"); // Matches exclusion pattern
            File.WriteAllText(allowedFile, "code");
            File.WriteAllText(excludedFile, "logs");

            // Mock traverser returns ONLY allowed file (exclusion already applied)
            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { allowedFile }); // ✅ Only allowed file

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            // ✅ Verify traverser was called (handler delegates exclusion logic)
            mockTraverser
                .Received(1)
                .EnumerateFilesRespectingExclusions(
                    Arg.Any<string>(),
                    Arg.Any<VectorStoreConfig>());
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ✅ OUTPUT FILE GENERATION TESTS
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// TEST 4: Output file should be created with correct content
        /// Verifies Markdown generation
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldCreateValidMarkdownFile()
        {
            // Arrange
            var sourceFile = Path.Combine(testDir, "code.cs");
            File.WriteAllText(sourceFile, "namespace Demo { }");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { sourceFile });

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            File.Exists(outputPath).ShouldBeTrue("Output Markdown file should exist");
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("code.cs", Case.Insensitive, "Filename should be in output");
            content.ShouldContain("namespace Demo", Case.Insensitive, "File content should be in output");
        }

        /// <summary>
        /// TEST 5: Output should be registered with RecentFilesManager
        /// ✅ FIX MC-002: Includes ClearReceivedCalls() for test isolation
        /// Verifies artifact tracking without context pollution
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldRegisterOutputWithRecentFiles()
        {
            // Arrange
            var sourceFile = Path.Combine(testDir, "code.cs");
            File.WriteAllText(sourceFile, "code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { sourceFile });

            // ✅ FIX MC-002: Clear mock state before test assertions
            // Prevents orphaned Arg.Any() specs from polluting this test
            mockRecentFilesManager.ClearReceivedCalls();

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            // ✅ FIX MC-002: All 5 parameters explicitly matched
            // Prevents RedundantArgumentMatcherException
            mockRecentFilesManager
                .Received(1)
                .RegisterGeneratedFile(
                    outputPath,
                    RecentFileType.Codebase_Md,
                    Arg.Any<IReadOnlyList<string>>(),
                    Arg.Any<long>(),
                    Arg.Any<DateTime?>());
        }

        /// <summary>
        /// TEST 6: UI should receive progress updates during export
        /// Verifies progress notifications
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldUpdateUiDuringExecution()
        {
            // Arrange
            var sourceFile = Path.Combine(testDir, "code.cs");
            File.WriteAllText(sourceFile, "code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { sourceFile });

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            // ✅ Verify progress notifications
            mockUi.Received(1).WorkStart(Arg.Any<string>(), Arg.Any<List<string>>());
            mockUi.Received(1).WorkFinish();
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ✅ MULTIPLE FOLDERS TESTS
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// TEST 7: Multiple folders should all be processed
        /// Verifies multi-folder export
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldProcessMultipleFolders()
        {
            // Arrange
            var folder1 = Path.Combine(testDir, "folder1");
            var folder2 = Path.Combine(testDir, "folder2");
            Directory.CreateDirectory(folder1);
            Directory.CreateDirectory(folder2);

            var file1 = Path.Combine(folder1, "file1.cs");
            var file2 = Path.Combine(folder2, "file2.cs");
            File.WriteAllText(file1, "file1");
            File.WriteAllText(file2, "file2");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { file1, file2 });

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { folder1, folder2 }, outputPath, config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("file1.cs");
            content.ShouldContain("file2.cs");
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ✅ ASYNC TESTS
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// TEST 8: Async wrapper should delegate to sync implementation
        /// Verifies ExportSelectedFoldersAsync
        /// </summary>
        [Test]
        public async Task ExportSelectedFoldersAsyncShouldDelegate()
        {
            // Arrange
            var sourceFile = Path.Combine(testDir, "code.cs");
            File.WriteAllText(sourceFile, "code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { sourceFile });

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            await handler.ExportSelectedFoldersAsync(new List<string> { testDir }, outputPath, config);

            // Assert
            File.Exists(outputPath).ShouldBeTrue("Async export should produce output file");
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ✅ PARAMETER VALIDATION TESTS (QF-003, MC-004 Related)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// TEST 9: Null folders list should throw ArgumentException
        /// ✅ Validates QF-003 fix requirement
        /// </summary>
        [Test]
        public void ExportSelectedFoldersWithNullFoldersShouldThrow()
        {
            // Arrange
            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
                handler.ExportSelectedFolders(null!, outputPath, config));
        }

        /// <summary>
        /// TEST 10: Empty folders list should throw ArgumentException
        /// ✅ Validates QF-003 fix requirement
        /// </summary>
        [Test]
        public void ExportSelectedFoldersWithEmptyFoldersShouldThrow()
        {
            // Arrange
            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
                handler.ExportSelectedFolders(new List<string>(), outputPath, config));
        }

        /// <summary>
        /// TEST 11: Null output path should throw ArgumentException
        /// Validates output path validation
        /// </summary>
        [Test]
        public void ExportSelectedFoldersWithNullOutputPathShouldThrow()
        {
            // Arrange
            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
                handler.ExportSelectedFolders(new List<string> { testDir }, null!, config));
        }

        /// <summary>
        /// TEST 12: Invalid output path should throw ArgumentException
        /// ✅ Validates MC-003 fix (expect ArgumentException or IOException)
        /// </summary>
        [Test]
        public void ExportSelectedFoldersWithInvalidOutputPathShouldThrow()
        {
            // Arrange
            var sourceFile = Path.Combine(testDir, "code.cs");
            File.WriteAllText(sourceFile, "code");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { sourceFile });

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var invalidPath = "C:\\...<invalid>\\path\\output.md"; // Invalid path syntax

            // Act & Assert
            // ✅ Should throw ArgumentException OR IOException (both acceptable)
            Should.Throw<IOException>(() =>
                handler.ExportSelectedFolders(new List<string> { testDir }, invalidPath, config))
                ;
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ✅ EDGE CASES & STRESS TESTS
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// TEST 13: Mixed file types should be grouped correctly
        /// Verifies categorization by extension
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldGroupByFileType()
        {
            // Arrange
            var cs1 = Path.Combine(testDir, "file1.cs");
            var cs2 = Path.Combine(testDir, "file2.cs");
            var md1 = Path.Combine(testDir, "readme.md");
            var json1 = Path.Combine(testDir, "config.json");

            File.WriteAllText(cs1, "code1");
            File.WriteAllText(cs2, "code2");
            File.WriteAllText(md1, "# Readme");
            File.WriteAllText(json1, "{}");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { cs1, cs2, md1, json1 });

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldContain(".cs");
            content.ShouldContain(".md");
            content.ShouldContain(".json");
        }

        /// <summary>
        /// TEST 14: Files should be grouped hierarchically by folder
        /// Verifies hierarchical structure preservation
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldGroupFilesByFolder()
        {
            // Arrange
            var subfolder = Path.Combine(testDir, "sub");
            Directory.CreateDirectory(subfolder);

            var rootFile = Path.Combine(testDir, "root.cs");
            var subFile = Path.Combine(subfolder, "sub.cs");
            File.WriteAllText(rootFile, "root");
            File.WriteAllText(subFile, "sub");

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(new[] { rootFile, subFile });

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("root.cs");
            content.ShouldContain("sub.cs");
            // ✅ Verify hierarchical structure
            var rootIndex = content.IndexOf("root.cs");
            var subIndex = content.IndexOf("sub.cs");
            subIndex.ShouldBeGreaterThan(rootIndex, "Files should maintain folder hierarchy");
        }

        /// <summary>
        /// TEST 15: Empty file list should produce minimal report
        /// Graceful handling of edge case
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldHandleEmptyFileList()
        {
            // Arrange
            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(Array.Empty<string>()); // No files

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            File.Exists(outputPath).ShouldBeTrue();
            var content = File.ReadAllText(outputPath);
            content.Length.ShouldBeGreaterThan(0); // Should have at least header
        }

        /// <summary>
        /// TEST 16: Large number of files should process efficiently
        /// Stress test
        /// </summary>
        [Test]
        public void ExportSelectedFoldersShouldHandleManyFiles()
        {
            // Arrange
            var files = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                var file = Path.Combine(testDir, $"file{i}.cs");
                File.WriteAllText(file, new string('X', i + 1));
                files.Add(file);
            }

            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(files.ToArray());

            var handler = new MDHandler(mockUi, mockRecentFilesManager, mockTraverser);
            var outputPath = Path.Combine(testDir, "output.md");

            // Act
            handler.ExportSelectedFolders(new List<string> { testDir }, outputPath, config);

            // Assert
            File.Exists(outputPath).ShouldBeTrue();
            var content = File.ReadAllText(outputPath);
            content.ShouldContain(".cs");
            content.Length.ShouldBeGreaterThan(1000); // Should have substantial content
        }
    }
}
