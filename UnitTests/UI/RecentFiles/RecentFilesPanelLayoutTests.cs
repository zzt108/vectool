using NUnit.Framework;
using oaiUI.RecentFiles;
using Shouldly;
using System;
using System.Linq;
using VecTool.RecentFiles;

namespace UnitTests.UI.RecentFiles
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class RecentFilesPanelLayoutTests
    {
        private sealed class MockRecentFilesManager : IRecentFilesManager
        {
            public System.Collections.Generic.IReadOnlyList<RecentFileInfo> GetRecentFiles() => Array.Empty<RecentFileInfo>();
            public void RegisterGeneratedFile(string filePath, RecentFileType fileType, System.Collections.Generic.IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null) { }
            public int CleanupExpiredFiles(DateTime? nowUtc = null) => 0;
            public void Save() { }
            public void Load() { }
        }

        private static ListView GetListView(RecentFilesPanel panel) =>
            panel.Controls.Find("lvRecentFiles", true).FirstOrDefault() as ListView
            ?? throw new InvalidOperationException("ListView not found");

        [Test]
        public void ColumnWidths_Should_Persist_And_Restore()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "VecToolLayoutTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            var mgr = new MockRecentFilesManager();

            using (var panel = new RecentFilesPanel(mgr, tmp))
            {
                var lv = GetListView(panel);
                // Ensure columns exist
                if (lv.Columns.Count == 0)
                {
                    // Force code-behind to create defaults
                    panel.Refresh();
                }

                lv.Columns[0].Width = 123;
                if (lv.Columns.Count > 1) lv.Columns[1].Width = 222;

                // Persist
                panel.SaveLayoutForTesting();
            }

            using (var panel2 = new RecentFilesPanel(mgr, tmp))
            {
                var lv2 = GetListView(panel2);
                lv2.Columns.Count.ShouldBeGreaterThanOrEqualTo(2);
                lv2.Columns[0].Width.ShouldBe(123);
                lv2.Columns[1].Width.ShouldBe(222);
            }
        }

        [Test]
        public void RowHeight_Should_Be_AtLeast_10Percent_Bigger()
        {
            var mgr = new MockRecentFilesManager();
            using var panel = new RecentFilesPanel(mgr, null);
            var lv = GetListView(panel);

            var baseHeight = lv.Font.Height + 6;
            var expectedMin = (int)Math.Ceiling(baseHeight * 1.10);

            lv.SmallImageList.ShouldNotBeNull();
            lv.SmallImageList!.ImageSize.Height.ShouldBeGreaterThanOrEqualTo(expectedMin);
        }
    }
}
