// ✅ FULL FILE VERSION
// Path: UI/VecTool.UI.WinUI/MainWindow.xaml.cs
// Phase 2.1: Complete persistence layer for Main tab

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
using VecTool.UI.WinUI.Infrastructure;
using Windows.Storage.Pickers;

namespace VecTool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly WinUiUserInterface userInterface;
        private readonly IRecentFilesManager recentFilesManager;

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

            // Load vector stores into ComboBox
            LoadVectorStoresIntoComboBox();

            // Load Settings tab data
            LoadSettingsTab();

            Log.Info("MainWindow initialized successfully");
        }

        #region Vector Store Management (Main Tab)

        /// <summary>
        /// Load all vector stores from JSON config into the ComboBox.
        /// Phase 2.1: Uses UiStateConfig.GetVectorStores() for real persistence.
        /// </summary>
        private void LoadVectorStoresIntoComboBox()
        {
            try
            {
                var config = UiStateConfig.FromAppConfig();
                var stores = config.GetVectorStores();

                ComboBoxVectorStores.Items.Clear();
                foreach (var store in stores)
                {
                    ComboBoxVectorStores.Items.Add(store);
                }

                // Restore last selection from persistence
                var lastSelected = config.GetSelectedVectorStore();
                if (!string.IsNullOrWhiteSpace(lastSelected) && stores.Contains(lastSelected))
                {
                    ComboBoxVectorStores.SelectedItem = lastSelected;
                }
                else if (ComboBoxVectorStores.Items.Count > 0)
                {
                    ComboBoxVectorStores.SelectedIndex = 0;
                }

                Log.Info("Vector stores loaded into ComboBox", new { Count = stores.Count, Selected = lastSelected });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load vector stores into ComboBox");
                userInterface.ShowMessage($"Error loading vector stores: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Handle vector store selection change.
        /// Phase 2.1: Loads folders and persists selection.
        /// </summary>
        private void ComboBoxVectorStoresSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedName = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedName))
                return;

            try
            {
                var config = UiStateConfig.FromAppConfig();

                // Persist selection
                config.SetSelectedVectorStore(selectedName);

                // Load folders for this vector store
                var folders = config.GetVectorStoreFolders(selectedName);
                LstSelectedFolders.Items.Clear();
                foreach (var folder in folders)
                {
                    LstSelectedFolders.Items.Add(folder);
                }

                Log.Info("Vector store selected", new { Store = selectedName, FolderCount = folders.Count });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load folders for vector store", new { Store = selectedName });
                userInterface.ShowMessage($"Error loading folders: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Create a new vector store from text input.
        /// Phase 2.1: Persists to UiStateConfig.AddVectorStore().
        /// </summary>
        private void BtnCreateNewVectorStoreClick(object sender, RoutedEventArgs e)
        {
            var newName = TxtNewVectorStoreName.Text?.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                userInterface.ShowMessage("Please enter a vector store name.", "Validation", MessageType.Warning);
                return;
            }

            // Sanitize name (remove invalid characters)
            var sanitized = string.Concat(newName.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                userInterface.ShowMessage("Invalid vector store name.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                var config = UiStateConfig.FromAppConfig();
                config.AddVectorStore(sanitized);

                // Refresh ComboBox
                LoadVectorStoresIntoComboBox();

                // Select the new store
                ComboBoxVectorStores.SelectedItem = sanitized;

                // Clear input
                TxtNewVectorStoreName.Text = string.Empty;

                Log.Info("Vector store created", new { Name = sanitized });
                userInterface.ShowMessage($"Vector store '{sanitized}' created successfully.", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create vector store", new { Name = sanitized });
                userInterface.ShowMessage($"Error creating vector store: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Adds a folder to the selected vector store via text input dialog.
        /// TODO (Gap 2): Replace with WinUI FolderPicker COM interop.
        /// </summary>
        private async void BtnSelectFoldersClick(object sender, RoutedEventArgs e)
        {
            Log.Info("BtnSelectFolders invoked");

            var selectedStore = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedStore))
            {
                userInterface.ShowMessage("Please select a vector store first.", "Validation", MessageType.Warning);
                return;
            }

            // TODO (Gap 2): Replace with FolderPicker COM interop
            // For now, use a text input dialog (temporary workaround)
            var folderPath = await PromptForFolderPathAsync();
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                Log.Info("Folder selection cancelled");
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                userInterface.ShowMessage($"Folder does not exist: {folderPath}", "Invalid Path", MessageType.Error);
                return;
            }

            try
            {
                var config = UiStateConfig.FromAppConfig();
                config.AddFolderToVectorStore(selectedStore, folderPath);

                // Refresh folder list
                var folders = config.GetVectorStoreFolders(selectedStore);
                LstSelectedFolders.Items.Clear();
                foreach (var folder in folders)
                {
                    LstSelectedFolders.Items.Add(folder);
                }

                Log.Info("Folder added to vector store", new { Store = selectedStore, Path = folderPath });
                userInterface.ShowMessage($"Folder added to '{selectedStore}': {folderPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add folder to vector store", new { Store = selectedStore, Path = folderPath });
                userInterface.ShowMessage($"Error adding folder: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Temporary text input dialog for folder path entry.
        /// TODO (Gap 2): Replace with WinUI FolderPicker COM interop.
        /// </summary>
        private async Task<string?> PromptForFolderPathAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "Enter Folder Path",
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            var textBox = new TextBox
            {
                PlaceholderText = @"C:\",
                Width = 500,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
            };

            // Add helpful text
            var stackPanel = new StackPanel { Spacing = 8 };
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Enter the full path to the folder you want to add:",
                TextWrapping = TextWrapping.Wrap
            });
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Note: Folder picker (browse button) will be available in Phase 2 (Gap 2).",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 4, 0, 0)
            });

            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? textBox.Text?.Trim() : null;
        }

        /// <summary>
        /// Removes the selected folder from the current vector store.
        /// </summary>
        private void BtnRemoveFolderClick(object sender, RoutedEventArgs e)
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
                    var config = UiStateConfig.FromAppConfig();
                    var folders = config.GetVectorStoreFolders(selectedStore);
                    LstSelectedFolders.Items.Clear();
                    foreach (var folder in folders)
                    {
                        LstSelectedFolders.Items.Add(folder);
                    }

                    Log.Info("Folder removed from vector store", new { Store = selectedStore, Path = selectedFolder });
                    userInterface.ShowMessage($"Folder removed: {selectedFolder}", "Success", MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove folder", new { Store = selectedStore, Path = selectedFolder });
                userInterface.ShowMessage($"Error removing folder: {ex.Message}", "Error", MessageType.Error);
            }
        }

        #endregion

        #region Settings Tab

        /// <summary>
        /// Load Settings tab ComboBox and initial data.
        /// Phase 2: Already implemented PerVectorStoreSettings.
        /// </summary>
        private void LoadSettingsTab()
        {
            try
            {
                var all = VectorStoreConfig.LoadAll();
                CmbSettingsVectorStore.Items.Clear();
                foreach (var name in all.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                {
                    CmbSettingsVectorStore.Items.Add(name);
                }

                if (CmbSettingsVectorStore.Items.Count > 0)
                {
                    CmbSettingsVectorStore.SelectedIndex = 0;
                }

                Log.Info("Settings tab loaded", new { Count = CmbSettingsVectorStore.Items.Count });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings tab");
                userInterface.ShowMessage($"Error loading settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Handle Settings tab vector store selection.
        /// Loads exclusion patterns with inheritance logic.
        /// </summary>
        private void CmbSettingsVectorStoreSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedName = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedName))
                return;

            try
            {
                var globalCfg = VectorStoreConfig.FromAppConfig();
                var allCfg = VectorStoreConfig.LoadAll();
                var perCfg = allCfg.TryGetValue(selectedName, out var cfg) ? cfg : null;

                var settings = PerVectorStoreSettings.From(selectedName, globalCfg, perCfg);

                // Bind to UI
                ChkInheritExcludedFiles.IsChecked = !settings.UseCustomExcludedFiles;
                ChkInheritExcludedFolders.IsChecked = !settings.UseCustomExcludedFolders;

                TxtExcludedFiles.Text = string.Join(Environment.NewLine, settings.CustomExcludedFiles);
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, settings.CustomExcludedFolders);

                // Enable/disable textboxes
                TxtExcludedFiles.IsEnabled = settings.UseCustomExcludedFiles;
                TxtExcludedFolders.IsEnabled = settings.UseCustomExcludedFolders;

                Log.Info("Settings loaded for vector store", new { Store = selectedName });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings", new { Store = selectedName });
                userInterface.ShowMessage($"Error loading settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Save Settings tab changes.
        /// </summary>
        private void BtnSaveVsSettingsClick(object sender, RoutedEventArgs e)
        {
            var selectedName = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedName))
            {
                userInterface.ShowMessage("Please select a vector store.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                var globalCfg = VectorStoreConfig.FromAppConfig();
                var allCfg = VectorStoreConfig.LoadAll();

                var useCustomFiles = ChkInheritExcludedFiles.IsChecked == false;
                var useCustomFolders = ChkInheritExcludedFolders.IsChecked == false;

                var customFiles = TxtExcludedFiles.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                var customFolders = TxtExcludedFolders.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                var settings = new PerVectorStoreSettings(
                    selectedName,
                    useCustomFiles,
                    useCustomFolders,
                    customFiles,
                    customFolders
                );

                PerVectorStoreSettings.Save(allCfg, settings, globalCfg);
                VectorStoreConfig.SaveAll(allCfg);

                Log.Info("Settings saved", new { Store = selectedName });
                userInterface.ShowMessage("Settings saved successfully.", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings", new { Store = selectedName });
                userInterface.ShowMessage($"Error saving settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Reset Settings tab to global defaults.
        /// </summary>
        private void BtnResetVsSettingsClick(object sender, RoutedEventArgs e)
        {
            var selectedName = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedName))
            {
                userInterface.ShowMessage("Please select a vector store.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                var globalCfg = VectorStoreConfig.FromAppConfig();
                ChkInheritExcludedFiles.IsChecked = true;
                ChkInheritExcludedFolders.IsChecked = true;
                TxtExcludedFiles.Text = string.Join(Environment.NewLine, globalCfg.ExcludedFiles);
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, globalCfg.ExcludedFolders);

                Log.Info("Settings reset to global", new { Store = selectedName });
                userInterface.ShowMessage("Settings reset to global defaults.", "Reset", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reset settings", new { Store = selectedName });
                userInterface.ShowMessage($"Error resetting settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Toggle exclusion pattern textboxes based on inheritance checkboxes.
        /// </summary>
        private void ChkInheritExcludedFilesCheckedChanged(object sender, RoutedEventArgs e)
        {
            TxtExcludedFiles.IsEnabled = (ChkInheritExcludedFiles.IsChecked == false);
        }

        private void ChkInheritExcludedFoldersCheckedChanged(object sender, RoutedEventArgs e)
        {
            TxtExcludedFolders.IsEnabled = (ChkInheritExcludedFolders.IsChecked == false);
        }

        #endregion

        #region Menu Handlers (Stubs - Gap 2)

        /// <summary>
        /// Convert to Markdown menu handler.
        /// TODO (Gap 2): Implement FolderPicker + FileSavePicker.
        /// </summary>
        private void ConvertToMdMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Convert to MD menu clicked");
            userInterface.ShowMessage("Convert to MD requires folder/file pickers (Gap 2).", "Not Implemented", MessageType.Warning);
        }

        /// <summary>
        /// Git Changes menu handler.
        /// TODO (Gap 2): Implement FolderPicker + FileSavePicker.
        /// </summary>
        private void GetGitChangesMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("GetGitChangesMenu (Actions menu) clicked");
            userInterface.ShowMessage("Get Git Changes requires folder/file pickers (Gap 2).", "Not Implemented", MessageType.Warning);
        }

        private void FileSizeSummaryMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("FileSizeSummaryMenu (Actions menu) clicked");
            userInterface.ShowMessage("File Size Summary requires folder/file pickers (Gap 2).", "Not Implemented", MessageType.Warning);
        }

        private void RunTestsClick(object sender, RoutedEventArgs e)
        {
            Log.Info("RunTests (Actions menu) clicked");
            userInterface.ShowMessage("Run Tests requires folder/file pickers (Gap 2).", "Not Implemented", MessageType.Warning);
        }

        private void ExitMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Exit menu clicked");
            this.Close();
        }

        private void AboutMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("About menu clicked");
            // TODO: Show About dialog (Gap 2)
            userInterface.ShowMessage("About dialog placeholder.", "About", MessageType.Information);
        }

        #endregion
    }
}
