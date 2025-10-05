// Path: OaiUI/RecentFiles/RecentFilesPanel.DataBinding.cs

#nullable enable

using NLogShared;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VecTool.RecentFiles;

namespace oaiUI.RecentFiles
{
    // Data-binding, filtering, and load wiring partial
    public partial class RecentFilesPanel : UserControl
    {
        CtxLogger _log = new ();

        private void RecentFilesPanelLoad(object? sender, EventArgs e)
        {
            try
            {
                // Wire UI events
                if (lvRecentFiles != null)
                {
                    lvRecentFiles.ColumnWidthChanged -= OnColumnWidthChanged;
                    lvRecentFiles.ColumnWidthChanged += OnColumnWidthChanged;
                }

                if (txtFilter != null)
                {
                    txtFilter.TextChanged -= txtFilterTextChanged;
                    txtFilter.TextChanged += txtFilterTextChanged;
                }

                if (btnRefresh != null)
                {
                    btnRefresh.Click -= btnRefreshClick;
                    btnRefresh.Click += btnRefreshClick;
                }

                // Layout and theme are safe to apply on Load
                LoadLayout();
                ApplyRowHeightScale();

                // Initial data load
                RefreshList();
                using var _ = _log.Ctx.Set()
                    .Add("component", "RecentFilesPanel")
                    .Add("area", "DataBinding");
                _log.Info("Load completed, events wired and initial refresh executed.");
            }
            catch (Exception ex)
            {
                using var _ = _log.Ctx.Set().Add("component", "RecentFilesPanel")
                          .Add("area", "DataBinding");
                          _log.Error(ex,"Error during panel load.");
            }
        }

        public void RefreshList()
        {
            if (lvRecentFiles is null)
                return;

            try
            {
                var filterText = (txtFilter?.Text ?? string.Empty).Trim();
                var items = recentFilesManager?.GetRecentFiles() ?? Array.Empty<RecentFileInfo>();

                if (!string.IsNullOrWhiteSpace(filterText))
                {
                    items = items
                        .Where(i => i.FileName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();
                }

                lvRecentFiles.BeginUpdate();
                lvRecentFiles.Items.Clear();

                foreach (var info in items)
                {
                    var typeText = info.FileType.ToString();
                    var sizeText = info.FileSizeBytes.ToString();
                    var whenText = info.GeneratedAt.ToString("yyyy-MM-dd HH:mm");

                    var lvi = new ListViewItem(info.FileName)
                    {
                        Tag = info
                    };

                    lvi.SubItems.Add(typeText);
                    lvi.SubItems.Add(sizeText);
                    lvi.SubItems.Add(whenText);

                    // Style missing files differently
                    if (!info.Exists)
                    {
                        lvi.ForeColor = Color.Gray;
                        lvi.Font = new Font(lvi.Font, FontStyle.Italic);
                    }

                    lvRecentFiles.Items.Add(lvi);
                }

                lvRecentFiles.EndUpdate();

                if (lblStatus != null)
                {
                    var total = items.Count();
                    lblStatus.Text = $"{total} file(s)";
                }

                _log.Ctx.Set().Add("component", "RecentFilesPanel")
                          .Add("area", "DataBinding")
                          .Add("filter", filterText)
                          .Add("count", lvRecentFiles.Items.Count);
                          _log.Info("List refreshed.");
            }
            catch (Exception ex)
            {

                _log.Ctx.Set().Add("component", "RecentFilesPanel")
                          .Add("area", "DataBinding");
                          _log.Error(ex,"Error during RefreshList.");
            }
        }

        private void txtFilterTextChanged(object? sender, EventArgs e)
        {
            try
            {
                RefreshList();
            }
            catch (Exception ex)
            {
                _log.Ctx.Set().Add("component", "RecentFilesPanel")
                          .Add("area", "DataBinding");
                          _log.Error(ex, "Error during Filter TextChanged.");
            }
        }

        private void btnRefreshClick(object? sender, EventArgs e)
        {
            try
            {
                RefreshList();
            }
            catch (Exception ex)
            {
                _log.Ctx.Set().Add("component", "RecentFilesPanel")
                          .Add("area", "DataBinding");
                          _log.Error(ex, "Error during Refresh button click.");
            }
        }
    }
}
