// File: Tests/RecentFiles/RecentFilesOutputManagerTests.cs
using Shouldly;
using NUnit.Framework;
using VecTool.RecentFiles;
using VecTool.Core.Configuration;

namespace Tests.RecentFiles
{
    [TestFixture]
    public class RecentFilesOutputManagerTests
    {
        private string _root = default!;
        private RecentFilesConfig _config = default!;
        private RecentFilesOutputManager _mgr = default!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "VecTool_Test_" + Guid.NewGuid().ToString("N"));
            _config = new RecentFilesConfig(maxCount: 10, retentionDays: 15, outputPath: _root);
            _mgr = new RecentFilesOutputManager(_config);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                try { Directory.Delete(_root, true); } catch { /* ignore */ }
            }
        }

        [Test]
        public void EnsureBaseDirectory_Should_Create_Path()
        {
            var dir = _mgr.EnsureBaseDirectory();
            Directory.Exists(dir).ShouldBeTrue();
            dir.ShouldBe(_root);
        }

        [Test]
        public void EnsureDatedDirectory_Should_Create_YyyyMmDd_Subdir()
        {
            var when = new DateTimeOffset(2025, 09, 27, 12, 0, 0, TimeSpan.Zero);
            var dated = _mgr.EnsureDatedDirectory(when);

            Directory.Exists(dated).ShouldBeTrue();
            Path.GetFileName(dated).ShouldBe("2025-09-27");
            Path.GetDirectoryName(dated)!.ShouldBe(_root);
        }

        [Test]
        public void CleanupOldFiles_Should_Remove_Files_Older_Than_Retention()
        {
            var now = new DateTimeOffset(2025, 09, 27, 0, 0, 0, TimeSpan.Zero);
            var oldDir = _mgr.EnsureDatedDirectory(now.AddDays(-20));
            var keepDir = _mgr.EnsureDatedDirectory(now.AddDays(-5));

            var oldFile = Path.Combine(oldDir, "old.txt");
            var keepFile = Path.Combine(keepDir, "keep.txt");
            File.WriteAllText(oldFile, "x");
            File.WriteAllText(keepFile, "y");

            File.SetLastWriteTimeUtc(oldFile, now.AddDays(-20).UtcDateTime);
            File.SetLastWriteTimeUtc(keepFile, now.AddDays(-5).UtcDateTime);

            var removed = _mgr.CleanupOldFiles(now);
            removed.ShouldBeGreaterThanOrEqualTo(1);
            File.Exists(oldFile).ShouldBeFalse();
            File.Exists(keepFile).ShouldBeTrue();
        }
    }
}
