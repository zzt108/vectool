// ✅ NEW FILE
#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VecTool.RecentFiles;
using oaiUI.Services;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// RecentFilesPanel partial: Data binding, refresh, filtering.
    /// </summary>
    public sealed partial class RecentFilesPanel : UserControl
    {
        // ==================== Data Binding ====================

        /// <summary>
        /// Refresh the list of recent files from the manager, applying the current filter.
        /// </summary>
        public void RefreshList()
        {
            if (lvRecentFiles == null) return;

            lvRecentFiles.BeginUpdate();
            try
            {
                lvRecentFiles.Items.Clear();

                IEnumerable<RecentFileInfo> items = recentFilesManager?.GetRecentFiles() ?? Array.Empty<RecentFileInfo>();

                // Apply filter
                var filter = txtFilter?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    items = items.Where(f => f.FileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var f in items)
                {
                    var item = new ListViewItem(f.FileName) { Tag = f };

                    // Columns: File, Type, Size in KB, Generated
                    item.SubItems.Add(f.FileType.ToString());
                    item.SubItems.Add($"{f.FileSizeBytes / 1024.0:0.0} KB");
                    item.SubItems.Add(f.GeneratedAt.ToString("yyyy-MM-dd HH:mm"));

                    if (!f.Exists)
                    {
                        item.ForeColor = Color.Gray;
                        item.Font = new Font(lvRecentFiles.Font, FontStyle.Italic);
                    }

                    lvRecentFiles.Items.Add(item);
                }

                if (lblStatus != null)
                    lblStatus.Text = $"{lvRecentFiles.Items.Count} files";
            }
            finally
            {
                lvRecentFiles.EndUpdate();
            }
        }

        /// <summary>
        /// Resolve current vector store folders using LastSelectionService (refactor-safe).
        /// </summary>
        private IReadOnlyList<string> GetCurrentVectorStoreSourceFolders()
        {
            try
            {
                var vsName = new LastSelectionService(uiStateDirectory).GetLastSelectedVectorStore();
                if (string.IsNullOrWhiteSpace(vsName))
                    return Array.Empty<string>();

                // TODO: Wire up actual vector store lookup when available
                // For now, return empty (no source folders)
                return Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static RecentFileType MapExtensionToType(string? ext)
        {
            var e = (ext ?? string.Empty).Trim().ToLowerInvariant();
            return e switch
            {
                ".docx" => RecentFileType.AllSourceDocx,
                ".pdf" => RecentFileType.AllSourcePdf,
                ".md" or ".markdown" => RecentFileType.AllSourceMd,
                _ => RecentFileType.Unknown
            };
        }
    }
}
