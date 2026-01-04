// File: UnitTests/RecentFiles/RecentFilesManagerIntegrationTests.cs
using Shouldly;
using NUnit.Framework;
using VecTool.RecentFiles;
using VecTool.Core.Configuration;

namespace UnitTests.RecentFiles
{
    [TestFixture, Category("Integration")]
    public class RecentFilesManagerIntegrationTests
    {
        private string testDirectory = null!;
        private IRecentFilesManager manager = null!;
        private InMemoryRecentFilesStore store = null!;

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
        public void RegisterGeneratedFile_GitChangesHandler_ShouldTrackFile()
        {
            // Arrange
            var testFile = Path.Combine(testDirectory, "git-changes.md");
            File.WriteAllText(testFile, "# Git Changes\n\nSome changes...");

            // Act
            manager.RegisterGeneratedFile(testFile, RecentFileType.Codebase_Md, new[] { testDirectory });

            // Assert
            var recent = manager.GetRecentFiles();
            recent.Count.ShouldBe(1);
            recent[0].FileType.ShouldBe(RecentFileType.Codebase_Md);
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
            manager.RegisterGeneratedFile(docxFile, RecentFileType.Codebase_Docx, new[] { testDirectory });
            manager.RegisterGeneratedFile(pdfFile, RecentFileType.Codebase_Pdf, new[] { testDirectory });
            manager.RegisterGeneratedFile(mdFile, RecentFileType.Codebase_Md, new[] { testDirectory });

            // Assert - Most recent should be first
            var recent = manager.GetRecentFiles();
            recent.Count.ShouldBe(3);
            recent[0].FilePath.ShouldBe(mdFile); // Last registered
            recent[1].FilePath.ShouldBe(pdfFile);
            recent[2].FilePath.ShouldBe(docxFile); // First registered
        }
    }
}
