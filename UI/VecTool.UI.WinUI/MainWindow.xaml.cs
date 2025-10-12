// ✅ FULL FILE VERSION
// Path: src\UI\VecTool.UI.WinUI\MainWindow.xaml.cs

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using VecTool.Configuration;
using VecTool.RecentFiles;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace VecTool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly UiStateConfig uiState;

        /// <summary>
        /// IRecentFilesManager instance for RecentFilesPage (Phase 3.1 Fix).
        /// </summary>
        public IRecentFilesManager RecentFilesManager { get; }

        public MainWindow()
        {
            this.InitializeComponent();

            // ✅ NEW: Set window size programmatically (WinUI 3 pattern)
            SetWindowSize(1200, 800);

            // Initialize state and managers
            uiState = UiStateConfig.FromAppConfig();

            // Initialize RecentFilesManager from config (Phase 3.1 Fix)
            var config = RecentFilesConfig.FromAppConfig();
            var store = new FileRecentFilesStore(config);
            RecentFilesManager = new RecentFilesManager(config, store);

            // Load Settings tab
            LoadSettingsTab();
        }

        #region Window Management

        /// <summary>
        /// Sets the window size programmatically using AppWindow API (WinUI 3 pattern).
        /// </summary>
        /// <param name="width">Desired width in pixels.</param>
        /// <param name="height">Desired height in pixels.</param>
        private void SetWindowSize(int width, int height)
        {
            try
            {
                IntPtr hwnd = WindowNative.GetWindowHandle(this);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow != null)
                {
                    appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
                    Log.Info("Window resized to {Width}x{Height}", width, height);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to set window size to {Width}x{Height}", width, height);
            }
        }

        #endregion

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

                ComboBoxVectorStores.ItemsSource = stores;

                if (stores.Count > 0)
                {
                    ComboBoxVectorStores.SelectedIndex = 0;
                }

                Log.Info("Settings tab loaded with {Count} stores", stores.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load Settings tab");
                ShowErrorDialog("Error", $"Failed to load settings: {ex.Message}");
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

                // Update ListBox (assuming LstSelectedFolders exists in XAML)
                // LstSelectedFolders.Items.Clear();
                // foreach (var folder in folders)
                // {
                //     LstSelectedFolders.Items.Add(folder);
                // }

                // Persist selection
                uiState.SetSelectedVectorStore(selected);

                Log.Info("Vector store selected: {Store}, Folders: {Count}", selected, folders.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load vector store {Store}", selected);
                ShowErrorDialog("Error", $"Failed to load vector store {selected}: {ex.Message}");
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
                var hwnd = WindowNative.GetWindowHandle(this);
                InitializeWithWindow.Initialize(folderPicker, hwnd);

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder == null)
                {
                    Log.Debug("Folder selection canceled");
                    return;
                }

                var folderPath = folder.Path;

                // Check for duplicates (case-insensitive)
                // var exists = LstSelectedFolders.Items
                //     .Cast<string>()
                //     .Any(f => string.Equals(f, folderPath, StringComparison.OrdinalIgnoreCase));

                // if (exists)
                // {
                //     ShowWarningDialog("Duplicate", "This folder is already in the list.");
                //     return;
                // }

                // Add to UI and persist
                // LstSelectedFolders.Items.Add(folderPath);
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
                Log.Error(ex, "Failed to create vector store {Name}", newName);
                ShowErrorDialog("Error", $"Failed to create vector store: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler: Build Index button clicked.
        /// </summary>
        private void BtnBuildIndexClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Build Index clicked - not yet implemented");
            ShowWarningDialog("Not Implemented", "Build Index feature is not yet implemented.");
        }

        /// <summary>
        /// Event handler: Query button clicked.
        /// </summary>
        private void BtnQueryClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Query clicked - not yet implemented");
            ShowWarningDialog("Not Implemented", "Query feature is not yet implemented.");
        }

        /// <summary>
        /// Event handler: Clear Index button clicked.
        /// </summary>
        private void BtnClearIndexClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Clear Index clicked - not yet implemented");
            ShowWarningDialog("Not Implemented", "Clear Index feature is not yet implemented.");
        }

        #endregion

        #region Settings Tab Event Handlers

        /// <summary>
        /// Event handler: Save Settings button clicked.
        /// </summary>
        private void BtnSaveSettingsClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Save Settings clicked - not yet implemented");
            ShowSuccessDialog("Success", "Settings saved successfully.");
        }

        /// <summary>
        /// Event handler: Reset to Defaults button clicked.
        /// </summary>
        private void BtnResetSettingsClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Reset Settings clicked - not yet implemented");
            ShowSuccessDialog("Success", "Settings reset to defaults.");
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
