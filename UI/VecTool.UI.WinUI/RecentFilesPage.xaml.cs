// File: Vectool.UI.WinUI/Pages/RecentFilesPage.xaml.cs

// Required Imports Template
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NLog; // NLog is mandatory for structured logging
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VecTool.Configuration;
using VecTool.Core.RecentFiles;
using VecTool.UI.WinUI;

namespace Vectool.UI.WinUI
{
    public sealed partial class RecentFilesPage : Page
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Persisted UI state (filter, specific store, last selection)
        private readonly UiStateConfig uiState;

        // View data
        private readonly ObservableCollection<RecentFileItem> items = new();
        private readonly ObservableCollection<string> storeIds = new();

        public RecentFilesPage()
        {
            InitializeComponent();

            // Resolve UiStateConfig from DI if available; fall back to in-memory store so page never crashes
            uiState = TryResolve<UiStateConfig>()
                      ?? new UiStateConfig(new InMemorySettingsStore());

            // Initialize UI with persisted state
            InitializeRecentFilesTab();
        }

        // Initializes filters and store list from persisted state and binds items
        private void InitializeRecentFilesTab()
        {
            var persistedFilter = uiState.GetRecentFilesFilter();
            var persistedStoreId = uiState.GetRecentFilesSpecificStoreId();

            // Set filter ComboBox selection by Tag
            SetFilterSelection(persistedFilter);

            // Populate SpecificStore ComboBox (placeholder; will be hydrated by service in app composition)
            RefreshStoreIds(persistedStoreId);

            // Enable/disable specific-store input based on filter
            SpecificStore.IsEnabled = persistedFilter == VectorStoreLinkFilter.Specific;

            // Bind items for current filter/storeId
            BindItems(persistedFilter, persistedStoreId);

            Log.Info("RecentFiles initialized with {Filter} and {StoreId}", persistedFilter, persistedStoreId ?? "(null)");
        }

        // Event: Filter changed by user
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var filter = GetSelectedFilter();
            SpecificStore.IsEnabled = filter == VectorStoreLinkFilter.Specific;

            // When switching away from Specific, clear the store selection
            string? storeId = filter == VectorStoreLinkFilter.Specific ? GetSelectedStoreId() : null;

            // Persist immediately
            uiState.SetRecentFilesFilter(filter);
            uiState.SetRecentFilesSpecificStoreId(storeId);

            BindItems(filter, storeId);

