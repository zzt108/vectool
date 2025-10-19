// ✅ FULL FILE VERSION
// Path: src/VecTool.UI/OaiUI/MainForm.cs
using oaiUI;
using oaiUI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vectool.UI.Versioning;
using VecTool.Configuration;
using VecTool.Core;
using VecTool.Handlers;
using VecTool.RecentFiles;

namespace Vectool.OaiUI
{
    public partial class MainForm : Form
    {
        // UI services and data
        private WinFormsUserInterface userInterface;
        private IRecentFilesManager recentFilesManager;

        // Selected folders bound to the listbox
        private readonly List<string> selectedFolders = new();

        // All known vector stores
        private Dictionary<string, VectorStoreConfig> allVectorStoreConfigs = new();

        // Persist last vector store selection
        private readonly ILastSelectionService lastSelection = new LastSelectionService();

        private readonly IVersionProvider _versionProvider;

        public MainForm(IVersionProvider versionProvider)
        {
            _versionProvider = versionProvider ?? throw new ArgumentNullException(nameof(versionProvider));

            InitializeComponent();

            // Initialize UI service wrappers
            userInterface = new WinFormsUserInterface(statusLabel, progressBar);

            // Initialize Recent Files
            var recentFilesConfig = RecentFilesConfig.FromAppConfig();
            Directory.CreateDirectory(recentFilesConfig.OutputPath);
            var recentFilesStore = new FileRecentFilesStore(recentFilesConfig);
            recentFilesManager = new RecentFilesManager(recentFilesConfig, recentFilesStore);
            recentFilesManager.Load();

            // Recent files panel created by designer; initialize once controls exist
            recentFilesPanel.Initialize(recentFilesManager);

            WireUpEvents();
            LoadVectorStoresIntoComboBox();
            UpdateFormTitle(); // Will now include version

        }

        private void WireUpEvents()
        {
            // Menu items
            convertToMdToolStripMenuItem.Click += convertToMdToolStripMenuItemClick;
            getGitChangesToolStripMenuItem.Click += getGitChangesToolStripMenuItemClick;
            fileSizeSummaryToolStripMenuItem.Click += fileSizeSummaryToolStripMenuItemClick;
            runTestsToolStripMenuItem.Click += runTestsToolStripMenuItemClick;
            exitToolStripMenuItem.Click += exitToolStripMenuItemClick;

            // Form controls
            btnSelectFolders.Click += btnSelectFoldersClick;
            comboBoxVectorStores.SelectedIndexChanged += comboBoxVectorStoresSelectedIndexChanged;
            btnCreateNewVectorStore.Click += btnCreateNewVectorStoreClick;

            // Tab change refresh (ensure recent files grid stays fresh)
            tabControl1.SelectedIndexChanged += (sender, args) =>
            {
                if (tabControl1.SelectedTab == tabPageRecentFiles)
                {
                    recentFilesPanel.RefreshList();
                }
            };
        }

        private void UpdateFormTitle()
        {
            var selectedName = comboBoxVectorStores.SelectedItem?.ToString();
            //var version = _versionProvider.InformationalVersion ?? _versionProvider.FileVersion;
            var version = _versionProvider.FileVersion;
            var baseName = $"VecTool v{version}";

            this.Text = string.IsNullOrEmpty(selectedName)
                ? baseName
                : $"{baseName} - {selectedName}";
        }

        private static string SanitizeFileName(string input, string replacement = "_")
        {
            // Guard: ensure a non-empty replacement character
            var replChar = string.IsNullOrEmpty(replacement) ? '_' : replacement[0];

            // Replace invalid filename chars with the replacement char
            if (string.IsNullOrEmpty(input))
                return "default";

            var sanitized = input;
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var ch in invalidChars)
            {
                sanitized = sanitized.Replace(ch, replChar);
            }

            // Normalize whitespace to replacement char
            foreach (var ch in new[] { ' ', '\t', '\r', '\n' })
            {
                sanitized = sanitized.Replace(ch, replChar);
            }

            // Collapse consecutive replacement chars safely (no infinite loop)
            var doubleRepl = new string(replChar, 2);
            var singleRepl = new string(replChar, 1);
            while (sanitized.Contains(doubleRepl, StringComparison.Ordinal))
            {
                sanitized = sanitized.Replace(doubleRepl, singleRepl, StringComparison.Ordinal);
            }

            // Trim leading/trailing replacement, dots, and spaces
            sanitized = sanitized.Trim(replChar, '.', ' ');

            return string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
        }

        private async void getGitChangesToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
            var gitChangesFileName = $"{vsName}.{branchName}.GIT.md";
            var mdExportFileName = $"{vsName}.{branchName}.md";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Git Changes As...",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                FileName = gitChangesFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var gitOutputPath = saveFileDialog.FileName;
            // Derive MD output path in same directory
            var mdOutputPath = Path.Combine(Path.GetDirectoryName(gitOutputPath)!,mdExportFileName);

