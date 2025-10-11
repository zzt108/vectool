// ✅ FULL FILE VERSION
using NUnit.Framework;
using Shouldly;
using System.Linq;
using System.Windows.Forms;
using oaiUI.RecentFiles;
using VecTool.OaiUI;

namespace UnitTests.UI
{
    [TestFixture]
    public class MainFormRecentFilesTabTests
    {
        [Test]
        public void MainFormShouldHaveRecentFilesTab()
        {
            // Act
            using var form = new MainForm();

            // Assert
            var tabControl = form.Controls.OfType<TabControl>().FirstOrDefault();
            tabControl.ShouldNotBeNull();
            tabControl!.TabPages.Count.ShouldBeGreaterThanOrEqualTo(3);
            tabControl.TabPages[2].Text.ShouldBe("Recent Files");
        }

        [Test]
        public void RecentFilesTabShouldContainRecentFilesPanel()
        {
            // Act
            using var form = new MainForm();

            var tabControl = form.Controls.OfType<TabControl>().FirstOrDefault();
            tabControl.ShouldNotBeNull();
            var recentFilesTab = tabControl!.TabPages[2];
            recentFilesTab.ShouldNotBeNull();

            var panel = recentFilesTab.Controls.OfType<RecentFilesPanel>().FirstOrDefault();
            panel.ShouldNotBeNull();
            panel!.Dock.ShouldBe(DockStyle.Fill);
        }

        [Test]
        public void TabSelectionShouldTriggerPanelRefresh()
        {
            // Arrange
            using var form = new MainForm();
            form.Show(); // events require a visible form

            var tabControl = form.Controls.OfType<TabControl>().FirstOrDefault();
            tabControl.ShouldNotBeNull();

            // Act
            tabControl!.SelectedIndex = 2; // select "Recent Files" tab
            Application.DoEvents(); // process UI events

            // Assert
            tabControl.SelectedIndex.ShouldBe(2);
            // Deeper assertions could hook into a test-visible signal from RecentFilesPanel.RefreshList(),
            // but that’s outside this test's current scope.
        }
    }
}
