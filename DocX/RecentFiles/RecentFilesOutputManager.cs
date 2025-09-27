// File: DocXHandler/RecentFiles/RecentFilesOutputManager.cs
using System;
using System.Globalization;
using System.IO;

namespace DocXHandler.RecentFiles
{
    // Manages creation and cleanup of the output directory structure.
    // Follows the configuration contract in RecentFilesConfig.
    public sealed class RecentFilesOutputManager
    {
        private readonly RecentFilesConfig _config;

        public RecentFilesOutputManager(RecentFilesConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // Ensures base output path exists and returns it.
        public string EnsureBaseDirectory()
        {
            var baseDir = _config.OutputPath;
            if (string.IsNullOrWhiteSpace(baseDir))
                throw new InvalidOperationException("OutputPath must not be empty.");

            try
            {
                Directory.CreateDirectory(baseDir);
                return baseDir;
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to ensure base output directory '{baseDir}'.", ex);
            }
        }

        // Ensures dated subdirectory (yyyy-MM-dd) exists for given timestamp.
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
                throw new IOException($"Failed to ensure dated directory '{dated}'.", ex);
            }
        }

        // Builds a safe output file path under the dated directory.
        // The caller is responsible for writing the actual file.
        public string BuildOutputPath(string fileName, DateTimeOffset when)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("fileName is required.", nameof(fileName));

            var datedDir = EnsureDatedDirectory(when);
            var safeName = SanitizeFileName(fileName);
            return Path.Combine(datedDir, safeName);
        }

        // Deletes files older than retention and prunes empty directories.
        // Returns the number of deleted files + directories.
        public int CleanupOldFiles(DateTimeOffset? now = null)
        {
            var count = 0;
            var baseDir = EnsureBaseDirectory();
            var reference = now ?? DateTimeOffset.UtcNow;
            if (_config.RetentionDays <= 0)
                return 0;

            var cutoff = reference.AddDays(-_config.RetentionDays);

            try
            {
                if (!Directory.Exists(baseDir))
                    return 0;

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

                // prune empty directories bottom-up
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
                throw new IOException($"Cleanup failed under '{baseDir}'.", ex);
            }

            return count;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private static bool IsDirectoryEmpty(string path)
        {
            return Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0;
        }
    }
}
