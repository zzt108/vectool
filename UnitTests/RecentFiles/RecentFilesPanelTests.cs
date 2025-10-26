// ✅ FULL FILE VERSION
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using oaiUI.RecentFiles;
using VecTool.RecentFiles;

namespace UnitTests.UI.RecentFiles
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA)]
    public partial class RecentFilesPanelTests
    {
        private MockRecentFilesManager mockManager = null!;
        private RecentFilesPanel panel = null!;

        [SetUp]
        public void SetUp()
        {
            mockManager = new MockRecentFilesManager();
            panel = new RecentFilesPanel().Initialize(mockManager, null);

        }

        [TearDown]
        public void TearDown()
        {
            panel?.Dispose();
        }

        [Test]
        public void RefreshListShouldPopulateListView()
        {
            // Arrange
            mockManager.AddFile("report1.docx", RecentFileType.Codebase_Docx, 100);
            mockManager.AddFile("report2.pdf", RecentFileType.Codebase_Pdf, 200);

            // Act
            panel.RefreshList();

            // Assert
            var listView = GetListView(panel);
            listView.Items.Count.ShouldBe(2);
            listView.Items[0].Text.ShouldBe("report1.docx");
            listView.Items[1].Text.ShouldBe("report2.pdf");
        }

        [Test]
        public void FilterShouldReduceVisibleItems()
        {
            // Arrange
            mockManager.AddFile("report.docx", RecentFileType.Codebase_Docx, 100);
            mockManager.AddFile("notes.pdf", RecentFileType.Codebase_Pdf, 200);
            panel.RefreshList();

            var filterBox = GetFilterTextBox(panel);
            var listView = GetListView(panel);

            // Act
            filterBox.Text = "report";
            Application.DoEvents(); // trigger TextChanged

            // Assert
            listView.Items.Count.ShouldBe(1);
            listView.Items[0].Text.ShouldBe("report.docx");
        }

        [Test]
        public void RefreshButtonShouldReloadData()
        {
            // Arrange
            mockManager.AddFile("c1.md", RecentFileType.Codebase_Md, 50);
            panel.RefreshList();
            var listView = GetListView(panel);
            listView.Items.Count.ShouldBe(1);

            // Act
            mockManager.AddFile("c2.md", RecentFileType.Codebase_Md, 75);
            var refreshButton = GetRefreshButton(panel);
            refreshButton.PerformClick();
            Application.DoEvents();

            // Assert
            listView.Items.Count.ShouldBe(2);
        }

        [Test]
        public void MissingFilesShouldBeStyledDifferently()
        {
            // Arrange
            mockManager.AddFile("exists.docx", RecentFileType.Codebase_Docx, 100, exists: true);
            mockManager.AddFile("missing.docx", RecentFileType.Codebase_Docx, 200, exists: false);

            // Act
            panel.RefreshList();

            // Assert
            var listView = GetListView(panel);

            // Verify existing file is NOT gray (normal styling applies)
            listView.Items[0].ForeColor.ShouldNotBe(System.Drawing.Color.Gray);
            listView.Items[0].Font.Italic.ShouldBeFalse();

            // Verify missing file IS gray and italic
            listView.Items[1].ForeColor.ShouldBe(System.Drawing.Color.Gray);
            listView.Items[1].Font.Italic.ShouldBeTrue();
        }

        [Test]
        public void MultiSelectShouldBeEnabled()
        {
            // Act
            var listView = GetListView(panel);

            // Assert
            listView.MultiSelect.ShouldBeTrue();
        }

        [Test]
        public void ListViewItemTagShouldStoreRecentFileInfo()
        {
            // Arrange
            mockManager.AddFile("report.docx", RecentFileType.Codebase_Docx, 100, exists: true);
            panel.RefreshList();

            // Act
            var listView = GetListView(panel);
            Application.DoEvents();

            // Assert
            listView.Items.Count.ShouldBe(1);
            listView.Items[0].Tag.ShouldNotBeNull();
            listView.Items[0].Tag.ShouldBeAssignableTo<RecentFileInfo>();
            var fileInfo = (RecentFileInfo)listView.Items[0].Tag;
            fileInfo.FileName.ShouldBe("report.docx");
            fileInfo.FileType.ShouldBe(RecentFileType.Codebase_Docx);
            fileInfo.FileSizeBytes.ShouldBe(100);
        }

        // Helper methods to access private controls
        private static ListView GetListView(RecentFilesPanel panel)
            => panel.Controls.Find("lvRecentFiles", true).FirstOrDefault() as ListView
               ?? throw new InvalidOperationException("ListView not found");

        private static TextBox GetFilterTextBox(RecentFilesPanel panel)
            => panel.Controls.Find("txtFilter", true).FirstOrDefault() as TextBox
               ?? throw new InvalidOperationException("Filter TextBox not found");

        private static Button GetRefreshButton(RecentFilesPanel panel)
            => panel.Controls.Find("btnRefresh", true).FirstOrDefault() as Button
               ?? throw new InvalidOperationException("Refresh Button not found");

        // Mock manager for testing
        private class MockRecentFilesManager : IRecentFilesManager
        {
            private readonly List<MockFileInfo> files = new();

            public void AddFile(string path, RecentFileType type, long size, bool exists = true)
            {
                files.Add(new MockFileInfo(path, type, size, exists));
            }

            public IReadOnlyList<RecentFileInfo> GetRecentFiles()
            {
                return files.Select(f => new RecentFileInfo(f.Path, DateTimeOffset.UtcNow, f.Type, new List<string>(), f.Size)).ToList();
            }

            public void RegisterGeneratedFile(string filePath, RecentFileType fileType, IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
            {
                // Not used in these tests
            }

            public int CleanupExpiredFiles(DateTime? nowUtc = null) => 0;

            public void Save() => throw new NotImplementedException();

            public void Load() => throw new NotImplementedException();

            public void RemoveFile(string path)
            {
                // Minimal behavior for tests: remove by path if present
                var idx = files.FindIndex(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) files.RemoveAt(idx);
            }

            private record MockFileInfo(string Path, RecentFileType Type, long Size, bool Exists);
        }
    }
}
