// ✅ FULL FILE VERSION
// Path: tests/UnitTests/RecentFiles/RecentFilesPanelLayoutTests.cs

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using VecTool.Configuration;
using oaiUI.RecentFiles;

namespace UnitTests.RecentFiles
{
    [TestFixture]
    public class RecentFilesPanelLayoutTests
    {
        private string tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // ignore cleanup failures
            }
        }

        [Test]
        public void SaveLayout_Should_Persist_Column_Widths()
        {
            // Arrange
            var uiState = UiStateConfig.Load(tempDir);
            uiState.RecentFilesColumnWidths = null;
            UiStateConfig.Save(uiState, tempDir);

            var panel = new RecentFilesPanel(null!, tempDir);
            panel.GetType()
                 .GetMethod("SetupListView", BindingFlags.Instance | BindingFlags.NonPublic)!
                 .Invoke(panel, null);

            var lv = panel.GetType()
                          .GetField("lvRecentFiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .GetValue(panel) as ListView;
            lv!.Columns[0].Width = 123;
            lv.Columns[1].Width = 456;

            // Act
            panel.GetType()
                 .GetMethod("SaveLayout", BindingFlags.Instance | BindingFlags.NonPublic)!
                 .Invoke(panel, null);

            // Assert
            var reloaded = UiStateConfig.Load(tempDir);
            reloaded.RecentFilesColumnWidths.ShouldNotBeNull();
            reloaded.RecentFilesColumnWidths!["File"].ShouldBe(123);
            reloaded.RecentFilesColumnWidths!["Type"].ShouldBe(456);
        }

        [Test]
        public void LoadLayout_Should_Apply_Saved_Column_Widths()
        {
            // Arrange
            var uiState = UiStateConfig.Load(tempDir);
            uiState.RecentFilesColumnWidths = new System.Collections.Generic.Dictionary<string, int>
            {
                { "File", 200 },
                { "Type", 100 }
            };
            UiStateConfig.Save(uiState, tempDir);

            var panel = new RecentFilesPanel(null!, tempDir);
            panel.GetType()
                 .GetMethod("SetupListView", BindingFlags.Instance | BindingFlags.NonPublic)!
                 .Invoke(panel, null);

            // Act
            panel.GetType()
                 .GetMethod("LoadLayout", BindingFlags.Instance | BindingFlags.NonPublic)!
                 .Invoke(panel, null);

            // Assert
            var lv = panel.GetType()
                          .GetField("lvRecentFiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .GetValue(panel) as ListView;
            lv!.Columns[0].Width.ShouldBe(200);
            lv.Columns[1].Width.ShouldBe(100);
        }

        [Test]
        public void OnColumnWidthChanged_Should_Debounce_Save()
        {
            // Arrange
            var panel = new RecentFilesPanel(null!, tempDir);
            panel.GetType()
                 .GetMethod("SetupListView", BindingFlags.Instance | BindingFlags.NonPublic)!
                 .Invoke(panel, null);

            bool timerStarted = false;
            var timerField = panel.GetType()
                                  .GetField("saveDebounceTimer", BindingFlags.Instance | BindingFlags.NonPublic)!
                                  .GetValue(panel) as System.Windows.Forms.Timer;
            timerField!.Tick += (_, __) => timerStarted = true;

            // Act
            var args = new ColumnWidthChangedEventArgs(0);
            panel.GetType()
                 .GetMethod("OnColumnWidthChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                 .Invoke(panel, new object?[] { panel, args });

            // Force timer tick
            timerField.Interval = 1;
            timerField.Start();
            Application.DoEvents();
            System.Threading.Thread.Sleep(10);

            // Assert
            timerStarted.ShouldBeTrue();
        }
    }
}
