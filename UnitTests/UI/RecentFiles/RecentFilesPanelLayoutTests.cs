// ✅ FULL FILE VERSION
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using oaiUI.RecentFiles;
using VecTool.RecentFiles;

namespace UnitTests.UI.RecentFiles
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA)]
    public class RecentFilesPanelLayoutTests
    {
        private sealed class MockRecentFilesManager : IRecentFilesManager
        {
            public System.Collections.Generic.IReadOnlyList<RecentFileInfo> GetRecentFiles()
                => Array.Empty<RecentFileInfo>();

            public void RegisterGeneratedFile(string filePath, RecentFileType fileType, System.Collections.Generic.IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
            {
                // not needed in layout tests
            }

            public int CleanupExpiredFiles(DateTime? nowUtc = null) => 0;

            public void Save() { /* not used */ }

            public void Load() { /* not used */ }

            public void RemoveFile(string path)
            {
                // No-op for layout tests; interface compliance only
            }
        }

        private static ListView GetListView(RecentFilesPanel panel)
            => panel.Controls.Find("lvRecentFiles", true).FirstOrDefault() as ListView
               ?? throw new InvalidOperationException("ListView not found");

        [Test]
        public void ColumnWidthsShouldPersistAndRestore()
        {
            // Arrange
            var tmp = Path.Combine(Path.GetTempPath(), "VecToolLayoutTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            var mgr = new MockRecentFilesManager();

            try
            {
                using var panel = new RecentFilesPanel(mgr, tmp);

                var lv = GetListView(panel);

                // Ensure columns exist; if none, force code-behind to create defaults
                if (lv.Columns.Count == 0)
                {
                    panel.RefreshList();
                    lv = GetListView(panel);
                }

                // Act: mutate widths, persist, recreate control, and re-check
                if (lv.Columns.Count > 0) lv.Columns[0].Width = 123;
                if (lv.Columns.Count > 1) lv.Columns[1].Width = 222;

                panel.SaveLayoutForTesting();

                using var panel2 = new RecentFilesPanel(mgr, tmp);
                var lv2 = GetListView(panel2);

                // Assert
                lv2.Columns.Count.ShouldBeGreaterThanOrEqualTo(2);
                lv2.Columns[0].Width.ShouldBe(123);
                lv2.Columns[1].Width.ShouldBe(222);
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { /* ignore */ }
            }
        }

        [Test]
        public void RowHeightShouldBeAtLeast10PercentBigger()
        {
            // Arrange
            var mgr = new MockRecentFilesManager();
            using var panel = new RecentFilesPanel(mgr, null);

            // Act
            var lv = GetListView(panel);
            var baseHeight = lv.Font.Height + 6; // WinForms default heuristics
            var expectedMin = (int)Math.Ceiling(baseHeight * 1.10);

            // Assert
            lv.SmallImageList.ShouldNotBeNull();
            lv.SmallImageList!.ImageSize.Height.ShouldBeGreaterThanOrEqualTo(expectedMin);
        }
    }
}
