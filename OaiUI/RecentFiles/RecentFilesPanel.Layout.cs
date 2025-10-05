// ✅ FULL FILE VERSION
// Path: OaiUI/RecentFiles/RecentFilesPanel.Layout.cs

#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VecTool.Configuration;

namespace oaiUI.RecentFiles
{
    // Layout, theme, and UI-state persistence partial
    public partial class RecentFilesPanel : UserControl
    {
        private void SetupListView()
        {
            if (lvRecentFiles is null) return;

            lvRecentFiles.View = View.Details;
            lvRecentFiles.FullRowSelect = true;
            lvRecentFiles.GridLines = true;
            lvRecentFiles.MultiSelect = true;

            if (lvRecentFiles.Columns.Count == 0)
            {
                lvRecentFiles.Columns.Add(new ColumnHeader { Text = "File", Width = 400, Name = "colFile" });
                lvRecentFiles.Columns.Add(new ColumnHeader { Text = "Type", Width = 120, Name = "colType", TextAlign = HorizontalAlignment.Left });
                lvRecentFiles.Columns.Add(new ColumnHeader { Text = "Size", Width = 120, Name = "colSize", TextAlign = HorizontalAlignment.Right });
                lvRecentFiles.Columns.Add(new ColumnHeader { Text = "Generated", Width = 220, Name = "colGenerated", TextAlign = HorizontalAlignment.Left });
            }
        }

        private void ApplyRowHeightScale()
        {
            if (lvRecentFiles is null) return;

            var state = UiStateConfig.Load(uiStateDirectory);
            var scale = state.RecentFilesRowHeightScale ?? DefaultRowHeightScale;

            // Compute a sensible row height based on font and desired scale
            var baseHeight = lvRecentFiles.Font.Height + 6;
            var desired = (int)Math.Ceiling(baseHeight * scale);

            rowHeightImageList?.Dispose();
            rowHeightImageList = new ImageList { ImageSize = new Size(1, Math.Max(desired, 18)) };
            lvRecentFiles.SmallImageList = rowHeightImageList;
        }

        private void ApplyThemeDark()
        {
            // Dark-ish theme; safe if overridden elsewhere
            var panelBack = Color.FromArgb(32, 32, 32);
            var panelFore = Color.Gainsboro;

            BackColor = panelBack;
            ForeColor = panelFore;

            if (tableLayoutPanel != null)
            {
                tableLayoutPanel.BackColor = panelBack;
                tableLayoutPanel.ForeColor = panelFore;
            }

            if (lblFilter != null)
            {
                lblFilter.BackColor = panelBack;
                lblFilter.ForeColor = panelFore;
            }

            if (lblStatus != null)
            {
                lblStatus.BackColor = panelBack;
                lblStatus.ForeColor = Color.Silver;
            }

            if (txtFilter != null)
            {
                txtFilter.BackColor = Color.FromArgb(45, 45, 45);
                txtFilter.ForeColor = panelFore;
                txtFilter.BorderStyle = BorderStyle.FixedSingle;
            }

            if (btnRefresh != null)
            {
                btnRefresh.FlatStyle = FlatStyle.System;
                btnRefresh.BackColor = panelBack;
                btnRefresh.ForeColor = panelFore;
            }

            if (lvRecentFiles != null)
            {
                lvRecentFiles.BackColor = Color.FromArgb(24, 24, 24);
                lvRecentFiles.ForeColor = Color.Gainsboro;
                lvRecentFiles.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        // --------------- Layout persistence ---------------

        private void OnColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
        {
            // Debounce to avoid thrashing disk while dragging splitter
            saveDebounceTimer.Stop();
            saveDebounceTimer.Start();
        }

        private void LoadLayout()
        {
            if (lvRecentFiles is null) return;

            var state = UiStateConfig.Load(uiStateDirectory);
            var widths = state.RecentFilesColumnWidths;

            if (widths is null || widths.Count == 0)
            {
                // First run: leave designer-defined widths
                return;
            }

            foreach (ColumnHeader col in lvRecentFiles.Columns)
            {
                if (widths.TryGetValue(col.Text, out var w) && w > 0)
                    col.Width = w;
            }
        }

        private void SaveLayout()
        {
            if (lvRecentFiles is null) return;

            var current = UiStateConfig.Load(uiStateDirectory);
            var map = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (ColumnHeader col in lvRecentFiles.Columns)
            {
                map[col.Text] = col.Width;
            }

            current.RecentFilesColumnWidths = map;
            current.RecentFilesRowHeightScale = DefaultRowHeightScale;

            UiStateConfig.Save(current, uiStateDirectory);
        }
    }
}
