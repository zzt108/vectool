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

            // ✅ NEW: First-run font from App.config if UiState has no font
            if (!state.RecentFilesFontSize.HasValue)
            {
                var fontSizeStr = System.Configuration.ConfigurationManager.AppSettings["recentFilesFontSize"];
                if (double.TryParse(fontSizeStr, out var points) && points > 6.0 && points <= 48.0)
                {
                    lvRecentFiles.Font = new System.Drawing.Font(lvRecentFiles.Font.FontFamily, (float)points, lvRecentFiles.Font.Style);
                    state.RecentFilesFontSize = points;
                    UiStateConfig.Save(state, uiStateDirectory);
                }
            }
            else
            {
                // Existing behavior: apply UiState font size
                lvRecentFiles.Font = new System.Drawing.Font(
                    lvRecentFiles.Font.FontFamily,
                    (float)state.RecentFilesFontSize.Value,
                    lvRecentFiles.Font.Style);
            }


            // ✅ NEW: Apply row height scale (UiState or default)
            var rowScale = state.RecentFilesRowHeightScale ?? DefaultRowHeightScale;
            if (rowScale <= 0) rowScale = DefaultRowHeightScale;

            // ListView row height is max(Font.Height, SmallImageList.ImageSize.Height)
            var baseHeight = lvRecentFiles.Font.Height;
            var targetHeight = Math.Max(baseHeight + 2, (int)Math.Round(baseHeight * rowScale));
            if (lvRecentFiles.SmallImageList == null ||
                lvRecentFiles.SmallImageList.ImageSize.Height != targetHeight)
            {
                var imgList = new ImageList
                {
                    ColorDepth = ColorDepth.Depth8Bit,
                    ImageSize = new System.Drawing.Size(1, targetHeight),
                    TransparentColor = System.Drawing.Color.Transparent
                };
                lvRecentFiles.SmallImageList = imgList;
            }

            // Track what we actually applied so SaveLayout can persist it
            appliedRowHeightScale = rowScale;
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
            current.RecentFilesRowHeightScale = appliedRowHeightScale;
            current.RecentFilesFontSize = lvRecentFiles.Font.Size;

            UiStateConfig.Save(current, uiStateDirectory);
        }

    }
}