            var evt = new LogEventInfo(LogLevel.Info, Log.Name, "RecentFiles filter changed");
            evt.Properties["Filter"] = filter.ToString();
            evt.Properties["StoreId"] = storeId ?? string.Empty;
            Log.Log(evt);
        }

        // Event: Specific store changed
        private void SpecificStore_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var filter = GetSelectedFilter();
            var storeId = GetSelectedStoreId();

            // Persist
            uiState.SetRecentFilesFilter(filter);
            uiState.SetRecentFilesSpecificStoreId(storeId);

            BindItems(filter, storeId);

            var evt = new LogEventInfo(LogLevel.Info, Log.Name, "RecentFiles specific store changed");
            evt.Properties["Filter"] = filter.ToString();
            evt.Properties["StoreId"] = storeId ?? string.Empty;
            Log.Log(evt);
        }

        // Event: Manual refresh
        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            var filter = GetSelectedFilter();
            var storeId = GetSelectedStoreId();

            BindItems(filter, storeId);
            Log.Info("RecentFiles refreshed for {Filter} and {StoreId}", filter, storeId ?? "(null)");
        }

        // Event: Single click action (selection)
        private void Files_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is RecentFileItem item)
            {
                uiState.SetRecentFilesLastSelection(item.Path);
                Log.Info("RecentFiles item clicked: {FileName} at {Path}", item.FileName, item.Path);
            }
        }

        // Event: Double-tap to open (placeholder for real navigation/open behavior)
        private void Files_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (Files.SelectedItem is RecentFileItem item)
            {
                // In real app: open file or navigate; here we just log
                var evt = new LogEventInfo(LogLevel.Info, Log.Name, "RecentFiles item double-tapped");
                evt.Properties["FileName"] = item.FileName;
                evt.Properties["Path"] = item.Path;
                evt.Properties["LinkedStoreName"] = item.LinkedStoreName ?? string.Empty;
                Log.Log(evt);
            }
        }

        // Reads current filter from the ComboBox Tag values
        private VectorStoreLinkFilter GetSelectedFilter()
        {
            if (Filter.SelectedItem is ComboBoxItem cbi && cbi.Tag is string tag)
            {
                if (Enum.TryParse<VectorStoreLinkFilter>(tag, ignoreCase: true, out var parsed))
                    return parsed;
            }
            return VectorStoreLinkFilter.All;
        }

        // Applies ComboBox selection for filter based on enum value
        private void SetFilterSelection(VectorStoreLinkFilter filter)
        {
            foreach (var obj in Filter.Items.OfType<ComboBoxItem>())
            {
                if ((obj.Tag as string)?.Equals(filter.ToString(), StringComparison.OrdinalIgnoreCase) == true)
                {
                    obj.IsSelected = true;
                    return;
                }
            }
            // Default to All if not found
            if (Filter.Items.OfType<ComboBoxItem>().FirstOrDefault() is { } first)
            {
                first.IsSelected = true;
            }
        }

        // Gets currently selected specific store id (or null)
        private string? GetSelectedStoreId()
        {
            return SpecificStore.SelectedItem as string;
        }

        // Populate SpecificStore list, optionally select a given id
        private void RefreshStoreIds(string? selectStoreId)
        {
            storeIds.Clear();

            // Placeholder: load known store ids; in production, populate via service
            foreach (var id in LoadKnownStoreIds())
            {
                storeIds.Add(id);
            }

            SpecificStore.ItemsSource = storeIds;

            if (!string.IsNullOrWhiteSpace(selectStoreId) && storeIds.Contains(selectStoreId))
            {
                SpecificStore.SelectedItem = selectStoreId;
            }
            else
            {
                SpecificStore.SelectedIndex = storeIds.Count > 0 ? 0 : -1;
            }
        }

        // Rebinds file items based on filter and store
        private void BindItems(VectorStoreLinkFilter filter, string? storeId)
        {
            items.Clear();

            foreach (var vm in LoadRecentFiles(filter, storeId))
            {
                items.Add(vm);
            }

            Files.ItemsSource = items;
        }

        // Loads recent file view models (placeholder; replace with service when wired)
        private IEnumerable<RecentFileItem> LoadRecentFiles(VectorStoreLinkFilter filter, string? storeId)
        {
            // TODO: Replace with IRecentFilesService.GetRecentFiles(filter, storeId) mapping when DI is wired
            // For now, return an empty list to keep the page functional
            yield break;
        }

        // Loads known store ids (placeholder; replace with service)
        private IEnumerable<string> LoadKnownStoreIds()
        {
            // TODO: Replace with a service call returning known vector store ids
            return Enumerable.Empty<string>();
        }

        // Generic DI resolver that does not throw
        private static T? TryResolve<T>() where T : class
        {
            try
            {
                var app = Application.Current as App;
                var sp = app?.GetType().GetProperty("Services")?.GetValue(app) as IServiceProvider;
                return sp?.GetService<T>();
            }
            catch
            {
                return null;
            }
        }
    }

    // UI projection model used by the XAML DataTemplate
    public sealed class RecentFileItem
    {
        public string FileName { get; }
        public string Path { get; }
        public string? LinkedStoreName { get; }

        public RecentFileItem(string path, string? linkedStoreName)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            FileName = System.IO.Path.GetFileName(path);
            LinkedStoreName = linkedStoreName;
        }
    }

    // Minimal in-memory settings store to ensure the page never crashes if DI is not yet wired
    internal sealed class InMemorySettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, string> _data = new(StringComparer.OrdinalIgnoreCase);

        public string? Get(string key) => _data.TryGetValue(key, out var v) ? v : null;

        public void Set(string key, string value) => _data[key] = value ?? string.Empty;
    }

    // Extension methods for UiStateConfig keys utilized by this page
    internal static class UiStateConfigExtensions
    {
        public static string? GetRecentFilesSpecificStoreId(this UiStateConfig ui) => ui.GetString("ui.recentFiles.storeId");

        public static void SetRecentFilesSpecificStoreId(this UiStateConfig ui, string? storeId) => ui.SetString("ui.recentFiles.storeId", storeId ?? string.Empty);

        public static void SetRecentFilesLastSelection(this UiStateConfig ui, string path) => ui.SetString("ui.recentFiles.lastSelection", path);

        // Generic helpers to bridge simple string values; real UiStateConfig has richer APIs
        public static string? GetString(this UiStateConfig ui, string key) => ui.GetType().GetMethod("GetString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.Invoke(ui, new object[] { key }) as string;

        public static void SetString(this UiStateConfig ui, string key, string value) => ui.GetType().GetMethod("SetString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.Invoke(ui, new object[] { key, value });
    }
}
