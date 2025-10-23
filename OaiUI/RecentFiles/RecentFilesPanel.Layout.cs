#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using VecTool.Configuration;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// RecentFilesPanel partial: Layout persistence (column widths, row height).
    /// </summary>
    public sealed partial class RecentFilesPanel : UserControl
    {
        // ==================== Layout Persistence ====================

        private void OnColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
        {
            // Debounce: restart timer on each change
            saveDebounceTimer.Stop();
            saveDebounceTimer.Start();
        }

        /// <summary>
        /// Load layout settings (column widths) from disk.
        /// </summary>
        private void LoadLayout()
        {
            if (lvRecentFiles is null) return;

            var state = UiStateConfig.Load(uiStateDirectory);
            var widths = state.RecentFilesColumnWidths;

            if (widths is null || widths.Count == 0)
            {
                // First run - leave designer-defined widths
                return;
            }

            foreach (ColumnHeader col in lvRecentFiles.Columns)
            {
                if (widths.TryGetValue(col.Text, out var w) && w > 0)
                    col.Width = w;
            }
        }

        /// <summary>
        /// Save layout settings (column widths) to disk.
        /// </summary>
        private void SaveLayout()
        {
            if (lvRecentFiles is null) return;

            var current = UiStateConfig.Load(uiStateDirectory);
            var map = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (ColumnHeader col in lvRecentFiles.Columns)
                map[col.Text] = col.Width;

            current.RecentFilesColumnWidths = map;
            current.RecentFilesRowHeightScale = DefaultRowHeightScale;

            UiStateConfig.Save(current, uiStateDirectory);
        }
    }
}
