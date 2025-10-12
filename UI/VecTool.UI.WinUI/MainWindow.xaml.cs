// ✅ FULL FILE VERSION
// Path: UI/VecTool.UI.WinUI/MainWindow.xaml.cs
// Phase 2.2: WinUI folder/file pickers with HWND initialization + correct handler names

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VecTool.Configuration;
using VecTool.Core;
using VecTool.Handlers;
using VecTool.RecentFiles;
using VecTool.UI.WinUI.Helpers;
using VecTool.UI.WinUI.Infrastructure;
using Windows.Storage.Pickers;

namespace VecTool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly WinUiUserInterface userInterface;
        private readonly IRecentFilesManager recentFilesManager;
        private readonly UiStateConfig uiState;

        public MainWindow()
        {
            this.InitializeComponent();
            Log.Info("MainWindow initializing");

            // Initialize UI service
            userInterface = new WinUiUserInterface(StatusText, StatusProgress, DispatcherQueue);

            // Initialize Recent Files
            var recentFilesConfig = RecentFilesConfig.FromAppConfig();
            Directory.CreateDirectory(recentFilesConfig.OutputPath);
            var recentFilesStore = new FileRecentFilesStore(recentFilesConfig);
            recentFilesManager = new RecentFilesManager(recentFilesConfig, recentFilesStore);
            recentFilesManager.Load();

            // Initialize UiStateConfig for persistence
            uiState = UiStateConfig.FromAppConfig();

            // Load vector stores into ComboBox
            LoadVectorStoresIntoComboBox();

            // Load Settings tab data
            LoadSettingsTab();

            Log.Info("MainWindow initialized successfully");
        }

        #region Vector Store Management (Main Tab)

        /// <summary>
        /// Load all vector stores from JSON config into the ComboBox.
        /// Phase 2.1: Uses UiStateConfig.GetVectorStores for real persistence.
        /// </summary>
        private void LoadVectorStoresIntoComboBox()
        {
            try
            {
                var stores = uiState.GetVectorStores();
                ComboBoxVectorStores.ItemsSource = stores;

                // Restore last selected store
                var lastSelected = uiState.GetSelectedVectorStore();
                if (!string.IsNullOrWhiteSpace(lastSelected) && stores.Contains(lastSelected))
                {
                    ComboBoxVectorStores.SelectedItem = lastSelected;
                }
                else if (stores.Count > 0)
                {
                    ComboBoxVectorStores.SelectedIndex = 0;
                }

                // Refresh folders for selected store
                var selected = ComboBoxVectorStores.SelectedItem as string;
                if (!string.IsNullOrWhiteSpace(selected))
                {
                    RefreshSelectedFolders(selected);
                }

                Log.Info("Loaded {Count} vector stores into ComboBox", stores.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load vector stores into ComboBox");
                userInterface.ShowMessage($"Error loading vector stores: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Handle vector store selection change.
        /// Phase 2.1: Persists selection via UiStateConfig.SetSelectedVectorStore.
        /// </summary>
        private void ComboBoxVectorStoresSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedName = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedName))
                return;

            try
            {
                // Persist selection
                uiState.SetSelectedVectorStore(selectedName);

                // Load folders for this vector store
                RefreshSelectedFolders(selectedName);

                Log.Info("Vector store selected: {Store}", selectedName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load folders for vector store: {Store}", selectedName);
                userInterface.ShowMessage($"Error loading folders: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Refresh folder list for the selected vector store.
        /// Phase 2.1: Reads from UiStateConfig.GetVectorStoreFolders.
        /// </summary>
        private void RefreshSelectedFolders(string storeName)
        {
            var folders = uiState.GetVectorStoreFolders(storeName);
            LstSelectedFolders.ItemsSource = folders;
            Log.Debug("Refreshed folder list for {Store}: {Count} items", storeName, folders.Count);
        }

        /// <summary>
        /// Create a new vector store from text input.
        /// Phase 2.1: Persists to UiStateConfig.AddVectorStore.
        /// </summary>
        private void BtnCreateNewVectorStore_Click(object sender, RoutedEventArgs e)
        {
            var newName = TxtNewVectorStoreName.Text?.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                userInterface.ShowMessage("Please enter a vector store name.", "Validation", MessageType.Warning);
                return;
            }

            // Sanitize name: remove invalid characters
            var sanitized = string.Concat(newName.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                userInterface.ShowMessage("Invalid vector store name.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                uiState.AddVectorStore(sanitized);

                // Refresh ComboBox
                LoadVectorStoresIntoComboBox();

                // Select the new store
                ComboBoxVectorStores.SelectedItem = sanitized;

                // Clear input
                TxtNewVectorStoreName.Text = string.Empty;

                Log.Info("Vector store created: {Name}", sanitized);
                userInterface.ShowMessage($"Vector store '{sanitized}' created successfully.", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create vector store: {Name}", sanitized);
                userInterface.ShowMessage($"Error creating vector store: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Adds a folder to the selected vector store via FolderPicker.
        /// Phase 2.2: Real picker with HWND initialization.
        /// </summary>
        private async void BtnSelectFolders_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("BtnSelectFolders invoked");

            var selectedStore = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedStore))
            {
                userInterface.ShowMessage("Please select a vector store first.", "Validation", MessageType.Warning);
                return;
            }

            // Phase 2.2: Use WinUI FolderPicker with HWND init
            var folder = await PickerHelper.PickFolderAsync(this, "Select folder to add");
            if (folder == null)
            {
                Log.Debug("Folder selection canceled");
                return;
            }

            var folderPath = folder.Path;
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                userInterface.ShowMessage("Invalid folder path.", "Error", MessageType.Error);
                return;
            }

            // Check for duplicates
            var existing = uiState.GetVectorStoreFolders(selectedStore);
            if (existing?.Contains(folderPath, StringComparer.OrdinalIgnoreCase) == true)
            {
                userInterface.ShowMessage("This folder is already added.", "Duplicate", MessageType.Information);
                return;
            }

            try
            {
                uiState.AddFolderToVectorStore(selectedStore, folderPath);
                RefreshSelectedFolders(selectedStore);
                userInterface.ShowMessage($"Added: {folderPath}", "Success", MessageType.Information);
                Log.Info("Folder added to {Store}: {Path}", selectedStore, folderPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add folder to {Store}: {Path}", selectedStore, folderPath);
                userInterface.ShowMessage($"Error adding folder: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Removes the selected folder from the current vector store.
        /// </summary>
        private void BtnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            var selectedFolder = LstSelectedFolders.SelectedItem?.ToString();
            var selectedStore = ComboBoxVectorStores.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(selectedFolder) || string.IsNullOrWhiteSpace(selectedStore))
            {
                userInterface.ShowMessage("Please select a folder to remove.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                var all = VectorStoreConfig.LoadAll();
                if (!all.TryGetValue(selectedStore, out var cfg))
                    return;

                if (cfg.RemoveFolderPath(selectedFolder))
                {
                    VectorStoreConfig.SaveAll(all);

                    // Refresh folder list
                    RefreshSelectedFolders(selectedStore);

                    Log.Info("Folder removed from {Store}: {Path}", selectedStore, selectedFolder);
                    userInterface.ShowMessage("Folder removed.", "Success", MessageType.Information);
                }
                else
                {
                    userInterface.ShowMessage("Folder not found.", "Info", MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove folder from {Store}: {Path}", selectedStore, selectedFolder);
                userInterface.ShowMessage($"Error removing folder: {ex.Message}", "Error", MessageType.Error);
            }
        }

        #endregion

        #region Settings Tab

        /// <summary>
        /// Load Settings tab with all vector stores and select the same store as Main tab.
        /// </summary>
        private void LoadSettingsTab()
        {
            try
            {
                var all = VectorStoreConfig.LoadAll();
                var stores = all.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

                CmbSettingsVectorStore.ItemsSource = stores;

                // Sync with Main tab selection
                var mainSelected = ComboBoxVectorStores.SelectedItem?.ToString();
                if (!string.IsNullOrWhiteSpace(mainSelected) && stores.Contains(mainSelected))
                {
                    CmbSettingsVectorStore.SelectedItem = mainSelected;
                }
                else if (stores.Count > 0)
                {
                    CmbSettingsVectorStore.SelectedIndex = 0;
                }

                Log.Info("Settings tab loaded with {Count} vector stores", stores.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load Settings tab");
                userInterface.ShowMessage($"Error loading Settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private void CmbSettingsVectorStoreSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
                return;

            try
            {
                var global = VectorStoreConfig.FromAppConfig();
                var all = VectorStoreConfig.LoadAll();
                all.TryGetValue(selected, out var perOrNull);

                var settings = PerVectorStoreSettings.From(selected, global, perOrNull);

                // Bind to UI
                ChkInheritExcludedFiles.IsChecked = !settings.UseCustomExcludedFiles;
                ChkInheritExcludedFolders.IsChecked = !settings.UseCustomExcludedFolders;

                TxtExcludedFiles.Text = string.Join(Environment.NewLine, settings.CustomExcludedFiles);
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, settings.CustomExcludedFolders);

                TxtExcludedFiles.IsEnabled = settings.UseCustomExcludedFiles;
                TxtExcludedFolders.IsEnabled = settings.UseCustomExcludedFolders;

                Log.Debug("Settings loaded for {Store}", selected);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings for {Store}", selected);
                userInterface.ShowMessage($"Error loading settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private void ChkInheritExcludedFilesCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (TxtExcludedFiles is not null) 
                TxtExcludedFiles.IsEnabled = ChkInheritExcludedFiles.IsChecked == false;
        }

        private void ChkInheritExcludedFoldersCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (TxtExcludedFolders is not null)
                TxtExcludedFolders.IsEnabled = ChkInheritExcludedFolders.IsChecked == false;
        }

        private void BtnSaveVsSettings_Click(object sender, RoutedEventArgs e)
        {
            var selected = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
            {
                userInterface.ShowMessage("Please select a vector store.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                var global = VectorStoreConfig.FromAppConfig();
                var all = VectorStoreConfig.LoadAll();

                var filesLines = TxtExcludedFiles.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                var foldersLines = TxtExcludedFolders.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                var settings = new PerVectorStoreSettings(
                    selected,
                    useCustomExcludedFiles: ChkInheritExcludedFiles.IsChecked == false,
                    useCustomExcludedFolders: ChkInheritExcludedFolders.IsChecked == false,
                    customExcludedFiles: filesLines,
                    customExcludedFolders: foldersLines
                );

                PerVectorStoreSettings.Save(all, settings, global);
                VectorStoreConfig.SaveAll(all);

                Log.Info("Settings saved for {Store}", selected);
                userInterface.ShowMessage("Settings saved successfully.", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings for {Store}", selected);
                userInterface.ShowMessage($"Error saving settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private void BtnResetVsSettings_Click(object sender, RoutedEventArgs e)
        {
            var selected = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
            {
                userInterface.ShowMessage("Please select a vector store.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                ChkInheritExcludedFiles.IsChecked = true;
                ChkInheritExcludedFolders.IsChecked = true;

                var global = VectorStoreConfig.FromAppConfig();
                TxtExcludedFiles.Text = string.Join(Environment.NewLine, global.ExcludedFiles);
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, global.ExcludedFolders);

                Log.Info("Settings reset to global for {Store}", selected);
                userInterface.ShowMessage("Settings reset to global defaults.", "Info", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reset settings for {Store}", selected);
                userInterface.ShowMessage($"Error resetting settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        #endregion

        #region Menu Actions (Phase 2.2 - Pickers)

        /// <summary>
        /// Select folders and output file for menu actions.
        /// Phase 2.2: Uses FolderPicker + FileSavePicker with HWND init.
        /// </summary>
        private async Task<(List<string> folders, string? outputPath)> SelectFoldersAndOutputAsync(string dialogTitle)
        {
            var folders = new List<string>();

            // Step 1: Pick folder
            var folder = await PickerHelper.PickFolderAsync(this, dialogTitle);
            if (folder == null)
            {
                Log.Debug("Folder selection canceled for {Title}", dialogTitle);
                return (folders, null);
            }

            folders.Add(folder.Path);
            Log.Info("Selected folder for {Title}: {Path}", dialogTitle, folder.Path);

            // Step 2: Pick output file
            var outputFile = await PickerHelper.PickSaveFileAsync(
                this,
                "output.md",
                new[]
                {
                    ("Markdown Files", new[] { ".md" }),
                    ("All Files", new[] { "*" })
                });

            if (outputFile == null)
            {
                Log.Debug("Output file selection canceled for {Title}", dialogTitle);
                return (folders, null);
            }

            Log.Info("Selected output file for {Title}: {Path}", dialogTitle, outputFile.Path);
            return (folders, outputFile.Path);
        }

        private async void ConvertToMdMenuClick(object sender, RoutedEventArgs e)
        {
            var (folders, outputPath) = await SelectFoldersAndOutputAsync("Convert to Markdown");
            if (folders.Count == 0 || string.IsNullOrWhiteSpace(outputPath))
            {
                userInterface.ShowMessage("Folder or output selection canceled.", "Canceled", MessageType.Information);
                return;
            }

            try
            {
                var config = GetCurrentVectorStoreConfig();
                var handler = new MDHandler(userInterface, recentFilesManager);
                handler.ExportSelectedFolders(folders, outputPath, config);
                userInterface.ShowMessage($"Markdown export complete: {outputPath}", "Success", MessageType.Information);
                Log.Info("Markdown export completed to {Output}", outputPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Markdown export failed");
                userInterface.ShowMessage($"Export failed: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void GetGitChangesMenuClick(object sender, RoutedEventArgs e)
        {
            var (folders, outputPath) = await SelectFoldersAndOutputAsync("Analyze Git Changes");
            if (folders.Count == 0 || string.IsNullOrWhiteSpace(outputPath))
            {
                userInterface.ShowMessage("Folder or output selection canceled.", "Canceled", MessageType.Information);
                return;
            }

            try
            {
                var handler = new GitChangesHandler(userInterface, recentFilesManager);
                await handler.GetGitChangesAsync(folders, outputPath);
                userInterface.ShowMessage($"Git changes analysis complete: {outputPath}", "Success", MessageType.Information);
                Log.Info("Git changes analysis completed to {Output}", outputPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Git changes analysis failed");
                userInterface.ShowMessage($"Analysis failed: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void FileSizeSummaryMenuClick(object sender, RoutedEventArgs e)
        {
            var folder = await PickerHelper.PickFolderAsync(this, "Select folder for size summary");
            if (folder == null)
            {
                Log.Debug("Folder selection canceled");
                return;
            }

            try
            {
                var config = GetCurrentVectorStoreConfig();
                var scanner = new FolderScanner();
                var files = scanner.GetFiles(folder.Path);
                var total = scanner.TotalSize(files);
                var readable = FileSizeSummarizer.ToHumanReadable(total);

                userInterface.ShowMessage($"Total size: {readable} ({files.Count} files)", "File Size Summary", MessageType.Information);
                Log.Info("File size summary: {Total} bytes, {Count} files", total, files.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "File size summary failed");
                userInterface.ShowMessage($"Summary failed: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void RunTestsClick(object sender, RoutedEventArgs e)
        {
            var solutionPath = FindSolutionFile();
            if (solutionPath == null)
            {
                userInterface.ShowMessage("Could not find VecTool.sln in parent directories.", "Solution Not Found", MessageType.Error);
                return;
            }

            try
            {
                var vsName = ComboBoxVectorStores.SelectedItem?.ToString() ?? "default";
                var handler = new TestRunnerHandler(userInterface, recentFilesManager);

                var selectedFolders = uiState.GetVectorStoreFolders(vsName);
                var result = await handler.RunTestsAsync(solutionPath, vsName, selectedFolders);

                if (result != null)
                {
                    userInterface.ShowMessage($"Tests completed. Results: {result}", "Test Results", MessageType.Information);
                }
                else
                {
                    userInterface.ShowMessage("Tests failed or were canceled.", "Test Results", MessageType.Warning);
                }

                Log.Info("RunTests completed for solution {Path}", solutionPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RunTests failed");
                userInterface.ShowMessage($"Test run failed: {ex.Message}", "Error", MessageType.Error);
            }
        }

        #endregion

        #region Menu Handlers

        private void ExitMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Exit menu clicked");
            Application.Current.Exit();
        }

        private void AboutMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("About menu clicked");
            userInterface.ShowMessage("VecTool WinUI 3 - Phase 2.2 Complete", "About", MessageType.Information);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get current VectorStoreConfig for handlers requiring exclusion rules.
        /// </summary>
        private VectorStoreConfig GetCurrentVectorStoreConfig()
        {
            var selectedName = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedName))
            {
                return VectorStoreConfig.FromAppConfig(); // fallback to global
            }

            var all = VectorStoreConfig.LoadAll();
            return all.TryGetValue(selectedName, out var cfg) ? cfg : VectorStoreConfig.FromAppConfig();
        }

        /// <summary>
        /// Find VecTool.sln by walking up from BaseDirectory.
        /// </summary>
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

        #endregion
    }
}
