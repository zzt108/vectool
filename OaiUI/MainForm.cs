using oaiUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.Core;
using VecTool.Handlers;
using VecTool.RecentFiles;
// NEW CODE GOES HERE
using oaiUI.Services; // LastSelectionService

namespace Vectool.OaiUI
{
    public partial class MainForm : Form
    {
        private WinFormsUserInterface userInterface;
        private IRecentFilesManager recentFilesManager;
        private List<string> selectedFolders = new();
        private Dictionary<string, VectorStoreConfig> allVectorStoreConfigs = new();
        // NEW CODE GOES HERE
        private readonly ILastSelectionService lastSelection = new LastSelectionService();

        public MainForm()
        {
            InitializeComponent();

            userInterface = new WinFormsUserInterface(statusLabel, progressBar);

            // Initialize RecentFilesManager with proper constructor
            var recentFilesConfig = RecentFilesConfig.FromAppConfig();
            Directory.CreateDirectory(recentFilesConfig.OutputPath);
            var recentFilesStore = new FileRecentFilesStore(recentFilesConfig);
            recentFilesManager = new RecentFilesManager(recentFilesConfig, recentFilesStore);
            recentFilesManager.Load();

            // The panel is created by the designer; initialize after it's ready
            recentFilesPanel.Initialize(recentFilesManager);

            WireUpEvents();
            LoadVectorStoresIntoComboBox();
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

            // Tab change refresh
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
            this.Text = string.IsNullOrEmpty(selectedName) ? "VecTool" : $"VecTool - {selectedName}";
        }

        private async void getGitChangesToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync());
            var defaultFileName = $"{vsName}.{branchName}.changes.md";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Git Changes As...",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

            var outputPath = saveFileDialog.FileName;

            try
            {
                userInterface.WorkStart("Generating Git changes file...", selectedFolders);
                var handler = new GitChangesHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.GetGitChanges(selectedFolders, outputPath));
                userInterface.ShowMessage($"Successfully generated file at\n{outputPath}", "Success", MessageType.Information);
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

        private static string SanitizeFileName(string input, string replacement = "_")
        {
            if (string.IsNullOrWhiteSpace(input)) return "default";
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = input;
            foreach (var invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, replacement[0]);
            }
            while (sanitized.Contains(replacement + replacement))
                sanitized = sanitized.Replace(replacement + replacement, replacement);
            sanitized = sanitized.Trim(replacement[0]);
            return string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
        }

        private async Task<string> GetCurrentBranchNameAsync()
        {
            try
            {
                var solutionPath = FindSolutionFile();
                if (solutionPath == null) return "unknown";
                var solutionDir = Path.GetDirectoryName(solutionPath) ?? Directory.GetCurrentDirectory();
                var gitRunner = new GitRunner(solutionDir);
                var branchName = await gitRunner.GetCurrentBranchAsync();
                return string.IsNullOrWhiteSpace(branchName) ? "unknown" : branchName;
            }
            catch
            {
                return "unknown";
            }
        }

        private async void convertToMdToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync());
            var defaultFileName = $"{vsName}.{branchName}.md";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save as Markdown...",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

            var outputPath = saveFileDialog.FileName;
            var config = GetCurrentVectorStoreConfig();

            try
            {
                userInterface.WorkStart("Generating MD file...", selectedFolders);
                var handler = new MDHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.ExportSelectedFolders(selectedFolders, outputPath, config));
                userInterface.ShowMessage($"Successfully generated file at\n{outputPath}", "Success", MessageType.Information);
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
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync());
            var defaultFileName = $"{vsName}.{branchName}.summary.txt";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save File Size Summary As...",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

            var outputPath = saveFileDialog.FileName;
            var config = GetCurrentVectorStoreConfig();

            try
            {
                userInterface.WorkStart("Generating file size summary...", selectedFolders);
                var handler = new FileSizeSummaryHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.GenerateFileSizeSummary(selectedFolders, outputPath, config));
                userInterface.ShowMessage($"Successfully generated file at\n{outputPath}", "Success", MessageType.Information);
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
            var solutionPath = FindSolutionFile();
            if (solutionPath == null)
            {
                userInterface.ShowMessage("Could not find VecTool.sln in parent directories.", "Solution Not Found", MessageType.Error);
                return;
            }

            var vsName = comboBoxVectorStores.SelectedItem?.ToString() ?? "default";
            var handler = new TestRunnerHandler(userInterface, recentFilesManager);

            try
            {
                userInterface.WorkStart("Running unit tests...", selectedFolders);
                await handler.RunTestsAsync(solutionPath, vsName, selectedFolders);
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"Test execution failed: {ex.Message}", "Test Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
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
                }
                SaveChangesToCurrentVectorStore();
            }
        }

        private void SaveChangesToCurrentVectorStore()
        {
            var currentVsName = comboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(currentVsName) || !allVectorStoreConfigs.ContainsKey(currentVsName)) return;

            allVectorStoreConfigs[currentVsName].FolderPaths = selectedFolders.ToList();
            VectorStoreConfig.SaveAll(allVectorStoreConfigs);
        }

        private void LoadVectorStoresIntoComboBox()
        {
            try
            {
                allVectorStoreConfigs = VectorStoreConfig.LoadAll();
                var names = allVectorStoreConfigs.Keys
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                comboBoxVectorStores.Items.Clear();
                if (names.Any())
                {
                    comboBoxVectorStores.Items.AddRange(names.Cast<object>().ToArray());

                    // ... existing code ...
                    // NEW CODE GOES HERE: restore last selected vector store if present
                    var last = lastSelection.GetLastSelectedVectorStore();
                    if (!string.IsNullOrWhiteSpace(last))
                    {
                        var idx = names.FindIndex(n => string.Equals(n, last, StringComparison.Ordinal));
                        if (idx >= 0)
                        {
                            comboBoxVectorStores.SelectedIndex = idx;
                        }
                        else
                        {
                            comboBoxVectorStores.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        comboBoxVectorStores.SelectedIndex = 0;
                    }
                    // ... existing code ...
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
                // NEW CODE GOES HERE: persist cleared selection
                lastSelection.SetLastSelectedVectorStore(null);
                return;
            }

            var config = allVectorStoreConfigs[selectedName];
            selectedFolders = config.FolderPaths.ToList();
            listBoxSelectedFolders.Items.Clear();
            listBoxSelectedFolders.Items.AddRange(selectedFolders.Cast<object>().ToArray());

            // NEW CODE GOES HERE: persist valid selection
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
            {
                return config;
            }
            return VectorStoreConfig.FromAppConfig();
        }

        private void exitToolStripMenuItemClick(object? sender, EventArgs e) => Application.Exit();
    }
}
