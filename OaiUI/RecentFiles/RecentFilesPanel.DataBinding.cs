// ✅ FULL FILE VERSION

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
        // Data Binding

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

                IEnumerable<RecentFileInfo> items = recentFilesManager?.GetRecentFiles()
                    ?? Array.Empty<RecentFileInfo>();

                // Apply filter
                var filter = txtFilter?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    items = items.Where(f =>
                        f.FileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var f in items)
                {
                    var item = new ListViewItem(f.FileName)
                    {
                        Tag = f
                    };

                    // Columns: File, Type, Size in KB, Generated
                    var typeDisplay = f.FileType != RecentFileType.Unknown
                        ? f.FileType.ToString()
                        : MapExtensionToType(Path.GetExtension(f.FileName), f.FileName).ToString();

                    item.SubItems.Add(typeDisplay);
                    item.SubItems.Add($"{f.FileSizeBytes / 1024.0:0.0} KB");
                    item.SubItems.Add(f.GeneratedAt.ToString("yyyy-MM-dd HH:mm"));

                    // 🔄 MODIFY - Apply type-based color coding BEFORE missing-file check
                    var fileType = f.FileType != RecentFileType.Unknown
                        ? f.FileType
                        : MapExtensionToType(Path.GetExtension(f.FileName), f.FileName);

                    item.ForeColor = GetColorForType(fileType);
                    item.UseItemStyleForSubItems = true;

                    // ✅ Keep missing-file styling as override (Gray + Italic)
                    if (!f.Exists)
                    {
                        item.ForeColor = Color.Gray;
                        item.Font = new Font(lvRecentFiles.Font, FontStyle.Italic);
                    }

                    lvRecentFiles.Items.Add(item);
                }

                if (lblStatus != null)
                {
                    lblStatus.Text = $"{lvRecentFiles.Items.Count} files";
                }
            }
            finally
            {
                lvRecentFiles.EndUpdate();
            }
        }

        /// <summary>
        /// 🔄 MODIFY - Get color for each RecentFileType.
        /// </summary>
        private static Color GetColorForType(RecentFileType type)
        {
            return type switch
            {
                RecentFileType.Plan => Color.Goldenrod,
                RecentFileType.Guide => Color.SteelBlue,
                RecentFileType.GitChanges => Color.OrangeRed,
                RecentFileType.TestResults => Color.MediumSeaGreen,
                RecentFileType.AllSourceMd => Color.MediumPurple,
                RecentFileType.AllSourceDocx => Color.LightSkyBlue,
                RecentFileType.AllSourcePdf => Color.LightCoral,
                RecentFileType.Unknown => Color.Gainsboro,
                _ => Color.Gainsboro
            };
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

        /// <summary>
        /// 🔄 MODIFY - Map file extension or filename pattern to RecentFileType.
        /// </summary>
        private static RecentFileType MapExtensionToType(string? ext, string? fileName = null)
        {
            var e = (ext ?? string.Empty).Trim('.').ToLowerInvariant();

            // ✅ NEW - Check filename patterns first (case-insensitive)
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var fn = fileName.ToUpperInvariant();
                if (fn.Contains("PLAN-") || fn.StartsWith("PLAN") || fn.Contains("PLAN"))
                    return RecentFileType.Plan;
                if (fn.Contains("GUIDE-") || fn.StartsWith("GUIDE"))
                    return RecentFileType.Guide;
            }

            // Fallback to extension-based detection
            return e switch
            {
                "md" or "markdown" when fileName?.ToUpperInvariant().Contains(".GIT.") == true
                    => RecentFileType.GitChanges,
                "md" or "markdown" when fileName?.ToUpperInvariant().Contains("TESTRESULTS") == true
                    => RecentFileType.TestResults,
                "md" or "markdown" => RecentFileType.AllSourceMd,
                "docx" => RecentFileType.AllSourceDocx,
                "pdf" => RecentFileType.AllSourcePdf,
                _ => RecentFileType.Unknown
            };
        }
    }
}
