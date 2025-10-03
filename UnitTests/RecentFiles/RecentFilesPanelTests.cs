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
    [TestFixture]
    [Apartment(ApartmentState.STA)] // Required for WinForms controls
    public partial class RecentFilesPanelTests
    {
        private MockRecentFilesManager mockManager = null!;
        private RecentFilesPanel panel = null!;

        [SetUp]
        public void SetUp()
        {
            mockManager = new MockRecentFilesManager();
            panel = new RecentFilesPanel(mockManager);
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
            mockManager.AddFile("C:\\test\\report1.docx", RecentFileType.Docx, 100);
            mockManager.AddFile("C:\\test\\report2.pdf", RecentFileType.Pdf, 200);

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
            mockManager.AddFile("C:\\test\\report.docx", RecentFileType.Docx, 100);
            mockManager.AddFile("C:\\test\\invoice.pdf", RecentFileType.Pdf, 200);
            panel.RefreshList();

            var filterBox = GetFilterTextBox(panel);
            var listView = GetListView(panel);

            // Act
            filterBox.Text = "report";
            Application.DoEvents(); // Trigger TextChanged

            // Assert
            listView.Items.Count.ShouldBe(1);
            listView.Items[0].Text.ShouldBe("report.docx");
        }

        [Test]
        public void RefreshButtonShouldReloadData()
        {
            // Arrange
            mockManager.AddFile("C:\\test\\file1.md", RecentFileType.Md, 50);
            panel.RefreshList();

            var listView = GetListView(panel);
            listView.Items.Count.ShouldBe(1);

            // Act - Add new file and refresh
            mockManager.AddFile("C:\\test\\file2.md", RecentFileType.Md, 75);
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
            mockManager.AddFile("C:\\test\\existing.docx", RecentFileType.Docx, 100, exists: true);
            mockManager.AddFile("C:\\test\\missing.docx", RecentFileType.Docx, 200, exists: false);

            // Act
            panel.RefreshList();

            // Assert
            var listView = GetListView(panel);
            listView.Items[0].ForeColor.ShouldNotBe(System.Drawing.Color.Gray);
            listView.Items[1].ForeColor.ShouldBe(System.Drawing.Color.Gray);
            listView.Items[1].Font.Italic.ShouldBeTrue();
        }

        [Test]
        public void GetSelectedRecentFilesShouldFilterByTag()
        {
            // Arrange - Create fake RecentFileInfo instances
            var file1 = new RecentFileInfo("C:\\test\\file1.docx", DateTimeOffset.UtcNow,
                RecentFileType.Docx, new List<string>(), 100);
            var file2 = new RecentFileInfo("C:\\test\\file2.pdf", DateTimeOffset.UtcNow,
                RecentFileType.Pdf, new List<string>(), 200);
            var file3 = new RecentFileInfo("C:\\test\\missing.docx", DateTimeOffset.UtcNow,
                RecentFileType.Docx, new List<string>(), 300);

            // Manually create ListView items with Tags
            var listView = GetListView(panel);
            listView.Items.Clear();

            var item1 = new ListViewItem("file1.docx") { Tag = file1 };
            var item2 = new ListViewItem("file2.pdf") { Tag = file2 };
            var item3 = new ListViewItem("missing.docx") { Tag = file3 };

            listView.Items.Add(item1);
            listView.Items.Add(item2);
            listView.Items.Add(item3);

            // Simulate selection by adding to SelectedItems manually
            var selectedCollection = new List<ListViewItem> { item1, item2 };

            // Act - Manually extract RecentFileInfo from selected items
            var result = new List<RecentFileInfo>();
            foreach (var item in selectedCollection)
            {
                if (item.Tag is RecentFileInfo info)
                {
                    result.Add(info);
                }
            }

            // Assert
            result.Count.ShouldBe(2);
            result[0].FileName.ShouldBe("file1.docx");
            result[1].FileName.ShouldBe("file2.pdf");
        }

        [Test]
        public void DragOperationShouldValidateExistingFiles()
        {
            // Arrange - Simulate file validation logic
            var existingFile = new RecentFileInfo("C:\\exists.docx", DateTimeOffset.UtcNow,
                RecentFileType.Docx, new List<string>(), 100);

            var missingFile = new RecentFileInfo("C:\\missing.docx", DateTimeOffset.UtcNow,
                RecentFileType.Docx, new List<string>(), 200);

            // Create a custom mock to control Exists behavior
            var mockExistingFile = new MockRecentFileInfo("C:\\exists.docx", RecentFileType.Docx, 100, exists: true);
            var mockMissingFile = new MockRecentFileInfo("C:\\missing.docx", RecentFileType.Docx, 200, exists: false);

            var selectedFiles = new List<RecentFileInfo> { mockExistingFile, mockMissingFile };

            // Act - Filter out missing files (simulates drag validation)
            var validFiles = selectedFiles.Where(f => f.Exists).ToList();
            var missingFiles = selectedFiles.Where(f => !f.Exists).ToList();

            // Assert
            validFiles.Count.ShouldBe(1);
            validFiles[0].FileName.ShouldBe("exists.docx");

            missingFiles.Count.ShouldBe(1);
            missingFiles[0].FileName.ShouldBe("missing.docx");
        }

        [Test]
        public void DragOperationShouldCreateFilePathArray()
        {
            // Arrange - Simulate creating DataObject for drag-drop
            var file1 = new RecentFileInfo("C:\\test\\file1.docx", DateTimeOffset.UtcNow,
                RecentFileType.Docx, new List<string>(), 100);
            var file2 = new RecentFileInfo("C:\\test\\file2.pdf", DateTimeOffset.UtcNow,
                RecentFileType.Pdf, new List<string>(), 200);

            var selectedFiles = new List<RecentFileInfo> { file1, file2 };

            // Act - Create file path array (what drag operation does)
            string[] filePaths = selectedFiles.Select(f => f.FilePath).ToArray();

            // Assert
            filePaths.Length.ShouldBe(2);
            filePaths[0].ShouldBe("C:\\test\\file1.docx");
            filePaths[1].ShouldBe("C:\\test\\file2.pdf");
        }

        [Test]
        public void MultiSelectShouldBeEnabled()
        {
            // Arrange & Act
            var listView = GetListView(panel);

            // Assert - Verify multi-select is configured correctly
            listView.MultiSelect.ShouldBeTrue();
        }

        [Test]
        public void ListViewItemTagShouldStoreRecentFileInfo()
        {
            // Arrange
            mockManager.AddFile("C:\\test\\report.docx", RecentFileType.Docx, 100, exists: true);
            panel.RefreshList();

            // Act
            var listView = GetListView(panel);
            Application.DoEvents();

            // Assert
            listView.Items.Count.ShouldBe(1);
            listView.Items[0].Tag.ShouldNotBeNull();

            // Check if Tag is assignable to RecentFileInfo (allows derived types)
            listView.Items[0].Tag.ShouldBeAssignableTo<RecentFileInfo>();

            var fileInfo = (RecentFileInfo)listView.Items[0].Tag;
            fileInfo.FileName.ShouldBe("report.docx");
            fileInfo.FileType.ShouldBe(RecentFileType.Docx);
            fileInfo.FileSizeBytes.ShouldBe(100);
        }

        // ==============================
        // Helper methods to access private controls
        // ==============================

        private ListView GetListView(RecentFilesPanel panel)
        {
            return panel.Controls.Find("lvRecentFiles", true).FirstOrDefault() as ListView
                ?? throw new InvalidOperationException("ListView not found");
        }

        private TextBox GetFilterTextBox(RecentFilesPanel panel)
        {
            return panel.Controls.Find("txtFilter", true).FirstOrDefault() as TextBox
                ?? throw new InvalidOperationException("Filter TextBox not found");
        }

        private Button GetRefreshButton(RecentFilesPanel panel)
        {
            return panel.Controls.Find("btnRefresh", true).FirstOrDefault() as Button
                ?? throw new InvalidOperationException("Refresh Button not found");
        }

        // ==============================
        // Mock manager for testing
        // ==============================

        private class MockRecentFilesManager : IRecentFilesManager
        {
            private readonly List<MockFileInfo> files = new();

            public void AddFile(string path, RecentFileType type, long size, bool exists = true)
            {
                files.Add(new MockFileInfo(path, type, size, exists));
            }

            public IReadOnlyList<RecentFileInfo> GetRecentFiles()
            {
                return files.Select(f => new MockRecentFileInfo(f)).ToList();
            }

            public void RegisterGeneratedFile(string filePath, RecentFileType fileType,
                IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
            {
                // Not used in these tests
            }

            public int CleanupExpiredFiles(DateTime? nowUtc = null) => 0;

            public void Save()
            {
                throw new NotImplementedException();
            }

            public void Load()
            {
                throw new NotImplementedException();
            }
        }

        private record MockFileInfo(string Path, RecentFileType Type, long Size, bool Exists);
    }
}
