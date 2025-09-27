// File: UnitTests/RecentFiles/RecentFilesManagerIntegrationTests.cs
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using DocXHandler;
using DocXHandler.RecentFiles;

namespace UnitTests.RecentFiles
{
    [TestFixture]
    public class RecentFilesManagerIntegrationTests
    {
        private string testDirectory;
        private IRecentFilesManager manager;
        private InMemoryRecentFilesStore store;

        [SetUp]
        public void SetUp()
        {
            testDirectory = Path.Combine(Path.GetTempPath(), "RecentFilesIntegrationTests");
            Directory.CreateDirectory(testDirectory);

            var config = new RecentFilesConfig(10, 15, testDirectory);
            store = new InMemoryRecentFilesStore();
            manager = new RecentFilesManager(config, store);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }

        [Test]
        public void RegisterGeneratedFile_DocxHandler_ShouldTrackFile()
        {
            // Arrange
            var handler = new DocXHandler.DocXHandler(null, manager);
            var testFile = Path.Combine(testDirectory, "test.docx");
            File.WriteAllText(testFile, "test content");

            // Act
            manager.RegisterGeneratedFile(testFile, RecentFileType.Docx, new[] { testDirectory });

            // Assert
            var recent = manager.GetRecentFiles();
            recent.Count.ShouldBe(1);
            recent[0].FilePath.ShouldBe(testFile);
            recent[0].FileType.ShouldBe(RecentFileType.Docx);
        }

        [Test]
        public void RegisterGeneratedFile_PdfHandler_ShouldTrackFile()
        {
            // Arrange
            var testFile = Path.Combine(testDirectory, "test.pdf");
            File.WriteAllText(testFile, "pdf content");

            // Act
            manager.RegisterGeneratedFile(testFile, RecentFileType.Pdf, new[] { testDirectory });

            // Assert
            var recent = manager.GetRecentFiles();
            recent.Count.ShouldBe(1);
            recent[0].FileType.ShouldBe(RecentFileType.Pdf);
        }

        [Test]
        public void RegisterGeneratedFile_GitChangesHandler_ShouldTrackFile()
        {
            // Arrange
            var testFile = Path.Combine(testDirectory, "git-changes.md");
            File.WriteAllText(testFile, "# Git Changes\n\nSome changes...");

            // Act
            manager.RegisterGeneratedFile(testFile, RecentFileType.Md, new[] { testDirectory });

            // Assert
            var recent = manager.GetRecentFiles();
            recent.Count.ShouldBe(1);
            recent[0].FileType.ShouldBe(RecentFileType.Md);
        }

        [Test]
        public void RegisterGeneratedFile_MultipleHandlers_ShouldMaintainOrder()
        {
            // Arrange
            var docxFile = Path.Combine(testDirectory, "test.docx");
            var pdfFile = Path.Combine(testDirectory, "test.pdf");
            var mdFile = Path.Combine(testDirectory, "test.md");

            File.WriteAllText(docxFile, "docx");
            File.WriteAllText(pdfFile, "pdf");
            File.WriteAllText(mdFile, "md");

            // Act - Register in sequence
            manager.RegisterGeneratedFile(docxFile, RecentFileType.Docx, new[] { testDirectory });
            manager.RegisterGeneratedFile(pdfFile, RecentFileType.Pdf, new[] { testDirectory });
            manager.RegisterGeneratedFile(mdFile, RecentFileType.Md, new[] { testDirectory });

            // Assert - Most recent should be first
            var recent = manager.GetRecentFiles();
            recent.Count.ShouldBe(3);
            recent[0].FilePath.ShouldBe(mdFile); // Last registered
            recent[1].FilePath.ShouldBe(pdfFile);
            recent[2].FilePath.ShouldBe(docxFile); // First registered
        }
    }
}
