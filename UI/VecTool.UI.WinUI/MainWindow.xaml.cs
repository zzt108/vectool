// Path: UI/VecTool.UI.WinUI/MainWindow.xaml.cs
// Phase 3.1: About Dialog, Settings Persistence, Complete Phase 2 Gaps

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
using VecTool.Core.Versioning;
using VecTool.Handlers;
using VecTool.RecentFiles;
using VecTool.UI.WinUI.Helpers;
using VecTool.UI.WinUI.Infrastructure;
using VecTool.UI.WinUI.About;
using Windows.Storage.Pickers;

namespace VecTool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly WinUiUserInterface userInterface;

        /// <summary>
        /// Exposed as public property for RecentFilesPage DI.
        /// </summary>
        public IRecentFilesManager RecentFilesManager { get; }

        private readonly UiStateConfig uiState;

        public MainWindow()
        {
            this.InitializeComponent();
            Log.Info("MainWindow initializing...");

            // Initialize UI service
            userInterface = new WinUiUserInterface(StatusText, StatusProgress, DispatcherQueue);

            // Initialize Recent Files
            var recentFilesConfig = RecentFilesConfig.FromAppConfig();
            Directory.CreateDirectory(recentFilesConfig.OutputPath);
            var recentFilesStore = new FileRecentFilesStore(recentFilesConfig);
            RecentFilesManager = new RecentFilesManager(recentFilesConfig, recentFilesStore);
            RecentFilesManager.Load();

            // Initialize UiStateConfig for persistence
            uiState = UiStateConfig.FromAppConfig();

            // Load vector stores into ComboBox
            LoadVectorStoresIntoComboBox();

            // Load Settings tab data
            LoadSettingsTab();

            Log.Info("MainWindow initialized successfully.");
        }

        #region Menu Handlers

        private void ExitMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Exit requested");
            Application.Current.Exit();
        }

        private async void ConvertToMdMenuClick(object sender, RoutedEventArgs e)
        {
            var (folders, outputPath) = await SelectFoldersAndOutputAsync("Export to Markdown");
            if (folders.Count == 0 || string.IsNullOrWhiteSpace(outputPath))
            {
                userInterface.ShowMessage("Folder or output selection canceled.", "Canceled", MessageType.Information);
                return;
            }

            try
            {
                var config = GetCurrentVectorStoreConfig();
                var handler = new MDHandler(userInterface, RecentFilesManager);
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
                var handler = new GitChangesHandler(userInterface, RecentFilesManager);
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
            var (folders, outputPath) = await SelectFoldersAndOutputAsync("File Size Summary");
            if (folders.Count == 0 || string.IsNullOrWhiteSpace(outputPath))
            {
                userInterface.ShowMessage("Folder or output selection canceled.", "Canceled", MessageType.Information);
                return;
            }

            try
            {
                var handler = new FileSizeSummaryHandler(userInterface, RecentFilesManager);
                handler.GenerateFileSizeSummary(folders, outputPath);
                userInterface.ShowMessage($"File size summary complete: {outputPath}", "Success", MessageType.Information);
                Log.Info("File size summary completed to {Output}", outputPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "File size summary failed");
                userInterface.ShowMessage($"Summary failed: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void RunTestsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var solutionPath = FindSolutionFile();
                if (string.IsNullOrWhiteSpace(solutionPath))
                {
                    userInterface.ShowMessage("VecTool.sln not found in parent directories.", "Error", MessageType.Error);
                    return;
                }

                var handler = new TestRunnerHandler(userInterface, RecentFilesManager);
                var selectedStore = ComboBoxVectorStores.SelectedItem?.ToString() ?? "default";
                var selectedFolders = LstSelectedFolders.ItemsSource as IReadOnlyList<string> ?? Array.Empty<string>();

                var resultPath = await handler.RunTestsAsync(solutionPath, selectedStore, selectedFolders);

                if (resultPath != null)
                {
                    userInterface.ShowMessage($"Test run completed. Report: {resultPath}", "Success", MessageType.Information);
                }
                else
                {
                    userInterface.ShowMessage("Test run failed or returned non-zero exit code.", "Warning", MessageType.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Test run failed");
                userInterface.ShowMessage($"Test run failed: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void AboutMenuClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var versionProvider = new VecTool.Core.Versioning.AssemblyVersionProvider();
                var aboutPage = new VecTool.UI.WinUI.About.AboutPage(versionProvider);

                var dialog = new ContentDialog
                {
                    Title = "About VecTool",
                    Content = aboutPage,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                Log.Info("About dialog opened with version provider");
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to show About dialog");
                userInterface.ShowMessage($"Failed to open About dialog: {ex.Message}", "Error", MessageType.Error);
            }
        }

        #endregion

        #region Main Tab - Vector Store Management

        /// <summary>
        /// Load vector stores into ComboBox and restore last selection.
        /// </summary>
        private void LoadVectorStoresIntoComboBox()
        {
            try
            {
                var stores = uiState.GetVectorStores();
                ComboBoxVectorStores.ItemsSource = stores;

                var lastSelected = uiState.GetSelectedVectorStore();
                if (!string.IsNullOrWhiteSpace(lastSelected) && stores.Contains(lastSelected))
                {
                    ComboBoxVectorStores.SelectedItem = lastSelected;
                }
                else if (stores.Count > 0)
                {
                    ComboBoxVectorStores.SelectedIndex = 0;
                }

                Log.Info("Loaded {Count} vector stores into ComboBox", stores.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load vector stores");
                userInterface.ShowMessage($"Error loading vector stores: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private void ComboBoxVectorStoresSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected)) return;

            try
            {
                uiState.SetSelectedVectorStore(selected);
                RefreshSelectedFolders(selected);
                Log.Info("Vector store selected: {Store}", selected);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to select vector store: {Store}", selected);
            }
        }

        private void RefreshSelectedFolders(string storeName)
        {
            try
            {
                var folders = uiState.GetVectorStoreFolders(storeName);
                LstSelectedFolders.ItemsSource = folders;
                Log.Debug("Refreshed folder list for {Store}: {Count} folders", storeName, folders.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to refresh folders for {Store}", storeName);
            }
        }

        private void BtnCreateNewVectorStoreClick(object sender, RoutedEventArgs e)
        {
            var newName = TxtNewVectorStoreName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                userInterface.ShowMessage("Please enter a vector store name.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                uiState.AddVectorStore(newName);
                LoadVectorStoresIntoComboBox();
                ComboBoxVectorStores.SelectedItem = newName;
                TxtNewVectorStoreName.Text = string.Empty;
                userInterface.ShowMessage($"Vector store '{newName}' created.", "Success", MessageType.Information);
                Log.Info("Vector store created: {Name}", newName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create vector store: {Name}", newName);
                userInterface.ShowMessage($"Error creating vector store: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void BtnSelectFoldersClick(object sender, RoutedEventArgs e)
        {
            var selectedStore = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedStore))
            {
                userInterface.ShowMessage("Please select a vector store first.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                var folder = await PickerHelper.PickFolderAsync(this);
                if (folder == null)
                {
                    Log.Info("Folder selection canceled");
                    return;
                }

                uiState.AddFolderToVectorStore(selectedStore, folder.Path);
                RefreshSelectedFolders(selectedStore);
                userInterface.ShowMessage($"Folder added: {folder.Path}", "Success", MessageType.Information);
                Log.Info("Folder added to {Store}: {Path}", selectedStore, folder.Path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add folder to {Store}", selectedStore);
                userInterface.ShowMessage($"Error adding folder: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private void BtnRemoveFolderClick(object sender, RoutedEventArgs e)
        {
            var selectedStore = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedStore))
            {
                userInterface.ShowMessage("Please select a vector store first.", "Validation", MessageType.Warning);
                return;
            }

            var selectedFolder = LstSelectedFolders.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedFolder))
            {
                userInterface.ShowMessage("Please select a folder to remove.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                uiState.RemoveFolderFromVectorStore(selectedStore, selectedFolder);
                RefreshSelectedFolders(selectedStore);
                userInterface.ShowMessage($"Folder removed: {selectedFolder}", "Success", MessageType.Information);
                Log.Info("Folder removed from {Store}: {Path}", selectedStore, selectedFolder);
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

                Log.Info("Loaded {Count} stores into Settings tab", stores.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load Settings tab");
                userInterface.ShowMessage($"Error loading settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private void CmbSettingsVectorStoreSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected)) return;

            try
            {
                var all = VectorStoreConfig.LoadAll();
                var global = VectorStoreConfig.FromAppConfig();

                if (all.TryGetValue(selected, out var config))
                {
                    // Load custom settings
                    ChkInheritExcludedFiles.IsChecked = !config.UseCustomExcludedFiles;
                    ChkInheritExcludedFolders.IsChecked = !config.UseCustomExcludedFolders;

                    var filesToShow = config.UseCustomExcludedFiles ? config.CustomExcludedFiles : global.ExcludedFiles;
                    var foldersToShow = config.UseCustomExcludedFolders ? config.CustomExcludedFolders : global.ExcludedFolders;

                    TxtExcludedFiles.Text = string.Join(Environment.NewLine, filesToShow);
                    TxtExcludedFolders.Text = string.Join(Environment.NewLine, foldersToShow);

                    TxtExcludedFiles.IsEnabled = config.UseCustomExcludedFiles;
                    TxtExcludedFolders.IsEnabled = config.UseCustomExcludedFolders;
                }
                else
                {
                    // Load global defaults
                    ChkInheritExcludedFiles.IsChecked = true;
                    ChkInheritExcludedFolders.IsChecked = true;
                    TxtExcludedFiles.Text = string.Join(Environment.NewLine, global.ExcludedFiles);
                    TxtExcludedFolders.Text = string.Join(Environment.NewLine, global.ExcludedFolders);
                    TxtExcludedFiles.IsEnabled = false;
                    TxtExcludedFolders.IsEnabled = false;
                }

                Log.Info("Settings loaded for {Store}", selected);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings for {Store}", selected);
                userInterface.ShowMessage($"Error loading settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private void ChkInheritExcludedFilesCheckedChanged(object sender, RoutedEventArgs e)
        {
            TxtExcludedFiles.IsEnabled = ChkInheritExcludedFiles.IsChecked == false;
        }

        private void ChkInheritExcludedFoldersCheckedChanged(object sender, RoutedEventArgs e)
        {
            TxtExcludedFolders.IsEnabled = ChkInheritExcludedFolders.IsChecked == false;
        }

        private void BtnSaveVsSettingsClick(object sender, RoutedEventArgs e)
        {
            var selectedStore = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedStore))
            {
                userInterface.ShowMessage("Please select a vector store.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                // Load all configs
                var allConfigs = VectorStoreConfig.LoadAll();

                if (!allConfigs.TryGetValue(selectedStore, out var config))
                {
                    config = VectorStoreConfig.FromAppConfig(); // Fallback to global
                }

                // Read UI controls
                var useCustomFiles = !(ChkInheritExcludedFiles.IsChecked ?? true);
                var useCustomFolders = !(ChkInheritExcludedFolders.IsChecked ?? true);

                var customFiles = TxtExcludedFiles.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                var customFolders = TxtExcludedFolders.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                // Update config
                config.UseCustomExcludedFiles = useCustomFiles;
                config.UseCustomExcludedFolders = useCustomFolders;
                config.CustomExcludedFiles = useCustomFiles ? customFiles : new List<string>();
                config.CustomExcludedFolders = useCustomFolders ? customFolders : new List<string>();

                // Save back
                allConfigs[selectedStore] = config;
                VectorStoreConfig.SaveAll(allConfigs);

                Log.Info("Settings saved for {Store} — CustomFiles={FileCount}, CustomFolders={FolderCount}",
                    selectedStore, customFiles.Count, customFolders.Count);

                userInterface.ShowMessage($"Settings saved for {selectedStore}.", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings for {Store}", selectedStore);
                userInterface.ShowMessage($"Error saving settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private void BtnResetVsSettingsClick(object sender, RoutedEventArgs e)
        {
            var selectedStore = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedStore))
            {
                userInterface.ShowMessage("Please select a vector store.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                // Load global defaults
                var globalConfig = VectorStoreConfig.FromAppConfig();

                // Reset UI controls to global defaults
                ChkInheritExcludedFiles.IsChecked = true;
                ChkInheritExcludedFolders.IsChecked = true;

                TxtExcludedFiles.Text = string.Join(Environment.NewLine, globalConfig.ExcludedFiles);
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, globalConfig.ExcludedFolders);

                TxtExcludedFiles.IsEnabled = false;
                TxtExcludedFolders.IsEnabled = false;

                // Clear custom flags in persisted config
                var allConfigs = VectorStoreConfig.LoadAll();
                if (allConfigs.TryGetValue(selectedStore, out var config))
                {
                    config.UseCustomExcludedFiles = false;
                    config.UseCustomExcludedFolders = false;
                    config.CustomExcludedFiles.Clear();
                    config.CustomExcludedFolders.Clear();

                    allConfigs[selectedStore] = config;
                    VectorStoreConfig.SaveAll(allConfigs);
                }

                Log.Info("Settings reset to global defaults for {Store}", selectedStore);
                userInterface.ShowMessage("Settings reset to global defaults.", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reset settings for {Store}", selectedStore);
                userInterface.ShowMessage($"Error resetting settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        #endregion

        #region Helpers

        private VectorStoreConfig GetCurrentVectorStoreConfig()
        {
            var selected = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
            {
                return VectorStoreConfig.FromAppConfig();
            }

            var all = VectorStoreConfig.LoadAll();
            return all.TryGetValue(selected, out var config) ? config : VectorStoreConfig.FromAppConfig();
        }

        private async Task<(List<string> folders, string outputPath)> SelectFoldersAndOutputAsync(string title)
        {
            var selectedStore = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedStore))
            {
                userInterface.ShowMessage("Please select a vector store first.", "Validation", MessageType.Warning);
                return (new List<string>(), string.Empty);
            }

            var folders = uiState.GetVectorStoreFolders(selectedStore);
            if (folders.Count == 0)
            {
                userInterface.ShowMessage("No folders selected in the current vector store.", "Validation", MessageType.Warning);
                return (new List<string>(), string.Empty);
            }

            var outputFolder = await PickerHelper.PickFolderAsync(this);
            if (outputFolder == null)
            {
                Log.Info("Output folder selection canceled");
                return (new List<string>(), string.Empty);
            }

            var outputPath = Path.Combine(outputFolder.Path, $"VecTool_{title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.md");
            return (folders, outputPath);
        }

        private string? FindSolutionFile()
        {
            var currentDir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrWhiteSpace(currentDir))
            {
                var solutionFiles = Directory.GetFiles(currentDir, "VecTool.sln", SearchOption.TopDirectoryOnly);
                if (solutionFiles.Length > 0)
                {
                    return solutionFiles[0];
                }
                currentDir = Directory.GetParent(currentDir)?.FullName;
            }
            return null;
        }

        #endregion
    }
}