            // ignored for now: Check if MD file exists and confirm overwrite once
            if (false && File.Exists(mdOutputPath))
            {
                var overwrite = MessageBox.Show(
                    $"MD export file already exists:\n{mdOutputPath}\n\nOverwrite?",
                    "Confirm Overwrite",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (overwrite != DialogResult.Yes) return;
            }

            try
            {
                await ExecuteGitChangesAndMdParallelAsync(gitOutputPath, mdOutputPath).ConfigureAwait(true);

                userInterface.ShowMessage(
                    $"Successfully generated:\n- Git Changes: {gitOutputPath}\n- MD Export: {mdOutputPath}",
                    "Success",
                    MessageType.Information
                );
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
                // Refresh Recent Files panel after both operations
                recentFilesPanel.RefreshList();
            }
        }

        /// <summary>
        /// Executes Git changes extraction and MD export in parallel.
        /// </summary>
        private async Task ExecuteGitChangesAndMdParallelAsync(string gitOutputPath, string mdOutputPath)
        {
            userInterface.WorkStart("Generating Git changes and MD export...", selectedFolders);

            var gitHandler = new GitChangesHandler(userInterface, recentFilesManager);
            var mdHandler = new MDHandler(userInterface, recentFilesManager);

            var vectorStoreConfig = GetCurrentVectorStoreConfig();

            // ✅ NEW: Execute both operations in parallel
            var gitTask = Task.Run(async () =>
                await gitHandler.GetGitChangesAsync(selectedFolders, gitOutputPath).ConfigureAwait(false)
            );

            var mdTask = mdHandler.ExportSelectedFoldersAsync(selectedFolders, mdOutputPath, vectorStoreConfig);
            

            await Task.WhenAll(gitTask, mdTask).ConfigureAwait(true);
        }

        private async void convertToMdToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
            var defaultFileName = $"{vsName}.{branchName}.md";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save as Markdown...",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var outputPath = saveFileDialog.FileName;
            var config = GetCurrentVectorStoreConfig();

