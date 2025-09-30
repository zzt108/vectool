namespace VecTool.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLogS;

/// <summary>
/// Configuration for vector store operations, including file exclusions.
/// </summary>
public class VectorStoreConfig
{
    private static readonly CtxLogger _log = new();
    
    private readonly HashSet<string> _excludedExtensions;
    private readonly HashSet<string> _excludedFolderNames;
    private readonly HashSet<string> _excludedFileNameParts;

    /// <summary>
    /// Gets the list of file extensions to exclude from processing.
    /// </summary>
    public List<string> ExcludedExtensions { get; }

    /// <summary>
    /// Gets the list of folder names to exclude from processing.
    /// </summary>
    public List<string> ExcludedFolderNames { get; }

    /// <summary>
    /// Gets the list of file name parts to exclude from processing.
    /// </summary>
    public List<string> ExcludedFileNameParts { get; }

    /// <summary>
    /// Gets or sets the root path for vector store operations.
    /// </summary>
    public string? VectorStoreRootPath { get; set; }

    public VectorStoreConfig()
    {
        ExcludedExtensions = new List<string>();
        ExcludedFolderNames = new List<string>();
        ExcludedFileNameParts = new List<string>();
        
        _excludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _excludedFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _excludedFileNameParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes the configuration and builds internal HashSets for fast lookups.
    /// </summary>
    public void Initialize()
    {
        _excludedExtensions.Clear();
        _excludedFolderNames.Clear();
        _excludedFileNameParts.Clear();

        foreach (var ext in ExcludedExtensions)
        {
            _excludedExtensions.Add(ext);
        }

        foreach (var folder in ExcludedFolderNames)
        {
            _excludedFolderNames.Add(folder);
        }

        foreach (var part in ExcludedFileNameParts)
        {
            _excludedFileNameParts.Add(part);
        }

        _log.Info($"VectorStoreConfig initialized: {ExcludedExtensions.Count} extensions, {ExcludedFolderNames.Count} folders, {ExcludedFileNameParts.Count} file parts excluded");
    }

    /// <summary>
    /// Determines if a file should be excluded based on configuration.
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <returns>True if file should be excluded, false otherwise</returns>
    public bool IsFileExcluded(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return true;

        var fileName = Path.GetFileName(filePath);
        var fileExtension = Path.GetExtension(filePath);

        // Check extension
        if (!string.IsNullOrEmpty(fileExtension) && _excludedExtensions.Contains(fileExtension))
        {
            _log.Trace($"File excluded by extension: {filePath}");
            return true;
        }

        // Check filename parts
        if (_excludedFileNameParts.Any(part => fileName.Contains(part, StringComparison.OrdinalIgnoreCase)))
        {
            _log.Trace($"File excluded by filename part: {filePath}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if a folder should be excluded based on configuration.
    /// </summary>
    /// <param name="folderPath">Full path to the folder</param>
    /// <returns>True if folder should be excluded, false otherwise</returns>
    public bool IsFolderExcluded(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return true;

        var folderName = new DirectoryInfo(folderPath).Name;

        if (_excludedFolderNames.Contains(folderName))
        {
            _log.Trace($"Folder excluded: {folderPath}");
            return true;
        }

        return false;
    }
}
