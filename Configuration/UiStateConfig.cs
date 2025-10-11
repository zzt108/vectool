// Description: Persists simple UI state (last selected vector store, Recent Files layout) 
// as JSON next to vectorStoreFolders.json if configured, otherwise under Generated.
// Also exposes legacy Recent Files getters/setters backed by ISettingsStore.
// Now includes vector-store APIs for Main tab (Phase 2.1).

#nullable enable

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;
using NLog;
using VecTool.Core.RecentFiles;

namespace VecTool.Configuration
{
    /// <summary>
    /// Persists simple UI state such as the last selected vector store name and Recent Files layout.
    /// Stored as JSON next to vectorStoreFolders.json if configured, otherwise under the app Generated folder.
    /// Also exposes legacy Recent Files getters/setters backed by an <see cref="ISettingsStore"/> for tests/in-memory usage.
    /// </summary>
    public sealed class UiStateConfig
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #region Legacy per-key settings for Recent Files (filter/specific store/last selection)

        private const string KEY_RECENT_FILTER = "ui.recentFiles.filter";
        private const string KEY_RECENT_STORE = "ui.recentFiles.storeId";
        private const string KEY_RECENT_LAST = "ui.recentFiles.lastSelection";

        // NEW: persist last selected VS name in ISettingsStore
        private const string KEY_SELECTED_VECTORSTORE = "ui.main.selectedVectorStore";

        private readonly ISettingsStore store;

