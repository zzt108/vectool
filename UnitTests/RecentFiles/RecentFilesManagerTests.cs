// Path: UnitTests/RecentFiles/RecentFilesManagerTests.cs
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using VecTool.Core.Configuration;
using VecTool.RecentFiles;

namespace UnitTests.RecentFiles
{
    [TestFixture, Category("Unit")]
    public class RecentFilesManagerTests
    {
        private static RecentFilesConfig TestConfig(int maxCount = 10, int retentionDays = 30) =>
            new(maxCount, retentionDays, "C:\\Temp\\Tests");

        [Test]
        public void RegisterFile_ShouldAddNewFile()
        {
            // Arrange
            var manager = new RecentFilesManager(TestConfig(), new InMemoryRecentFilesStore());

            // Act
            manager.RegisterGeneratedFile("C:\\test.docx", RecentFileType.Codebase_Docx, new[] { "C:\\Src" });
            var files = manager.GetRecentFiles();

            // Assert
            files.Count.ShouldBe(1);
            files[0].FileName.ShouldBe("test.docx");
        }

        [Test]
        public void RegisterFile_WhenFileExists_ShouldUpdateTimestamp()
        {
            // Arrange
            var manager = new RecentFilesManager(TestConfig(), new InMemoryRecentFilesStore());
            var now = DateTime.Now;
            manager.RegisterGeneratedFile("C:\\test.docx", RecentFileType.Codebase_Docx, new[] { "C:\\Src" }, 1, now.AddMinutes(-10));

            // Act
            manager.RegisterGeneratedFile("C:\\test.docx", RecentFileType.Codebase_Docx, new[] { "C:\\Src" }, 1, now);
            var files = manager.GetRecentFiles();

            // Assert
            files.Count.ShouldBe(1);
            files[0].GeneratedAt.ShouldBe(now);
        }

        [Test]
        public void MaxFileLimit_ShouldRemoveOldest()
        {
            // Arrange
            var manager = new RecentFilesManager(TestConfig(maxCount: 2), new InMemoryRecentFilesStore());
            manager.RegisterGeneratedFile("C:\\oldest.txt", RecentFileType.Unknown, Array.Empty<string>(), 1, DateTime.Now.AddMinutes(-3));
            manager.RegisterGeneratedFile("C:\\middle.txt", RecentFileType.Unknown, Array.Empty<string>(), 1, DateTime.Now.AddMinutes(-2));

            // Act
            manager.RegisterGeneratedFile("C:\\newest.txt", RecentFileType.Unknown, Array.Empty<string>(), 1, DateTime.Now.AddMinutes(-1));
            var files = manager.GetRecentFiles();

            // Assert
            files.Count.ShouldBe(2);
            files.Any(f => f.FileName == "oldest.txt").ShouldBeFalse();
            files.Any(f => f.FileName == "newest.txt").ShouldBeTrue();
        }

        [Test]
        public void CleanupExpiredFiles_ShouldRemoveOldFiles()
        {
            // Arrange
            var manager = new RecentFilesManager(TestConfig(retentionDays: 7), new InMemoryRecentFilesStore());
            var now = DateTime.Now;
            manager.RegisterGeneratedFile("C:\\expired.txt", RecentFileType.Unknown, Array.Empty<string>(), 1, now.AddDays(-8));
            manager.RegisterGeneratedFile("C:\\current.txt", RecentFileType.Unknown, Array.Empty<string>(), 1, now.AddDays(-1));

            // Act
            int removed = manager.CleanupExpiredFiles(now);

            // Assert
            removed.ShouldBe(1);
            var files = manager.GetRecentFiles();
            files.Count.ShouldBe(1);
            files[0].FileName.ShouldBe("current.txt");
        }

        [Test]
        public void SaveAndLoad_ShouldPersistAndRestoreState()
        {
            // Arrange
            var store = new InMemoryRecentFilesStore();
            var manager1 = new RecentFilesManager(TestConfig(), store);
            manager1.RegisterGeneratedFile("C:\\file1.pdf", RecentFileType.Codebase_Pdf, new[] { "C:\\Src1" });
            manager1.Save();

            // Act
            var manager2 = new RecentFilesManager(TestConfig(), store); // Load happens in constructor
            var files = manager2.GetRecentFiles();

            // Assert
            files.Count.ShouldBe(1);
            files[0].FileType.ShouldBe(RecentFileType.Codebase_Pdf);
        }
    }
}
