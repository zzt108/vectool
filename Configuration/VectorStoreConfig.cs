using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NLog;

namespace VecTool.Configuration
{
    /// <summary>
    /// Holds global and per-vector-store configuration.
    /// Provides loading from appSettings and JSON, with safe defaults.
    /// </summary>
    public sealed class VectorStoreConfig
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        #region Properties

        /// <summary>
        /// List of source code folder paths to be processed.
        /// </summary>
        public List<string> FolderPaths { get; set; } = new();

        /// <summary>
        /// File extensions/patterns to exclude, e.g., ".bin", "*.dll".
        /// Inherited from global config unless overridden.
        /// </summary>
        public List<string> ExcludedFiles { get; set; } = new();

        /// <summary>
        /// Folder names to exclude, e.g., "bin", "obj".
        /// Inherited from global config unless overridden.
        /// </summary>
        public List<string> ExcludedFolders { get; set; } = new();

        #endregion

        #region Defaults

        /// <summary>
        /// Opinionated default file exclusions used when appSettings are missing.
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultExcludedFiles = new[] { ".bin", ".exe", ".dll", ".pdb", "*.log" };

        /// <summary>
        /// Opinionated default folder exclusions used when appSettings are missing.
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultExcludedFolders = new[] { "bin", "obj", ".git", ".vs", "packages" };

        #endregion

        #region Static Factory and Persistence

        /// <summary>
        /// Creates a VectorStoreConfig from global app.config settings.
        /// Falls back to sensible defaults if appSettings keys are missing or empty.
        /// </summary>
        public static VectorStoreConfig FromAppConfig()
        {
            var config = new VectorStoreConfig();

            var files = ReadListFromAppSettings("excludedFiles");
            var folders = ReadListFromAppSettings("excludedFolders");

            if (files.Count == 0)
            {
                config.ExcludedFiles = new List<string>(DefaultExcludedFiles);
                Log.Info("AppSetting 'excludedFiles' was missing or empty. Using default values: {DefaultValues}", string.Join(", ", DefaultExcludedFiles));
            }
            else
            {
                config.ExcludedFiles = files;
            }

            if (folders.Count == 0)
            {
                config.ExcludedFolders = new List<string>(DefaultExcludedFolders);
                Log.Info("AppSetting 'excludedFolders' was missing or empty. Using default values: {DefaultValues}", string.Join(", ", DefaultExcludedFolders));
            }
            else
            {
                config.ExcludedFolders = folders;
            }

            return config;
        }

        /// <summary>
        /// Helper to read and parse a delimited list from appSettings.
        /// </summary>
        private static List<string> ReadListFromAppSettings(string key)
        {
            try
            {
                var rawValue = ConfigurationManager.AppSettings[key];
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    return new List<string>();
                }

                return rawValue
                    .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (ConfigurationErrorsException ex)
            {
                Log.Warn(ex, "Failed to read appSetting '{Key}'. Returning empty list.", key);
                return new List<string>();
            }
        }

        #endregion

        #region Exclusion Logic

        /// <summary>
        /// Checks if a file should be excluded based on wildcard patterns.
        /// </summary>
        public bool IsFileExcluded(string fileName)
        {
            foreach (var pattern in ExcludedFiles)
            {
                // Simple wildcard to regex conversion
                string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                {
                    Log.Trace("File '{FileName}' excluded by pattern '{Pattern}'", fileName, pattern);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a folder should be excluded by an exact name match.
        /// </summary>
        public bool IsFolderExcluded(string folderName)
        {
            bool isExcluded = ExcludedFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase);
            if (isExcluded)
            {
                Log.Trace("Folder '{FolderName}' is in the excluded list", folderName);
            }
            return isExcluded;
        }

        #endregion

        #region Instance Methods

        /// <summary>
        /// Creates a deep copy of this configuration instance.
        /// </summary>
        public VectorStoreConfig Clone()
        {
            return new VectorStoreConfig
            {
                FolderPaths = new List<string>(FolderPaths),
                ExcludedFiles = new List<string>(ExcludedFiles),
                ExcludedFolders = new List<string>(ExcludedFolders)
            };
        }

        #endregion

        #region JSON Persistence (for Per-Store Configs)

        /// <summary>
        /// Loads all vector store configurations from the JSON file specified in app.config.
        /// </summary>
        public static Dictionary<string, VectorStoreConfig> LoadAll(string? configPath = null)
        {
            string vectorStoreFoldersPath = configPath ??
                ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ??
                @"..\..\vectorStoreFolders.json";

            if (!File.Exists(vectorStoreFoldersPath))
            {
                return new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                string json = File.ReadAllText(vectorStoreFoldersPath);
                var configs = JsonSerializer.Deserialize<Dictionary<string, VectorStoreConfig>>(json);

                var validConfigs = configs ?? new Dictionary<string, VectorStoreConfig>();
                Log.Debug("Loaded {Count} vector store configurations from {Path}", validConfigs.Count, vectorStoreFoldersPath);

                // Ensure dictionary uses case-insensitive comparer
                return new Dictionary<string, VectorStoreConfig>(validConfigs, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading vector store configurations from {Path}", vectorStoreFoldersPath);
                return new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Saves all vector store configurations to the JSON file specified in app.config.
        /// </summary>
        public static void SaveAll(Dictionary<string, VectorStoreConfig> configs, string? configPath = null)
        {
            string vectorStoreFoldersPath = configPath ??
                ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ??
                @"..\..\vectorStoreFolders.json";

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(configs, options);
                File.WriteAllText(vectorStoreFoldersPath, json);

                Log.Debug("Saved {Count} vector store configurations to {Path}", configs.Count, vectorStoreFoldersPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving vector store configurations to {Path}", vectorStoreFoldersPath);
            }
        }

        #endregion
    }
}
