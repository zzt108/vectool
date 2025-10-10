// ✅ FULL FILE VERSION
// Path: src/VecTool.UI/OaiUI/MainForm.FolderSelection.cs

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VecTool.Handlers;

namespace Vectool.UI
{
    public partial class MainForm : Form
    {
        // Handles selecting a folder to include in the current session's selected folder list.
        // Behavior preserved: adds unique existing folders, updates the list UI, and persists current vector store changes.
        private void btnSelectFoldersClick(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select a folder to add",
                ShowNewFolderButton = false
            };

            var result = dialog.ShowDialog(this);
            if (result != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                return;
            }

            var selectedPath = dialog.SelectedPath.Trim();

            if (!Directory.Exists(selectedPath))
            {
                userInterface.ShowMessage($"Folder does not exist: {selectedPath}", "Folder Not Found", MessageType.Warning);
                return;
            }

            // Avoid duplicates (case-insensitive)
            var alreadyPresent = selectedFolders.Any(f => string.Equals(f, selectedPath, StringComparison.OrdinalIgnoreCase));
            if (alreadyPresent)
            {
                userInterface.ShowMessage("This folder is already in the list.", "Duplicate Folder", MessageType.Information);
                return;
            }

            selectedFolders.Add(selectedPath);
            listBoxSelectedFolders.Items.Add(selectedPath);

            // Persist changes for the currently selected vector store
            SaveChangesToCurrentVectorStore();
        }

        // Handles the top-level Exit menu item and closes the main window.
        private void exitToolStripMenuItemClick(object? sender, EventArgs e)
        {
            Close();
        }
    }
}
