// Path: Configuration/UiStateConfig.cs
#nullable enable

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.Json;
using VecTool.Core.RecentFiles;

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
