// Folder: VecTool.Configuration
// File: VectorStoreConfig.cs

#nullable enable

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;
// using LogCtx; // Uncomment if LogCtx is initialized at app startup

namespace VecTool.Configuration
{
    /// <summary>
    /// Vector store configuration: global defaults and per-store overrides.
    /// - ExcludedFiles: file name patterns to exclude (supports exact or wildcard-like simple checks).
    /// - ExcludedFolders: folder names to exclude.
    /// - FolderPaths: selected root folders for a given vector store (UI-managed list).
    /// </summary>
    public sealed class VectorStoreConfig
    {
        /// <summary>
        /// File name patterns to exclude (case-insensitive normalization applied in Normalize()).
        /// </summary>
        public List<string> ExcludedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Folder names to exclude (case-insensitive normalization applied in Normalize()).
        /// </summary>
        public List<string> ExcludedFolders { get; set; } = new List<string>();

        /// <summary>
        /// Selected root folders for the vector store, persisted to JSON alongside exclusions.
        /// </summary>
        public List<string> FolderPaths { get; set; } = new List<string>();

        /// <summary>
        /// Ensures lists are non-null, trims, and de-duplicates values case-insensitively.
        /// Call after deserialization and before persistence to keep shape stable.
        /// </summary>
        public void Normalize()
        {
            ExcludedFiles ??= new List<string>();
            ExcludedFolders ??= new List<string>();
            FolderPaths ??= new List<string>();

            ExcludedFiles = ExcludedFiles
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            ExcludedFolders = ExcludedFolders
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            FolderPaths = FolderPaths
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Deep clone to avoid aliasing between composed configurations in the UI.
        /// </summary>
        public VectorStoreConfig Clone()
        {
            return new VectorStoreConfig
            {
                ExcludedFiles = new List<string>(ExcludedFiles ?? new List<string>()),
                ExcludedFolders = new List<string>(ExcludedFolders ?? new List<string>()),
                FolderPaths = new List<string>(FolderPaths ?? new List<string>())
            };
        }

        // --------------------------- Central configuration IO ---------------------------

        private static string ResolveDefaultPath()
        {
            // Deterministic app-scoped path to avoid CWD issues and ensure persistence across restarts.
            // Example: <app>\Generated\vectorStoreFolders.json
            var baseDir = AppContext.BaseDirectory;
            var dir = Path.Combine(baseDir, "Generated");
            return Path.Combine(dir, "vectorStoreFolders.json");
        }

        /// <summary>
        /// Priority order:
        /// 1) Explicit path argument
        /// 2) AppSettings["vectorStoreFoldersPath"]
        /// 3) Deterministic default under AppContext.BaseDirectory\Generated
        /// </summary>
        private static string ResolveConfigPath(string? configPath)
        {
            var fromConfig = ConfigurationManager.AppSettings["vectorStoreFoldersPath"];
            var path = configPath ?? fromConfig ?? ResolveDefaultPath();

            // Ensure directory exists, even on first run.
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return path;
        }

        /// <summary>
        /// Load all named vector store configs from JSON file, returning an empty dictionary if the file does not exist.
        /// </summary>
        public static Dictionary<string, VectorStoreConfig> LoadAll(string? configPath = null)
        {
            var path = ResolveConfigPath(configPath);

            try
            {
                if (!File.Exists(path))
                {
                    // Return empty on first run; do not create the file yet.
                    return new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);
                }

                var json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, VectorStoreConfig>>(json)
                           ?? new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);

                // Normalize entries to avoid null lists and clean up whitespace/dupes.
                foreach (var kv in dict)
                {
                    kv.Value?.Normalize();
                }

                // using (var scope = LogCtx.Scope("VectorStoreConfig.LoadAll").Add("path", path).Add("count", dict.Count))
                // {
                //     LogCtx.Info("Vector store configurations loaded.");
                // }

                return new Dictionary<string, VectorStoreConfig>(dict, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                // using (var scope = LogCtx.Scope("VectorStoreConfig.LoadAll").Add("path", path))
                // {
                //     LogCtx.Warn(ex, "Failed to load vector store configurations from path");
                // }

                // Fail safe: return empty rather than throw; UI handles empty set gracefully.
                return new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Save all named vector store configs to JSON file.
        /// </summary>
        public static void SaveAll(Dictionary<string, VectorStoreConfig> configs, string? configPath = null)
        {
            var path = ResolveConfigPath(configPath);

            try
            {
                // Normalize before writing to ensure stable shape.
                foreach (var kv in configs)
                {
                    kv.Value?.Normalize();
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(configs, options);
                File.WriteAllText(path, json);

                // using (var scope = LogCtx.Scope("VectorStoreConfig.SaveAll").Add("path", path).Add("count", configs.Count))
                // {
                //     LogCtx.Info("Vector store configurations saved.");
                // }
            }
            catch (Exception ex)
            {
                // using (var scope = LogCtx.Scope("VectorStoreConfig.SaveAll").Add("path", path))
                // {
                //     LogCtx.Error(ex, "Failed to save vector store configurations to path");
                // }
                throw;
            }
        }

        // ----------- Global defaults support for Settings tab composition -----------

        /// <summary>
        /// Reads optional global default exclusions from App.config:
        /// - vectorStoreDefaultExcludedFiles: newline or semicolon separated
        /// - vectorStoreDefaultExcludedFolders: newline or semicolon separated
        /// </summary>
        public static VectorStoreConfig FromAppConfig()
        {
            var defaults = new VectorStoreConfig();

            try
            {
                var filesRaw = ConfigurationManager.AppSettings["vectorStoreDefaultExcludedFiles"];
                var foldersRaw = ConfigurationManager.AppSettings["vectorStoreDefaultExcludedFolders"];

                defaults.ExcludedFiles = SplitMulti(filesRaw);
                defaults.ExcludedFolders = SplitMulti(foldersRaw);
                defaults.Normalize();

                // using (var scope = LogCtx.Scope("VectorStoreConfig.FromAppConfig")
                //     .Add("files", defaults.ExcludedFiles.Count)
                //     .Add("folders", defaults.ExcludedFolders.Count))
                // {
                //     LogCtx.Info("Global defaults loaded from app config.");
                // }
            }
            catch (Exception ex)
            {
                // LogCtx.Warn(ex, "Failed to parse global defaults from app config.");
                defaults.Normalize();
            }

            return defaults;
        }

        private static List<string> SplitMulti(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>();

            // Accept newline and semicolon separated items.
            var normalized = raw.Replace("\r", string.Empty, StringComparison.Ordinal);
            var parts = normalized
                .Split(new[] { "\n", ";" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return parts;
        }
    }
}
