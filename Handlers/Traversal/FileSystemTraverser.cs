namespace VecTool.Handlers.Traversal;

using System;
using System.Collections.Generic;
using System.IO;
using VecTool.Configuration;
using NLogS;

/// <summary>
/// Handles folder traversal and file enumeration with exclusion support.
/// </summary>
public sealed class FileSystemTraverser
{
    private static readonly CtxLogger _log = new();
    private readonly IUserInterface? _ui;

    public FileSystemTraverser(IUserInterface? ui)
    {
        _ui = ui;
    }

    /// <summary>
    /// Recursively processes folders with custom processing logic.
    /// </summary>
    public void ProcessFolder<T>(
        string folderPath,
        T context,
        VectorStoreConfig vectorStoreConfig,
        Action<string, T, VectorStoreConfig> processFile,
        Action<T, string> writeFolderName,
        Action<T>? writeFolderEnd = null)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return;

        var folderName = new DirectoryInfo(folderPath).Name;

        if (FileValidator.IsFolderExcluded(folderName, vectorStoreConfig))
        {
            _log.Trace($"Skipping excluded folder: {folderPath}");
            return;
        }

        _ui?.UpdateStatus($"Processing folder: {folderPath}");
        _log.Debug($"Processing folder: {folderPath}");

        writeFolderName(context, folderName);

        // Process files
        string[] files = Array.Empty<string>();
        try { files = Directory.GetFiles(folderPath); } 
        catch (Exception ex) 
        { 
            _log.Warn(ex, $"Failed to enumerate files in: {folderPath}");
        }

        foreach (var file in files)
        {
            try
            {
                processFile(file, context, vectorStoreConfig);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error processing file: {file}");
                throw;
            }
        }

        // Process subfolders recursively
        string[] subfolders = Array.Empty<string>();
        try { subfolders = Directory.GetDirectories(folderPath); } 
        catch (Exception ex) 
        { 
            _log.Warn(ex, $"Failed to enumerate subdirectories in: {folderPath}");
        }

        foreach (var sub in subfolders)
        {
            ProcessFolder(sub, context, vectorStoreConfig, processFile, writeFolderName, writeFolderEnd);
        }

        writeFolderEnd?.Invoke(context);
    }

    /// <summary>
    /// Enumerates all files in a folder tree respecting exclusions.
    /// </summary>
    public IEnumerable<string> EnumerateFilesRespectingExclusions(
        string root, 
        VectorStoreConfig config)
    {
        if (string.IsNullOrWhiteSpace(root))
            yield break;

        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            var folderName = new DirectoryInfo(current).Name;

            if (FileValidator.IsFolderExcluded(folderName, config))
                continue;

            // Enumerate files
            string[] files = Array.Empty<string>();
            try 
            { 
                files = Directory.GetFiles(current); 
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to enumerate files in: {current}");
                continue;
            }

            foreach (var f in files)
            {
                var fileName = Path.GetFileName(f);
                if (FileValidator.IsFileExcluded(fileName, config))
                    continue;

                if (!FileValidator.IsFileValid(f, null))
                    continue;

                yield return f;
            }

            // Enumerate subdirectories
            string[] subfolders = Array.Empty<string>();
            try 
            { 
                subfolders = Directory.GetDirectories(current); 
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to enumerate subdirectories in: {current}");
                continue;
            }

            foreach (var sub in subfolders)
                stack.Push(sub);
        }
    }
}
