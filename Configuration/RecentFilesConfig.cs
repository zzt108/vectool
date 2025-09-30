namespace VecTool.Configuration;

using System;
using System.IO;
using NLogS;

/// <summary>
/// Configuration for recent files feature.
/// </summary>
public class RecentFilesConfig
{
    private static readonly CtxLogger _log = new();

    /// <summary>
    /// Gets or sets the maximum number of recent files to track.
    /// </summary>
    public int MaxCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the file path for storing recent files data.
    /// </summary>
    public string StorageFilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VecTool",
        "recent_files.json"
    );

    /// <summary>
    /// Gets or sets whether recent files tracking is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Validates the configuration and ensures storage directory exists.
    /// </summary>
    public void Validate()
    {
        if (MaxCount <= 0)
        {
            _log.Warn($"Invalid MaxCount: {MaxCount}, resetting to default (10)");
            MaxCount = 10;
        }

        if (string.IsNullOrWhiteSpace(StorageFilePath))
        {
            _log.Warn("StorageFilePath is empty, using default location");
            StorageFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VecTool",
                "recent_files.json"
            );
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(StorageFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _log.Info($"Created recent files storage directory: {directory}");
        }

        _log.Info($"RecentFilesConfig validated: MaxCount={MaxCount}, Path={StorageFilePath}, Enabled={IsEnabled}");
    }
}
