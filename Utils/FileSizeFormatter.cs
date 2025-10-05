// ✅ FULL FILE VERSION
using System;

namespace VecTool.Utils
{
    /// <summary>
    /// Utility for converting file sizes in bytes into human-readable strings.
    /// </summary>
    public static class FileSizeFormatter
    {
        /// <summary>
        /// Converts a size in bytes to a human-readable string (e.g., "1.5 MB").
        /// </summary>
        /// <param name="bytes">Size in bytes.</param>
        /// <returns>Formatted size string with appropriate unit.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when bytes is negative.</exception>
        public static string Format(long bytes)
        {
            if (bytes < 0) throw new ArgumentOutOfRangeException(nameof(bytes), "Byte size cannot be negative.");

            string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.##} {units[unitIndex]}";
        }
        /// <summary>
        /// Gets a human-readable file size for the specified file path.
        /// </summary>
        public static string GetFileSizeFormatted(string filePath) // TODO: Move to PathHelpers, public for testability
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var fileInfo = new FileInfo(filePath);
            return FileSizeFormatter.Format(fileInfo.Length);
        }

    }
}
