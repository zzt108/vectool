using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DocXHandler.RecentFiles;
using oaiUI.RecentFiles;

namespace UnitTests.UI.RecentFiles
{
    [TestFixture]
    [Apartment(ApartmentState.STA)] // Required for WinForms controls
    public class RecentFilesPanelTests
    {
        private MockRecentFilesManager mockManager;
        private RecentFilesPanel panel;

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
            mockManager.AddFile("C:\\test1.docx", RecentFileType.Docx, 100);
            mockManager.AddFile("C:\\test2.pdf", RecentFileType.Pdf, 200);

            // Act
            panel.RefreshList();

            // Assert
            var listView = GetListView(panel);
            listView.Items.Count.ShouldBe(2);
            listView.Items[0].Text.ShouldBe("test1.docx");
            listView.Items[1].Text.ShouldBe("test2.pdf");
        }

        [Test]
        public void FilterShouldReduceVisibleItems()
        {
            // Arrange
            mockManager.AddFile("C:\\report.docx", RecentFileType.Docx, 100);
            mockManager.AddFile("C:\\summary.pdf", RecentFileType.Pdf, 200);
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
            mockManager.AddFile("C:\\initial.md", RecentFileType.Md, 50);
            panel.RefreshList();

            var listView = GetListView(panel);
            listView.Items.Count.ShouldBe(1);

            // Act - Add new file and refresh
            mockManager.AddFile("C:\\new.md", RecentFileType.Md, 75);
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
            mockManager.AddFile("C:\\exists.docx", RecentFileType.Docx, 100, exists: true);
            mockManager.AddFile("C:\\missing.docx", RecentFileType.Docx, 200, exists: false);

            // Act
            panel.RefreshList();

            // Assert
            var listView = GetListView(panel);
            listView.Items[0].ForeColor.ShouldNotBe(System.Drawing.Color.Gray);
            listView.Items[1].ForeColor.ShouldBe(System.Drawing.Color.Gray);
            listView.Items[1].Font.Italic.ShouldBeTrue();
        }

        // Helper methods to access private controls
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
                return files.Select(f => new MockRecentFileInfo(f)).ToList();
            }

            public void RegisterGeneratedFile(string filePath, RecentFileType fileType,
                IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
            {
                // Not used in these tests
            }

            public int CleanupExpiredFiles(DateTime? nowUtc = null) => 0;
        }

        private record MockFileInfo(string Path, RecentFileType Type, long Size, bool Exists);

        private class MockRecentFileInfo : RecentFileInfo
        {
            private readonly bool mockExists;

            public MockRecentFileInfo(MockFileInfo info)
                : base(info.Path, DateTimeOffset.UtcNow, info.Type, new List<string>(), info.Size)
            {
                mockExists = info.Exists;
            }

            // Override the Exists property to return our mock value
            public override bool Exists => mockExists;
        }
    }
}
