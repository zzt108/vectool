// Required Imports Template
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NLog; // NLog is mandatory for structured logging
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VecTool.Configuration;
using VecTool.Constants;
using VecTool.Handlers;
using VecTool.RecentFiles;
using VecTool.Core.Versioning;
using VecTool.UI.WinUI.About;
using VecTool.UI.WinUI.Infrastructure;

namespace VecTool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly IUserInterface userInterface;
        private readonly IRecentFilesManager recentFilesManager;

        public MainWindow()
        {
            Log.Info("MainWindow initializing");

            InitializeComponent();

            // Initialize UI wrapper with WinUI controls and dispatcher
            userInterface = new WinUiUserInterface(StatusText, StatusProgress, DispatcherQueue);

            // Initialize Recent Files system
            var config = RecentFilesConfig.FromAppConfig();
            var store = new FileRecentFilesStore(config);
            recentFilesManager = new RecentFilesManager(config, store);

            // Initialize vector store UI
            LoadVectorStoresIntoComboBox();

            // Initialize Settings tab ComboBox
            SettingsTabInitializeData();

            // Navigate RecentFiles to dedicated Frame in tab
            RecentFilesFrame.Navigate(typeof(RecentFilesPage));

            Log.Info("MainWindow initialized");
        }

        #region Menu Handlers

        /// <summary>
        /// Menu handlers - preserve parity with WinForms handlers
        /// </summary>
        private async void ConvertToMdMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("ConvertToMd invoked");

            try
            {
                // Phase 2.2: WinUI folder picker with COM interop
                // Placeholder: Shows message instead of picker until Gap #2 is fixed
                userInterface.ShowMessage(
                    "Folder picker not yet implemented. Please see Phase 2.2 (Gap #2).",
                    "Not Implemented",
                    MessageType.Warning);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ConvertToMd failed");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void GetGitChangesMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("GetGitChanges invoked");

            try
            {
                // Phase 2.2: WinUI folder picker with COM interop
                // Placeholder: Shows message instead of picker until Gap #2 is fixed
                userInterface.ShowMessage(
                    "Folder picker not yet implemented. Please see Phase 2.2 (Gap #2).",
                    "Not Implemented",
                    MessageType.Warning);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetGitChanges failed");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void FileSizeSummaryMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("FileSizeSummary invoked");

            try
            {
                // Phase 2.2: WinUI folder picker with COM interop
                // Placeholder: Shows message instead of picker until Gap #2 is fixed
                userInterface.ShowMessage(
                    "Folder picker not yet implemented. Please see Phase 2.2 (Gap #2).",
                    "Not Implemented",
                    MessageType.Warning);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "FileSizeSummary failed");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void RunTestsClick(object sender, RoutedEventArgs e)
        {
            Log.Info("RunTests invoked");
            try
            {
                // ✅ NEW: Use the correct class name and signature
                var handler = new TestRunnerHandler(userInterface, recentFilesManager);

                // TODO: Wire up actual solution path, store ID, and selected folders from UI state
                var solutionPath = @"C:\path\to\your\solution.sln"; // Replace with actual path from config
                var storeId = ComboBoxVectorStores.SelectedItem?.ToString() ?? "default";
                var selectedFolders = LstSelectedFolders.Items.Cast<string>().ToList();

                var resultPath = await handler.RunTestsAsync(
                    solutionPath,
                    storeId,
                    selectedFolders,
                    CancellationToken.None);

                if (resultPath != null)
                {
                    userInterface.ShowMessage($"Tests completed. Report: {resultPath}", "Run Tests", MessageType.Information);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "RunTests failed");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private void ExitMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Exit invoked");
            Close();
        }

        private async void AboutMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("About invoked");

            try
            {
                // Create IVersionProvider from assembly metadata - parity with WinForms AboutForm
                var versionProvider = new AssemblyVersionProvider();

                // AboutPage is a Page displaying version info - wrap in ContentDialog for modal behavior
                var aboutContent = new AboutPage(versionProvider);
                var dialog = new ContentDialog
                {
                    Title = "About VecTool",
                    Content = aboutContent,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot // Critical for WinUI dialog parenting
                };

                await dialog.ShowAsync();

                Log.Info("About dialog closed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "About dialog failed");
                userInterface.ShowMessage($"Could not display About dialog: {ex.Message}", "Error", MessageType.Error);
            }
        }

        #endregion

        #region Vector Store Management

        /// <summary>
        /// Loads available vector stores into ComboBox from UiStateConfig.
        /// Preserves last selected store from persisted state.
        /// </summary>
        private void LoadVectorStoresIntoComboBox()
        {
            try
            {
                var config = UiStateConfig.FromAppConfig();
                var stores = config.GetVectorStores();

                ComboBoxVectorStores.Items.Clear();
                foreach (var store in stores)
                    ComboBoxVectorStores.Items.Add(store);

                // Select last used store
                var lastStore = config.GetSelectedVectorStore();
                if (!string.IsNullOrWhiteSpace(lastStore) && stores.Contains(lastStore))
                    ComboBoxVectorStores.SelectedItem = lastStore;
                else if (ComboBoxVectorStores.Items.Count > 0)
                    ComboBoxVectorStores.SelectedIndex = 0;

                Log.Info("Vector stores loaded into ComboBox", new { Count = stores.Count, LastStore = lastStore });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load vector stores");
                userInterface.ShowMessage($"Error loading vector stores: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Persists selected vector store when user changes selection.
        /// Loads folders for the selected store into the folder list.
        /// </summary>
        private void ComboBoxVectorStoresSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedName = ComboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedName))
            {
                LstSelectedFolders.Items.Clear();
                return;
            }

            try
            {
                var config = UiStateConfig.FromAppConfig();

                // Persist selection
                config.SetSelectedVectorStore(selectedName);

                // Load folders
                var folders = config.GetVectorStoreFolders(selectedName);
                LstSelectedFolders.Items.Clear();
                foreach (var folder in folders)
                    LstSelectedFolders.Items.Add(folder);

                Log.Info("Vector store selected", new { Store = selectedName, FolderCount = folders.Count });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load folders for vector store", new { Store = selectedName });
                userInterface.ShowMessage($"Error loading folders: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Placeholder for WinUI FolderPicker with COM interop (Phase 2.2).
        /// Shows message instead of picker until Gap #2 is fixed.
        /// Preserves WinForms parity from MainForm.VectorStoreManagement.cs.
        /// </summary>
        private void BtnSelectFoldersClick(object sender, RoutedEventArgs e)
        {
            Log.Info("BtnSelectFolders invoked");

            // Phase 2.2: WinUI folder picker requires COM interop
            // Example code (currently commented out):
            // var picker = new FolderPicker();
            // var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            // WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            // var folder = await picker.PickSingleFolderAsync();

            userInterface.ShowMessage(
                "Folder picker not yet implemented. Please see Phase 2.2 (Gap #2).",
                "Not Implemented",
                MessageType.Warning);
        }

        /// <summary>
        /// Creates a new vector store with sanitized name. Adds to ComboBox and selects it.
        /// </summary>
        private void BtnCreateNewVectorStoreClick(object sender, RoutedEventArgs e)
        {
            var newName = TxtNewVectorStoreName.Text?.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                userInterface.ShowMessage("Please enter a vector store name.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                var sanitized = SanitizeVectorStoreName(newName);
                var config = UiStateConfig.FromAppConfig();

                var existing = config.GetVectorStores();
                if (existing.Contains(sanitized, StringComparer.OrdinalIgnoreCase))
                {
                    userInterface.ShowMessage($"Vector store '{sanitized}' already exists.", "Duplicate", MessageType.Warning);
                    return;
                }

                config.AddVectorStore(sanitized);

                ComboBoxVectorStores.Items.Add(sanitized);
                ComboBoxVectorStores.SelectedItem = sanitized;
                TxtNewVectorStoreName.Text = string.Empty;

                Log.Info("Vector store created", new { Store = sanitized });
                userInterface.ShowMessage($"Vector store '{sanitized}' created successfully.", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create vector store", new { Name = newName });
                userInterface.ShowMessage($"Error creating vector store: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Sanitizes vector store name by removing invalid path characters.
        /// Preserves WinForms parity from MainForm.VectorStoreManagement.cs.
        /// </summary>
        private static string SanitizeVectorStoreName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
            return sanitized;
        }

        #endregion

        #region Settings Tab

        /// <summary>
        /// Initializes Settings tab ComboBox with available vector stores.
        /// Called from constructor after LoadVectorStoresIntoComboBox.
        /// </summary>
        private void SettingsTabInitializeData()
        {
            try
            {
                var config = UiStateConfig.FromAppConfig();
                var stores = config.GetVectorStores();

                CmbSettingsVectorStore.Items.Clear();
                foreach (var store in stores)
                    CmbSettingsVectorStore.Items.Add(store);

                Log.Info("Settings tab initialized", new { StoreCount = CmbSettingsVectorStore.Items.Count });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize Settings tab");
            }
        }

        /// <summary>
        /// Loads settings for the selected vector store.
        /// Uses PerVectorStoreSettings view model to handle inheritance logic.
        /// </summary>
        private void CmbSettingsVectorStoreSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedName = CmbSettingsVectorStore.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedName))
            {
                // Clear UI when no store selected
                TxtExcludedFiles.Text = string.Empty;
                TxtExcludedFolders.Text = string.Empty;
                ChkInheritExcludedFiles.IsChecked = true;
                ChkInheritExcludedFolders.IsChecked = true;
                TxtExcludedFiles.IsEnabled = false;
                TxtExcludedFolders.IsEnabled = false;
                return;
            }

            try
            {
                var global = VectorStoreConfig.FromAppConfig();
                var all = VectorStoreConfig.LoadAll();
                all.TryGetValue(selectedName, out var per);

                // Build view model - mirrors MainForm.SettingsTab.cs logic
                var vm = PerVectorStoreSettings.From(selectedName, global, per);

                // Inherit checkboxes are inverse of "use custom"
                ChkInheritExcludedFiles.IsChecked = !vm.UseCustomExcludedFiles;
                ChkInheritExcludedFolders.IsChecked = !vm.UseCustomExcludedFolders;

                TxtExcludedFiles.Text = string.Join(Environment.NewLine, vm.CustomExcludedFiles);
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, vm.CustomExcludedFolders);

                TxtExcludedFiles.IsEnabled = vm.UseCustomExcludedFiles;
                TxtExcludedFolders.IsEnabled = vm.UseCustomExcludedFolders;

                Log.Info("Settings loaded for vector store", new { Store = selectedName });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings for vector store", new { Store = selectedName });
                userInterface.ShowMessage($"Error loading settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Toggles TxtExcludedFiles enabled state when inheritance checkbox changes.
        /// Mirrors WinForms chkInheritExcludedFiles_CheckedChanged handler.
        /// </summary>
        private void ChkInheritExcludedFilesCheckedChanged(object sender, RoutedEventArgs e)
        {
            TxtExcludedFiles.IsEnabled = ChkInheritExcludedFiles.IsChecked != true;
        }

        /// <summary>
        /// Toggles TxtExcludedFolders enabled state when inheritance checkbox changes.
        /// Mirrors WinForms chkInheritExcludedFolders_CheckedChanged handler.
        /// </summary>
        private void ChkInheritExcludedFoldersCheckedChanged(object sender, RoutedEventArgs e)
        {
            TxtExcludedFolders.IsEnabled = ChkInheritExcludedFolders.IsChecked != true;
        }

        /// <summary>
        /// Saves current settings for the selected vector store.
        /// Uses PerVectorStoreSettings.Save to persist to JSON file.
        /// </summary>
        private void BtnSaveVsSettingsClick(object sender, RoutedEventArgs e)
        {
            var name = CmbSettingsVectorStore.SelectedItem?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                userInterface.ShowMessage("Please select a vector store.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                var global = VectorStoreConfig.FromAppConfig();
                var all = VectorStoreConfig.LoadAll();

                var files = SplitLines(TxtExcludedFiles.Text);
                var folders = SplitLines(TxtExcludedFolders.Text);

                var vm = new PerVectorStoreSettings(
                    name,
                    useCustomExcludedFiles: ChkInheritExcludedFiles.IsChecked != true,
                    useCustomExcludedFolders: ChkInheritExcludedFolders.IsChecked != true,
                    customExcludedFiles: files,
                    customExcludedFolders: folders
                );

                PerVectorStoreSettings.Save(all, vm, global);
                VectorStoreConfig.SaveAll(all);

                Log.Info("Settings saved", new { Store = name });
                userInterface.ShowMessage("Settings saved successfully.", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings", new { Store = name });
                userInterface.ShowMessage($"Error saving settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Resets per-vector-store settings to inherit from global settings.
        /// Clears custom exclusion lists and re-enables inheritance checkboxes.
        /// </summary>
        private void BtnResetVsSettingsClick(object sender, RoutedEventArgs e)
        {
            var name = CmbSettingsVectorStore.SelectedItem?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                userInterface.ShowMessage("Please select a vector store.", "Validation", MessageType.Warning);
                return;
            }

            try
            {
                var all = VectorStoreConfig.LoadAll();

                // Remove per-store settings to reset to global
                if (all.ContainsKey(name))
                {
                    all.Remove(name);
                    VectorStoreConfig.SaveAll(all);
                }

                // Reset UI to inherited state
                ChkInheritExcludedFiles.IsChecked = true;
                ChkInheritExcludedFolders.IsChecked = true;
                TxtExcludedFiles.Text = string.Empty;
                TxtExcludedFolders.Text = string.Empty;
                TxtExcludedFiles.IsEnabled = false;
                TxtExcludedFolders.IsEnabled = false;

                Log.Info("Settings reset to global for vector store", new { Store = name });
                userInterface.ShowMessage($"Settings for '{name}' reset to global defaults.", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reset settings", new { Store = name });
                userInterface.ShowMessage($"Error resetting settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private static List<string> SplitLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(l => l.Trim())
                       .Where(l => !string.IsNullOrWhiteSpace(l))
                       .ToList();
        }

        #endregion
    }
}
