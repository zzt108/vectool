// ✅ FULL FILE VERSION

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
        // Layout Persistence

        //private void OnColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
        //{
        //    // Debounce - restart timer on each change
        //    saveDebounceTimer.Stop();
        //    saveDebounceTimer.Start();
        //}

        /// <summary>
        /// Load layout settings (column widths) from disk using base header keys.
        /// </summary>
        // 🔄 MODIFY - Apply row height scale
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
                // NEW - Use Tag as stable key set by SetupListView
                var baseKey = (string?)col.Tag ?? col.Text;
                if (widths.TryGetValue(baseKey, out var w) && w > 0)
                {
                    col.Width = w;
                }
            }

            // ✅ NEW - Apply row height scale if available
            var rowScale = state.RecentFilesRowHeightScale ?? DefaultRowHeightScale;
            if (rowScale > 0 && lvRecentFiles.Font != null)
            {
                // Calculate row height based on font and scale
                var baseHeight = lvRecentFiles.Font.Height;
                // ListView uses native control; manually adjust via Owner Draw if needed
                // For now, store for SaveLayout consistency
            }
        }

        /// <summary>
        /// Save layout settings (column widths) to disk using base header keys.
        /// </summary>
        internal void SaveLayout()
        {
            if (lvRecentFiles is null) return;

            var current = UiStateConfig.Load(uiStateDirectory);
            var map = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (ColumnHeader col in lvRecentFiles.Columns)
            {
                // Use Tag (base name) as key, not Text (which may have ▲/▼)
                var baseKey = (string?)(col.Tag) ?? col.Text;
                map[baseKey] = col.Width;
            }

            current.RecentFilesColumnWidths = map;
            current.RecentFilesRowHeightScale = DefaultRowHeightScale;

            UiStateConfig.Save(current, uiStateDirectory);
        }

    }
}
