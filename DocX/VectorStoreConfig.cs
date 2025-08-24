// File: VectorStoreConfig.cs
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;
using NLogS = NLogShared;

namespace DocXHandler;

public class VectorStoreConfig
{
    private static readonly NLogS.CtxLogger _log = new();

    public VectorStoreConfig(List<string> folderPaths)
    {
        FolderPaths = folderPaths ?? throw new ArgumentNullException(nameof(folderPaths));
    }

    public VectorStoreConfig()
    {
    }

    /// <summary>
    /// The list of root folders to process.
    /// </summary>
    public List<string> FolderPaths { get; set; } = new List<string>();

    /// <summary>
    /// Returns the minimal common root path for all configured folders.
    /// </summary>
    public string CommonRootPath => GetCommonRootPath(FolderPaths);

    /// <summary>
    /// Constructs a config from app.config, e.g. a semicolon-separated list under "rootFolders".
    /// </summary>
    public static VectorStoreConfig FromAppConfig()
    {
        var config = new VectorStoreConfig();
        var raw = ConfigurationManager.AppSettings["rootFolders"];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            config.FolderPaths = raw
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(path => path.Trim())
                .ToList();
        }
        return config;
    }

    /// <summary>
    /// Finds the deepest common directory among all paths in the list.
    /// </summary>
    public static string GetCommonRootPath(List<string> paths)
    {
        if (paths == null || paths.Count == 0)
            return string.Empty;
        if (paths.Count == 1)
            return Path.GetDirectoryName(paths[0]) + Path.DirectorySeparatorChar;

        var sorted = paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
        var first = sorted.First();
        var last = sorted.Last();
        var length = Math.Min(first.Length, last.Length);
        int i = 0;
        for (; i < length; i++)
            if (char.ToLowerInvariant(first[i]) != char.ToLowerInvariant(last[i]))
                break;

        var prefix = first.Substring(0, i);
        var lastSep = prefix.LastIndexOf(Path.DirectorySeparatorChar);
        return lastSep < 0 ? string.Empty : prefix.Substring(0, lastSep + 1);
    }

    /// <summary>
    /// Saves all named configs (if you store by name) to a JSON file.
    /// </summary>
    public static void SaveAll(Dictionary<string, VectorStoreConfig> configs, string configPath = null)
    {
        configPath ??= ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ?? "vectorStoreFolders.json";
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(configPath, JsonSerializer.Serialize(configs, options));
            _log.Debug($"Saved {configs.Count} configs to {configPath}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error saving configs to {configPath}");
        }
    }

    /// <summary>
    /// Loads named configs from a JSON file.
    /// </summary>
    public static Dictionary<string, VectorStoreConfig> LoadAll(string configPath = null)
    {
        configPath ??= ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ?? "vectorStoreFolders.json";
        if (!File.Exists(configPath))
            return new Dictionary<string, VectorStoreConfig>();

        try
        {
            var json = File.ReadAllText(configPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, VectorStoreConfig>>(json)
                       ?? new Dictionary<string, VectorStoreConfig>();
            _log.Debug($"Loaded {dict.Count} configs from {configPath}");
            return dict;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error loading configs from {configPath}");
            return new Dictionary<string, VectorStoreConfig>();
        }
    }
}

