// File: Configuration/PerVectorStoreSettings.cs
// Description: Per-vector-store exclusion settings with inheritance from global app.config.
//              Shared between WinForms (VecTool.UI) and WinUI (VecTool.UI.WinUI).
//              Preserves JSON shape owned by VectorStoreConfig.

using System;
using System.Collections.Generic;
using System.Linq;

namespace VecTool.Configuration
{
    /// <summary>
    /// View model for per-vector-store exclusion settings with inheritance logic.
    /// Handles merging of global defaults (from app.config) with per-store overrides.
    /// </summary>
    public sealed class PerVectorStoreSettings
    {
        #region Properties

        public string Name { get; }
        public bool UseCustomExcludedFiles { get; }
        public bool UseCustomExcludedFolders { get; }
        public List<string> CustomExcludedFiles { get; }
        public List<string> CustomExcludedFolders { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Public constructor matching WinForms/WinUI call sites (5 args).
        /// </summary>
        /// <param name="name">Vector store name (required, non-empty).</param>
        /// <param name="useCustomExcludedFiles">True if using custom files (not inherited).</param>
        /// <param name="useCustomExcludedFolders">True if using custom folders (not inherited).</param>
        /// <param name="customExcludedFiles">Custom excluded file patterns.</param>
        /// <param name="customExcludedFolders">Custom excluded folder patterns.</param>
        /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
        public PerVectorStoreSettings(
            string name,
            bool useCustomExcludedFiles,
            bool useCustomExcludedFolders,
            IEnumerable<string>? customExcludedFiles,
            IEnumerable<string>? customExcludedFolders)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Vector store name is required.", nameof(name));

            Name = name;
            UseCustomExcludedFiles = useCustomExcludedFiles;
            UseCustomExcludedFolders = useCustomExcludedFolders;

            CustomExcludedFiles = (customExcludedFiles ?? Enumerable.Empty<string>())
                .Select(Normalize)
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            CustomExcludedFolders = (customExcludedFolders ?? Enumerable.Empty<string>())
                .Select(Normalize)
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Factory method: merges global defaults with per-store config.
        /// Determines whether settings differ from global (= custom) or inherit.
        /// </summary>
        /// <param name="name">Vector store name.</param>
        /// <param name="global">Global defaults from app.config.</param>
        /// <param name="perOrNull">Per-store config from JSON, or null if no custom config exists.</param>
        /// <returns>View model with resolved inheritance flags and effective settings.</returns>
        public static PerVectorStoreSettings From(
            string name,
            VectorStoreConfig global,
            VectorStoreConfig? perOrNull)
        {
            var globalFiles = (global?.ExcludedFiles ?? new List<string>())
                .Select(Normalize).ToList();
            var globalFolders = (global?.ExcludedFolders ?? new List<string>())
                .Select(Normalize).ToList();

            var perFiles = (perOrNull?.ExcludedFiles ?? new List<string>())
                .Select(Normalize).ToList();
            var perFolders = (perOrNull?.ExcludedFolders ?? new List<string>())
                .Select(Normalize).ToList();

            bool filesDiffer = !SequenceEqualIgnoreOrder(perFiles, globalFiles);
            bool foldersDiffer = !SequenceEqualIgnoreOrder(perFolders, globalFolders);

            return new PerVectorStoreSettings(
                name,
                useCustomExcludedFiles: filesDiffer,
                useCustomExcludedFolders: foldersDiffer,
                customExcludedFiles: filesDiffer ? perFiles : globalFiles,
                customExcludedFolders: foldersDiffer ? perFolders : globalFolders);
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Converts view model to effective VectorStoreConfig for persistence.
        /// Merges custom settings with existing per-store config (preserves FolderPaths).
        /// </summary>
        /// <param name="global">Global defaults from app.config.</param>
        /// <param name="existingPerOrNull">Existing per-store config, or null if new.</param>
        /// <returns>VectorStoreConfig ready for JSON serialization.</returns>
        public VectorStoreConfig ToEffectiveVectorStoreConfig(
            VectorStoreConfig global,
            VectorStoreConfig? existingPerOrNull)
        {
            var basePer = existingPerOrNull?.Clone() ?? new VectorStoreConfig();

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
            PerVectorStoreSettings settings,
            VectorStoreConfig global)
        {
            if (all is null)
                throw new ArgumentNullException(nameof(all));
            if (settings is null)
                throw new ArgumentNullException(nameof(settings));
            if (global is null)
                throw new ArgumentNullException(nameof(global));

            all[settings.Name] = settings.ToEffectiveVectorStoreConfig(
                global,
                all.TryGetValue(settings.Name, out var ex) ? ex : null);
        }

        #endregion

        #region Helpers

        private static string Normalize(string s) => (s ?? string.Empty).Trim();

        /// <summary>
        /// Compares two lists ignoring order and whitespace differences.
        /// </summary>
        private static bool SequenceEqualIgnoreOrder(List<string> a, List<string> b)
        {
            var aa = (a?.Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(Normalize)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList()) ?? new List<string>();

            var bb = (b?.Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(Normalize)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList()) ?? new List<string>();

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
