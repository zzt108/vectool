#nullable enable
using LogCtxShared;
using NLogShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VecTool.Core.Helpers;
using VecTool.Core.Models;
using VecTool.Core.Services;

namespace VecTool.UI.Panels
{
    /// <summary>
    /// Main browser panel for AI Prompts Library with tree hierarchy, 
    /// search results, and 4 core actions (Copy, Edit, New, Git).
    /// </summary>
    public sealed partial class PromptsBrowserPanel : UserControl
    {
        private static readonly CtxLogger log = new();

        private PromptSearchEngine searchEngine;
        private FavoritesManager favoritesManager;
        private string? promptsRepositoryPath;

        private List<PromptFile> currentResults = new();
        private string currentSearchQuery = string.Empty;

        /// <summary>
        /// Constructor for designer support.
        /// </summary>
        public PromptsBrowserPanel()
        {
            InitializeComponent();
            searchEngine = null!;
            favoritesManager = null!;
        }

        /// <summary>
        /// Initialize panel with dependencies (call after designer initialization).
        /// </summary>
        public void Initialize(
            PromptSearchEngine searchEngine,
            FavoritesManager favoritesManager,
            string? promptsRepositoryPath)
        {
            using var ctx = log.Ctx.Set(new Props()
                .Add("RepositoryPath", promptsRepositoryPath ?? "null"));

            this.searchEngine = searchEngine ?? throw new ArgumentNullException(nameof(searchEngine));
            this.favoritesManager = favoritesManager ?? throw new ArgumentNullException(nameof(favoritesManager));
            this.promptsRepositoryPath = promptsRepositoryPath;

            log.Info("PromptsBrowserPanel initialized.");

            // ✅ NEW: Initial load
            RefreshPanel();
        }

        /// <summary>
        /// Refresh tree and list views based on current search query.
        /// </summary>
        public void RefreshPanel()
        {
            using var ctx = log.Ctx.Set(new Props()
                .Add("SearchQuery", currentSearchQuery));

            try
            {
                if (searchEngine == null)
                {
                    log.Warn("SearchEngine not initialized, skipping refresh.");
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

                log.Debug($"Refresh complete: {currentResults.Count} results.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to refresh panel.");
                ShowError("Failed to refresh prompts list.");
            }
        }

        private void PopulateTreeView(List<PromptFile> files)
        {
            treeViewHierarchy.Nodes.Clear();

            var areaGroups = files
                .GroupBy(f => f.Metadata.Area)
                .OrderBy(g => g.Key);

            foreach (var areaGroup in areaGroups)
            {
                var areaNode = new TreeNode(areaGroup.Key) { Tag = areaGroup.Key };

                var projectGroups = areaGroup
                    .GroupBy(f => f.Metadata.Project)
                    .OrderBy(g => g.Key);

                foreach (var projectGroup in projectGroups)
                {
                    var projectNode = new TreeNode(projectGroup.Key) { Tag = projectGroup.Key };

                    var categoryGroups = projectGroup
                        .GroupBy(f => f.Metadata.Category)
                        .OrderBy(g => g.Key);

                    foreach (var categoryGroup in categoryGroups)
                    {
                        var categoryNode = new TreeNode($"{categoryGroup.Key} ({categoryGroup.Count()})")
                        {
                            Tag = categoryGroup.ToList()
                        };

                        projectNode.Nodes.Add(categoryNode);
                    }

                    areaNode.Nodes.Add(projectNode);
                }

                treeViewHierarchy.Nodes.Add(areaNode);
            }

            treeViewHierarchy.ExpandAll();
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
                    file.LastModified.ToString("yyyy-MM-dd HH:mm")
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
            MessageBox.Show(this, message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Action button handlers

        private void CopySelectedToClipboard()
        {
            using var ctx = log.Ctx.Set(new Props());

            try
            {
                var selected = GetSelectedPromptFile();
                if (selected == null)
                {
                    ShowError("No prompt selected.");
                    return;
                }

                Clipboard.SetText(selected.Content);
                log.Info($"Copied to clipboard: {selected.Metadata.FileName}");
                MessageBox.Show(this, "Content copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to copy to clipboard.");
                ShowError("Failed to copy content.");
            }
        }

        private void EditSelectedPrompt()
        {
            using var ctx = log.Ctx.Set(new Props());

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

                log.Info($"Opened in editor: {selected.FullPath}");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to open editor.");
                ShowError("Failed to open file in editor.");
            }
        }

        private void CreateNewVersion()
        {
            using var ctx = log.Ctx.Set(new Props());

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

                log.Info($"Created new version: {newPath}");
                MessageBox.Show(this, $"New version created:\n{newName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to create new version.");
                ShowError("Failed to create new version.");
            }
        }

        private void OpenInGit()
        {
            using var ctx = log.Ctx.Set(new Props());

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
                    log.Info("Opened GitExtensions.");
                }
                catch
                {
                    // Fallback: open folder in Explorer
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = promptsRepositoryPath,
                        UseShellExecute = true
                    });
                    log.Warn("GitExtensions not found, opened folder in Explorer.");
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to open Git.");
                ShowError("Failed to open Git tool.");
            }
        }

        private void ToggleFavorite()
        {
            using var ctx = log.Ctx.Set(new Props());

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

                log.Debug($"Toggled favorite: {selected.FullPath}");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to toggle favorite.");
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
    }
}
