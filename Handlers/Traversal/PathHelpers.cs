namespace VecTool.Handlers.Traversal;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Static helpers for path manipulation and file I/O.
/// </summary>
public static class PathHelpers
{
    /// <summary>
    /// Safely reads all text from a file, returning empty string on failure.
    /// </summary>
    public static string SafeReadAllText(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Safely gets the directory name, returning path itself on failure.
    /// </summary>
    public static string SafeDirectoryName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        try
        {
            return new DirectoryInfo(path).Name;
        }
        catch
        {
            return path;
        }
    }

    /// <summary>
    /// Finds the root folder that owns the given file from a list of roots.
    /// Returns the longest matching root or the file's directory.
    /// </summary>
    public static string? FindOwningRoot(List<string> roots, string file)
    {
        if (roots == null || roots.Count == 0)
            return Path.GetDirectoryName(file);

        string? best = null;
        
        foreach (var r in roots)
        {
            if (string.IsNullOrWhiteSpace(r))
                continue;

            if (file.StartsWith(r, StringComparison.OrdinalIgnoreCase))
            {
                if (best == null || r.Length > best.Length)
                    best = r;
            }
        }

        return best ?? Path.GetDirectoryName(file);
    }

    /// <summary>
    /// Makes a path relative to a root, returning full path on failure.
    /// </summary>
    public static string MakeRelativeSafe(string? root, string full)
    {
        if (string.IsNullOrWhiteSpace(full))
            return string.Empty;

        try
        {
            if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
                return Path.GetRelativePath(root, full);
        }
        catch
        {
            // Fall through to return full path
        }

        return full;
    }

    /// <summary>
    /// Normalizes path separators to the platform standard.
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Checks if a path is a subdirectory of another path.
    /// </summary>
    public static bool IsSubdirectoryOf(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(basePath))
            return false;

        try
        {
            var pathInfo = new DirectoryInfo(path);
            var baseInfo = new DirectoryInfo(basePath);

            return pathInfo.FullName.StartsWith(baseInfo.FullName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
