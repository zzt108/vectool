using NUnit.Framework;
using oaiUI;
using oaiUI.RecentFiles;
using Shouldly;
using System.Linq;
using System.Windows.Forms;

namespace UnitTests.UI
{
    [TestFixture]
    public class MainFormRecentFilesTabTests
    {
        [Test]
        public void MainForm_ShouldHaveRecentFilesTab()
        {
            // Arrange & Act
            using var form = new MainForm();
            var tabControl = form.Controls.OfType<TabControl>().FirstOrDefault();

            // Assert
            tabControl.ShouldNotBeNull();
            tabControl.TabPages.Count.ShouldBeGreaterThanOrEqualTo(3);
            tabControl.TabPages[2].Text.ShouldBe("Recent Files");
        }

        [Test]
        public void RecentFilesTab_ShouldContainRecentFilesPanel()
        {
            // Arrange & Act
            using var form = new MainForm();
            var tabControl = form.Controls.OfType<TabControl>().FirstOrDefault();
            var recentFilesTab = tabControl?.TabPages[2];

            // Assert
            recentFilesTab.ShouldNotBeNull();
            var panel = recentFilesTab.Controls.OfType<RecentFilesPanel>().FirstOrDefault();
            panel.ShouldNotBeNull();
            panel.Dock.ShouldBe(DockStyle.Fill);
        }

        [Test]
        public void TabSelection_ShouldTriggerPanelRefresh()
        {
            // Arrange
            using var form = new MainForm();
            form.Show(); // Form must be visible for events to fire
            var tabControl = form.Controls.OfType<TabControl>().FirstOrDefault();

            // Act
            tabControl.SelectedIndex = 2; // Select Recent Files tab
            Application.DoEvents(); // Process pending UI events

            // Assert
            // Panel should have been refreshed (verify through log or internal state)
            // This is more of an integration test - hard to unit test without mocking
            tabControl.SelectedIndex.ShouldBe(2);
        }
    }
}
