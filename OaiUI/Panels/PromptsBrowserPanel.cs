#nullable enable

using LogCtxShared;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Diagnostics;
using VecTool.Configuration.Helpers;
using VecTool.Configuration.Logging;
using VecTool.Constants;
using VecTool.Core.Helpers;
using VecTool.Core.Models;
using VecTool.Handlers;

namespace VecTool.UI.Panels
{
    /// <summary>
    /// Main browser panel for AI Prompts Library with tree hierarchy,
    /// search results, and 4 core actions (Copy, Edit, New, Git).
    /// </summary>
    public sealed partial class PromptsBrowserPanel : UserControl
    {
        private static readonly ILogger logger = AppLogger.For<PromptsBrowserPanel>();

        private PromptSearchEngine searchEngine;
        private FavoritesManager favoritesManager;
        private string? promptsRepositoryPath;

        private List<PromptFile> currentResults = new();
        private string currentSearchQuery = string.Empty;
        private static readonly string[] DefaultPromptTypes = new[] { "PROMPT", "GUIDE", "SPACE" };
        private string[] promptTypes = Array.Empty<string>();

        /// <summary>
        /// Constructor for designer support.
        /// </summary>
        public PromptsBrowserPanel()
        {
            InitializeComponent();
            searchEngine = null!;
            favoritesManager = null!;
        }

