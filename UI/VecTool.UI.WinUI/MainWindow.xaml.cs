// UI/VecTool.UI.WinUI/MainWindow.xaml.cs
// WinUI 3 migration-compliant MainWindow with NLog, DispatcherQueue, and VisualTreeHelper

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using VecTool.Configuration;
using VecTool.RecentFiles;

namespace VecTool.UI.WinUI;

public sealed partial class MainWindow : Window
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    private UiStateConfig uiStateConfig;
    public IRecentFilesManager? RecentFilesManager { get; private set; }

    public MainWindow()
    {
        this.InitializeComponent();

        // WinUI 3: set size programmatically (no Width/Height on Window in XAML)
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 700));

        // Initialize configuration
        uiStateConfig = UiStateConfig.FromAppConfig();

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

    #region Lifecycle & Initialization

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Info("Window loaded, content tree ready");
        UpdateUIState();
    }

    #endregion

    #region Event Handlers

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Info("Save button clicked");

        // Example: UI marshaling with DispatcherQueue
        var dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        dq.TryEnqueue(() =>
        {
            Log.Info("Save operation dispatched to UI thread");
            // Perform save logic here
        });
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Info("Cancel button clicked");
        Close();
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

    #region Drag & Drop Handlers

    private void ContentPanel_DragOver(object sender, DragEventArgs e)
    {
        // 🚫 WPF pattern: e.Effects = DragDropEffects.Copy
        // ✅ WinUI 3 pattern:
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop files here";
        e.Handled = true;
    }

    private async void ContentPanel_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var paths = items.Select(i => i.Path).ToList();

                Log.Info("Files dropped: {Count} items", paths.Count);

                foreach (var path in paths)
                {
                    Log.Info("Processing dropped file: {Path}", path);
                }

                ProcessDroppedFiles(paths);
            }
        }
        catch (Exception ex)
        {
            var evt = new LogEventInfo(LogLevel.Error, Log.Name, "Drop operation failed");
            evt.Exception = ex;
            Log.Log(evt);

            // ✅ WinUI 3 dialog pattern with XamlRoot
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = "Failed to process dropped files.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    #endregion

    #region UI State Management

    private void UpdateUIState()
    {
        if (BtnSaveVsSettings != null)
        {
            BtnSaveVsSettings.IsEnabled = true;
        }

        Log.Info("UI state updated");
    }

    private void RefreshVisualTree()
    {
        // ✅ WinUI 3: VisualTreeHelper from Microsoft.UI.Xaml.Media
        if (this.Content is DependencyObject root)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(root);
            Log.Info("Visual tree has {ChildCount} children", childCount);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                Log.Info("Child {Index}: {Type}", i, child.GetType().Name);
            }
        }
    }

    #endregion

    #region Business Logic Orchestration

    private void ProcessDroppedFiles(List<string> paths)
    {
        Log.Info("Processing {Count} dropped files", paths.Count);

        // TODO: Wire to actual file processing service
        foreach (var path in paths)
        {
            Log.Info("File queued for processing: {Path}", path);
        }
    }

    #endregion

    #region Helpers

    private void NavigateToPanel(Type panelType)
    {
        Log.Info("Navigating to panel: {PanelType}", panelType.Name);

        // Example: WinUI 3 Frame navigation
        // ContentFrame.Navigate(panelType);
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
