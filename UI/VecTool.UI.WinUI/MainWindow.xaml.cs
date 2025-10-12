using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using VecTool.Configuration;
using VecTool.RecentFiles;
using VecTool.UI.WinUI.Infrastructure;

namespace VecTool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private UiStateConfig uiStateConfig;

        #region Test Accessors

        /// <summary>
        /// Exposes ComboBox for unit testing.
        /// In WinUI 3, x:Name fields are internal/private in generated code.
        /// </summary>
        public ComboBox ComboBoxVectorStoresAccessor => ComboBoxVectorStores;

        /// <summary>
        /// Exposes Select Folders button for unit testing.
        /// </summary>
        public Button BtnSelectFoldersAccessor => BtnSelectFolders;
        public void btnCreateNewVectorStoreClickAccessor(object sender, RoutedEventArgs e) => btnCreateNewVectorStoreClick(sender, e);

        #endregion


        public IRecentFilesManager? RecentFilesManager { get; private set; }

        public MainWindow()
        {
            this.InitializeComponent();
            NLogBootstrap.Init(); // Ensure logging is ready
            // Initialize configuration
            uiStateConfig = UiStateConfig.FromAppConfig();

            InitializeMainTab();  //  Initialize empty state handling
            LoadVectorStores();

            // WinUI 3: set size programmatically (no Width/Height on Window in XAML)
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 700));


            // Initialize RecentFilesManager
            try
            {
                var recentFilesConfig = RecentFilesConfig.FromAppConfig();
                var store = new FileRecentFilesStore(recentFilesConfig);
                RecentFilesManager = new RecentFilesManager(recentFilesConfig, store);
                RecentFilesManager.Load();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize RecentFilesManager");
                RecentFilesManager = null;
            }

            // Populate UI
            LoadVectorStores();
        }

        #region Main Tab Initialization

        /// <summary>
        /// Initializes Main tab controls based on available vector stores.
        /// Disables dropdown and related controls when no stores exist.
        /// </summary>
        private void InitializeMainTab()
        {
            var allStores = VectorStoreConfig.LoadAll();
            bool hasStores = allStores.Count > 0;

            // Enable/disable controls based on store availability
            ComboBoxVectorStores.IsEnabled = hasStores;
            BtnSelectFolders.IsEnabled = hasStores; // Assuming this button exists

            if (!hasStores)
            {
                Log.Info("No vector stores available - Main tab controls disabled");
            }
            else
            {
                ComboBoxVectorStores.SelectedItem = uiStateConfig.GetSelectedVectorStore();
                Log.Info("Vector stores available: {Count}", allStores.Count);
            }
        }

        #endregion

        #region Main Tab Event Handlers

        // Matches XAML: SelectionChanged="comboBoxVectorStoresSelectedIndexChanged"
        private void comboBoxVectorStoresSelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ComboBoxVectorStores.SelectedItem is string storeName)
                {
                    Log.Info("Vector store selected: {Store}", storeName);
                    BtnSelectFolders.IsEnabled = true;
                    LoadFoldersForStore(storeName);
                    uiStateConfig.SetSelectedVectorStore(storeName);
                }
                else
                {
                    BtnSelectFolders.IsEnabled = false;
                    LstSelectedFolders.Items.Clear();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling vector store selection");
            }
        }

        // Matches XAML: Click="btnCreateNewVectorStoreClick"
        private void btnCreateNewVectorStoreClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var storeName = TxtNewVectorStoreName.Text?.Trim();

                if (string.IsNullOrWhiteSpace(storeName))
                {
                    ShowError("Please enter a vector store name.");
                    return;
                }

                uiStateConfig.AddVectorStore(storeName);

                var hasStores = VectorStoreConfig.LoadAll().Count > 0;
                if (hasStores)
                {
                    ComboBoxVectorStores.IsEnabled = true;
                    BtnSelectFolders.IsEnabled = true;
                    Log.Info("Main tab controls enabled after vector store creation");
                }

                LoadVectorStores();
                ComboBoxVectorStores.SelectedItem = storeName;
                TxtNewVectorStoreName.Text = string.Empty;

                Log.Info("Vector store created: {Store}", storeName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating vector store");
                ShowError($"Failed to create vector store: {ex.Message}");
            }
        }

        // Matches XAML: Click="btnSelectFoldersClick"
        private async void btnSelectFoldersClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                // Initialize with window handle
                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

                var folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null && ComboBoxVectorStores.SelectedItem is string storeName)
                {
                    uiStateConfig.AddFolderToVectorStore(storeName, folder.Path);
                    LoadFoldersForStore(storeName);
                    Log.Info("Folder added: {Path}", folder.Path);

                    // ❌ REMOVE: RecentFilesManager?.Add(folder.Path);
                    // ❌ REMOVE: RecentFilesManager?.Save();
                    // Recent Files flow is handled in the dedicated page/services.
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error selecting folder");
                ShowError($"Failed to select folder: {ex.Message}");
            }
        }

        #endregion

        #region Settings Tab Event Handlers

        // Matches XAML: SelectionChanged="cmbSettingsVectorStoreSelectedIndexChanged"
        private void cmbSettingsVectorStoreSelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbSettingsVectorStore.SelectedItem is string storeName)
                {
                    LoadSettingsForStore(storeName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading settings for store");
            }
        }

        // Matches XAML: Checked/Unchecked="chkInheritExcludedFilesCheckedChanged"
        private void chkInheritExcludedFilesCheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                // If inherited (checked), disable textbox; if custom, enable it
                TxtExcludedFiles.IsEnabled = chkInheritExcludedFiles.IsChecked != true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling inherit excluded files checkbox");
            }
        }

        // Matches XAML: Checked/Unchecked="chkInheritExcludedFoldersCheckedChanged"
        private void chkInheritExcludedFoldersCheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                // If inherited (checked), disable textbox; if custom, enable it
                TxtExcludedFolders.IsEnabled = chkInheritExcludedFolders.IsChecked != true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling inherit excluded folders checkbox");
            }
        }

        // Matches XAML: Click="btnSaveVsSettingsClick"
        private void btnSaveVsSettingsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CmbSettingsVectorStore.SelectedItem is not string storeName)
                {
                    ShowError("Select a vector store to save settings.");
                    return;
                }

                // Build view model from UI
                var useCustomFiles = chkInheritExcludedFiles.IsChecked != true;
                var useCustomFolders = chkInheritExcludedFolders.IsChecked != true;

                var customFiles = (TxtExcludedFiles.Text ?? string.Empty)
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();

                var customFolders = (TxtExcludedFolders.Text ?? string.Empty)
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();

                var vm = new SettingsViewModel
                {
                    VectorStoreName = storeName,
                    UseCustomExcludedFiles = useCustomFiles,
                    UseCustomExcludedFolders = useCustomFolders,
                    CustomExcludedFiles = customFiles,
                    CustomExcludedFolders = customFolders,
                };

                // Load global + all per-store, merge and persist
                var global = VectorStoreConfig.FromAppConfig();
                var all = VectorStoreConfig.LoadAll();
                SettingsViewModel.Save(all, vm, global);
                VectorStoreConfig.SaveAll(all);

                Log.Info("Settings saved for vector store: {Store}", storeName);
                ShowInfo("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving vector store settings");
                ShowError($"Failed to save settings: {ex.Message}");
            }
        }

        // Matches XAML: Click="btnResetVsSettingsClick"
        private void btnResetVsSettingsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CmbSettingsVectorStore.SelectedItem is string storeName)
                {
                    LoadSettingsForStore(storeName);
                    Log.Info("Settings reset for store: {Store}", storeName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error resetting vector store settings");
            }
        }

        #endregion

        #region Helper Methods

        private void LoadVectorStores()
        {
            try
            {
                var stores = uiStateConfig.GetVectorStores();

                ComboBoxVectorStores.Items.Clear();
                CmbSettingsVectorStore.Items.Clear();

                foreach (var store in stores)
                {
                    ComboBoxVectorStores.Items.Add(store);
                    CmbSettingsVectorStore.Items.Add(store);
                }

                // Restore last selected vector store if available
                var lastSelected = uiStateConfig.GetSelectedVectorStore();
                if (!string.IsNullOrEmpty(lastSelected) && stores.Contains(lastSelected))
                {
                    ComboBoxVectorStores.SelectedItem = lastSelected;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading vector stores");
            }
        }

        private void LoadFoldersForStore(string storeName)
        {
            try
            {
                LstSelectedFolders.Items.Clear();

                var folders = uiStateConfig.GetVectorStoreFolders(storeName);
                foreach (var folder in folders)
                {
                    LstSelectedFolders.Items.Add(folder);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading folders for store: {Store}", storeName);
            }
        }

        private void LoadSettingsForStore(string storeName)
        {
            try
            {
                var global = VectorStoreConfig.FromAppConfig();
                var allConfigs = VectorStoreConfig.LoadAll();
                var perStore = allConfigs.TryGetValue(storeName, out var cfg) ? cfg : null;

                var settings = SettingsViewModel.Load(storeName, global, perStore);

                // Mirror to UI
                chkInheritExcludedFiles.IsChecked = !settings.UseCustomExcludedFiles;
                chkInheritExcludedFolders.IsChecked = !settings.UseCustomExcludedFolders;

                TxtExcludedFiles.Text = string.Join(Environment.NewLine, settings.CustomExcludedFiles);
                TxtExcludedFolders.Text = string.Join(Environment.NewLine, settings.CustomExcludedFolders);

                TxtExcludedFiles.IsEnabled = settings.UseCustomExcludedFiles;
                TxtExcludedFolders.IsEnabled = settings.UseCustomExcludedFolders;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading settings for store: {Store}", storeName);
            }
        }

        private async void ShowInfo(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Information",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void ShowError(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        #endregion
    }
}
