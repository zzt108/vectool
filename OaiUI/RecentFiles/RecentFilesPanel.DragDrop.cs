// ✅ FULL FILE VERSION
// Path: OaiUI/RecentFiles/RecentFilesPanel.DragDrop.cs

#nullable enable

using LogCtxShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace oaiUI.RecentFiles
{
    // Drag-and-drop behavior partial (inbound file drops + outbound item drags)
    public partial class RecentFilesPanel : UserControl
    {
        // Wires drag-drop events on the ListView; safe to call multiple times.
        private void WireDragDrop()
        {
            if (lvRecentFiles is null)
                return;

            lvRecentFiles.AllowDrop = true;

            // Avoid duplicate subscriptions if WireRuntime re-invokes this
            lvRecentFiles.ItemDrag -= OnListViewItemDrag;
            lvRecentFiles.DragEnter -= OnListViewDragEnter;
            lvRecentFiles.DragDrop -= OnListViewDragDrop;

            lvRecentFiles.ItemDrag += OnListViewItemDrag;
            lvRecentFiles.DragEnter += OnListViewDragEnter;
            lvRecentFiles.DragDrop += OnListViewDragDrop;

            _log.Ctx.Set().Add("component", "RecentFilesPanel")
                      .Add("area", "DragDrop");
                      _log.Info("DragDrop wiring completed for RecentFiles ListView.");
        }

        // Sets cursor effect for inbound file drag based on availability of FileDrop.
        private void OnListViewDragEnter(object? sender, DragEventArgs e)
        {
            try
            {
                var ok = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop);
                e.Effect = ok ? DragDropEffects.Copy : DragDropEffects.None;

                //LogCtx.Log.With("component", "RecentFilesPanel")
                //          .With("area", "DragDrop")
                //          .With("canDrop", ok)
                //          .Info("DragEnter evaluated.");
            }
            catch (Exception ex)
            {
                //LogCtx.Log.With("component", "RecentFilesPanel")
                //          .With("area", "DragDrop")
                //          .With("err", ex)
                //          .Error("Error during DragEnter.");
                e.Effect = DragDropEffects.None;
            }
        }

        // Handles inbound file drop onto the ListView; filters non-existing paths and refreshes the list.
        private void OnListViewDragDrop(object? sender, DragEventArgs e)
        {
            try
            {
                if (e.Data is null || !e.Data.GetDataPresent(DataFormats.FileDrop))
                    return;

                var dropped = e.Data.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
                var existing = dropped.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

                //LogCtx.Log.With("component", "RecentFilesPanel")
                //          .With("area", "DragDrop")
                //          .With("droppedCount", dropped.Length)
                //          .With("existingCount", existing.Length)
                //          .Info("DragDrop received.");

                // Optional: integrate with a recent files service if available
                // Safe no-op if not configured in Phase 2
                // recentFilesManager?.AddFiles(existing);

                if (lblStatus != null)
                {
                    lblStatus.Text = existing.Length == 0
                        ? "No existing files were dropped."
                        : $"Dropped {existing.Length} file(s).";
                }

                // Trigger a refresh so UI reflects potential changes
                RefreshList();
            }
            catch (Exception ex)
            {
                //LogCtx.Log.With("component", "RecentFilesPanel")
                //          .With("area", "DragDrop")
                //          .With("err", ex)
                //          .Error("Error during DragDrop.");
            }
        }

        // Starts an outbound drag with selected existing file paths so the user can drop to external targets (e.g., Explorer).
        private void OnListViewItemDrag(object? sender, ItemDragEventArgs e)
        {
            try
            {
                var paths = GetSelectedExistingFilePaths();
                if (paths.Length == 0)
                    return;

                var data = new DataObject(DataFormats.FileDrop, paths);
                DoDragDrop(data, DragDropEffects.Copy);

                //LogCtx.Log.With("component", "RecentFilesPanel")
                //          .With("area", "DragDrop")
                //          .With("count", paths.Length)
                //          .Info("Outbound item drag initiated.");
            }
            catch (Exception ex)
            {
                //LogCtx.Log.With("component", "RecentFilesPanel")
                //          .With("area", "DragDrop")
                //          .With("err", ex)
                //          .Error("Error during ItemDrag.");
            }
        }

        // Collects selected existing file paths from the ListView.
        private string[] GetSelectedExistingFilePaths()
        {
            if (lvRecentFiles is null || lvRecentFiles.SelectedItems.Count == 0)
                return Array.Empty<string>();

            var result = new List<string>(lvRecentFiles.SelectedItems.Count);

            foreach (ListViewItem item in lvRecentFiles.SelectedItems)
            {
                // Prefer Tag if it's a full path; fallback to first column text
                var candidate =
                    item.Tag as string ??
                    (item.SubItems.Count > 0 ? item.SubItems[0].Text : item.Text);

                if (!string.IsNullOrWhiteSpace(candidate) &&
                    Path.IsPathRooted(candidate) &&
                    File.Exists(candidate))
                {
                    result.Add(candidate);
                }
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        // Basic extension-to-type mapping stub for future metadata enrichment.
        private static string MapExtensionToType(string? ext)
        {
            if (string.IsNullOrWhiteSpace(ext))
                return "Unknown";

            ext = ext.Trim().ToLowerInvariant();
            if (!ext.StartsWith(".")) ext = "." + ext;

            return ext switch
            {
                ".md" or ".markdown" => "Markdown",
                ".txt" => "Text",
                ".cs" or ".vb" or ".fs" or ".ts" or ".js" or ".json" or ".yml" or ".yaml" or ".xml" => "Code",
                ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".svg" => "Image",
                ".pdf" => "Document",
                _ => "Unknown"
            };
        }

        // Placeholder for deriving vector-store source folders if needed in drag metadata.
        private IReadOnlyList<string> GetCurrentVectorStoreSourceFolders()
        {
            // Phase 2: keep conservative; provide a stable but empty set
            return Array.Empty<string>();
        }
    }
}