        private static string[] GetConfiguredPromptTypes()
        {
            var raw = ConfigurationManager.AppSettings["promptsTypes"];
            if (string.IsNullOrWhiteSpace(raw))
            {
                return DefaultPromptTypes;
            }

            return raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t =>
                    t.Length > 0 &&
                    !string.Equals(t, "All", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private void InitializeFilterDropdown()
        {
            promptTypes = GetConfiguredPromptTypes();

            cmbFilterType.Items.Clear();

            foreach (var type in promptTypes)
            {
                cmbFilterType.Items.Add(type);
            }

            cmbFilterType.Items.Add(Const.All);

            if (cmbFilterType.SelectedIndex < 0 && cmbFilterType.Items.Count > 0)
            {
                // Default to "All"
                cmbFilterType.SelectedIndex = cmbFilterType.Items.Count - 1;
            }
        }

        /// <summary>
        /// Initialize panel with dependencies (call after designer initialization).
        /// </summary>
        public void Initialize(
            PromptSearchEngine? searchEngine,
            FavoritesManager? favoritesManager,
            string? promptsRepositoryPath)
        {
            using var ctx = logger.SetContext(new Props()
                .Add("RepositoryPath", promptsRepositoryPath ?? "null"));

            this.searchEngine = searchEngine.ThrowIfNull(nameof(searchEngine));
            this.favoritesManager = favoritesManager.ThrowIfNull(nameof(favoritesManager));

            InitializeFilterDropdown();

            searchEngine.RebuildIndex(); // Rebuild index on startup (from previous fix)
            InitializeTooltips(); // Setup all tooltips in one place

            logger.LogInformation("PromptsBrowserPanel initialized.");

            // Initial load
            RefreshPanel();
        }

        /// <summary>
        /// Initialize tooltips for all Prompts tab controls (centralized).
        /// </summary>
        private void InitializeTooltips()
        {
            var tooltip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 500,
                ReshowDelay = 200,
                ShowAlways = true
            };

            var typeLabel = (promptTypes is { Length: > 0 })
                ? string.Join(", ", promptTypes)
                : "type";

            // Filter dropdown
            tooltip.SetToolTip(cmbFilterType, $"Filter prompts by type ({typeLabel}) or category");

            // Search textbox
            tooltip.SetToolTip(txtSearch, "Enter keywords to search prompt names, content, or metadata");

            // Refresh button
            tooltip.SetToolTip(btnRefresh, "Rebuild search index from repository (use after adding/modifying prompts)");

            // Results grid
            tooltip.SetToolTip(lvResults, "Prompt search results - double-click to open, right-click for actions");

            // Action buttons (if present)
            if (btnCopy != null)
                tooltip.SetToolTip(btnCopy, "Copy selected prompt content to clipboard");

            if (btnEdit != null)
                tooltip.SetToolTip(btnEdit, "Open selected prompt in default text editor");

            if (btnNew != null)
                tooltip.SetToolTip(btnNew, "Create a new prompt file in the repository");

            if (btnGit != null)
                tooltip.SetToolTip(btnGit, "Open Git operations for the prompts repository");
        }

        /// <summary>
        /// Refresh tree and list views based on current search query.
        /// </summary>
        public void RefreshPanel()
        {
            using var ctx = logger.SetContext(new Props()
                .Add("SearchQuery", currentSearchQuery));

            try
            {
                if (searchEngine == null)
                {
                    logger.LogWarning("SearchEngine not initialized, skipping refresh.");
                    return;
                }

                // Empty search → show hierarchy
                if (string.IsNullOrWhiteSpace(currentSearchQuery))
                {
                    currentResults = searchEngine.Search(string.Empty);
                    PopulateTreeView(currentResults);
                    lvResults.Items.Clear();
                    UpdateStatusLabel();
                }
                else
                {
                    // Non-empty search → flat list
                    currentResults = searchEngine.Search(currentSearchQuery);
                    treeViewHierarchy.Nodes.Clear();
                    PopulateListView(currentResults);
                    UpdateStatusLabel();
                }

                logger.LogDebug($"Refresh complete: {currentResults.Count} results.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh panel.");
                ShowError("Failed to refresh prompts list.");
            }
        }

        private static TreeNode FindOrCreateChildNode(TreeNodeCollection nodes, string text)
        {
            foreach (TreeNode node in nodes)
            {
                if (string.Equals(node.Text, text, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }
            }

            var created = new TreeNode(text)
            {
                // Tag will be initialized by AddFileToNode
                Tag = null
            };

            nodes.Add(created);
            return created;
        }

        // ✅ NEW: helper to accumulate files on a node.Tag as List<PromptFile>
        private static void AddFileToNode(TreeNode node, PromptFile file)
        {
            if (node.Tag is not List<PromptFile> list)
            {
                list = new List<PromptFile>();
                node.Tag = list;
            }

            // Avoid duplicates in case the same file is processed multiple times
            if (!list.Contains(file))
            {
                list.Add(file);
            }
        }

        private void PopulateTreeView(List<PromptFile> files)
        {
            treeViewHierarchy.BeginUpdate();
            try
            {
                treeViewHierarchy.Nodes.Clear();

                // Sort by display hierarchy string for stable ordering
                var orderedFiles = files
                    .OrderBy(f => string.Join("/",
                        f.Metadata.GetDisplayHierarchy() ?? Array.Empty<string>()),
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var file in orderedFiles)
                {
                    // GetDisplayHierarchy already skips Const.NA and empty segments
                    var hierarchy = file.Metadata.GetDisplayHierarchy();

                    // If there is no known area/project/category, place under a single root bucket.
                    if (hierarchy.Count == 0)
                    {
                        hierarchy = new[] { "(uncategorized)" };
                    }

                    TreeNodeCollection currentLevel = treeViewHierarchy.Nodes;
                    TreeNode? currentNode = null;

                    foreach (var segment in hierarchy)
                    {
                        var nextNode = FindOrCreateChildNode(currentLevel, segment);

                        // Every level accumulates all descendant files in Tag
                        AddFileToNode(nextNode, file);

                        currentNode = nextNode;
                        currentLevel = nextNode.Nodes;
                    }

                    // If for some reason no hierarchy segment was created,
                    // attach directly to a synthetic root node.
                    if (currentNode == null)
                    {
                        var rootNode = FindOrCreateChildNode(treeViewHierarchy.Nodes, "(uncategorized)");
                        AddFileToNode(rootNode, file);
                    }
                }

                treeViewHierarchy.ExpandAll();
            }
            finally
            {
                treeViewHierarchy.EndUpdate();
            }
        }

        private void PopulateListView(List<PromptFile> files)
        {
            lvResults.Items.Clear();

            foreach (var file in files.OrderBy(f => f.IsFavorite ? 0 : 1).ThenBy(f => f.Metadata.Name))
            {
                var item = new ListViewItem(new[]
                {
                    file.IsFavorite ? "☑" : "☐",
                    file.Metadata.Name,
                    file.Metadata.Version,
                    file.Metadata.Type,
                    file.Metadata.Category,
                    file.LastModified.ToString("yyyy-MM-dd HH:mm"),
                    Path.GetFileName(file.RelativePath)
                })
                {
                    Tag = file
                };

                lvResults.Items.Add(item);
            }
        }

        private void UpdateStatusLabel()
        {
            lblStatus.Text = $"{currentResults.Count} prompt(s) found";
        }

        private void ShowError(string message)
        {
            MessageBox.Show(this, message, "LogError", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Action button handlers

        private void CopySelectedToClipboard()
        {
            using var ctx = logger.SetContext(new Props());

            try
            {
                var selected = GetSelectedPromptFile();
                if (selected == null)
                {
                    ShowError("No prompt selected.");
                    return;
                }

                Clipboard.SetText(selected.Content);
                logger.LogInformation($"Copied to clipboard: {selected.Metadata.FileName}");
                MessageBox.Show(this, "Content copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to copy to clipboard.");
                ShowError("Failed to copy content.");
            }
        }

        private void EditSelectedPrompt()
        {
            using var ctx = logger.SetContext(new Props());

            try
            {
                var selected = GetSelectedPromptFile();
                if (selected == null)
                {
                    ShowError("No prompt selected.");
                    return;
                }

                // Open with default editor (Notepad/VS Code/etc)
                Process.Start(new ProcessStartInfo
                {
                    FileName = selected.FullPath,
                    UseShellExecute = true
                });

                logger.LogInformation($"Opened in editor: {selected.FullPath}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to open editor.");
                ShowError("Failed to open file in editor.");
            }
        }

        private void CreateNewVersion()
        {
            using var ctx = logger.SetContext(new Props());

            try
            {
                var selected = GetSelectedPromptFile();
                if (selected == null)
                {
                    ShowError("No prompt selected.");
                    return;
                }

                // Simple version dialog
                var newVersion = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter new version (e.g., 1.1):",
                    "Create New Version",
                    IncrementVersion(selected.Metadata.Version));

                if (string.IsNullOrWhiteSpace(newVersion)) return;

                // Create copy with new version in filename
                var dir = Path.GetDirectoryName(selected.FullPath)!;
                var oldName = Path.GetFileName(selected.FullPath);
                var newName = oldName.Replace($"-{selected.Metadata.Version}-", $"-{newVersion}-");
                var newPath = Path.Combine(dir, newName);

                File.Copy(selected.FullPath, newPath);
                searchEngine.RebuildIndex();
                RefreshPanel();

                logger.LogInformation($"Created new version: {newPath}");
                MessageBox.Show(this, $"New version created:\n{newName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create new version.");
                ShowError("Failed to create new version.");
            }
        }

        private void OpenInGit()
        {
            using var ctx = logger.SetContext(new Props());

            try
            {
                if (string.IsNullOrWhiteSpace(promptsRepositoryPath) || !Directory.Exists(promptsRepositoryPath))
                {
                    ShowError("Prompts repository path not configured or not found.");
                    return;
                }

                if (!GitHelper.IsGitRepository(promptsRepositoryPath))
                {
                    ShowError("Prompts repository is not a Git repository.");
                    return;
                }

                // Open GitExtensions (or fall back to Explorer)
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "GitExtensions.exe",
                        Arguments = $"browse \"{promptsRepositoryPath}\"",
                        UseShellExecute = true
                    });
                    logger.LogInformation("Opened GitExtensions.");
                }
                catch
                {
                    // Fallback: open folder in Explorer
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = promptsRepositoryPath,
                        UseShellExecute = true
                    });
                    logger.LogWarning("GitExtensions not found, opened folder in Explorer.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to open Git.");
                ShowError("Failed to open Git tool.");
            }
        }

