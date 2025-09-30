using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DocXHandler.RecentFiles;
using NLogS = NLogShared;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// VS Code-styled panel for displaying and managing recent files.
    /// Supports drag-and-drop for file operations and context menu for file management.
    /// </summary>
    public partial class RecentFilesPanel : UserControl
    {
        private readonly NLogShared.CtxLogger log = new();
        private readonly IRecentFilesManager? recentFilesManager;
        private List<RecentFileInfo> currentFiles = new();
        private ContextMenuStrip contextMenu = null!;

        public RecentFilesPanel(IRecentFilesManager? manager = null)
        {
            recentFilesManager = manager;
            InitializeComponent();
            SetupListView();
            ApplyVSCodeTheme();
            SetupDragDrop();
            SetupContextMenu();
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
                MessageBox.Show($"Error refreshing list: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        /// <summary>
        /// Setup drag-and-drop event handlers.
        /// </summary>
        private void SetupDragDrop()
        {
            lvRecentFiles.ItemDrag += LvRecentFilesItemDrag;
            lvRecentFiles.MouseDown += LvRecentFilesMouseDown;
        }

        /// <summary>
        /// Setup context menu for file operations.
        /// </summary>
        private void SetupContextMenu()
        {
            contextMenu = new ContextMenuStrip();

            // Open file (NO shortcut key - handled via KeyDown)
            var openItem = new ToolStripMenuItem("Open (Enter)", null, OpenFile_Click);
            contextMenu.Items.Add(openItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Show in Explorer
            var explorerItem = new ToolStripMenuItem("Show in File Explorer", null, ShowInExplorer_Click);
            explorerItem.ShortcutKeys = Keys.Control | Keys.E;
            contextMenu.Items.Add(explorerItem);

            // Copy path
            var copyPathItem = new ToolStripMenuItem("Copy Path", null, CopyPath_Click);
            copyPathItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.C;
            contextMenu.Items.Add(copyPathItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Delete file (NO shortcut key - handled via KeyDown)
            var deleteItem = new ToolStripMenuItem("Delete (Del)", null, DeleteFile_Click);
            deleteItem.ForeColor = Color.Red;
            contextMenu.Items.Add(deleteItem);

            // Assign to ListView
            lvRecentFiles.ContextMenuStrip = contextMenu;

            // Handle opening event to enable/disable items
            contextMenu.Opening += ContextMenu_Opening;

            // Handle keyboard shortcuts for Enter and Delete
            lvRecentFiles.KeyDown += LvRecentFiles_KeyDown;
        }

        /// <summary>
        /// Handle Enter and Delete key presses on ListView.
        /// </summary>
        private void LvRecentFiles_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    OpenFile_Click(this, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Keys.Delete:
                    DeleteFile_Click(this, EventArgs.Empty);
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Enable/disable context menu items based on selection and file existence.
        /// </summary>
        private void ContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            var selectedFiles = GetSelectedRecentFiles();
            bool hasSelection = selectedFiles.Count > 0;
            bool allExist = selectedFiles.All(f => f.Exists);

            // Enable/disable items based on selection and file existence
            foreach (ToolStripMenuItem item in contextMenu.Items.OfType<ToolStripMenuItem>())
            {
                if (item.Text.StartsWith("Open") || item.Text.StartsWith("Show in File Explorer"))
                {
                    item.Enabled = hasSelection && allExist;
                }
                else if (item.Text.StartsWith("Copy Path"))
                {
                    item.Enabled = hasSelection;
                }
                else if (item.Text.StartsWith("Delete"))
                {
                    item.Enabled = hasSelection && allExist;
                }
            }
        }

        /// <summary>
        /// Open selected files with default application.
        /// </summary>
        private void OpenFile_Click(object? sender, EventArgs e)
        {
            var selectedFiles = GetSelectedRecentFiles();
            foreach (var file in selectedFiles.Where(f => f.Exists))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = file.FilePath,
                        UseShellExecute = true
                    });
                    log.Info($"Opened file: {file.FilePath}");
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Failed to open file: {file.FilePath}");
                    MessageBox.Show($"Could not open file: {file.FileName}\n{ex.Message}",
                        "Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Show selected files in Windows Explorer.
        /// </summary>
        private void ShowInExplorer_Click(object? sender, EventArgs e)
        {
            var selectedFiles = GetSelectedRecentFiles();
            foreach (var file in selectedFiles.Where(f => f.Exists))
            {
                try
                {
                    Process.Start("explorer.exe", $"/select,\"{file.FilePath}\"");
                    log.Info($"Showed in explorer: {file.FilePath}");
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Failed to show in explorer: {file.FilePath}");
                    MessageBox.Show($"Could not show file in Explorer: {file.FileName}\n{ex.Message}",
                        "Explorer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Copy selected file paths to clipboard.
        /// </summary>
        private void CopyPath_Click(object? sender, EventArgs e)
        {
            var selectedFiles = GetSelectedRecentFiles();
            if (selectedFiles.Count == 0) return;

            try
            {
                string paths = selectedFiles.Count == 1
                    ? selectedFiles[0].FilePath
                    : string.Join(Environment.NewLine, selectedFiles.Select(f => f.FilePath));

                Clipboard.SetText(paths);
                log.Info($"Copied {selectedFiles.Count} file paths to clipboard");

                // Show brief confirmation in status
                lblStatus.Text = $"Copied {selectedFiles.Count} path(s) to clipboard";
                var timer = new System.Windows.Forms.Timer { Interval = 2000 };
                timer.Tick += (s, args) =>
                {
                    lblStatus.Text = $"{currentFiles.Count} files";
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to copy paths to clipboard");
                MessageBox.Show($"Could not copy paths to clipboard\n{ex.Message}",
                    "Clipboard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Delete selected files with confirmation.
        /// </summary>
        private void DeleteFile_Click(object? sender, EventArgs e)
        {
            var selectedFiles = GetSelectedRecentFiles().Where(f => f.Exists).ToList();
            if (selectedFiles.Count == 0) return;

            string message = selectedFiles.Count == 1
                ? $"Are you sure you want to delete:\n{selectedFiles[0].FileName}?"
                : $"Are you sure you want to delete {selectedFiles.Count} files?";

            var result = MessageBox.Show(message, "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            int deletedCount = 0;
            var errors = new List<string>();

            foreach (var file in selectedFiles)
            {
                try
                {
                    File.Delete(file.FilePath);
                    deletedCount++;
                    log.Info($"Deleted file: {file.FilePath}");
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Failed to delete file: {file.FilePath}");
                    errors.Add($"{file.FileName}: {ex.Message}");
                }
            }

            // Show results
            if (errors.Any())
            {
                string errorMsg = $"Deleted {deletedCount} files, but {errors.Count} failed:\n\n" +
                                 string.Join("\n", errors.Take(5));
                if (errors.Count > 5) errorMsg += $"\n... and {errors.Count - 5} more";

                MessageBox.Show(errorMsg, "Delete Results", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Refresh the list
            RefreshList();
        }

        /// <summary>
        /// Handle mouse down to prepare for drag operation.
        /// </summary>
        private void LvRecentFilesMouseDown(object? sender, MouseEventArgs e)
        {
            // Only handle left button for drag
            if (e.Button != MouseButtons.Left) return;

            // Check if we're clicking on an actual item
            var hitTest = lvRecentFiles.HitTest(e.Location);
            if (hitTest.Item == null) return;

            // If clicked item is not selected, select only that item
            if (!hitTest.Item.Selected)
            {
                lvRecentFiles.SelectedItems.Clear();
                hitTest.Item.Selected = true;
            }
        }

        /// <summary>
        /// Handle drag operation initiation.
        /// </summary>
        private void LvRecentFilesItemDrag(object? sender, ItemDragEventArgs e)
        {
            try
            {
                // Get all selected files
                var selectedFiles = GetSelectedRecentFiles();
                if (selectedFiles.Count == 0)
                {
                    log.Warn("No files selected for drag operation.");
                    return;
                }

                // Validate that all files exist
                var missingFiles = selectedFiles.Where(f => !f.Exists).ToList();
                if (missingFiles.Any())
                {
                    log.Warn($"Cannot drag missing files: {string.Join(", ", missingFiles.Select(f => f.FileName))}");
                    MessageBox.Show(
                        $"Cannot drag missing files:\n{string.Join(", ", missingFiles.Select(f => f.FileName))}\n\nFiles no longer exist.",
                        "Missing Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create file path array for drag operation
                string[] filePaths = selectedFiles.Select(f => f.FilePath).ToArray();

                // Create DataObject with FileDrop format
                var dataObject = new DataObject(DataFormats.FileDrop, filePaths);

                // Initiate drag-and-drop operation
                DoDragDrop(dataObject, DragDropEffects.Copy | DragDropEffects.Move);

                log.Info($"Drag operation initiated for {filePaths.Length} files.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to initiate drag operation.");
                MessageBox.Show($"Error during drag operation: {ex.Message}",
                    "Drag Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Get all currently selected RecentFileInfo objects.
        /// </summary>
        private List<RecentFileInfo> GetSelectedRecentFiles()
        {
            var selected = new List<RecentFileInfo>();
            foreach (ListViewItem item in lvRecentFiles.SelectedItems)
            {
                if (item.Tag is RecentFileInfo fileInfo)
                {
                    selected.Add(fileInfo);
                }
            }
            return selected;
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

                // Store reference for drag operation and context menu
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
                ? $"{totalCount} files"
                : $"{visibleCount} of {totalCount} files";
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
    }
}