        /// <summary>
        /// Construct with an <see cref="ISettingsStore"/> implementation, typically an app-level or in-memory store.
        /// </summary>
        public UiStateConfig(ISettingsStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        #endregion

        #region Recent Files - ISettingsStore-backed properties

        /// <summary>
        /// Get the persisted Recent Files filter (defaults to <see cref="VectorStoreLinkFilter.All"/>).
        /// </summary>
        public VectorStoreLinkFilter GetRecentFilesFilter()
        {
            var raw = store.Get(KEY_RECENT_FILTER);
            return Enum.TryParse<VectorStoreLinkFilter>(raw, out var parsed)
                ? parsed
                : VectorStoreLinkFilter.All;
        }

        /// <summary>
        /// Persist the Recent Files filter.
        /// </summary>
        public void SetRecentFilesFilter(VectorStoreLinkFilter filter)
        {
            store.Set(KEY_RECENT_FILTER, filter.ToString());
        }

        /// <summary>
        /// Get the specific store id used by the Recent Files filter, if any.
        /// </summary>
        public string? GetRecentFilesSpecificStoreId()
        {
            return store.Get(KEY_RECENT_STORE);
        }

        /// <summary>
        /// Persist the specific store id used by the Recent Files filter, or null/empty to clear.
        /// </summary>
        public void SetRecentFilesSpecificStoreId(string? storeId)
        {
            store.Set(KEY_RECENT_STORE, string.IsNullOrWhiteSpace(storeId) ? null : storeId);
        }

        /// <summary>
        /// Get the last selected Recent File path, if any.
        /// </summary>
        public string? GetLastSelectedRecentFilePath()
        {
            return store.Get(KEY_RECENT_LAST);
        }

        /// <summary>
        /// Persist the last selected Recent File path, or null/empty to clear.
        /// </summary>
        public void SetLastSelectedRecentFilePath(string? path)
        {
            store.Set(KEY_RECENT_LAST, string.IsNullOrWhiteSpace(path) ? null : path);
        }

        #endregion

        #region Vector Store APIs (Phase 2.1) - Main tab persistence

        /// <summary>
        /// Simple app-level factory (Phase 2.1). For now, uses an in-memory settings store 
        /// – can be replaced with an app-level store later.
        /// </summary>
        public static UiStateConfig FromAppConfig()
        {
            // For now, use an in-memory settings store – can be replaced with an app-level store later
            return new UiStateConfig(new InMemorySettingsStore());
        }

        /// <summary>
        /// List available vector stores (keys of JSON config).
        /// </summary>
        public List<string> GetVectorStores()
        {
            try
            {
                var all = VectorStoreConfig.LoadAll();
                var stores = all.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

                Log.Debug("GetVectorStores returned {Count} stores", stores.Count);
                return stores;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load vector stores");
                return new List<string>();
            }
        }

        /// <summary>
        /// Read persisted selection from ISettingsStore.
        /// </summary>
        public string? GetSelectedVectorStore()
        {
            try
            {
                var selected = store.Get(KEY_SELECTED_VECTORSTORE);
                Log.Debug("GetSelectedVectorStore returned: {Name}", selected ?? "(none)");
                return selected;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get selected vector store");
                return null;
            }
        }

        /// <summary>
        /// Set persisted selection in ISettingsStore.
        /// </summary>
        public void SetSelectedVectorStore(string storeName)
        {
            if (string.IsNullOrWhiteSpace(storeName))
            {
                Log.Warn("Attempted to set empty vector store name");
                return;
            }

            try
            {
                store.Set(KEY_SELECTED_VECTORSTORE, storeName);
                Log.Info("Vector store selection persisted", new { Store = storeName });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to persist selected vector store: {Name}", storeName);
            }
        }

        /// <summary>
        /// Read folders for a store from VectorStoreConfig JSON.
        /// </summary>
        public List<string> GetVectorStoreFolders(string storeName)
        {
            try
            {
                var all = VectorStoreConfig.LoadAll();

                if (all.TryGetValue(storeName, out var cfg))
                {
                    var folders = cfg.FolderPaths?.ToList() ?? new List<string>();
                    Log.Debug("GetVectorStoreFolders({Store}) returned {Count} folders", storeName, folders.Count);
                    return folders;
                }

                Log.Warn("Vector store not found: {Store}", storeName);
                return new List<string>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load folders for store: {Store}", storeName);
                return new List<string>();
            }
        }

        /// <summary>
        /// Add a new store if missing, then save to JSON.
        /// </summary>
        public void AddVectorStore(string storeName)
        {
            if (string.IsNullOrWhiteSpace(storeName))
            {
                Log.Warn("Attempted to add vector store with empty name");
                return;
            }

            try
            {
                var all = VectorStoreConfig.LoadAll();

                if (!all.ContainsKey(storeName))
                {
                    all[storeName] = new VectorStoreConfig();
                    VectorStoreConfig.SaveAll(all);
                    Log.Info("Vector store added to config", new { Store = storeName });
                }
                else
                {
                    Log.Debug("Vector store already exists: {Store}", storeName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add vector store: {Store}", storeName);
            }
        }

        /// <summary>
        /// Add folder to a store and save to JSON.
        /// </summary>
        public void AddFolderToVectorStore(string storeName, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(storeName) || string.IsNullOrWhiteSpace(folderPath))
            {
                Log.Warn("Attempted to add folder with empty store or path");
                return;
            }

            try
            {
                var all = VectorStoreConfig.LoadAll();

                if (!all.TryGetValue(storeName, out var cfg))
                {
                    cfg = new VectorStoreConfig();
                    all[storeName] = cfg;
                    Log.Debug("Created new vector store config for: {Store}", storeName);
                }

                if (cfg.AddFolderPath(folderPath))
                {
                    VectorStoreConfig.SaveAll(all);
                    Log.Info("Folder added to vector store", new { Store = storeName, Path = folderPath });
                }
                else
                {
                    Log.Debug("Folder already exists in store: {Path}", folderPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add folder to store: {Store}, {Path}", storeName, folderPath);
            }
        }

        #endregion

        #region JSON-backed UI state for Recent Files grid layout

        /// <summary>
        /// JSON-backed UI state for Recent Files grid layout.
        /// </summary>
        public sealed class UiState
        {
            /// <summary>
            /// Map from column header text to width (in pixels).
            /// </summary>
            public Dictionary<string, int> RecentFilesColumnWidths { get; set; }
                = new Dictionary<string, int>(StringComparer.Ordinal);

            /// <summary>
            /// Optional row-height scale to apply on load (null means use default scale).
            /// </summary>
            public double? RecentFilesRowHeightScale { get; set; }
        }

        /// <summary>
        /// Load UI state from uiState.json in the resolved directory.
        /// </summary>
        public static UiState Load(string? directory = null)
        {
            try
            {
                var path = ResolveUiStatePath(directory);
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var state = JsonSerializer.Deserialize<UiState>(json);
                    return state ?? new UiState();
                }

                return new UiState();
            }
            catch
            {
                // Defensive: never crash UI on corrupt/missing file, return defaults.
                return new UiState();
            }
        }

        /// <summary>
        /// Save UI state to uiState.json in the resolved directory.
        /// </summary>
        public static void Save(UiState state, string? directory = null)
        {
            if (state is null)
                throw new ArgumentNullException(nameof(state));

            try
            {
                var path = ResolveUiStatePath(directory);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // Defensive: swallow to avoid UI disruption during resizes/drags 
                // (consider logging via LogCtx if desired).
            }
        }

        private static string ResolveUiStatePath(string? directory)
        {
            string baseDir = directory ?? InferConfigDirectory()
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");

            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);

            return Path.Combine(baseDir, "uiState.json");
        }

        private static string? InferConfigDirectory()
        {
            // If vectorStoreFoldersPath is configured, place uiState.json alongside it.
            var configured = ConfigurationManager.AppSettings["vectorStoreFoldersPath"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                try
                {
                    var dir = Path.GetDirectoryName(configured);
                    if (!string.IsNullOrWhiteSpace(dir))
                        return dir!;
                }
                catch
                {
                    // Ignore and use default.
                }
            }

            return null;
        }

        #endregion
    }
}
