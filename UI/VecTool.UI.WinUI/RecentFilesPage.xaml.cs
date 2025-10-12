// ✅ FULL FILE VERSION
// Path: UI/VecTool.UI.WinUI/Pages/RecentFilesPage.xaml.cs
// Phase 3.1: Complete Drag-and-Drop Support (Inbound + Outbound)

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VecTool.Configuration;
using VecTool.Core.RecentFiles;
using VecTool.RecentFiles;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace VecTool.UI.WinUI.Pages
{
    public sealed partial class RecentFilesPage : Page
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly UiStateConfig uiState;

        public RecentFilesPage()
        {
            InitializeComponent();

            // Subscribe to events AFTER InitializeComponent
            Filter.SelectionChanged += FilterSelectionChanged;
            SpecificStore.SelectionChanged += SpecificStoreSelectionChanged;

            // Resolve UiStateConfig from DI
            uiState = TryResolveUiStateConfig() ?? new UiStateConfig(new InMemorySettingsStore());

            // Initialize UI with persisted state
            InitializeRecentFilesTab();
        }

        #region Initialization

        /// <summary>
        /// Initialize Recent Files tab: load stores, set filters, bind data.
        /// </summary>
        private void InitializeRecentFilesTab()
        {
            try
            {
                // Load known stores into Specific Store dropdown
                var storeIds = LoadKnownStoreIds().ToList();
                SpecificStore.ItemsSource = storeIds;

                // Restore persisted filter
                var filter = uiState.GetRecentFilesFilter();
                var storeId = uiState.GetRecentFilesSpecificStoreId();

                // Set filter ComboBox
                Filter.SelectedIndex = filter switch
                {
                    VectorStoreLinkFilter.All => 0,
                    VectorStoreLinkFilter.Linked => 1,
                    VectorStoreLinkFilter.Unlinked => 2,
                    VectorStoreLinkFilter.SpecificStore => 3,
                    _ => 0
                };

                // Show/hide Specific Store dropdown
                SpecificStore.Visibility = filter == VectorStoreLinkFilter.SpecificStore
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Set specific store if applicable
                if (!string.IsNullOrWhiteSpace(storeId) && storeIds.Contains(storeId))
                {
                    SpecificStore.SelectedItem = storeId;
                }

                // Bind items
                BindItems(filter, storeId);

                Log.Info("Recent Files tab initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize Recent Files tab");
                ShowErrorDialog("Initialization Error", ex.Message);
            }
        }

        #endregion

        #region Filter Handlers

        private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var filter = GetSelectedFilter();

            // Show/hide Specific Store dropdown
            SpecificStore.Visibility = filter == VectorStoreLinkFilter.SpecificStore
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Persist filter
            uiState.SetRecentFilesFilter(filter);

            // Refresh grid
            var storeId = GetSelectedStoreId();
            BindItems(filter, storeId);
        }

        private void SpecificStoreSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var storeId = GetSelectedStoreId();

            // Persist specific store ID
            uiState.SetRecentFilesSpecificStoreId(storeId);

            // Refresh grid
            var filter = GetSelectedFilter();
            BindItems(filter, storeId);
        }

        #endregion

        #region Data Binding

        /// <summary>
        /// Load recent files, apply filter, and bind to GridView.
        /// </summary>
        private void BindItems(VectorStoreLinkFilter filter, string? storeId)
        {
            try
            {
                var items = LoadRecentFiles(filter, storeId).ToList();
                Files.ItemsSource = items;
                Log.Debug("Bound {Count} recent files to grid (filter={Filter}, store={Store})",
                    items.Count, filter, storeId ?? "none");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to bind recent files");
                Files.ItemsSource = new List<RecentFileItem>();
            }
        }

        /// <summary>
        /// Load recent files from RecentFilesManager and map to view models with filtering.
        /// </summary>
        private IEnumerable<RecentFileItem> LoadRecentFiles(VectorStoreLinkFilter filter, string? storeId)
        {
            // Resolve IRecentFilesManager from MainWindow
            var manager = TryResolveRecentFilesManager();
            if (manager == null)
            {
                Log.Warn("IRecentFilesManager not available — returning empty list");
                yield break;
            }

            // Get all recent files
            var allFiles = manager.GetRecentFiles();

            // Map RecentFileInfo → RecentFileItem with filtering
            foreach (var info in allFiles)
            {
                // Determine linked store name from SourceFolders
                string? linkedStoreName = info.SourceFolders.Count > 0
                    ? DetermineStoreNameFromFolder(info.SourceFolders.First())
                    : null;

                bool isLinked = !string.IsNullOrWhiteSpace(linkedStoreName);

                // Apply filter logic
                bool include = filter switch
                {
                    VectorStoreLinkFilter.All => true,
                    VectorStoreLinkFilter.Linked => isLinked,
                    VectorStoreLinkFilter.Unlinked => !isLinked,
                    VectorStoreLinkFilter.SpecificStore => string.Equals(linkedStoreName, storeId, StringComparison.OrdinalIgnoreCase),
                    _ => true
                };

                if (!include) continue;

                // Map to view model
                yield return new RecentFileItem(path: info.FilePath, linkedStoreName: linkedStoreName);
            }
        }

        /// <summary>
        /// Load known vector store IDs for Specific Store dropdown.
        /// </summary>
        private IEnumerable<string> LoadKnownStoreIds()
        {
            try
            {
                var all = VectorStoreConfig.LoadAll();
                return all.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load vector store IDs for Recent Files filter");
                return Array.Empty<string>();
            }
        }

        #endregion

        #region Drag-Drop Support

        /// <summary>
        /// Handle drag-over to validate dropped file types and set cursor effect.
        /// </summary>
        private void FilesDragOver(object sender, DragEventArgs e)
        {
            // Accept only file storage items
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "Drop files to register";
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.IsGlyphVisible = true;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        /// <summary>
        /// Handle drop event to register dragged files with IRecentFilesManager.
        /// </summary>
        private async void FilesDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    Log.Warn("Dropped content does not contain storage items");
                    return;
                }

                var items = await e.DataView.GetStorageItemsAsync();
                var files = items.OfType<StorageFile>().ToList();

                if (files.Count == 0)
                {
                    Log.Warn("No files found in dropped content");
                    return;
                }

                var manager = TryResolveRecentFilesManager();
                if (manager == null)
                {
                    Log.Error("Cannot register dropped files: RecentFilesManager unavailable");
                    ShowErrorDialog("Registration Failed", "Recent Files manager is not available.");
                    return;
                }

                // Register each file with current vector store
                var currentStore = uiState.GetSelectedVectorStore();
                foreach (var file in files)
                {
                    var filePath = file.Path;

                    // Validate file type (optional: extend filter)
                    var ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (ext != ".cs" && ext != ".md" && ext != ".txt" && ext != ".json")
                    {
                        Log.Warn("Skipping unsupported file type: {Path}", filePath);
                        continue;
                    }

                    var fileInfo = new FileInfo(filePath);
                    manager.RegisterGeneratedFile(
                        filePath,
                        RecentFileType.Unknown, // Infer from extension if needed
                        new[] { currentStore },
                        fileInfo.Length);

                    Log.Info("Registered dropped file: {Path} → {Store}", filePath, currentStore);
                }

                manager.Save();

                // Refresh grid
                var filter = GetSelectedFilter();
                var storeId = GetSelectedStoreId();
                BindItems(filter, storeId);

                Log.Info("Dropped {Count} files registered successfully", files.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to process dropped files");
                ShowErrorDialog("Drop Failed", ex.Message);
            }
        }

        /// <summary>
        /// Handle drag-out to external apps (outbound drag-drop).
        /// </summary>
        private async void FilesDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            try
            {
                // Collect selected file paths that exist
                var selectedPaths = e.Items
                    .OfType<RecentFileItem>()
                    .Where(item => File.Exists(item.Path))
                    .Select(item => item.Path)
                    .ToList();

                if (selectedPaths.Count == 0)
                {
                    e.Cancel = true;
                    Log.Warn("Drag canceled: No valid files selected");
                    return;
                }

                // Create StorageFile references for WinUI drag-drop
                var storageFiles = new List<IStorageItem>();
                foreach (var path in selectedPaths)
                {
                    var file = await StorageFile.GetFileFromPathAsync(path);
                    storageFiles.Add(file);
                }

                e.Data.SetStorageItems(storageFiles, readOnly: true);
                e.Data.RequestedOperation = DataPackageOperation.Copy;

                Log.Info("Drag started with {Count} files", storageFiles.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error starting drag operation");
                e.Cancel = true;
            }
        }

        #endregion

        #region Context Menu Handlers

        /// <summary>
        /// Item click handler for double-click to open file.
        /// </summary>
        private async void FilesItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as RecentFileItem;
            if (item == null || !File.Exists(item.Path))
            {
                Log.Warn("Cannot open file: Invalid selection or file missing");
                return;
            }

            await OpenFileAsync(item);
        }

        private async void MenuOpenFileClick(object sender, RoutedEventArgs e)
        {
            var item = Files.SelectedItem as RecentFileItem;
            if (item != null) await OpenFileAsync(item);
        }

        private async Task OpenFileAsync(RecentFileItem item)
        {
            if (!File.Exists(item.Path))
            {
                Log.Warn("Cannot open file: File missing at {Path}", item.Path);
                ShowErrorDialog("File Not Found", $"File no longer exists:\n{item.Path}");
                return;
            }

            try
            {
                var file = await StorageFile.GetFileFromPathAsync(item.Path);
                await Windows.System.Launcher.LaunchFileAsync(file);
                Log.Info("Opened file: {Path}", item.Path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open file: {Path}", item.Path);
                ShowErrorDialog("Failed to open file", ex.Message);
            }
        }

        private async void MenuOpenFolderClick(object sender, RoutedEventArgs e)
        {
            var item = Files.SelectedItem as RecentFileItem;
            if (item == null || string.IsNullOrWhiteSpace(item.Path))
            {
                Log.Warn("Cannot open folder: Invalid selection");
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(item.Path);
                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                {
                    Log.Warn("Folder does not exist: {Directory}", directory);
                    ShowErrorDialog("Folder Not Found", $"Folder no longer exists:\n{directory}");
                    return;
                }

                var folder = await StorageFolder.GetFolderFromPathAsync(directory);
                await Windows.System.Launcher.LaunchFolderAsync(folder);
                Log.Info("Opened folder: {Directory}", directory);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open folder for {Path}", item.Path);
                ShowErrorDialog("Failed to open folder", ex.Message);
            }
        }

        private void MenuCopyPathClick(object sender, RoutedEventArgs e)
        {
            var item = Files.SelectedItem as RecentFileItem;
            if (item == null || string.IsNullOrWhiteSpace(item.Path))
            {
                Log.Warn("Cannot copy path: Invalid selection");
                return;
            }

            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(item.Path);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                Log.Info("Copied path to clipboard: {Path}", item.Path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy path: {Path}", item.Path);
                ShowErrorDialog("Copy Failed", ex.Message);
            }
        }

        private void MenuRemoveClick(object sender, RoutedEventArgs e)
        {
            var item = Files.SelectedItem as RecentFileItem;
            if (item == null)
            {
                Log.Warn("Cannot remove: Invalid selection");
                return;
            }

            try
            {
                var manager = TryResolveRecentFilesManager();
                if (manager == null)
                {
                    Log.Error("Cannot remove: RecentFilesManager unavailable");
                    return;
                }

                // Remove from manager
                manager.RemoveFile(item.Path);
                manager.Save();

                // Refresh grid
                var filter = GetSelectedFilter();
                var storeId = GetSelectedStoreId();
                BindItems(filter, storeId);

                Log.Info("Removed file from recent list: {Path}", item.Path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove file: {Path}", item.Path);
                ShowErrorDialog("Remove Failed", ex.Message);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Get currently selected filter from ComboBox.
        /// </summary>
        private VectorStoreLinkFilter GetSelectedFilter()
        {
            return Filter.SelectedIndex switch
            {
                0 => VectorStoreLinkFilter.All,
                1 => VectorStoreLinkFilter.Linked,
                2 => VectorStoreLinkFilter.Unlinked,
                3 => VectorStoreLinkFilter.SpecificStore,
                _ => VectorStoreLinkFilter.All
            };
        }

        /// <summary>
        /// Get currently selected store ID from SpecificStore ComboBox.
        /// </summary>
        private string? GetSelectedStoreId()
        {
            return SpecificStore.SelectedItem?.ToString();
        }

        /// <summary>
        /// Map source folder → vector store name using VectorStoreConfig.
        /// </summary>
        private string? DetermineStoreNameFromFolder(string sourceFolder)
        {
            try
            {
                var all = VectorStoreConfig.LoadAll();
                foreach (var kvp in all)
                {
                    if (kvp.Value.FolderPaths?.Any(f => sourceFolder.Contains(f, StringComparison.OrdinalIgnoreCase)) == true)
                    {
                        return kvp.Key;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to resolve store name for folder: {Folder}", sourceFolder);
            }
            return null;
        }

        /// <summary>
        /// Resolve UiStateConfig from DI or create in-memory fallback.
        /// </summary>
        private UiStateConfig? TryResolveUiStateConfig()
        {
            try
            {
                // For now, create a simple in-memory store
                // TODO: Replace with app-level DI when available
                return UiStateConfig.FromAppConfig();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to resolve UiStateConfig");
                return null;
            }
        }

        /// <summary>
        /// Resolve IRecentFilesManager from MainWindow's public property.
        /// </summary>
        private IRecentFilesManager? TryResolveRecentFilesManager()
        {
            try
            {
                // WinUI 3 doesn't expose MainWindow property by default
                // Access via App.Current or public property on MainWindow
                if (App.Current is App app && app.MainWindow is MainWindow mainWindow)
                {
                    return mainWindow.RecentFilesManager;
                }

                Log.Warn("MainWindow not available — RecentFilesManager unavailable");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to resolve IRecentFilesManager");
                return null;
            }
        }

        /// <summary>
        /// Show error dialog to user.
        /// </summary>
        private async void ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        #endregion

        #region View Models

        /// <summary>
        /// View model for Recent Files grid items.
        /// </summary>
        public sealed class RecentFileItem
        {
            public string Path { get; }
            public string? LinkedStoreName { get; }
            public bool IsLinked => !string.IsNullOrWhiteSpace(LinkedStoreName);

            public RecentFileItem(string path, string? linkedStoreName)
            {
                Path = path ?? throw new ArgumentNullException(nameof(path));
                LinkedStoreName = linkedStoreName;
            }
        }

        #endregion
    }
}
