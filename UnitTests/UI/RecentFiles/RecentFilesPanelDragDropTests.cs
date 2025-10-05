// ✅ FULL FILE VERSION
// Path: tests/UnitTests/RecentFiles/RecentFilesPanelDragDropTests.cs

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using oaiUI.RecentFiles;

namespace UnitTests.RecentFiles
{
    [TestFixture]
    public class RecentFilesPanelDragDropTests
    {
        private RecentFilesPanel panel = null!;

        [SetUp]
        public void SetUp()
        {
            panel = new RecentFilesPanel();
            panel.GetType()
                 .GetMethod("SetupListView", BindingFlags.Instance | BindingFlags.NonPublic)!
                 .Invoke(panel, null);
        }

        [Test]
        public void WireDragDrop_Should_NotThrow_And_Set_AllowDrop()
        {
            // Act
            Should.NotThrow(() =>
                panel.GetType()
                     .GetMethod("WireDragDrop", BindingFlags.Instance | BindingFlags.NonPublic)!
                     .Invoke(panel, null));

            // Assert
            var lv = panel.GetType()
                          .GetField("lvRecentFiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .GetValue(panel) as ListView;
            lv.ShouldNotBeNull();
            lv!.AllowDrop.ShouldBeTrue();

            // Test behavior: can we wire events without throwing?
            Should.NotThrow(() =>
                panel.GetType()
                     .GetMethod("WireDragDrop", BindingFlags.Instance | BindingFlags.NonPublic)!
                     .Invoke(panel, null));
        }

        [Test]
        public void OnListViewDragEnter_Should_Set_Copy_Effect_For_FileDrop()
        {
            // Arrange
            var lv = panel.GetType()
                          .GetField("lvRecentFiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .GetValue(panel) as ListView;
            var tempFile = Path.GetTempFileName();
            var data = new DataObject(DataFormats.FileDrop, new[] { tempFile });
            var args = new DragEventArgs(data, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.Copy);

            // Act
            Should.NotThrow(() =>
                panel.GetType()
                     .GetMethod("OnListViewDragEnter", BindingFlags.Instance | BindingFlags.NonPublic)!
                     .Invoke(panel, new object?[] { lv, args }));

            // Assert
            args.Effect.ShouldBe(DragDropEffects.Copy);

            // Cleanup
            File.Delete(tempFile);
        }

        [Test]
        public void OnListViewDragEnter_Should_Set_None_Effect_For_Non_FileDrop()
        {
            // Arrange
            var lv = panel.GetType()
                          .GetField("lvRecentFiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .GetValue(panel) as ListView;
            var data = new DataObject(DataFormats.Text, "some text");
            var args = new DragEventArgs(data, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.Copy);

            // Act
            Should.NotThrow(() =>
                panel.GetType()
                     .GetMethod("OnListViewDragEnter", BindingFlags.Instance | BindingFlags.NonPublic)!
                     .Invoke(panel, new object?[] { lv, args }));

            // Assert
            args.Effect.ShouldBe(DragDropEffects.None);
        }

        [Test]
        public void OnListViewDragDrop_Should_Update_Status_For_Existing_Files()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                var data = new DataObject(DataFormats.FileDrop, new[] { tempFile });
                var lv = panel.GetType()
                              .GetField("lvRecentFiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                              .GetValue(panel) as ListView;
                var statusLabel = panel.GetType()
                                       .GetField("lblStatus", BindingFlags.Instance | BindingFlags.NonPublic)!
                                       .GetValue(panel) as Label;
                statusLabel.ShouldNotBeNull();

                var args = new DragEventArgs(data, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.Copy);

                // Act
                Should.NotThrow(() =>
                    panel.GetType()
                         .GetMethod("OnListViewDragDrop", BindingFlags.Instance | BindingFlags.NonPublic)!
                         .Invoke(panel, new object?[] { lv, args }));

                // Assert
                statusLabel!.Text.ShouldContain("Dropped 1 file");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void GetSelectedExistingFilePaths_Should_Return_Empty_When_No_Selection()
        {
            // Act
            var result = panel.GetType()
                              .GetMethod("GetSelectedExistingFilePaths", BindingFlags.Instance | BindingFlags.NonPublic)!
                              .Invoke(panel, null) as string[];

            // Assert
            result.ShouldNotBeNull();
            result!.Length.ShouldBe(0);
        }

        [Test]
        public void MapExtensionToType_Should_Map_Common_Extensions()
        {
            // Act & Assert
            var mapMethod = panel.GetType()
                                 .GetMethod("MapExtensionToType", BindingFlags.Static | BindingFlags.NonPublic)!;

            ((string)mapMethod.Invoke(null, new object?[] { ".md" })!).ShouldBe("Markdown");
            ((string)mapMethod.Invoke(null, new object?[] { ".txt" })!).ShouldBe("Text");
            ((string)mapMethod.Invoke(null, new object?[] { ".cs" })!).ShouldBe("Code");
            ((string)mapMethod.Invoke(null, new object?[] { ".png" })!).ShouldBe("Image");
            ((string)mapMethod.Invoke(null, new object?[] { ".pdf" })!).ShouldBe("Document");
            ((string)mapMethod.Invoke(null, new object?[] { ".unknown" })!).ShouldBe("Unknown");
        }
    }
}
