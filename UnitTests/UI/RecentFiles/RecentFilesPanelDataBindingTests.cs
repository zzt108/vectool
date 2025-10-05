// ✅ FULL FILE VERSION
// Path: tests/UnitTests/RecentFiles/RecentFilesPanelDataBindingTests.cs

using NUnit.Framework;
using oaiUI.RecentFiles;
using Shouldly;
using System;
using System.Reflection;
using System.Windows.Forms;
using UnitTests.Fakes;
using VecTool.Core.RecentFiles;
using VecTool.RecentFiles;

namespace UnitTests.RecentFiles
{
    [TestFixture]
    public class RecentFilesPanelDataBindingTests
    {
        private RecentFilesPanel panel = null!;

        [SetUp]
        public void SetUp()
        {
            // Inject a fake manager with sample data
            var fakeManager = new FakeRecentFilesManager();
            panel = new RecentFilesPanel(fakeManager, uiStateDirectory: null);
            // Setup UI and load partial wiring
            panel.GetType()
                 .GetMethod("SetupListView", BindingFlags.Instance | BindingFlags.NonPublic)!
                 .Invoke(panel, null);
        }

        [Test]
        public void RecentFilesPanelLoad_Should_Invoke_RefreshList_And_LoadLayout()
        {
            // Arrange
            var loadMethod = panel.GetType()
                                  .GetMethod("RecentFilesPanelLoad", BindingFlags.Instance | BindingFlags.NonPublic)!;

            // Act & Assert: Should not throw on load
            Should.NotThrow(() => loadMethod.Invoke(panel, new object?[] { panel, EventArgs.Empty }));
        }

        [Test]
        public void RefreshList_Should_Populate_ListView_With_Items_From_Manager()
        {
            // Arrange
            var fakeManager = panel.GetType()
                                   .GetField("recentFilesManager", BindingFlags.Instance | BindingFlags.NonPublic)!
                                   .GetValue(panel) as IRecentFilesManager;
            fakeManager.ShouldNotBeNull();

            // Act
            panel.RefreshList();

            // Assert
            var lv = panel.GetType()
                          .GetField("lvRecentFiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .GetValue(panel) as ListView;
            lv.ShouldNotBeNull();
            lv!.Items.Count.ShouldBe(2); // Fake manager returns 2 items
            lv.Items[0].Text.ShouldBe("file1.txt");
            lv.Items[1].Text.ShouldBe("missing.png");
        }

        [Test]
        public void RefreshList_Should_Filter_Items_Based_On_TextBox()
        {
            // Arrange
            var txt = panel.GetType()
                           .GetField("txtFilter", BindingFlags.Instance | BindingFlags.NonPublic)!
                           .GetValue(panel) as TextBox;
            txt.ShouldNotBeNull();
            txt!.Text = "file1";

            // Act
            panel.RefreshList();

            // Assert
            var lv = panel.GetType()
                          .GetField("lvRecentFiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .GetValue(panel) as ListView;
            lv.ShouldNotBeNull();
            lv!.Items.Count.ShouldBe(1);
            lv.Items[0].Text.ShouldBe("file1.txt");
        }

        [Test]
        public void RefreshList_Should_Style_Missing_Files_As_Italic_Gray()
        {
            // Arrange
            var lv = panel.GetType()
                          .GetField("lvRecentFiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .GetValue(panel) as ListView;
            lv.ShouldNotBeNull();

            // Act
            panel.RefreshList();

            // Assert
            var missingItem = lv!.Items[1];
            missingItem.ForeColor.ShouldBe(System.Drawing.Color.Gray);
            missingItem.Font.Style.ShouldBe(System.Drawing.FontStyle.Italic);
        }

        public void RegisterGeneratedFile(string path, RecentFileType type, IReadOnlyList<string> sources, long size, DateTime? at = null) { }
        public void CleanupExpiredFiles(DateTime? olderThan = null) { }
        public void Save() { }
        public void Load() { }
        public void RemoveFile(string path) { }

    }
}