            try
            {
                userInterface.WorkStart($"Generating MD file...", selectedFolders);
                var handler = new MDHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.ExportSelectedFolders(selectedFolders, outputPath, config)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated file at\r\n{outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        private async void fileSizeSummaryToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
            var defaultFileName = $"{vsName}.{branchName}.summary.txt";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save File Size Summary As...",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var outputPath = saveFileDialog.FileName;
            var config = GetCurrentVectorStoreConfig();

            try
            {
                userInterface.WorkStart($"Generating file size summary...", selectedFolders);
                var handler = new FileSizeSummaryHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.GenerateFileSizeSummary(selectedFolders, outputPath, config)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated file at\r\n{outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        private async void runTestsToolStripMenuItemClick(object? sender, EventArgs e)
        {
            //var solutionPath = FindSolutionFile();
            //if (solutionPath is null)
            //{
            //    userInterface.ShowMessage("Could not find VecTool.sln in parent directories.", "Solution Not Found", MessageType.Error);
            //    return;
            //}

            var vsName = comboBoxVectorStores.SelectedItem?.ToString() ?? "default";
            var handler = new TestRunnerHandler(userInterface, recentFilesManager);

            try
            {
                userInterface.WorkStart("Running unit tests...", selectedFolders);
                await handler.RunTestsAsync(vsName, selectedFolders).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"Test execution failed: {ex.Message}", "Test Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
                recentFilesPanel.RefreshList();
            }
        }

        private async Task<string> GetCurrentBranchNameAsync()
        {
            try
            {
                // Prefer deriving branch from the selected vector store folders' repo, not the app repo.
                var preferredWorkingDir = ResolvePreferredWorkingDirectory(selectedFolders);
                if (!string.IsNullOrWhiteSpace(preferredWorkingDir))
                {
                    var git = new GitRunner(preferredWorkingDir);
                    var branch = await git.GetCurrentBranchAsync().ConfigureAwait(false);
                    return string.IsNullOrWhiteSpace(branch) ? "unknown" : branch;
                }

                // Fallback to previous behavior: solution directory
                var solutionPath = FindSolutionFile();
                var solutionDir = solutionPath is null
                    ? AppDomain.CurrentDomain.BaseDirectory
                    : Path.GetDirectoryName(solutionPath)!;

                var gitFallback = new GitRunner(solutionDir);
                var fallbackBranch = await gitFallback.GetCurrentBranchAsync().ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(fallbackBranch) ? "unknown" : fallbackBranch;
            }
            catch
            {
                return "unknown";
            }
        }

        private static string? ResolvePreferredWorkingDirectory(IReadOnlyList<string> folders)
        {
            if (folders == null || folders.Count == 0)
                return null;

            string? firstExisting = null;
            foreach (var folder in folders)
            {
                if (string.IsNullOrWhiteSpace(folder))
                    continue;

                if (firstExisting is null && Directory.Exists(folder))
                    firstExisting = folder;

                var root = FindRepoRoot(folder);
                if (!string.IsNullOrWhiteSpace(root))
                    return root;
            }

            return firstExisting;
        }

        private static string? FindRepoRoot(string? startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return null;

            var dir = new DirectoryInfo(startPath);
            while (dir != null)
            {
                var gitDir = Path.Combine(dir.FullName, ".git");
                if (Directory.Exists(gitDir) || File.Exists(gitDir))
                    return dir.FullName;

                dir = dir.Parent;
            }
            return null;
        }

        private string? FindSolutionFile()
        {
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (currentDir != null)
            {
                var solutionFile = Path.Combine(currentDir.FullName, "VecTool.sln");
                if (File.Exists(solutionFile))
                    return solutionFile;

                currentDir = currentDir.Parent;
            }
            return null;
        }

        private void btnSelectFoldersClick(object? sender, EventArgs e)
        {
            using var folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select a folder to add",
                ShowNewFolderButton = true
            };

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                var selectedPath = folderBrowserDialog.SelectedPath;
                if (!selectedFolders.Contains(selectedPath))
                {
                    selectedFolders.Add(selectedPath);
                    listBoxSelectedFolders.Items.Add(selectedPath);
                    SaveChangesToCurrentVectorStore();
                }
            }
        }

        private void SaveChangesToCurrentVectorStore()
        {
            var currentVsName = comboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(currentVsName) || !allVectorStoreConfigs.ContainsKey(currentVsName))
                return;

            allVectorStoreConfigs[currentVsName].FolderPaths = selectedFolders.ToList();
            VectorStoreConfig.SaveAll(allVectorStoreConfigs);
        }

        private void LoadVectorStoresIntoComboBox()
        {
            try
            {
                allVectorStoreConfigs = VectorStoreConfig.LoadAll() ?? new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);
                var names = allVectorStoreConfigs.Keys
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                comboBoxVectorStores.Items.Clear();
                if (names.Any())
                {
                    comboBoxVectorStores.Items.AddRange(names.Cast<object>().ToArray());

                    // restore last selected vector store if present
                    var last = lastSelection.GetLastSelectedVectorStore();
                    if (!string.IsNullOrWhiteSpace(last))
                    {
                        var idx = names.FindIndex(n => string.Equals(n, last, StringComparison.Ordinal));
                        if (idx >= 0)
                            comboBoxVectorStores.SelectedIndex = idx;
                        else
                            comboBoxVectorStores.SelectedIndex = 0;
                    }
                    else
                    {
                        comboBoxVectorStores.SelectedIndex = 0;
                    }
                }
                else
                {
                    comboBoxVectorStores.Items.Clear();
                    selectedFolders.Clear();
                    listBoxSelectedFolders.Items.Clear();
                    UpdateFormTitle();
                }

                UpdateFormTitle();
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"Failed to load vector stores: {ex.Message}", "Warning", MessageType.Warning);
            }
        }

        private void comboBoxVectorStoresSelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedName = comboBoxVectorStores.SelectedItem?.ToString();
            UpdateFormTitle();

            if (string.IsNullOrEmpty(selectedName) || !allVectorStoreConfigs.ContainsKey(selectedName))
            {
                selectedFolders.Clear();
                listBoxSelectedFolders.Items.Clear();

                // persist cleared selection
                lastSelection.SetLastSelectedVectorStore(null);
                return;
            }

            var config = allVectorStoreConfigs[selectedName];
            selectedFolders.Clear();
            selectedFolders.AddRange(config.FolderPaths ?? Enumerable.Empty<string>());
            listBoxSelectedFolders.Items.Clear();
            listBoxSelectedFolders.Items.AddRange(selectedFolders.Cast<object>().ToArray());

            // persist valid selection
            lastSelection.SetLastSelectedVectorStore(selectedName);
        }

        private void btnCreateNewVectorStoreClick(object? sender, EventArgs e)
        {
            var newName = txtNewVectorStoreName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                userInterface.ShowMessage("Please enter a name for the new vector store.", "Input Required", MessageType.Warning);
                return;
            }

            if (allVectorStoreConfigs.ContainsKey(newName))
            {
                userInterface.ShowMessage($"A vector store named '{newName}' already exists.", "Duplicate Name", MessageType.Warning);
                return;
            }

            var newConfig = VectorStoreConfig.FromAppConfig();
            newConfig.FolderPaths = new List<string>();

            allVectorStoreConfigs[newName] = newConfig;
            VectorStoreConfig.SaveAll(allVectorStoreConfigs);

            LoadVectorStoresIntoComboBox();
            comboBoxVectorStores.SelectedItem = newName;
            txtNewVectorStoreName.Clear();

            userInterface.ShowMessage($"Vector store '{newName}' created.", "Success", MessageType.Information);
            UpdateFormTitle();
        }

        private VectorStoreConfig GetCurrentVectorStoreConfig()
        {
            var selectedName = comboBoxVectorStores.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedName) && allVectorStoreConfigs.TryGetValue(selectedName, out var config))
                return config;

            return VectorStoreConfig.FromAppConfig();
        }

        private void exitToolStripMenuItemClick(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItemClick(object? sender, EventArgs e)
        {
            using (var dlg = new AboutForm())
            {
                dlg.ShowDialog(this);
            }
        }
    }
}
