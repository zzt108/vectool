// Path: Configuration/UiStateConfig.cs
#nullable enable

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.Json;
using VecTool.Core.RecentFiles;
using static System.Formats.Asn1.AsnWriter;

namespace VecTool.Configuration
{
    /// <summary>
    /// Persists simple UI state such as the last selected vector store name and Recent Files layout.
    /// Stored as JSON next to vectorStoreFolders.json if configured, otherwise under the app "Generated" folder.
    /// Also exposes legacy Recent Files getters/setters backed by an ISettingsStore for tests/in-memory usage.
    /// </summary>
    public sealed class UiStateConfig
    {
        // Legacy per-key settings for Recent Files filter/specific store/last selection.
        private const string KEY_RECENT_FILTER = "ui.recentFiles.filter";
        private const string KEY_RECENT_STORE = "ui.recentFiles.storeId";
        private const string KEY_RECENT_LAST = "ui.recentFiles.lastSelection";

        private readonly ISettingsStore _store;

        /// <summary>
        /// Construct with an <see cref="ISettingsStore"/> implementation, typically an app-level or in-memory store.
        /// </summary>
        public UiStateConfig(ISettingsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// Get the persisted Recent Files filter; defaults to <see cref="VectorStoreLinkFilter.All"/>.
        /// </summary>
        public VectorStoreLinkFilter GetRecentFilesFilter()
        {
            var raw = _store.Get(KEY_RECENT_FILTER);
            return Enum.TryParse<VectorStoreLinkFilter>(raw, out var parsed) ? parsed : VectorStoreLinkFilter.All;
        }

        /// <summary>
        /// Persist the Recent Files filter.
        /// </summary>
        public void SetRecentFilesFilter(VectorStoreLinkFilter filter)
        {
            _store.Set(KEY_RECENT_FILTER, filter.ToString());
        }

        /// <summary>
        /// Get the specific store id used by the Recent Files filter, if any.
        /// </summary>
        public string? GetRecentFilesSpecificStoreId()
        {
            return _store.Get(KEY_RECENT_STORE);
        }

        /// <summary>
        /// Persist the specific store id used by the Recent Files filter, or null/empty to clear.
        /// </summary>
        public void SetRecentFilesSpecificStoreId(string? storeId)
        {
            _store.Set(KEY_RECENT_STORE, string.IsNullOrWhiteSpace(storeId) ? null : storeId);
        }

        /// <summary>
        /// Get the last selected Recent File path, if any.
        /// </summary>
        public string? GetLastSelectedRecentFilePath()
        {
            return _store.Get(KEY_RECENT_LAST);
        }

        /// <summary>
        /// Persist the last selected Recent File path, or null/empty to clear.
        /// </summary>
        public void SetLastSelectedRecentFilePath(string? path)
        {
            _store.Set(KEY_RECENT_LAST, string.IsNullOrWhiteSpace(path) ? null : path);
        }

        // File: Configuration/UiStateConfig.cs
        // ADDITIONS: Main tab vector store management methods (extend existing class)
        // Location: Add these methods to the existing UiStateConfig class after the Recent Files methods

        #region Main Tab - Vector Store Management

        /// <summary>
        /// Get list of all known vector store names from vectorStoreFolders.json.
        /// Returns empty list if file doesn't exist or can't be loaded.
        /// </summary>
        public List<string> GetVectorStores()
        {
            try
            {
                var all = VectorStoreConfig.LoadAll();
                return all?.Keys
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();
            }
            catch
            {
                // Defensive: never crash UI on load failures
                return new List<string>();
            }
        }

        /// <summary>
        /// Get the last selected vector store name from persisted UI state.
        /// Returns null if no selection was persisted.
        /// </summary>
        public string? GetSelectedVectorStore()
        {
            return _store.Get("ui.main.selectedVectorStore");
        }

        /// <summary>
        /// Persist the selected vector store name to UI state.
        /// Pass null or empty to clear the selection.
        /// </summary>
        public void SetSelectedVectorStore(string? storeName)
        {
            _store.Set("ui.main.selectedVectorStore",
                string.IsNullOrWhiteSpace(storeName) ? null : storeName);
        }

        /// <summary>
        /// Get the folder paths associated with a specific vector store.
        /// Returns empty list if the store doesn't exist or has no folders.
        /// </summary>
        /// <param name="storeName">Vector store name (case-sensitive).</param>
        public List<string> GetVectorStoreFolders(string storeName)
        {
            if (string.IsNullOrWhiteSpace(storeName))
                return new List<string>();

            try
            {
                var all = VectorStoreConfig.LoadAll();
                if (all.TryGetValue(storeName, out var config))
                    return config.FolderPaths?.ToList() ?? new List<string>();

                return new List<string>();
            }
            catch
            {
                // Defensive: return empty on errors
                return new List<string>();
            }
        }

        /// <summary>
        /// Add a new vector store with global defaults from app.config.
        /// Persists immediately to vectorStoreFolders.json.
        /// </summary>
        /// <param name="storeName">New vector store name (must be unique).</param>
        /// <returns>True if created successfully, false if name already exists or save failed.</returns>
        public bool AddVectorStore(string storeName)
        {
            if (string.IsNullOrWhiteSpace(storeName))
                return false;

            try
            {
                var all = VectorStoreConfig.LoadAll();

                // Check for duplicate (case-insensitive)
                if (all.Keys.Any(k => string.Equals(k, storeName, StringComparison.OrdinalIgnoreCase)))
                    return false;

                // Create new config from global defaults
                var newConfig = VectorStoreConfig.FromAppConfig();
                newConfig.FolderPaths = new List<string>(); // Empty folder list for new stores

                all[storeName] = newConfig;
                VectorStoreConfig.SaveAll(all);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Add a folder path to a specific vector store's folder list.
        /// Creates the store if it doesn't exist.
        /// Persists immediately to vectorStoreFolders.json.
        /// </summary>
        /// <param name="storeName">Vector store name.</param>
        /// <param name="folderPath">Folder path to add (must exist).</param>
        /// <returns>True if added successfully, false if duplicate or save failed.</returns>
        public bool AddFolderToVectorStore(string storeName, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(storeName) || string.IsNullOrWhiteSpace(folderPath))
                return false;

            try
            {
                var all = VectorStoreConfig.LoadAll();

                // Get or create config
                if (!all.TryGetValue(storeName, out var config))
                {
                    config = VectorStoreConfig.FromAppConfig();
                    config.FolderPaths = new List<string>();
                    all[storeName] = config;
                }

                // Avoid duplicates (case-insensitive)
                if (config.FolderPaths?.Any(f =>
                    string.Equals(f, folderPath, StringComparison.OrdinalIgnoreCase)) == true)
                    return false;

                config.FolderPaths ??= new List<string>();
                config.FolderPaths.Add(folderPath);

                VectorStoreConfig.SaveAll(all);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Remove a folder path from a specific vector store's folder list.
        /// Persists immediately to vectorStoreFolders.json.
        /// </summary>
        /// <param name="storeName">Vector store name.</param>
        /// <param name="folderPath">Folder path to remove.</param>
        /// <returns>True if removed successfully, false if not found or save failed.</returns>
        public bool RemoveFolderFromVectorStore(string storeName, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(storeName) || string.IsNullOrWhiteSpace(folderPath))
                return false;

            try
            {
                var all = VectorStoreConfig.LoadAll();
                if (!all.TryGetValue(storeName, out var config))
                    return false;

                if (config.FolderPaths == null || config.FolderPaths.Count == 0)
                    return false;

                // Case-insensitive removal
                var removed = config.FolderPaths.RemoveAll(f =>
                    string.Equals(f, folderPath, StringComparison.OrdinalIgnoreCase));

                if (removed > 0)
                {
                    VectorStoreConfig.SaveAll(all);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Update the folder list for a specific vector store (replaces existing list).
        /// Persists immediately to vectorStoreFolders.json.
        /// </summary>
        /// <param name="storeName">Vector store name.</param>
        /// <param name="folders">New folder list (replaces existing).</param>
        /// <returns>True if saved successfully, false otherwise.</returns>
        public bool SetVectorStoreFolders(string storeName, IEnumerable<string> folders)
        {
            if (string.IsNullOrWhiteSpace(storeName))
                return false;

            try
            {
                var all = VectorStoreConfig.LoadAll();

                if (!all.TryGetValue(storeName, out var config))
                {
                    config = VectorStoreConfig.FromAppConfig();
                    all[storeName] = config;
                }

                config.FolderPaths = (folders ?? Enumerable.Empty<string>())
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                VectorStoreConfig.SaveAll(all);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion


        /// <summary>
        /// JSON-backed UI state for Recent Files grid layout.
        /// </summary>
        public sealed class UiState
        {
            /// <summary>
            /// Map from column header text to width in pixels.
            /// </summary>
            public Dictionary<string, int> RecentFilesColumnWidths { get; set; } =
                new Dictionary<string, int>(StringComparer.Ordinal);

            /// <summary>
            /// Optional row-height scale to apply on load; null means use default scale.
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
            if (state is null) throw new ArgumentNullException(nameof(state));

            try
            {
                var path = ResolveUiStatePath(directory);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(path, json);
            }
            catch
            {
                // Defensive: swallow to avoid UI disruption during resize drags; consider logging via LogCtx if desired.
            }
        }

        private static string ResolveUiStatePath(string? directory)
        {
            string baseDir = directory ?? InferConfigDirectory() ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");

            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

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
                    {
                        return dir!;
                    }
                }
                catch
                {
                    // Ignore and use default.
                }
            }

            return null;
        }
    }
}
