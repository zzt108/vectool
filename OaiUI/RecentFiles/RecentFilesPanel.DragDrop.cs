// ✅ NEW FILE
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VecTool.RecentFiles;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// RecentFilesPanel partial: Drag-drop operations (inbound/outbound).
    /// </summary>
    public sealed partial class RecentFilesPanel : UserControl
    {
        // ==================== Drag-Drop ====================

        private void WireDragDrop()
        {
            if (lvRecentFiles is null) return;

            // Inbound
            lvRecentFiles.AllowDrop = true;
            lvRecentFiles.DragEnter += OnListViewDragEnter;
            lvRecentFiles.DragDrop += OnListViewDragDrop;

            // Outbound
            lvRecentFiles.ItemDrag += OnListViewItemDrag;
        }

        // ==================== Inbound Handlers ====================

        private void OnListViewDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void OnListViewDragDrop(object? sender, DragEventArgs e)
        {
            if (recentFilesManager is null) return;
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true) return;

            try
            {
                var data = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (data is null || data.Length == 0) return;

                var sourceFolders = GetCurrentVectorStoreSourceFolders();
                var added = 0;

                foreach (var path in data)
                {
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    if (!File.Exists(path)) continue;

                    var type = new RecentFileType().MapExtensionToType(Path.GetExtension(path), Path.GetFileName(path));
                    long size = 0;

                    try { size = new FileInfo(path).Length; }
                    catch { size = 0; }

                    // Register and associate with current vector store folders
                    recentFilesManager.RegisterGeneratedFile(
                        filePath: path,
                        fileType: type,
                        sourceFolders: sourceFolders,
                        fileSizeBytes: size,
                        generatedAtUtc: DateTime.UtcNow);

                    added++;
                }

                if (added > 0)
                {
                    recentFilesManager.Save();
                    RefreshList();
                }
            }
            catch
            {
                // Be defensive - never crash on bad drops
            }
        }

        // ==================== Outbound Handler ====================

        private void OnListViewItemDrag(object? sender, ItemDragEventArgs e)
        {
            var filePaths = GetSelectedExistingFilePaths();
            if (filePaths.Length == 0) return;

            var sc = new StringCollection();
            sc.AddRange(filePaths);

            var data = new DataObject();
            data.SetFileDropList(sc);

            try
            {
                data.SetData("Preferred DropEffect", DragDropEffects.Copy);
            }
            catch { /* ignore */ }

            DoDragDrop(data, DragDropEffects.Copy);
        }

        private string[] GetSelectedExistingFilePaths()
        {
            if (lvRecentFiles is null) return Array.Empty<string>();

            var list = new List<string>();
            foreach (ListViewItem item in lvRecentFiles.SelectedItems)
            {
                if (item.Tag is RecentFileInfo info && info.Exists && !string.IsNullOrWhiteSpace(info.FilePath))
                    list.Add(info.FilePath);
            }

            return list.ToArray();
        }
    }
}
