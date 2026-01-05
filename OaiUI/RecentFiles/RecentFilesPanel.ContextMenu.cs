// ✅ FULL FILE VERSION
// Path: OaiUI/RecentFiles/RecentFilesPanel.ContextMenu.cs
#nullable enable

using System.Diagnostics;
using VecTool.RecentFiles;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// RecentFilesPanel partial: Context menu initialization and handlers.
    /// </summary>
    public sealed partial class RecentFilesPanel : UserControl
    {
        // Context menu controls
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

            // Attach to ListView explicitly
            if (lvRecentFiles is not null)
            {
                lvRecentFiles.ContextMenuStrip = contextMenu;
            }
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
        /// Remove the selected file from the recent files list.
        /// </summary>
        private void OnRemoveFromList(object? sender, EventArgs e)
        {
            var file = GetSelectedFile();
            if (file is null || string.IsNullOrWhiteSpace(file.FilePath))
                return;

            try
            {
                if (recentFilesManager is not null)
                {
                    recentFilesManager.RemoveFile(file.FilePath);
                    RefreshList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to remove file from list:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}