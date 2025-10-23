// Path: OaiUI/RecentFiles/RecentFilesPanel.ContextMenu.cs

#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VecTool.RecentFiles;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// RecentFilesPanel partial: Right-click context menu operations.
    /// </summary>
    public sealed partial class RecentFilesPanel : UserControl
    {
        // Context menu components (instantiated in WireRuntime)
        private ContextMenuStrip? contextMenu;
        private ToolStripMenuItem? menuOpenFile;
        private ToolStripMenuItem? menuShowInExplorer;
        private ToolStripMenuItem? menuCopyPath;
        private ToolStripSeparator? menuSeparator;
        private ToolStripMenuItem? menuRemove;

        /// <summary>
        /// Initialize and wire the context menu to the ListView.
        /// </summary>
        private void InitializeContextMenu()
        {
            if (lvRecentFiles is null) return;

            // Create context menu
            contextMenu = new ContextMenuStrip();
            menuOpenFile = new ToolStripMenuItem("Open file in default app", null, OnOpenFile);
            menuShowInExplorer = new ToolStripMenuItem("Show in Explorer", null, OnShowInExplorer);
            menuCopyPath = new ToolStripMenuItem("Copy path to clipboard", null, OnCopyPath);
            menuSeparator = new ToolStripSeparator();
            menuRemove = new ToolStripMenuItem("Remove from recent list", null, OnRemoveFromList);

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                menuOpenFile,
                menuShowInExplorer,
                menuCopyPath,
                menuSeparator,
                menuRemove
            });

            // Dynamically enable/disable menu items based on selection
            contextMenu.Opening += OnContextMenuOpening;

            // Attach to ListView
            lvRecentFiles.ContextMenuStrip = contextMenu;
        }

        /// <summary>
        /// Dynamically enable/disable menu items based on current selection and file existence.
        /// </summary>
        private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var selectedFile = GetSelectedFile();
            var hasSelection = selectedFile is not null;
            var fileExists = hasSelection && File.Exists(selectedFile!.FilePath);

            if (menuOpenFile is not null)
                menuOpenFile.Enabled = fileExists;

            if (menuShowInExplorer is not null)
                menuShowInExplorer.Enabled = hasSelection && !string.IsNullOrWhiteSpace(selectedFile?.FilePath);

            if (menuCopyPath is not null)
                menuCopyPath.Enabled = hasSelection;

            if (menuRemove is not null)
                menuRemove.Enabled = hasSelection;
        }

        /// <summary>
        /// Get the selected RecentFileInfo from the ListView, or null if none/invalid.
        /// </summary>
        private RecentFileInfo? GetSelectedFile()
        {
            if (lvRecentFiles is null || lvRecentFiles.SelectedItems.Count == 0)
                return null;

            return lvRecentFiles.SelectedItems[0].Tag as RecentFileInfo;
        }

        /// <summary>
        /// Open the selected file in its default application.
        /// </summary>
        private void OnOpenFile(object? sender, EventArgs e)
        {
            var file = GetSelectedFile();
            if (file is null || string.IsNullOrWhiteSpace(file.FilePath))
                return;

            if (!File.Exists(file.FilePath))
            {
                MessageBox.Show(
                    $"File not found:\n{file.FilePath}",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = file.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open file:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Open Windows Explorer and select the file.
        /// </summary>
        private void OnShowInExplorer(object? sender, EventArgs e)
        {
            var file = GetSelectedFile();
            if (file is null || string.IsNullOrWhiteSpace(file.FilePath))
                return;

            try
            {
                var directory = Path.GetDirectoryName(file.FilePath);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    // Use /select to highlight the file in Explorer
                    Process.Start("explorer.exe", $"/select,\"{file.FilePath}\"");
                }
                else
                {
                    MessageBox.Show(
                        $"Directory not found:\n{directory}",
                        "Directory Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to show in Explorer:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Copy the selected file path to the clipboard.
        /// </summary>
        private void OnCopyPath(object? sender, EventArgs e)
        {
            var file = GetSelectedFile();
            if (file is null || string.IsNullOrWhiteSpace(file.FilePath))
                return;

            try
            {
                Clipboard.SetText(file.FilePath);

                // Optional: Brief visual feedback
                if (lblStatus is not null)
                {
                    var originalText = lblStatus.Text;
                    lblStatus.Text = "✓ Path copied to clipboard";

                    // Reset after 2 seconds
                    var resetTimer = new System.Windows.Forms.Timer { Interval = 2000 };
                    resetTimer.Tick += (s, args) =>
                    {
                        if (lblStatus is not null)
                            lblStatus.Text = originalText;
                        resetTimer.Stop();
                        resetTimer.Dispose();
                    };
                    resetTimer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to copy path:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Remove the selected file from the recent list.
        /// </summary>
        private void OnRemoveFromList(object? sender, EventArgs e)
        {
            var file = GetSelectedFile();
            if (file is null || string.IsNullOrWhiteSpace(file.FilePath))
                return;

            var result = MessageBox.Show(
                $"Remove from recent files list?\n\n{Path.GetFileName(file.FilePath)}",
                "Confirm Remove",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                // Remove from manager and refresh
                recentFilesManager?.RemoveFile(file.FilePath);
                recentFilesManager?.Save();
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to remove file:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Cleanup context menu resources on disposal.
        /// </summary>
        private void DisposeContextMenu()
        {
            if (contextMenu is not null)
            {
                contextMenu.Opening -= OnContextMenuOpening;
                contextMenu.Dispose();
                contextMenu = null;
            }

            menuOpenFile = null;
            menuShowInExplorer = null;
            menuCopyPath = null;
            menuSeparator = null;
            menuRemove = null;
        }
    }
}
