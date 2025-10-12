// ✅ FULL FILE VERSION
// Path: UI/VecTool.UI.WinUI/MainWindow.xaml.cs
// NOTE: WinUI 3 implementation using PerVectorStoreSettings for Phase 3.1

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using VecTool.Configuration;
using VecTool.RecentFiles; // NEW: Required for RecentFilesManager implementation
using Windows.Storage.Pickers;

namespace VecTool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly UiStateConfig uiState;

        // NEW: IRecentFilesManager instance for RecentFilesPage Phase 3.1 Fix
        public IRecentFilesManager RecentFilesManager { get; }

        public MainWindow()
        {
            this.InitializeComponent();

            uiState = UiStateConfig.FromAppConfig();

            // NEW: Initialize RecentFilesManager from config Phase 3.1 Fix
            var config = RecentFilesConfig.FromAppConfig();
            var store = new FileRecentFilesStore(config);
            RecentFilesManager = new RecentFilesManager(config, store);

            LoadSettingsTab();
        }

        #region Settings Tab

        /// <summary>
        /// Load Settings tab: populate combo box with vector stores.
        /// </summary>
        private void LoadSettingsTab()
        {
            try
            {
                var all = VectorStoreConfig.LoadAll();
                var stores = all.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

                CmbSettingsVectorStore.ItemsSource = stores;
                if (stores.Count > 0)
                {
                    CmbSettingsVectorStore.SelectedIndex = 0;
                }

                Log.Info("Settings tab loaded with {Count} stores", stores.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load Settings tab");
                ShowErrorDialog("Error", $"Failed to load settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler: Settings tab vector store selection changed.
        /// Uses PerVectorStoreSettings.From to compute effective settings.
        /// </summary>
        private void CmbSettingsVectorStoreSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }

            try
            {
                var global = VectorStoreConfig.FromAppConfig();
                var all = VectorStoreConfig.LoadAll();
                all.TryGetValue(selected, out var perConfig); // May be null if new store

                // Use PerVectorStoreSettings to compute effective settings
                var vm = PerVectorStoreSettings.From(selected, global, perConfig);

                // Bind to UI: Inherit checkboxes are inverse of UseCustom
                ChkInheritExcludedFiles.IsChecked = !vm.UseCustomExcludedFiles;
                ChkInheritExcludedFolders.IsChecked = !vm.UseCustomExcludedFolders;
                TxtExcludedFiles.Text = string.Join(Environment.NewLine, vm.CustomExcludedFiles);
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, vm.CustomExcludedFolders);
                TxtExcludedFiles.IsEnabled = vm.UseCustomExcludedFiles;
                TxtExcludedFolders.IsEnabled = vm.UseCustomExcludedFolders;

                Log.Info("Settings loaded for {Store}", selected);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings for {Store}", selected);
                ShowErrorDialog("Error", ex.Message);
            }
        }

        /// <summary>
        /// Event handler: Inherit Excluded Files checkbox toggled.
        /// </summary>
        private void ChkInheritExcludedFilesCheckedChanged(object sender, RoutedEventArgs e)
        {
            if(TxtExcludedFiles is not null)
            TxtExcludedFiles.IsEnabled = !(ChkInheritExcludedFiles.IsChecked ?? true);
        }

        /// <summary>
        /// Event handler: Inherit Excluded Folders checkbox toggled.
        /// </summary>
        private void ChkInheritExcludedFoldersCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (TxtExcludedFolders is not null)
                TxtExcludedFolders.IsEnabled = !(ChkInheritExcludedFolders.IsChecked ?? true);
        }

        /// <summary>
        /// Event handler: Save button clicked.
        /// Uses PerVectorStoreSettings.Save for proper serialization.
        /// </summary>
        private void BtnSaveVsSettingsClick(object sender, RoutedEventArgs e)
        {
            var selectedStore = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedStore))
            {
                ShowWarningDialog("Validation", "Please select a vector store.");
                return;
            }

            try
            {
                var global = VectorStoreConfig.FromAppConfig();
                var all = VectorStoreConfig.LoadAll();

                // Read UI state
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

                // Create PerVectorStoreSettings view model
                var vm = new PerVectorStoreSettings(
                    selectedStore,
                    useCustomExcludedFiles: useCustomFiles,
                    useCustomExcludedFolders: useCustomFolders,
                    customExcludedFiles: customFiles,
                    customExcludedFolders: customFolders);

                // Save via PerVectorStoreSettings: updates all in place
                PerVectorStoreSettings.Save(all, vm, global);
                VectorStoreConfig.SaveAll(all);

                Log.Info("Settings saved for {Store}: CustomFiles={FileCount}, CustomFolders={FolderCount}",
                    selectedStore, customFiles.Count, customFolders.Count);

                ShowSuccessDialog("Success", $"Settings saved for '{selectedStore}'.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings for {Store}", selectedStore);
                ShowErrorDialog("Error", $"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler: Reset button clicked.
        /// Uses PerVectorStoreSettings.Save to reset to inherited defaults.
        /// </summary>
        private void BtnResetVsSettingsClick(object sender, RoutedEventArgs e)
        {
            var selectedStore = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedStore))
            {
                ShowWarningDialog("Validation", "Please select a vector store.");
                return;
            }

            try
            {
                var global = VectorStoreConfig.FromAppConfig();
                var all = VectorStoreConfig.LoadAll();

                // Create default VM: inherit everything from global
                var defaultVm = new PerVectorStoreSettings(
                    selectedStore,
                    useCustomExcludedFiles: false,
                    useCustomExcludedFolders: false,
                    customExcludedFiles: new List<string>(),
                    customExcludedFolders: new List<string>());

                // Save the reset state
                PerVectorStoreSettings.Save(all, defaultVm, global);
                VectorStoreConfig.SaveAll(all);

                // Update UI to reflect global defaults
                ChkInheritExcludedFiles.IsChecked = true;
                ChkInheritExcludedFolders.IsChecked = true;
                TxtExcludedFiles.Text = string.Join(Environment.NewLine, global.ExcludedFiles);
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, global.ExcludedFolders);
                TxtExcludedFiles.IsEnabled = false;
                TxtExcludedFolders.IsEnabled = false;

                Log.Info("Settings reset to global defaults for {Store}", selectedStore);
                ShowSuccessDialog("Success", "Settings reset to global defaults.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reset settings for {Store}", selectedStore);
                ShowErrorDialog("Error", $"Error resetting settings: {ex.Message}");
            }
        }

        #endregion

        #region Menu Event Handlers

        /// <summary>
        /// Event handler: Exit menu item clicked.
        /// </summary>
        private void ExitMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Exit menu clicked - closing application");
            this.Close();
        }

        /// <summary>
        /// Event handler: About menu item clicked.
        /// </summary>
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

                await dialog.ShowAsync();
                Log.Info("About dialog displayed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to show About dialog");
                ShowErrorDialog("Error", $"Failed to show About dialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler: Convert to MD menu item clicked.
        /// </summary>
        private async void ConvertToMdMenuClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Info("Convert to MD menu clicked");
                // TODO: Implement MD conversion logic
                // This requires porting the WinForms handler logic from MainForm.FileOperations.cs
                var dialog = new ContentDialog
                {
                    Title = "Not Implemented",
                    Content = "Convert to MD feature is not yet implemented in WinUI version.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to execute Convert to MD");
                ShowErrorDialog("Error", ex.Message);
            }
        }

        /// <summary>
        /// Event handler: Get Git Changes menu item clicked.
        /// </summary>
        private async void GetGitChangesMenuClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Info("Get Git Changes menu clicked");
                // TODO: Implement Git Changes export logic
                // This requires porting the WinForms handler logic from MainForm.FileOperations.cs
                var dialog = new ContentDialog
                {
                    Title = "Not Implemented",
                    Content = "Get Git Changes feature is not yet implemented in WinUI version.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to execute Get Git Changes");
                ShowErrorDialog("Error", ex.Message);
            }
        }

        /// <summary>
        /// Event handler: File Size Summary menu item clicked.
        /// </summary>
        private async void FileSizeSummaryMenuClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Info("File Size Summary menu clicked");
                // TODO: Implement File Size Summary export logic
                // This requires porting the WinForms handler logic from MainForm.FileOperations.cs
                var dialog = new ContentDialog
                {
                    Title = "Not Implemented",
                    Content = "File Size Summary feature is not yet implemented in WinUI version.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to execute File Size Summary");
                ShowErrorDialog("Error", ex.Message);
            }
        }

        /// <summary>
        /// Event handler: Run Tests menu item clicked.
        /// </summary>
        private async void RunTestsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Info("Run Tests menu clicked");
                // TODO: Implement test runner logic
                // This requires porting the WinForms handler logic from MainForm.FileOperations.cs
                var dialog = new ContentDialog
                {
                    Title = "Not Implemented",
                    Content = "Run Tests feature is not yet implemented in WinUI version.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to execute Run Tests");
                ShowErrorDialog("Error", ex.Message);
            }
        }

        #endregion

        #region Main Tab Event Handlers

        /// <summary>
        /// Event handler: ComboBox vector stores selection changed.
        /// </summary>
        private void ComboBoxVectorStoresSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
            {
                Log.Debug("Vector store selection cleared");
                return;
            }

            try
            {
                // Load folders for the selected vector store
                var folders = uiState.GetVectorStoreFolders(selected);

                // Update ListBox
                LstSelectedFolders.Items.Clear();
                foreach (var folder in folders)
                {
                    LstSelectedFolders.Items.Add(folder);
                }

                // Persist selection
                uiState.SetSelectedVectorStore(selected);

                Log.Info("Vector store selected: {Store}, Folders: {Count}", selected, folders.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load vector store: {Store}", selected);
                ShowErrorDialog("Error", $"Failed to load vector store '{selected}': {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler: Select Folders button clicked.
        /// </summary>
        private async void BtnSelectFoldersClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedStore = ComboBoxVectorStores.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(selectedStore))
                {
                    ShowWarningDialog("Validation", "Please select a vector store first.");
                    return;
                }

                // Use WinUI 3 folder picker
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                folderPicker.FileTypeFilter.Add("*");

                // Initialize with window handle (WinUI 3 requirement)
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

                var folder = await folderPicker.PickSingleFolderAsync();

                if (folder == null)
                {
                    Log.Debug("Folder selection canceled");
                    return;
                }

                var folderPath = folder.Path;

                // Check for duplicates (case-insensitive)
                var exists = LstSelectedFolders.Items
                    .Cast<string>()
                    .Any(f => string.Equals(f, folderPath, StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    ShowWarningDialog("Duplicate", "This folder is already in the list.");
                    return;
                }

                // Add to UI and persist
                LstSelectedFolders.Items.Add(folderPath);
                uiState.AddFolderToVectorStore(selectedStore, folderPath);

                Log.Info("Folder added: {Path} to store {Store}", folderPath, selectedStore);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to select folder");
                ShowErrorDialog("Error", $"Failed to select folder: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler: Create New Vector Store button clicked.
        /// </summary>
        private void BtnCreateNewVectorStoreClick(object sender, RoutedEventArgs e)
        {
            var newName = TxtNewVectorStoreName.Text?.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                ShowWarningDialog("Validation", "Please enter a vector store name.");
                return;
            }

            try
            {
                // Add new vector store to config
                uiState.AddVectorStore(newName);

                // Refresh ComboBox
                var stores = uiState.GetVectorStores();
                ComboBoxVectorStores.ItemsSource = stores;
                ComboBoxVectorStores.SelectedItem = newName;

                // Clear input
                TxtNewVectorStoreName.Text = string.Empty;

                Log.Info("Vector store created: {Store}", newName);
                ShowSuccessDialog("Success", $"Vector store '{newName}' created successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create vector store: {Name}", newName);
                ShowErrorDialog("Error", $"Failed to create vector store: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler: Remove Folder button clicked.
        /// </summary>
        private void BtnRemoveFolderClick(object sender, RoutedEventArgs e)
        {
            var selectedStore = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedStore))
            {
                ShowWarningDialog("Validation", "Please select a vector store first.");
                return;
            }

            var selectedItems = LstSelectedFolders.SelectedItems.Cast<string>().ToList();

            if (selectedItems.Count == 0)
            {
                ShowWarningDialog("Validation", "Please select one or more folders to remove.");
                return;
            }

            try
            {
                foreach (var folder in selectedItems)
                {
                    uiState.RemoveFolderFromVectorStore(selectedStore, folder);
                    LstSelectedFolders.Items.Remove(folder);
                    Log.Info("Folder removed: {Path} from store {Store}", folder, selectedStore);
                }

                ShowSuccessDialog("Success", $"Removed {selectedItems.Count} folder(s).");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove folders");
                ShowErrorDialog("Error", $"Failed to remove folders: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private async void ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void ShowWarningDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void ShowSuccessDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        #endregion
    }
}
