namespace VecTool.RecentFiles;

using System;
using System.IO;
using VecTool.Configuration;

/// <summary>
/// Pure text persistence interface for recent files data.
/// </summary>
public interface IRecentFilesStore
{
    /// <summary>
    /// Reads JSON data from storage.
    /// </summary>
    string? Read();

    /// <summary>
    /// Writes JSON data to storage.
    /// </summary>
    void Write(string json);
}

/// <summary>
/// File-based implementation of recent files storage.
/// </summary>
public sealed class FileRecentFilesStore : IRecentFilesStore
{
    private readonly string _jsonPath;

    public FileRecentFilesStore(RecentFilesConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        _jsonPath = config.StorageFilePath;
    }

    public string? Read()
    {
        try
        {
            if (!File.Exists(_jsonPath))
                return null;

            return File.ReadAllText(_jsonPath);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to read recent files JSON from {_jsonPath}.", ex);
        }
    }

    public void Write(string json)
    {
        if (json is null)
            throw new ArgumentNullException(nameof(json));

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_jsonPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_jsonPath, json);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to write recent files JSON to {_jsonPath}.", ex);
        }
    }
}
