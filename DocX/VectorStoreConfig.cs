using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NLogS = NLogShared;

namespace DocXHandler;

public interface IUserInterface
{
    int TotalWork { get; set; }

    void ShowMessage(string message, string title = "Information", MessageType type = MessageType.Information);
    void UpdateProgress(int current);
    void UpdateStatus(string statusText);
    void WorkStart(string workText, List<string> selectedFolders);
    void WorkFinish();
}

public enum MessageType
{
    Information,
    Warning,
    Error
}

public class VectorStoreConfig
{
    private static readonly NLogS.CtxLogger _log = new();

    public List<string> FolderPaths { get; set; } = new List<string>();
    public List<string> ExcludedFiles { get; set; } = new List<string>();
    public List<string> ExcludedFolders { get; set; } = new List<string>();

    public string CommonRootPath => VectorStoreConfig.GetCommonRootPath(FolderPaths);

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
            ExcludedFolders = excludedFoldersConfig.ToLower().Split(',')
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
        bool isExcluded = false;
        foreach (var pattern in ExcludedFolders)
        {
            isExcluded = folderName.Contains('\\'+pattern);
            if (isExcluded)
            {
                _log.Trace($"Folder '{folderName}' excluded '{pattern}'");
                break;
            }
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

    /// <summary>
    /// Gets the minimal common root path from a list of full file paths.
    /// It uses a clever trick by sorting the paths and comparing the first and last elements.
    /// </summary>
    /// <param name="paths">A list of full file paths.</param>
    /// <returns>The common directory path ending with a separator, or an empty string if no common path is found.</returns>
    public static string GetCommonRootPath(List<string> paths)
    {
        // --- Step 1: Handle edge cases ---
        // If the list is null, empty, or has only one path, we can take a shortcut.
        if (paths == null || paths.Count == 0)
        {
            return string.Empty;
        }

        if (paths.Count == 1)
        {
            string singlePathDir = Path.GetDirectoryName(paths[0]);
            if (string.IsNullOrEmpty(singlePathDir))
            {
                return string.Empty;
            }
            // Ensure the path ends with a directory separator (e.g., '\')
            return singlePathDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        // --- Step 2: The magic sort ---
        // Sort the paths alphabetically. This brings the most different paths to the start and end of the list.
        var sortedPaths = paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        string firstPath = sortedPaths.First();
        string lastPath = sortedPaths.Last();
        int minLength = Math.Min(firstPath.Length, lastPath.Length);

        // --- Step 3: Find the common prefix ---
        // Find the last character index where the first and last paths still match.
        int lastMatchingIndex = -1;
        for (int i = 0; i < minLength; i++)
        {
            if (char.ToLower(firstPath[i]) != char.ToLower(lastPath[i]))
            {
                break;
            }
            lastMatchingIndex = i;
        }

        if (lastMatchingIndex == -1)
        {
            return string.Empty; // No common characters found at all.
        }

        // --- Step 4: Trim to the last complete folder ---
        // The common string might end mid-filename (e.g., "C:\Users\Test\Docu").
        // We need to find the last directory separator to get the full common path.
        string commonPrefix = firstPath.Substring(0, lastMatchingIndex + 1);
        int lastSeparatorIndex = commonPrefix.LastIndexOf(Path.DirectorySeparatorChar);

        if (lastSeparatorIndex == -1)
        {
            return string.Empty; // Common part doesn't even contain a full directory.
        }

        // Return the path up to and including the last separator.
        return commonPrefix.Substring(0, lastSeparatorIndex + 1);
    }

}
