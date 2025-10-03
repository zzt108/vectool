// Path: oaiVectorStore/VectorStoreConfig.cs

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace VecTool.Configuration
{
    public class VectorStoreConfig
    {
        private static string? _defaultPath;

        public List<string> FolderPaths { get; set; } = new List<string>();
        public List<string> ExcludedFiles { get; set; } = new List<string>();
        public List<string> ExcludedFolders { get; set; } = new List<string>();

        public VectorStoreConfig Clone()
        {
            return new VectorStoreConfig
            {
                FolderPaths = new List<string>(FolderPaths),
                ExcludedFiles = new List<string>(ExcludedFiles),
                ExcludedFolders = new List<string>(ExcludedFolders)
            };
        }

        public static VectorStoreConfig FromAppConfig()
        {
            var config = new VectorStoreConfig();
            var excludedFiles = ConfigurationManager.AppSettings["excludedFiles"] ?? "";
            var excludedFolders = ConfigurationManager.AppSettings["excludedFolders"] ?? "";

            config.ExcludedFiles.AddRange(excludedFiles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            config.ExcludedFolders.AddRange(excludedFolders.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));

            return config;
        }

        private static string GetDefaultPath()
        {
            if (_defaultPath != null) return _defaultPath;

            _defaultPath = ConfigurationManager.AppSettings["vectorStoreFoldersPath"];
            if (string.IsNullOrWhiteSpace(_defaultPath))
            {
                _defaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VecTool",
                    "vectorStoreFolders.json");
            }
            return _defaultPath;
        }

        public static Dictionary<string, VectorStoreConfig> LoadAll(string? path = null)
        {
            var filePath = path ?? GetDefaultPath();
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);
            }

            var json = File.ReadAllText(filePath);
            var settings = new JsonSerializerSettings
            {
                // This is needed to make the dictionary key comparer work correctly
                // It's a bit of a hack, but it works for this case
                // In a real-world scenario, you might want a custom converter
                // but for now, this is fine.
            };

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, VectorStoreConfig>>(json, settings);

            return new Dictionary<string, VectorStoreConfig>(deserialized ?? new Dictionary<string, VectorStoreConfig>(), StringComparer.OrdinalIgnoreCase);
        }

        public static void SaveAll(Dictionary<string, VectorStoreConfig> configs, string? path = null)
        {
            var filePath = path ?? GetDefaultPath();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonConvert.SerializeObject(configs, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