        private void ToggleFavorite()
        {
            using var ctx = logger.SetContext(new Props());

            try
            {
                var selected = GetSelectedPromptFile();
                if (selected == null) return;

                selected.IsFavorite = !selected.IsFavorite;

                var favorites = favoritesManager.LoadFavorites(GetFavoritesConfigPath());

                if (selected.IsFavorite)
                {
                    if (!favorites.Contains(selected.FullPath))
                        favorites.Add(selected.FullPath);
                }
                else
                {
                    favorites.Remove(selected.FullPath);
                }

                favoritesManager.SaveFavorites(GetFavoritesConfigPath(), favorites);
                RefreshPanel();

                logger.LogDebug($"Toggled favorite: {selected.FullPath}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to toggle favorite.");
            }
        }

        // Helpers

        private PromptFile? GetSelectedPromptFile()
        {
            if (lvResults.SelectedItems.Count > 0)
            {
                return lvResults.SelectedItems[0].Tag as PromptFile;
            }

            return null;
        }

        private string IncrementVersion(string version)
        {
            if (double.TryParse(version, out var v))
                return (v + 0.1).ToString("0.0");

            return "1.1";
        }

        private string GetFavoritesConfigPath()
        {
            var configPath = System.Configuration.ConfigurationManager.AppSettings["favoritesConfigPath"];
            return configPath ?? Path.Combine(promptsRepositoryPath ?? ".", "favorites.json");
        }

        private void OpenRenamePromptDialog()
        {
            using var ctx = logger.SetContext(new Props());

            try
            {
                var selected = GetSelectedPromptFile();
                if (selected == null)
                {
                    ShowError("No prompt selected.");
                    return;
                }

                using var dlg = new PromptRenameForm(selected);
                var owner = FindForm();

                if (dlg.ShowDialog(owner) == DialogResult.OK && dlg.WasRenamed)
                {
                    logger.LogInformation("Prompt file renamed, rebuilding index.");

                    searchEngine.RebuildIndex();
                    RefreshPanel();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to rename prompt file from browser panel.");
                ShowError("Failed to rename prompt file.");
            }
        }
    }
}