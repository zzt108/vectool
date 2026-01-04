#nullable enable
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
                        : f.FileType.MapExtensionToType(Path.GetExtension(f.FileName), f.FileName).ToString();

                    item.SubItems.Add(typeDisplay);
                    item.SubItems.Add($"{f.FileSizeBytes / 1024.0:0.0} KB");
                    item.SubItems.Add(f.GeneratedAt.ToString("yyyy-MM-dd HH:mm"));

                    // 🔄 MODIFY - Apply type-based color coding BEFORE missing-file check
                    var fileType = f.FileType != RecentFileType.Unknown
                        ? f.FileType
                        : f.FileType.MapExtensionToType(Path.GetExtension(f.FileName), f.FileName);

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
                RecentFileType.Git_Md => Color.OrangeRed,
                RecentFileType.TestResults_Md => Color.MediumSeaGreen,
                RecentFileType.Codebase_Md => Color.MediumPurple,
                RecentFileType.Codebase_Docx => Color.LightSkyBlue,
                RecentFileType.Codebase_Pdf => Color.LightCoral,
                RecentFileType.Repomix_Xml => Color.DeepSkyBlue, 
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
    }
}
