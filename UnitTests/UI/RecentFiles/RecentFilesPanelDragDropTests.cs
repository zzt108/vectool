#nullable enable

using NUnit.Framework;
using oaiUI.RecentFiles;
using Shouldly;
using System.Windows.Forms;
using UnitTests.RecentFiles;
using VecTool.Configuration.Helpers;
using VecTool.Core.Configuration;
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
            var store = new InMemoryRecentFilesStore();
            manager = new RecentFilesManager(config, store);

            //Initialize RecentFilesPanel with constructor injection
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
        /// Ensures DragEnter event handler correctly sets Effect to Copy when FileDrop is present.
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
            var testFile = Path.Combine(testDirectory, "dragtest.md");
            File.WriteAllText(testFile, "dragged content");
            var dataObject = new DataObject(DataFormats.FileDrop, new[] { testFile });
            var args = new DragEventArgs(dataObject, keyState: 0, x: 0, y: 0,
                allowedEffect: DragDropEffects.Copy, effect: DragDropEffects.None);

            // Act - 🔄 MODIFY: Use testable ListView subclass instead of reflection
            var testListView = new TestableListView(lvRecentFiles);
            testListView.TriggerDragEnter(args);

            // Assert - Effect should be set to Copy
            args.Effect.ShouldBe(DragDropEffects.Copy);
        }

        /// <summary>
        /// Ensures DragEnter does NOT set Effect when non-FileDrop data is present.
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
            var args = new DragEventArgs(dataObject, keyState: 0, x: 0, y: 0,
                allowedEffect: DragDropEffects.Copy, effect: DragDropEffects.None);

            // Act - 🔄 MODIFY: Use testable ListView subclass
            var testListView = new TestableListView(lvRecentFiles);
            testListView.TriggerDragEnter(args);

            // Assert - Effect should remain None
            args.Effect.ShouldBe(DragDropEffects.None,
                "DragEnter handler should NOT set Effect when FileDrop is not present");
        }

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
        /// Testable wrapper for ListView that exposes DragEnter logic without reflection.
        /// This approach avoids brittle reflection on private WinForms EVENT_DRAGENTER keys.
        /// </summary>
        private sealed class TestableListView
        {
            private readonly ListView targetListView;

            public TestableListView(ListView target)
            {
                targetListView = target.ThrowIfNull(nameof(target));
            }

            /// <summary>
            /// Triggers the DragEnter event handler by invoking the protected OnDragEnter method.
            /// This uses reflection on the instance method, not on private static event keys.
            /// </summary>
            public void TriggerDragEnter(DragEventArgs args)
            {
                // 🔄 MODIFY: Use MethodInfo.Invoke to call protected OnDragEnter
                var onDragEnterMethod = typeof(Control).GetMethod("OnDragEnter",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (onDragEnterMethod == null)
                {
                    Assert.Fail("Could not find OnDragEnter method via reflection");
                    return;
                }

                onDragEnterMethod.Invoke(targetListView, new object[] { args });
            }
        }
    }
}