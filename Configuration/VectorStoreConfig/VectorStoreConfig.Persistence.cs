using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace VecTool.Configuration
{
    /// <summary>
    /// Persistence operations for VectorStoreConfig (JSON, app.config).
    /// </summary>
    public partial class VectorStoreConfig
    {
        private static readonly ILogger logger =
            LoggerFactory.Create(b => b.AddNLog()).CreateLogger<VectorStoreConfig>();

        /// <summary>
        /// Create a VectorStoreConfig from app.config settings.
        /// </summary>
        public static VectorStoreConfig FromAppConfig()
        {
            var config = new VectorStoreConfig();
            config.LoadExcludedFilesConfig();
            config.LoadExcludedFoldersConfig();
            return config;
        }

        /// <summary>
        /// Load excluded files from app.config.
        /// </summary>
        public void LoadExcludedFilesConfig()
        {
            string? excludedFilesConfig = ConfigurationManager.AppSettings["excludedFiles"];
            if (!string.IsNullOrEmpty(excludedFilesConfig))
            {
                ExcludedFiles = excludedFilesConfig.Split(',').Select(f => f.Trim()).ToList();
            }
        }

        /// <summary>
        /// Load excluded folders from app.config.
        /// </summary>
        public void LoadExcludedFoldersConfig()
        {
            string? excludedFoldersConfig = ConfigurationManager.AppSettings["excludedFolders"];
            if (!string.IsNullOrEmpty(excludedFoldersConfig))
            {
                ExcludedFolders = excludedFoldersConfig.Split(',').Select(f => f.Trim()).ToList();
            }
        }

        /// <summary>
        /// Load all vector store configurations from JSON.
        /// </summary>
        public static Dictionary<string, VectorStoreConfig> LoadAll(string? configPath = null)
        {
            string vectorStoreFoldersPath = configPath
                ?? ConfigurationManager.AppSettings["vectorStoreFoldersPath"]
                ?? @"..\..\..\..\..\vectorStoreFolders.json";

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
                    logger.LogDebug($"Loaded {configs.Count} vector store configurations");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"LogError loading vector store configurations from {vectorStoreFoldersPath}");
                }
            }

            return configs;
        }

        /// <summary>
        /// Save all vector store configurations to JSON.
        /// </summary>
        public static void SaveAll(Dictionary<string, VectorStoreConfig> configs, string? configPath = null)
        {
            string vectorStoreFoldersPath = configPath
                ?? ConfigurationManager.AppSettings["vectorStoreFoldersPath"]
                ?? @"..\..\..\..\..\vectorStoreFolders.json";

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(configs, options);
                File.WriteAllText(vectorStoreFoldersPath, json);
                logger.LogDebug($"Saved {configs.Count} vector store configurations to {vectorStoreFoldersPath}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"LogError saving vector store configurations to {vectorStoreFoldersPath}");
            }
        }
    }
}