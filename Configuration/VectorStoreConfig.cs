// Path: Configuration/VectorStoreConfig.cs

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

        public List<string> FolderPaths { get; set; } = new();
        public List<string> ExcludedFileNameParts { get; set; } = new();
        public List<string> ExcludedFolderNames { get; set; } = new();
        public List<string> ExcludedExtensions { get; set; } = new();

        /// <summary>
        /// Determines if a folder should be excluded based on its name.
        /// </summary>
        public bool IsFolderExcluded(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return true;

            return ExcludedFolderNames.Any(excluded => folderName.Equals(excluded, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if a file should be excluded based on its name or extension.
        /// </summary>
        public bool IsFileExcluded(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return true;

            var extension = Path.GetExtension(fileName);

            // Check against excluded extensions
            if (!string.IsNullOrEmpty(extension) && ExcludedExtensions.Any(ext => extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check against excluded file name parts
            return ExcludedFileNameParts.Any(part => fileName.Contains(part, StringComparison.OrdinalIgnoreCase));
        }

        public VectorStoreConfig Clone()
        {
            return new VectorStoreConfig
            {
                FolderPaths = new List<string>(FolderPaths),
                ExcludedFileNameParts = new List<string>(ExcludedFileNameParts),
                ExcludedFolderNames = new List<string>(ExcludedFolderNames),
                ExcludedExtensions = new List<string>(ExcludedExtensions)
            };
        }

        public static VectorStoreConfig FromAppConfig()
        {
            var config = new VectorStoreConfig();
            var excludedFiles = ConfigurationManager.AppSettings["excludedFiles"] ?? "";
            var excludedFolders = ConfigurationManager.AppSettings["excludedFolders"] ?? "";
            var excludedExtensions = ConfigurationManager.AppSettings["excludedExtensions"] ?? "";

            config.ExcludedFileNameParts.AddRange(excludedFiles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            config.ExcludedFolderNames.AddRange(excludedFolders.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            config.ExcludedExtensions.AddRange(excludedExtensions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));

            return config;
        }

        private static string GetDefaultPath()
        {
            if (_defaultPath != null)
                return _defaultPath;

            _defaultPath = ConfigurationManager.AppSettings["vectorStoreFoldersPath"];
            if (string.IsNullOrWhiteSpace(_defaultPath))
            {
                _defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VecTool", "vectorStoreFolders.json");
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
            var settings = new JsonSerializerSettings();
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
