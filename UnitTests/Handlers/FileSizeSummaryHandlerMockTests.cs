// File: UnitTests/Handlers/FileSizeSummaryHandlerMockTests.cs
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles; // adjust if IFileSystemTraverser lives in Core.Traversal

namespace UnitTests.Handlers
{
    [TestFixture]
    public class FileSizeSummaryHandlerMockTests
    {
        private string testDir = default!;
        private VectorStoreConfig config = default!;
        private IUserInterface mockUi = default!;
        private IRecentFilesManager mockRecentFilesManager = default!;
        // Use interface for safe NSubstitute proxying (no ctor args allowed for interfaces).
        private IFileSystemTraverser mockTraverser = default!;

        
        [SetUp]
        public void Setup()
        {
            testDir = Path.Combine(Path.GetTempPath(), "FileSizeSummaryHandlerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);

            config = new VectorStoreConfig();
            mockUi = Substitute.For<IUserInterface>();
            mockRecentFilesManager = Substitute.For<IRecentFilesManager>();

            // Do NOT provide constructor args for interfaces; configure via Returns.
            mockTraverser = Substitute.For<IFileSystemTraverser>();

            // Default mock behavior; tests can override with ConfigureTraverser(…).
            ConfigureTraverserDefault();
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
                // Best-effort cleanup for temp test data.
            }
        }

        // Helper: safe default to avoid NoLastCall issues in tests that forget to configure explicitly.
        private void ConfigureTraverserDefault()
        {
            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(Array.Empty<string>());
        }

        // Helper: per-test configuration to return a specific file set.
        private void ConfigureTraverser(params string[] files)
        {
            mockTraverser
                .EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>())
                .Returns(files ?? Array.Empty<string>());
        }

        [Test]
        public void ConstructorShouldAcceptTraverserInjection()
        {
            // Arrange
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);

            // Assert
            handler.ShouldNotBeNull();
            // Ensure no traversal call happened yet during construction.
            mockTraverser.Received(0).EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>());
        }

        [Test]
        public void GenerateFileSizeSummaryShouldIncludeMultipleFolders()
        {
            // Arrange
            var folderA = Path.Combine(testDir, "A");
            var folderB = Path.Combine(testDir, "B");
            Directory.CreateDirectory(folderA);
            Directory.CreateDirectory(folderB);

            var a1 = Path.Combine(folderA, "a1.cs");
            var b1 = Path.Combine(folderB, "b1.cs");
            File.WriteAllText(a1, "// a1");
            File.WriteAllText(b1, "// b1");

            ConfigureTraverser(a1, b1);
            var output = Path.Combine(testDir, "sizes.md");
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);

            // Act
            handler.GenerateFileSizeSummary(new List<string> { folderA, folderB }, output, config);

            // Assert
            mockTraverser.Received(2).EnumerateFilesRespectingExclusions(Arg.Any<string>(), Arg.Any<VectorStoreConfig>());
            File.Exists(output).ShouldBeTrue();
        }

        [Test]
        public void GenerateFileSizeSummaryShouldProduceFormattedReport()
        {
            // Arrange
            var folder = Path.Combine(testDir, "Src");
            Directory.CreateDirectory(folder);
            var f1 = Path.Combine(folder, "main.cs");
            File.WriteAllText(f1, "// main");
            ConfigureTraverser(f1);

            var output = Path.Combine(testDir, "sizes.md");
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);

            // Act
            handler.GenerateFileSizeSummary(new List<string> { folder }, output, config);

            // Assert
            File.Exists(output).ShouldBeTrue();
            var content = File.ReadAllText(output);
            content.ShouldContain("File Size Summary");
            content.ShouldContain("main.cs");
        }

        [Test]
        public void GenerateFileSizeSummaryShouldRecordOutputFileSize()
        {
            // Arrange
            var folder = Path.Combine(testDir, "Src");
            Directory.CreateDirectory(folder);
            var f1 = Path.Combine(folder, "main.cs");
            File.WriteAllText(f1, new string('X', 128));
            ConfigureTraverser(f1);

            var output = Path.Combine(testDir, "sizes.md");
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);

            // Act
            handler.GenerateFileSizeSummary(new List<string> { folder }, output, config);

            // Assert
            mockRecentFilesManager.Received(1).RegisterGeneratedFile(
                Arg.Is<string>(p => p.EndsWith("sizes.md", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<RecentFileType>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Is<long>(s => s > 0));
        }

        [Test]
        public void GenerateFileSizeSummaryShouldRegisterOutputWithRecentFilesManager()
        {
            // Arrange
            var folder = Path.Combine(testDir, "Src");
            Directory.CreateDirectory(folder);
            var f1 = Path.Combine(folder, "main.cs");
            File.WriteAllText(f1, "//");
            ConfigureTraverser(f1);

            var output = Path.Combine(testDir, "sizes.md");
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);

            // Act
            handler.GenerateFileSizeSummary(new List<string> { folder }, output, config);

            // Assert
            mockRecentFilesManager.Received(1).RegisterGeneratedFile(
                Arg.Is<string>(p => p.EndsWith("sizes.md", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<RecentFileType>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<long>());
        }

        [Test]
        public void GenerateFileSizeSummaryShouldUpdateUiDuringExecution()
        {
            // Arrange
            var folder = Path.Combine(testDir, "Src");
            Directory.CreateDirectory(folder);
            var f1 = Path.Combine(folder, "main.cs");
            File.WriteAllText(f1, "//");
            ConfigureTraverser(f1);

            var output = Path.Combine(testDir, "sizes.md");
            var handler = new FileSizeSummaryHandler(mockUi, mockRecentFilesManager, mockTraverser, testDir);

            // Act
            handler.GenerateFileSizeSummary(new List<string> { folder }, output, config);

            // Assert
            mockUi.Received().ShowMessage(Arg.Is<string>(s => s.Contains("Processing", StringComparison.OrdinalIgnoreCase)));
            mockUi.Received().ShowMessage(Arg.Is<string>(s => s.Contains("Done", StringComparison.OrdinalIgnoreCase)));
        }
    }
}