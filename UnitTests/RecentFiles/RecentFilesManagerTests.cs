using NUnit.Framework;
using Shouldly;
using VecTool.Configuration;
using VecTool.RecentFiles;

namespace UnitTests.RecentFiles
{
    [TestFixture]
    public class RecentFilesManagerTests
    {
        private static RecentFilesConfig Config(int maxCount = 10, int retentionDays = 15)
            => new RecentFilesConfig(maxCount, retentionDays, outputPath: @"%APPDATA%\VecTool\Generated");

        [Test]
        public void RegisterFile_ShouldTrackNewFiles()
        {
            var mgr = new RecentFilesManager(Config(), new InMemoryRecentFilesStore());
            mgr.RegisterGeneratedFile(@"C:\out\a.docx", RecentFileType.Docx, new[] { @"C:\src" }, 123);
            mgr.RegisterGeneratedFile(@"C:\out\b.md", RecentFileType.Md, new[] { @"C:\src" }, 456);

            var list = mgr.GetRecentFiles();
            list.Count.ShouldBe(2);
            list[0].FilePath.ShouldBe(@"C:\out\b.md");
            list[1].FilePath.ShouldBe(@"C:\out\a.docx");
        }

        [Test]
        public void FileRetention_ShouldRespectDateLimits()
        {
            var cfg = Config(maxCount: 10, retentionDays: 7);
            var store = new InMemoryRecentFilesStore();
            var mgr = new RecentFilesManager(cfg, store);

            var now = new DateTime(2025, 09, 27, 12, 0, 0, DateTimeKind.Utc);
            mgr.RegisterGeneratedFile(@"C:\out\keep.md", RecentFileType.Md, Array.Empty<string>(), 1, now);
            mgr.RegisterGeneratedFile(@"C:\out\old.md", RecentFileType.Md, Array.Empty<string>(), 1, now.AddDays(-8));

            var removed = mgr.CleanupExpiredFiles(now);
            removed.ShouldBe(1);

            var list = mgr.GetRecentFiles();
            list.Count.ShouldBe(1);
            list[0].FilePath.ShouldBe(@"C:\out\keep.md");
        }

        [Test]
        public void MaxFileLimit_ShouldRemoveOldest()
        {
            var cfg = Config(maxCount: 2, retentionDays: 999);
            var mgr = new RecentFilesManager(cfg, new InMemoryRecentFilesStore());

            var t = DateTime.UtcNow;
            mgr.RegisterGeneratedFile(@"C:\out\1.md", RecentFileType.Md, Array.Empty<string>(), 1, t.AddMinutes(-3));
            mgr.RegisterGeneratedFile(@"C:\out\2.md", RecentFileType.Md, Array.Empty<string>(), 1, t.AddMinutes(-2));
            mgr.RegisterGeneratedFile(@"C:\out\3.md", RecentFileType.Md, Array.Empty<string>(), 1, t.AddMinutes(-1));

            var list = mgr.GetRecentFiles();
            list.Count.ShouldBe(2);
            list[0].FilePath.ShouldBe(@"C:\out\3.md");
            list[1].FilePath.ShouldBe(@"C:\out\2.md");
        }

        [Test]
        public void GetRecentFiles_ShouldReturnSortedList()
        {
            var mgr = new RecentFilesManager(Config(), new InMemoryRecentFilesStore());
            var baseTime = DateTime.UtcNow;

            mgr.RegisterGeneratedFile(@"C:\out\a.md", RecentFileType.Md, Array.Empty<string>(), 1, baseTime.AddMinutes(-1));
            mgr.RegisterGeneratedFile(@"C:\out\b.md", RecentFileType.Md, Array.Empty<string>(), 1, baseTime.AddMinutes(-2));
            mgr.RegisterGeneratedFile(@"C:\out\c.md", RecentFileType.Md, Array.Empty<string>(), 1, baseTime.AddMinutes(-3));

            var list = mgr.GetRecentFiles();
            list[0].FilePath.ShouldBe(@"C:\out\a.md");
            list[1].FilePath.ShouldBe(@"C:\out\b.md");
            list[2].FilePath.ShouldBe(@"C:\out\c.md");
        }

        [Test]
        public void SaveLoad_ShouldRoundTripViaJsonStore()
        {
            var store = new InMemoryRecentFilesStore();
            var mgr1 = new RecentFilesManager(Config(), store);

            mgr1.RegisterGeneratedFile(@"C:\out\a.pdf", RecentFileType.Pdf, new[] { @"C:\src1" }, 10);
            mgr1.RegisterGeneratedFile(@"C:\out\b.docx", RecentFileType.Docx, new[] { @"C:\src2" }, 20);
            mgr1.Save();

            var mgr2 = new RecentFilesManager(Config(), store);
            mgr2.Load();

            var list = mgr2.GetRecentFiles();
            list.Count.ShouldBe(2);
            list[0].FilePath.ShouldBe(@"C:\out\b.docx");
            list[1].FilePath.ShouldBe(@"C:\out\a.pdf");
        }
    }
}
