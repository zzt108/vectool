// ✅ FULL FILE VERSION
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NLog;
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
            var (folders, outputPath) = await SelectFoldersAndOutputAsync(".md", "Save Markdown File").ConfigureAwait(true);
            if (folders == null || string.IsNullOrEmpty(outputPath)) return;

            try
            {
                userInterface.WorkStart("Generating MD...", new List<string>(folders));
                var config = GetCurrentVectorStoreConfig();
                var handler = new MDHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.ExportSelectedFolders(new List<string>(folders), outputPath, config)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated {outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ConvertToMd failed");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        private async void GetGitChangesMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("GetGitChanges invoked");
            var (folders, outputPath) = await SelectFoldersAndOutputAsync(".changes.md", "Save Git Changes File").ConfigureAwait(true);
            if (folders == null || string.IsNullOrEmpty(outputPath)) return;

            try
            {
                userInterface.WorkStart("Generating Git changes...", new List<string>(folders));
                var handler = new GitChangesHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.GetGitChanges(new List<string>(folders), outputPath)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated {outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetGitChanges failed");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        private async void FileSizeSummaryMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("FileSizeSummary invoked");
            var (folders, outputPath) = await SelectFoldersAndOutputAsync(".summary.txt", "Save File Size Summary").ConfigureAwait(true);
            if (folders == null || string.IsNullOrEmpty(outputPath)) return;

            try
            {
                userInterface.WorkStart("Generating file size summary...", new List<string>(folders));
                var config = GetCurrentVectorStoreConfig();
                var handler = new FileSizeSummaryHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.GenerateFileSizeSummary(new List<string>(folders), outputPath, config)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated {outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "FileSizeSummary failed");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        private void ExitMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("Exit invoked");
            this.Close();
        }

        private void RunTestsClick(object sender, RoutedEventArgs e)
        {
            Log.Info("RunTests invoked - not implemented yet");
            userInterface.ShowMessage("Run Tests feature is not yet implemented.", "Info", MessageType.Information);
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
                var stores = GetVectorStores(config);

                ComboBoxVectorStores.Items.Clear();
                foreach (var store in stores)
                {
                    ComboBoxVectorStores.Items.Add(store);
                }

                // Select last used store
                var lastStore = GetSelectedVectorStore(config);
                if (!string.IsNullOrEmpty(lastStore) && ComboBoxVectorStores.Items.Contains(lastStore))
                {
                    ComboBoxVectorStores.SelectedItem = lastStore;
                }
                else if (ComboBoxVectorStores.Items.Count > 0)
                {
                    ComboBoxVectorStores.SelectedIndex = 0;
                }

                Log.Info("Vector stores loaded into ComboBox", new { StoreCount = ComboBoxVectorStores.Items.Count });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load vector stores into ComboBox");
                userInterface.ShowMessage($"Error loading vector stores: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Handles vector store selection change.
        /// Loads selected folders for the chosen store.
        /// </summary>
        private void ComboBoxVectorStores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxVectorStores.SelectedItem is not string selectedStore)
            {
                LstSelectedFolders.Items.Clear();
                return;
            }

            try
            {
                var config = UiStateConfig.FromAppConfig();
                SetSelectedVectorStore(config, selectedStore); // Persist selection

                var folders = GetVectorStoreFolders(config, selectedStore);

                LstSelectedFolders.Items.Clear();
                foreach (var folder in folders)
                {
                    LstSelectedFolders.Items.Add(folder);
                }

                Log.Info("Vector store selected", new { Store = selectedStore, FolderCount = folders.Count });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load folders for vector store", new { Store = selectedStore });
                userInterface.ShowMessage($"Error loading folders: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Creates a new vector store with sanitized name.
        /// Adds to ComboBox and selects it.
        /// </summary>
        private void BtnCreateNewVectorStore_Click(object sender, RoutedEventArgs e)
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
                var existing = GetVectorStores(config);

                if (existing.Contains(sanitized, StringComparer.OrdinalIgnoreCase))
                {
                    userInterface.ShowMessage($"Vector store '{sanitized}' already exists.", "Duplicate", MessageType.Warning);
                    return;
                }

                AddVectorStore(config, sanitized);

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
        /// Opens folder picker and adds selected folders to the current vector store.
        /// </summary>
        private async void BtnSelectFolders_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxVectorStores.SelectedItem is not string selectedStore)
            {
                userInterface.ShowMessage("Please select a vector store first.", "No Store", MessageType.Warning);
                return;
            }

            try
            {
                // TODO: Phase 2 - Implement WinUI FolderPicker (requires COM init + Window handle)
                // For now, show placeholder message
                userInterface.ShowMessage("Folder selection will be implemented in Phase 2 with proper WinUI pickers.", "Info", MessageType.Information);

                // Placeholder for future implementation:
                // var picker = new Windows.Storage.Pickers.FolderPicker();
                // var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                // WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                // var folder = await picker.PickSingleFolderAsync();
                // if (folder != null)
                // {
                //     var config = UiStateConfig.FromAppConfig();
                //     AddFolderToVectorStore(config, selectedStore, folder.Path);
                //     LstSelectedFolders.Items.Add(folder.Path);
                //     Log.Info("Folder added", new { Store = selectedStore, Path = folder.Path });
                // }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to select folders");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Sanitizes vector store name by removing invalid file system characters.
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
                var stores = GetVectorStores(config);

                CmbSettingsVectorStore.Items.Clear();
                foreach (var store in stores)
                {
                    CmbSettingsVectorStore.Items.Add(store);
                }

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
        private void CmbSettingsVectorStore_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
        private void ChkInheritExcludedFiles_CheckedChanged(object sender, RoutedEventArgs e)
        {
            TxtExcludedFiles.IsEnabled = ChkInheritExcludedFiles.IsChecked != true;
        }

        /// <summary>
        /// Toggles TxtExcludedFolders enabled state when inheritance checkbox changes.
        /// Mirrors WinForms chkInheritExcludedFolders_CheckedChanged handler.
        /// </summary>
        private void ChkInheritExcludedFolders_CheckedChanged(object sender, RoutedEventArgs e)
        {
            TxtExcludedFolders.IsEnabled = ChkInheritExcludedFolders.IsChecked != true;
        }

        /// <summary>
        /// Saves current settings for the selected vector store.
        /// Uses PerVectorStoreSettings.Save to persist to JSON file.
        /// </summary>
        private void BtnSaveVsSettings_Click(object sender, RoutedEventArgs e)
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
        /// Resets settings to global defaults (inheritance enabled, global patterns shown).
        /// Mirrors WinForms btnResetVsSettings_Click handler.
        /// </summary>
        private void BtnResetVsSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChkInheritExcludedFiles.IsChecked = true;
                ChkInheritExcludedFolders.IsChecked = true;

                var global = VectorStoreConfig.FromAppConfig();
                TxtExcludedFiles.Text = string.Join(Environment.NewLine, global.ExcludedFiles ?? new List<string>());
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, global.ExcludedFolders ?? new List<string>());

                TxtExcludedFiles.IsEnabled = false;
                TxtExcludedFolders.IsEnabled = false;

                Log.Info("Settings reset to global defaults");
                userInterface.ShowMessage("Settings reset to global defaults.", "Reset", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reset settings");
                userInterface.ShowMessage($"Error resetting settings: {ex.Message}", "Error", MessageType.Error);
            }
        }

        /// <summary>
        /// Splits multiline text into distinct, trimmed, non-empty lines.
        /// Mirrors MainForm.SettingsTab.cs SplitLines helper.
        /// </summary>
        private static List<string> SplitLines(string text)
        {
            return (text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        #endregion

        #region UiStateConfig Helper Extensions (Temporary until API is finalized)

        /// <summary>
        /// Gets list of vector store names from config.
        /// TODO: Replace with UiStateConfig.GetVectorStores() when implemented.
        /// </summary>
        private static List<string> GetVectorStores(UiStateConfig config)
        {
            // Placeholder: Read from app.config <vectorStores> section or return hardcoded list
            // In production, this will call config.GetVectorStores()
            return new List<string> { "VecToolDev", "TestStore" };
        }

        /// <summary>
        /// Gets currently selected vector store name.
        /// TODO: Replace with UiStateConfig.GetSelectedVectorStore() when implemented.
        /// </summary>
        private static string? GetSelectedVectorStore(UiStateConfig config)
        {
            // Placeholder: Read from persisted state
            return "VecToolDev";
        }

        /// <summary>
        /// Sets currently selected vector store name.
        /// TODO: Replace with UiStateConfig.SetSelectedVectorStore() when implemented.
        /// </summary>
        private static void SetSelectedVectorStore(UiStateConfig config, string storeName)
        {
            // Placeholder: Persist to app.config or state file
            Log.Info("Vector store selection persisted", new { Store = storeName });
        }

        /// <summary>
        /// Gets folders associated with a vector store.
        /// TODO: Replace with UiStateConfig.GetVectorStoreFolders() when implemented.
        /// </summary>
        private static List<string> GetVectorStoreFolders(UiStateConfig config, string storeName)
        {
            // Placeholder: Read from app.config or state file
            return storeName == "VecToolDev"
                ? new List<string> { @"C:\Git\vectoolDev" }
                : new List<string>();
        }

        /// <summary>
        /// Adds a new vector store to config.
        /// TODO: Replace with UiStateConfig.AddVectorStore() when implemented.
        /// </summary>
        private static void AddVectorStore(UiStateConfig config, string storeName)
        {
            // Placeholder: Persist to app.config
            Log.Info("Vector store added to config", new { Store = storeName });
        }

        /// <summary>
        /// Adds a folder to a vector store.
        /// TODO: Replace with UiStateConfig.AddFolderToVectorStore() when implemented.
        /// </summary>
        private static void AddFolderToVectorStore(UiStateConfig config, string storeName, string folderPath)
        {
            // Placeholder: Persist folder association
            Log.Info("Folder added to vector store", new { Store = storeName, Path = folderPath });
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Helpers - mimic WinForms helpers for parity
        /// </summary>
        private Task<(string[]? folders, string? outputPath)> SelectFoldersAndOutputAsync(string ext, string title)
        {
            // TODO: Phase 2 - Implement WinUI 3 folder/file pickers
            // Use Windows.Storage.Pickers.FolderPicker and FileSavePicker with proper COM initialization
            // For now, return empty to allow compilation and preserve handler signatures
            return Task.FromResult<(string[]?, string?)>((Array.Empty<string>(), null));
        }

        private string? TryFindSolutionPath()
        {
            // TODO: Implement solution file discovery - walk up from BaseDirectory
            return null;
        }

        private string GetSelectedVectorStoreName()
        {
            // TODO: Read from ComboBox or state when UI is complete
            return "VecToolDev";
        }

        private VectorStoreConfig GetCurrentVectorStoreConfig()
        {
            // TODO: Load from persisted vector store configs
            return new VectorStoreConfig();
        }

        #endregion
    }
}
