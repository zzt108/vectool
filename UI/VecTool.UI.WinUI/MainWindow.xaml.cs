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

        // ✅ NEW: Exposed as public property for RecentFilesPage DI
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
                // TestRunnerHandler.RunTestsAsync requires 3 mandatory params: solutionPath, storeId, selectedFolders
                var selectedStore = ComboBoxVectorStores.SelectedItem?.ToString() ?? "default";
                var selectedFolders = LstSelectedFolders.ItemsSource as IReadOnlyList<string>
                    ?? Array.Empty<string>();
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
            var dialog = new ContentDialog
            {
                Title = "About VecTool",
                Content = "VecTool WinUI 3 Migration\nVersion: 4.0 (Phase 2)\n© 2025",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
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
            if (string.IsNullOrWhiteSpace(selected))
                return;

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
                    Log.Info("Folder selection canceled by user");
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
            var selectedFolder = LstSelectedFolders.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(selectedStore) || string.IsNullOrWhiteSpace(selectedFolder))
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
            if (string.IsNullOrWhiteSpace(selected))
                return;

            try
            {
                var all = VectorStoreConfig.LoadAll();
                var global = VectorStoreConfig.FromAppConfig();
                var perOrNull = all.TryGetValue(selected, out var cfg) ? cfg : null;

                var settings = PerVectorStoreSettings.From(selected, global, perOrNull);

                // Bind to UI
                ChkInheritExcludedFiles.IsChecked = !settings.UseCustomExcludedFiles;
                ChkInheritExcludedFolders.IsChecked = !settings.UseCustomExcludedFolders;

                TxtExcludedFiles.Text = string.Join(Environment.NewLine, settings.CustomExcludedFiles);
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, settings.CustomExcludedFolders);

                TxtExcludedFiles.IsEnabled = settings.UseCustomExcludedFiles;
                TxtExcludedFolders.IsEnabled = settings.UseCustomExcludedFolders;

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
            if (TxtExcludedFiles is not null)
            TxtExcludedFiles.IsEnabled = ChkInheritExcludedFiles.IsChecked == false;
        }

        private void ChkInheritExcludedFoldersCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (TxtExcludedFolders is not null)
                TxtExcludedFolders.IsEnabled = ChkInheritExcludedFolders.IsChecked == false;
        }

        private void BtnSaveVsSettingsClick(object sender, RoutedEventArgs e)
        {
            var selected = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
            {
                userInterface.ShowMessage("Please select a vector store.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                var all = VectorStoreConfig.LoadAll();
                var global = VectorStoreConfig.FromAppConfig();

                var useCustomFiles = ChkInheritExcludedFiles.IsChecked == false;
                var useCustomFolders = ChkInheritExcludedFolders.IsChecked == false;

                var filesLines = TxtExcludedFiles.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();

                var foldersLines = TxtExcludedFolders.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();

                var customExcludedFiles = useCustomFiles ? filesLines : global.ExcludedFiles;
                var customExcludedFolders = useCustomFolders ? foldersLines : global.ExcludedFolders;

                var settings = new PerVectorStoreSettings(
                    selected,
                    useCustomFiles,
                    useCustomFolders,
                    customExcludedFiles,
                    customExcludedFolders);

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

        private void BtnResetVsSettingsClick(object sender, RoutedEventArgs e)
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

        #region Helpers

        private VectorStoreConfig GetCurrentVectorStoreConfig()
        {
            var selected = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
            {
                return VectorStoreConfig.FromAppConfig();
            }

            var all = VectorStoreConfig.LoadAll();
            return all.TryGetValue(selected, out var cfg) ? cfg : VectorStoreConfig.FromAppConfig();
        }

        private async Task<(List<string> folders, string? outputPath)> SelectFoldersAndOutputAsync(string title)
        {
            var folders = new List<string>();
            var outputPath = string.Empty;

            try
            {
                var folderPicker = await PickerHelper.PickFolderAsync(this);
                if (folderPicker == null)
                    return (folders, null);

                folders.Add(folderPicker.Path);

                var savePicker = await PickerHelper.PickSaveFileAsync(
                    this,
                    title,
                    new[] { ("Markdown Files", new[] { ".md" }) }
                );
                if (savePicker != null)
                {
                    outputPath = savePicker.Path;
                }

                return (folders, outputPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error selecting folders/output");
                return (folders, null);
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

        #endregion
    }
}
