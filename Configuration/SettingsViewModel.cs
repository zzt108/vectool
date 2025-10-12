using System;
using System.Collections.Generic;
using System.Linq;

namespace VecTool.Configuration
{
    /// <summary>
    /// Testable ViewModel for Settings tab exclusion logic.
    /// Decoupled from WinUI controls to enable unit testing.
    /// Replaces direct TextBox/CheckBox instantiation in tests.
    /// </summary>
    public sealed class SettingsViewModel
    {
        #region Properties

        /// <summary>
        /// The name of the vector store being configured.
        /// </summary>
        public string VectorStoreName { get; set; } = string.Empty;

        /// <summary>
        /// If true, the store uses custom excluded files (not inherited from global).
        /// </summary>
        public bool UseCustomExcludedFiles { get; set; }

        /// <summary>
        /// If true, the store uses custom excluded folders (not inherited from global).
        /// </summary>
        public bool UseCustomExcludedFolders { get; set; }

        /// <summary>
        /// Effective list of excluded file patterns for this store.
        /// May be custom or inherited from global config.
        /// </summary>
        public List<string> CustomExcludedFiles { get; set; } = new();

        /// <summary>
        /// Effective list of excluded folder patterns for this store.
        /// May be custom or inherited from global config.
        /// </summary>
        public List<string> CustomExcludedFolders { get; set; } = new();

        #endregion

        #region Factory Methods

        /// <summary>
        /// Factory: Load effective settings for a vector store.
        /// Determines whether settings differ from global (custom) or are inherited.
        /// </summary>
        /// <param name="storeName">Vector store name.</param>
        /// <param name="global">Global defaults from app.config.</param>
        /// <param name="perStore">Per-store config from JSON, or null if no custom config exists.</param>
        /// <returns>View model with resolved inheritance flags and effective settings.</returns>
        public static SettingsViewModel Load(string storeName, VectorStoreConfig global, VectorStoreConfig? perStore)
        {
            if (string.IsNullOrWhiteSpace(storeName))
                throw new ArgumentException("Store name is required.", nameof(storeName));
            if (global == null)
                throw new ArgumentNullException(nameof(global));

            var vm = new SettingsViewModel { VectorStoreName = storeName };

            // Files: Check if per-store differs from global
            if (perStore == null || AreEqual(global.ExcludedFiles, perStore.ExcludedFiles))
            {
                vm.UseCustomExcludedFiles = false;
                vm.CustomExcludedFiles = new List<string>(global.ExcludedFiles);
            }
            else
            {
                vm.UseCustomExcludedFiles = true;
                vm.CustomExcludedFiles = new List<string>(perStore.ExcludedFiles);
            }

            // Folders: Check if per-store differs from global
            if (perStore == null || AreEqual(global.ExcludedFolders, perStore.ExcludedFolders))
            {
                vm.UseCustomExcludedFolders = false;
                vm.CustomExcludedFolders = new List<string>(global.ExcludedFolders);
            }
            else
            {
                vm.UseCustomExcludedFolders = true;
                vm.CustomExcludedFolders = new List<string>(perStore.ExcludedFolders);
            }

            return vm;
        }

        #endregion

        #region Text Parsing

        /// <summary>
        /// Parse multiline text into a list of trimmed, non-empty lines.
        /// Handles both Windows (\r\n) and Unix (\n) line endings.
        /// Removes duplicates (case-insensitive).
        /// </summary>
        /// <param name="text">Multi-line input text.</param>
        /// <returns>List of distinct, trimmed non-empty lines.</returns>
        public static List<string> ParseMultilineText(string text)
        {
            return (text ?? string.Empty)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Converts view model to effective VectorStoreConfig for persistence.
        /// Merges custom settings with existing per-store config; preserves FolderPaths.
        /// </summary>
        /// <param name="global">Global defaults from app.config.</param>
        /// <param name="existingPerOrNull">Existing per-store config, or null if new.</param>
        /// <returns>VectorStoreConfig ready for JSON serialization.</returns>
        public VectorStoreConfig ToEffectiveVectorStoreConfig(VectorStoreConfig global, VectorStoreConfig? existingPerOrNull)
        {
            if (global == null)
                throw new ArgumentNullException(nameof(global));

            var basePer = existingPerOrNull?.Clone() ?? new VectorStoreConfig();

            // Apply custom or inherited exclusions
            basePer.ExcludedFiles = UseCustomExcludedFiles
                ? CustomExcludedFiles
                : global.ExcludedFiles.Select(Normalize).ToList();

            basePer.ExcludedFolders = UseCustomExcludedFolders
                ? CustomExcludedFolders
                : global.ExcludedFolders.Select(Normalize).ToList();

            // Preserve existing folder paths (not part of Settings tab)
            basePer.FolderPaths = existingPerOrNull?.FolderPaths?.ToList()
                ?? basePer.FolderPaths
                ?? new List<string>();

            return basePer;
        }

        /// <summary>
        /// Saves settings to the in-memory dictionary of vector store configs.
        /// Call VectorStoreConfig.SaveAll(all) afterward to persist to JSON.
        /// </summary>
        /// <param name="all">Dictionary of all vector store configs (modified in place).</param>
        /// <param name="settings">Settings to save.</param>
        /// <param name="global">Global defaults for fallback.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public static void Save(
            Dictionary<string, VectorStoreConfig> all,
            SettingsViewModel settings,
            VectorStoreConfig global)
        {
            if (all == null)
                throw new ArgumentNullException(nameof(all));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (global == null)
                throw new ArgumentNullException(nameof(global));

            all[settings.VectorStoreName] = settings.ToEffectiveVectorStoreConfig(
                global,
                all.TryGetValue(settings.VectorStoreName, out var ex) ? ex : null);
        }

        #endregion

        #region Helpers

        private static string Normalize(string s) => (s ?? string.Empty).Trim();

        /// <summary>
        /// Compares two lists ignoring order and whitespace differences.
        /// Case-insensitive comparison.
        /// </summary>
        private static bool AreEqual(List<string> a, List<string> b)
        {
            var aa = a?.Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(Normalize)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            var bb = b?.Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(Normalize)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (aa.Count != bb.Count)
                return false;

            for (int i = 0; i < aa.Count; i++)
            {
                if (!string.Equals(aa[i], bb[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        #endregion
    }
}
