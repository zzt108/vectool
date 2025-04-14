using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NLogS = NLogShared;

namespace DocXHandler
{
    public class VectorStoreConfig
    {
        private static readonly NLogS.CtxLogger _log = new();

        public List<string> FolderPaths { get; set; } = new List<string>();
        public List<string> ExcludedFiles { get; set; } = new List<string>();
        public List<string> ExcludedFolders { get; set; } = new List<string>();

        // Create a VectorStoreConfig from app.config settings
        public static VectorStoreConfig FromAppConfig()
        {
            var config = new VectorStoreConfig();
            config.LoadExcludedFilesConfig();
            config.LoadExcludedFoldersConfig();
            return config;
        }

        // Load excluded files from app.config
        public void LoadExcludedFilesConfig()
        {
            string? excludedFilesConfig = ConfigurationManager.AppSettings["excludedFiles"];
            if (!string.IsNullOrEmpty(excludedFilesConfig))
            {
                ExcludedFiles = excludedFilesConfig.Split(',')
                    .Select(f => f.Trim())
                    .ToList();
            }
        }

        // Load excluded folders from app.config
        public void LoadExcludedFoldersConfig()
        {
            string? excludedFoldersConfig = ConfigurationManager.AppSettings["excludedFolders"];
            if (!string.IsNullOrEmpty(excludedFoldersConfig))
            {
                ExcludedFolders = excludedFoldersConfig.Split(',')
                    .Select(f => f.Trim())
                    .ToList();
            }
        }

        // Check if a file should be excluded
        public bool IsFileExcluded(string fileName)
        {
            foreach (var pattern in ExcludedFiles)
            {
                string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                {
                    _log.Trace($"File '{fileName}' excluded by pattern '{pattern}'");
                    return true;
                }
            }
            return false;
        }

        // Check if a folder should be excluded
        public bool IsFolderExcluded(string folderName)
        {
            bool isExcluded = ExcludedFolders.Contains(folderName);
            if (isExcluded)
            {
                _log.Trace($"Folder '{folderName}' is in excluded list");
            }
            return isExcluded;
        }

        // Add a folder path if it doesn't exist
        public bool AddFolderPath(string folderPath)
        {
            if (!FolderPaths.Contains(folderPath))
            {
                FolderPaths.Add(folderPath);
                return true;
            }
            return false;
        }

        // Remove a folder path
        public bool RemoveFolderPath(string folderPath)
        {
            return FolderPaths.Remove(folderPath);
        }

        // Clear all folder paths
        public void ClearFolderPaths()
        {
            FolderPaths.Clear();
        }

        // Create a deep copy of this configuration
        public VectorStoreConfig Clone()
        {
            return new VectorStoreConfig
            {
                FolderPaths = new List<string>(FolderPaths),
                ExcludedFiles = new List<string>(ExcludedFiles),
                ExcludedFolders = new List<string>(ExcludedFolders)
            };
        }

        // Load all vector store configurations
        public static Dictionary<string, VectorStoreConfig> LoadAll(string? configPath = null)
        {
            string vectorStoreFoldersPath = configPath ??
                ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ??
                @"..\..\vectorStoreFolders.json";

            Dictionary<string, VectorStoreConfig> configs = new();

            if (File.Exists(vectorStoreFoldersPath))
            {
                try
                {
                    string json = File.ReadAllText(vectorStoreFoldersPath);
                    var deserializedConfigs = JsonSerializer.Deserialize<Dictionary<string, VectorStoreConfig>>(json);
                    if (deserializedConfigs != null)
                    {
                        configs = deserializedConfigs;
                    }

                    _log.Debug($"Loaded {configs.Count} vector store configurations");
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Error loading vector store configurations from {vectorStoreFoldersPath}");
                }
            }

            return configs;
        }

        // Save all vector store configurations
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

                _log.Debug($"Saved {configs.Count} vector store configurations to {vectorStoreFoldersPath}");
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error saving vector store configurations to {vectorStoreFoldersPath}");
            }
        }
    }
}
