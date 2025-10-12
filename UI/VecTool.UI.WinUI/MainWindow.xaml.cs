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
using VecTool.Core.RecentFiles; // ✅ NEW: Required for IRecentFilesManager interface
using VecTool.RecentFiles; // ✅ NEW: Required for RecentFilesManager implementation

namespace VecTool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly UiStateConfig uiState;

        // ✅ NEW: IRecentFilesManager instance for RecentFilesPage (Phase 3.1 Fix)
        public IRecentFilesManager RecentFilesManager { get; }

        public MainWindow()
        {
            this.InitializeComponent();
            uiState = UiStateConfig.FromAppConfig();

            // ✅ NEW: Initialize RecentFilesManager from config (Phase 3.1 Fix)
            var config = VecTool.Core.RecentFiles.RecentFilesConfig.FromAppConfig();
            RecentFilesManager = new VecTool.RecentFiles.RecentFilesManager(config);

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
            if (string.IsNullOrWhiteSpace(selected)) return;

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
            TxtExcludedFiles.IsEnabled = !(ChkInheritExcludedFiles.IsChecked ?? true);
        }

        /// <summary>
        /// Event handler: Inherit Excluded Folders checkbox toggled.
        /// </summary>
        private void ChkInheritExcludedFoldersCheckedChanged(object sender, RoutedEventArgs e)
        {
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
                    customExcludedFolders: customFolders
                );

                // Save via PerVectorStoreSettings (updates all in place)
                PerVectorStoreSettings.Save(all, vm, global);
                VectorStoreConfig.SaveAll(all);

                Log.Info("Settings saved for {Store}: CustomFiles={FileCount}, CustomFolders={FolderCount}",
                    selectedStore, customFiles.Count, customFolders.Count);

                ShowSuccessDialog("Success", $"Settings saved for {selectedStore}.");
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
                    customExcludedFolders: new List<string>()
                );

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

        #region Helper Methods

        private void ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            dialog.ShowAsync();
        }

        private void ShowWarningDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            dialog.ShowAsync();
        }

        private void ShowSuccessDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            dialog.ShowAsync();
        }

        #endregion
    }
}
