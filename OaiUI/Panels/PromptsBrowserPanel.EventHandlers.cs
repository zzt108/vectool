#nullable enable
using System;
using System.Linq;
using System.Windows.Forms;
using VecTool.Core.Models;

namespace VecTool.UI.Panels
{
    /// <summary>
    /// PromptsBrowserPanel partial: Event handlers for UI controls.
    /// </summary>
    public sealed partial class PromptsBrowserPanel : UserControl
    {
        // ✅ Event Handlers (Wired by Designer)

        private void txtSearchTextChanged(object? sender, EventArgs e)
        {
            currentSearchQuery = txtSearch.Text?.Trim() ?? string.Empty;
            RefreshPanel();
        }

        private void btnRefreshClick(object? sender, EventArgs e)
        {
            searchEngine?.RebuildIndex();
            RefreshPanel();
        }

        private void cmbFilterTypeSelectedIndexChanged(object? sender, EventArgs e)
        {
            var filter = cmbFilterType.SelectedItem?.ToString() ?? "All";

            if (filter == "All")
            {
                RefreshPanel();
                return;
            }

            // Filter by type prefix (PROMPT*, GUIDE*, SPACE*)
            var typePrefix = filter.Replace("*", "");
            var filtered = currentResults.Where(p => p.Metadata.Type.StartsWith(typePrefix, StringComparison.OrdinalIgnoreCase)).ToList();
            PopulateListView(filtered);
            UpdateStatusLabel();
        }

        private void treeViewHierarchyAfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is List<PromptFile> files)
            {
                PopulateListView(files);
            }
        }

        private void lvResultsItemActivate(object? sender, EventArgs e)
        {
            // Double-click → Edit
            EditSelectedPrompt();
        }

        private void lvResultsMouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var hitTest = lvResults.HitTest(e.Location);
                if (hitTest.Item != null && hitTest.SubItem?.Text == "★")
                {
                    ToggleFavorite();
                }

                return;
            }

            // ✅ NEW: right-click context menu support
            if (e.Button == MouseButtons.Right)
            {
                var hit = lvResults.HitTest(e.Location);
                if (hit.Item != null)
                {
                    hit.Item.Selected = true;
                    lvResults.FocusedItem = hit.Item;
                }

                if (lvResults.SelectedItems.Count > 0)
                {
                    contextMenuResults.Show(lvResults, e.Location);
                }
            }
        }

        private void mnuRenamePromptClick(object? sender, EventArgs e)
        {
            OpenRenamePromptDialog();
        }

        private void btnCopyClick(object? sender, EventArgs e)
        {
            CopySelectedToClipboard();
        }

        private void btnEditClick(object? sender, EventArgs e)
        {
            EditSelectedPrompt();
        }

        private void btnNewClick(object? sender, EventArgs e)
        {
            CreateNewVersion();
        }

        private void btnGitClick(object? sender, EventArgs e)
        {
            OpenInGit();
        }
    }
}
