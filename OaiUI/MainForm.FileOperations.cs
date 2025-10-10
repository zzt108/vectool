// ✅ FULL FILE VERSION
// File: OaiUI/MainForm.FolderSelection.cs

using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Vectool.UI
{
    public partial class MainForm : Form
    {
        // NOTE:
        // - This partial relies on fields declared in MainForm.Fields.cs:
        //   selectedFolders (List<string>), userInterface (dynamic)

        // Opens a folder picker and adds the chosen folder to the selection
        private void selectFolderToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            try { userInterface?.ShowStatus("Selecting folders..."); } catch { /* ignore */ }

            using var dlg = new FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                Description = "Select a folder to add"
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var folder = dlg.SelectedPath;

                if (string.IsNullOrWhiteSpace(folder))
                {
                    try { userInterface?.ShowMessage("No folder selected.", "Info"); } catch { /* ignore */ }
                    try { userInterface?.ShowStatus("Idle"); } catch { /* ignore */ }
                    return;
                }

                if (!Directory.Exists(folder))
                {
                    try { userInterface?.ShowMessage("Selected folder does not exist.", "Info"); } catch { /* ignore */ }
                    try { userInterface?.ShowStatus("Idle"); } catch { /* ignore */ }
                    return;
                }

                var exists = selectedFolders.Any(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
                if (exists)
                {
                    try { userInterface?.ShowMessage("Folder already selected.", "Info"); } catch { /* ignore */ }
                }
                else
                {
                    selectedFolders.Add(folder);
                    try { userInterface?.ShowMessage($"Added folder:\n{folder}", "Success"); } catch { /* ignore */ }
                }
            }

            try { userInterface?.ShowStatus("Idle"); } catch { /* ignore */ }
        }

        // Clears all selected folders
        private void clearSelectedFoldersToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                try { userInterface?.ShowMessage("No folders to clear.", "Info"); } catch { /* ignore */ }
                return;
            }

            selectedFolders.Clear();
            try { userInterface?.ShowMessage("Cleared all selected folders.", "Success"); } catch { /* ignore */ }
            try { userInterface?.ShowStatus("Idle"); } catch { /* ignore */ }
        }

        // Re-adds a folder via a quick input (optional helper if bound from UI)
        private void addFolderPathManuallyToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // If userInterface supports a prompt dialog, use it; otherwise skip silently
            try
            {
                var folder = userInterface?.PromptText("Add folder by path", "Enter an absolute folder path:");
                if (string.IsNullOrWhiteSpace(folder))
                    return;

                if (!Directory.Exists(folder))
                {
                    userInterface?.ShowMessage("Folder does not exist.", "Info");
                    return;
                }

                var exists = selectedFolders.Any(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
                if (exists)
                {
                    userInterface?.ShowMessage("Folder already selected.", "Info");
                    return;
                }

                selectedFolders.Add(folder);
                userInterface?.ShowMessage($"Added folder:\n{folder}", "Success");
            }
            catch
            {
                // Swallow if userInterface has no PromptText; this is an optional UX handler
            }
        }
    }
}
