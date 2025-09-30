using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DocXHandler.RecentFiles;
using NLogS = NLogShared;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// VS Code-styled panel for displaying and managing recent files.
    /// </summary>
    public partial class RecentFilesPanel : UserControl
    {
        private readonly NLogShared.CtxLogger log = new();
        private readonly IRecentFilesManager? recentFilesManager;
        private List<RecentFileInfo> currentFiles = new();

        // ... existing code (constructor) ...

        public RecentFilesPanel()
        {
            InitializeComponent();
            SetupListView();
            ApplyVSCodeTheme();
        }

        // NEW CODE GOES HERE

        public RecentFilesPanel(IRecentFilesManager? manager) : this()
        {
            recentFilesManager = manager;
        }

        /// <summary>
        /// Refreshes the list from the manager.
        /// </summary>
        public void RefreshList()
        {
            try
            {
                if (recentFilesManager == null)
                {
                    log.Warn("RecentFilesManager is null, cannot refresh.");
                    return;
                }

                currentFiles = recentFilesManager.GetRecentFiles().ToList();
                ApplyFilterAndPopulateListView();
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to refresh recent files list.");
                MessageBox.Show($"Error refreshing list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupListView()
        {
            lvRecentFiles.View = View.Details;
            lvRecentFiles.FullRowSelect = true;
            lvRecentFiles.GridLines = true;
            lvRecentFiles.MultiSelect = true;
            lvRecentFiles.AllowColumnReorder = false;
            lvRecentFiles.HideSelection = false;

            // Define columns
            lvRecentFiles.Columns.Add("Name", 250);
            lvRecentFiles.Columns.Add("Generated", 150);
            lvRecentFiles.Columns.Add("Size", 80);
            lvRecentFiles.Columns.Add("Type", 80);
            lvRecentFiles.Columns.Add("Source Folders", 200);
        }

        private void ApplyVSCodeTheme()
        {
            // VS Code dark theme colors
            Color bgDark = ColorTranslator.FromHtml("#1E1E1E");
            Color fgLight = ColorTranslator.FromHtml("#D4D4D4");
            Color accentBlue = ColorTranslator.FromHtml("#007ACC");

            this.BackColor = bgDark;
            this.ForeColor = fgLight;

            lvRecentFiles.BackColor = ColorTranslator.FromHtml("#252526");
            lvRecentFiles.ForeColor = fgLight;

            txtFilter.BackColor = ColorTranslator.FromHtml("#3C3C3C");
            txtFilter.ForeColor = fgLight;
            txtFilter.BorderStyle = BorderStyle.FixedSingle;

            btnRefresh.BackColor = accentBlue;
            btnRefresh.ForeColor = Color.White;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;

            lblStatus.BackColor = bgDark;
            lblStatus.ForeColor = ColorTranslator.FromHtml("#858585");
        }

        private void ApplyFilterAndPopulateListView()
        {
            lvRecentFiles.Items.Clear();

            string filterText = txtFilter.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrWhiteSpace(filterText)
                ? currentFiles
                : currentFiles.Where(f =>
                    f.FileName.ToLowerInvariant().Contains(filterText) ||
                    f.FileType.ToString().ToLowerInvariant().Contains(filterText) ||
                    f.SourceFolders.Any(sf => sf.ToLowerInvariant().Contains(filterText))
                ).ToList();

            foreach (var file in filtered)
            {
                var item = new ListViewItem(file.FileName);
                item.SubItems.Add(file.GeneratedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(FormatFileSize(file.FileSizeBytes));
                item.SubItems.Add(file.FileType.ToString());
                item.SubItems.Add(string.Join(", ", file.SourceFolders.Take(2)));
                item.Tag = file;

                // Visual feedback for missing files
                if (!file.Exists)
                {
                    item.ForeColor = Color.Gray;
                    item.Font = new Font(item.Font, FontStyle.Italic);
                }

                lvRecentFiles.Items.Add(item);
            }

            UpdateStatusLabel(filtered.Count, currentFiles.Count);
        }

        private void UpdateStatusLabel(int visibleCount, int totalCount)
        {
            lblStatus.Text = visibleCount == totalCount
                ? $"{totalCount} file(s)"
                : $"{visibleCount} of {totalCount} file(s)";
        }

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        // ... existing code (event handlers) ...

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            ApplyFilterAndPopulateListView();
        }

        private void RecentFilesPanel_Load(object sender, EventArgs e)
        {
            RefreshList();
        }

        // ... existing code ...
    }
}
