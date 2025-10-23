// ✅ FULL FILE VERSION - NEW TEST
// Path: UnitTests/UI/RecentFiles/RecentFilesPanelDragDropTests.cs
#nullable enable

using NUnit.Framework;
using oaiUI.RecentFiles;
using Shouldly;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using UnitTests.RecentFiles;
using VecTool.Configuration;
using VecTool.RecentFiles;

namespace UnitTests.UI.RecentFiles
{
    /// <summary>
    /// Tests for RecentFilesPanel drag-and-drop functionality to prevent regressions.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)] // Required for WinForms UI tests
    public sealed class RecentFilesPanelDragDropTests
    {
        private RecentFilesPanel? panel;
        private RecentFilesManager? manager;
        private string testDirectory = null!;

        [SetUp]
        public void Setup()
        {
            // Create a temporary test directory
            testDirectory = Path.Combine(Path.GetTempPath(), $"RecentFilesPanelTests_{System.Guid.NewGuid()}");
            Directory.CreateDirectory(testDirectory);

            // Create a test file
            var testFile = Path.Combine(testDirectory, "test.txt");
            File.WriteAllText(testFile, "test content");

            // Initialize RecentFilesManager with test config
            var config = new RecentFilesConfig(10, 30, testDirectory);
            var store = new InMemoryRecentFilesStore(); // ✅ NEW - Add store instance
            manager = new RecentFilesManager(config, store); // 🔄 MODIFY - Pass store to constructor

            // Initialize RecentFilesPanel
            // 🔄 MODIFY - Initialize RecentFilesPanel with constructor injection
            panel = new RecentFilesPanel(); 
            panel.Initialize(manager, testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            panel?.Dispose();

            // Clean up test directory
            if (Directory.Exists(testDirectory))
            {
                try
                {
                    Directory.Delete(testDirectory, recursive: true);
                }
                catch
                {
                    // Best-effort cleanup
                }
            }
        }

        /// <summary>
        /// ✅ NEW - Ensures DragEnter event handler correctly sets Effect to Copy when FileDrop is present.
        /// This test guards against regressions where AllowDrop is not enabled or drag handlers are not wired.
        /// </summary>
        [Test]
        public void DragEnter_ShouldSetEffectToCopy_WhenFileDropPresent()
        {
            // Arrange
            panel.ShouldNotBeNull();
            panel.RefreshList(); // Ensure panel is fully initialized

            // Get the ListView control
            var lvRecentFiles = GetListViewControl(panel);
            lvRecentFiles.ShouldNotBeNull("ListView control should be accessible");

            // Verify AllowDrop is enabled (regression guard)
            lvRecentFiles.AllowDrop.ShouldBeTrue("AllowDrop must be true for drag-drop to work");

            // Create a DataObject with FileDrop format
            var testFile = Path.Combine(testDirectory, "drag_test.md");
            File.WriteAllText(testFile, "dragged content");

            var dataObject = new DataObject(DataFormats.FileDrop, new[] { testFile });
            var args = new DragEventArgs(
                dataObject,
                keyState: 0,
                x: 0,
                y: 0,
                allowedEffect: DragDropEffects.Copy,
                effect: DragDropEffects.None);

            // Act - Invoke the DragEnter event
            InvokeDragEnter(lvRecentFiles, args);

            // Assert - Effect should be set to Copy
            args.Effect.ShouldBe(
                DragDropEffects.Copy,
                "DragEnter handler should set Effect to Copy when FileDrop is present");
        }

        /// <summary>
        /// ✅ NEW - Ensures DragEnter does NOT set Effect when non-FileDrop data is present.
        /// </summary>
        [Test]
        public void DragEnter_ShouldNotSetEffect_WhenNoFileDrop()
        {
            // Arrange
            panel.ShouldNotBeNull();
            panel.RefreshList();

            var lvRecentFiles = GetListViewControl(panel);
            lvRecentFiles.ShouldNotBeNull();

            // Create a DataObject with Text format (not FileDrop)
            var dataObject = new DataObject(DataFormats.Text, "some text");
            var args = new DragEventArgs(
                dataObject,
                keyState: 0,
                x: 0,
                y: 0,
                allowedEffect: DragDropEffects.Copy,
                effect: DragDropEffects.None);

            // Act
            InvokeDragEnter(lvRecentFiles, args);

            // Assert - Effect should remain None
            args.Effect.ShouldBe(
                DragDropEffects.None,
                "DragEnter handler should NOT set Effect when FileDrop is not present");
        }

        // ==================== Helper Methods ====================

        /// <summary>
        /// Reflection-based helper to get the internal ListView control from RecentFilesPanel.
        /// </summary>
        private static ListView? GetListViewControl(RecentFilesPanel panel)
        {
            var field = panel.GetType().GetField("lvRecentFiles",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(panel) as ListView;
        }

        /// <summary>
        /// Invokes the DragEnter event handler by simulating the event call.
        /// </summary>
        private static void InvokeDragEnter(ListView listView, DragEventArgs args)
        {
            // Reflection: get the DragEnter event and invoke all subscribed handlers
            var dragEnterField = typeof(Control).GetField("EVENT_DRAGENTER",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (dragEnterField == null)
            {
                Assert.Fail("Could not find EVENT_DRAGENTER field via reflection");
                return;
            }

            var eventKey = dragEnterField.GetValue(null);
            var eventsField = typeof(Component).GetProperty("Events",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (eventsField == null)
            {
                Assert.Fail("Could not find Events property via reflection");
                return;
            }

            var events = eventsField.GetValue(listView) as System.ComponentModel.EventHandlerList;
            var handler = events?[eventKey] as DragEventHandler;

            if (handler != null)
            {
                handler.Invoke(listView, args);
            }
            else
            {
                Assert.Fail("DragEnter event handler is not wired - this indicates AllowDrop or WireDragDrop regression");
            }
        }
    }
}
