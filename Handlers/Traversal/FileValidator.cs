namespace VecTool.Handlers.Traversal;

using System;
using System.IO;
using System.Linq;
using VecTool.Configuration;

/// <summary>
/// Validates files and folders for processing based on exclusion rules.
/// </summary>
public static class FileValidator
{
    private static readonly string[] _binaryExtensions = 
    {
        ".dll", ".exe", ".pdb", ".obj", ".so", ".dylib",
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".bmp", ".svg",
        ".pdf", ".docx", ".xlsx", ".pptx",
        ".zip", ".7z", ".gz", ".tar", ".rar",
        ".bin", ".dat", ".db", ".sqlite"
    };

    /// <summary>
    /// Determines if a folder should be excluded from processing.
    /// </summary>
    public static bool IsFolderExcluded(string folderName, VectorStoreConfig config)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return true;

        try
        {
            return config.IsFolderExcluded(folderName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines if a file should be excluded from processing.
    /// </summary>
    public static bool IsFileExcluded(string fileName, VectorStoreConfig config)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return true;

        try
        {
            return config.IsFileExcluded(fileName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a file is suitable for processing.
    /// </summary>
    public static bool IsFileValid(string path, string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Don't process the output file itself
        if (!string.IsNullOrEmpty(outputPath) &&
            string.Equals(path, outputPath, StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var fi = new FileInfo(path);
            
            // File must exist
            if (!fi.Exists)
                return false;

            // File must have content
            if (fi.Length == 0)
                return false;

            // Skip binary files
            var ext = Path.GetExtension(path);
            if (_binaryExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines if a file extension is binary.
    /// </summary>
    public static bool IsBinaryExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        return _binaryExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
