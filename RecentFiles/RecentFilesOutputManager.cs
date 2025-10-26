namespace VecTool.RecentFiles;

using System;
using System.Globalization;
using System.IO;
using VecTool.Configuration;

/// <summary>
/// Manages creation and cleanup of the output directory structure.
/// Follows the configuration contract in RecentFilesConfig.
/// </summary>
public sealed class RecentFilesOutputManager
{
    private readonly RecentFilesConfig _config;

    public static RecentFilesOutputManager Factory()
    {
        var _config = RecentFilesConfig.FromAppConfig();
        return new RecentFilesOutputManager(_config);
    }

    public RecentFilesOutputManager(RecentFilesConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Ensures base output path exists and returns it.
    /// </summary>
    public string EnsureBaseDirectory()
    {
        var baseDir = _config.StorageFilePath;
        if (string.IsNullOrWhiteSpace(baseDir))
            throw new InvalidOperationException("StorageFilePath must not be empty.");

        try
        {
            var directory = Path.GetDirectoryName(baseDir);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            return directory ?? baseDir;
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to ensure base output directory {baseDir}.", ex);
        }
    }

    /// <summary>
    /// Ensures dated subdirectory (yyyy-MM-dd) exists for given timestamp.
    /// </summary>
    public string EnsureDatedDirectory(DateTimeOffset when)
    {
        var baseDir = EnsureBaseDirectory();
        var dated = Path.Combine(baseDir, when.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        try
        {
            Directory.CreateDirectory(dated);
            return dated;
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to ensure dated directory {dated}.", ex);
        }
    }

    /// <summary>
    /// Builds a safe output file path under the dated directory with enum-derived suffix.
    /// </summary>
    public string BuildOutputPath(string fileName, RecentFileType fileType, DateTimeOffset? when = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("fileName is required.", nameof(fileName));

        
        string? datedDir = null;
        if (when.HasValue)
            EnsureDatedDirectory(when.Value);

        // ✅ NEW: Append enum suffix/extension
        var baseNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var enumSuffix = fileType.ToFileSuffix(); // converts TestResultsMd -> _test-results.md
        var finalName = $"{baseNameWithoutExt}{enumSuffix}";

        var safeName = SanitizeFileName(finalName);
        return Path.Combine(datedDir?? string.Empty, safeName);
    }

    /// <summary>
    /// Deletes files older than retention and prunes empty directories.
    /// Returns the number of deleted files + directories.
    /// </summary>
    public int CleanupOldFiles(DateTimeOffset? now = null)
    {
        var count = 0;
        var baseDir = EnsureBaseDirectory();
        var reference = now ?? DateTimeOffset.UtcNow;

        if (_config.MaxCount <= 0)
            return 0;

        var cutoff = reference.AddDays(-_config.MaxCount);

        try
        {
            if (!Directory.Exists(baseDir))
                return 0;

            // Delete old files
            foreach (var file in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var lastWrite = File.GetLastWriteTimeUtc(file);
                    if (lastWrite < cutoff.UtcDateTime)
                    {
                        File.Delete(file);
                        count++;
                    }
                }
                catch
                {
                    // swallow per-file errors to keep cleanup robust
                }
            }

            // prune empty directories (bottom-up)
            foreach (var dir in Directory.EnumerateDirectories(baseDir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (IsDirectoryEmpty(dir))
                    {
                        Directory.Delete(dir, false);
                        count++;
                    }
                }
                catch
                {
                    // swallow per-dir errors
                }
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Cleanup failed under {baseDir}.", ex);
        }

        return count;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    private static bool IsDirectoryEmpty(string path)
    {
        return Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0;
    }
}
