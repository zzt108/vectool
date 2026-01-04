namespace VecTool.Handlers.Traversal
{
    using global::VecTool.Configuration;
    using global::VecTool.Utils;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using VecTool.Configuration.Logging;

    /// <summary>
    /// Validates files and folders for processing based on exclusion rules.
    /// </summary>
    public static class FileValidator
    {
        private static readonly ILogger logger =
            AppLogger.Create("FileValidator");

        /// <summary>
        /// Determines if a folder should be excluded from processing.
        /// </summary>
        public static bool IsFolderExcluded(string folderName, IVectorStoreConfig config)
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
        public static bool IsFileExcluded(string fileName, IVectorStoreConfig config)
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
                {
                    logger.LogTrace($"File does not exist: {path}");
                    return false;
                }

                // File must have content
                if (fi.Length == 0)
                {
                    logger.LogTrace($"File has no content: {path}");
                    return false;
                }

                var ext = Path.GetExtension(path);
                if (IsBinary(ext, path))
                {
                    logger.LogTrace($"File marked as binary by MimeTypeProvider: {path}");
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if a file is binary by checking mdTags.json first,
        /// then falling back to heuristic detection for unknown extensions.
        /// </summary>
        /// <param name="fileExtension">The file extension (e.g., ".ttf", ".bin")</param>
        /// <param name="filePath">The full path to the file for content inspection if needed</param>
        /// <returns>True if the file is binary, false if it's text</returns>
        public static bool IsBinary(string fileExtension, string? filePath)
        {
            // First check mdTags.json (authoritative)
            var mimeType = MimeTypeProvider.GetMimeType(fileExtension);

            if (mimeType != null)
            {
                // Known extension - use mdTags.json value
                return mimeType.Equals("application/binary", StringComparison.OrdinalIgnoreCase);
            }

            // Unknown extension - use heuristic detection
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                // Cannot probe file - assume binary
                return true;
            }

            return DetectBinaryByContent(filePath);
        }

        /// <summary>
        /// Heuristic binary detection: reads first 8KB and checks for null bytes.
        /// </summary>
        private static bool DetectBinaryByContent(string filePath)
        {
            try
            {
                const int bufferSize = 8192; // 8KB sample
                var buffer = new byte[bufferSize];

                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                int bytesRead = fs.Read(buffer, 0, bufferSize);

                // Check for null bytes (0x00) - strong indicator of binary content
                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == 0x00)
                    {
                        return true; // Found null byte = binary
                    }
                }

                // No null bytes found = likely text
                return false;
            }
            catch
            {
                // Cannot read file - assume text (safer fallback)
                return false;
            }
        }

        /// <summary>
        /// Determines if a file should be included in export (MD/DOCX) based on:
        /// 1. VectorStoreConfig exclusion rules (from app.config)
        /// 2. MimeTypeProvider binary detection (from mdTags.json)
        /// 3. File system validity checks
        /// </summary>
        public static bool ShouldIncludeInExport(string filePath, IVectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var folderPath = Path.GetDirectoryName(filePath) ?? string.Empty;

            if (IsFolderExcluded(folderPath, config))
            {
                return false;
            }

            var fileName = Path.GetFileName(filePath);

            // 1️⃣ Check config-based exclusions (app.config)
            if (IsFileExcluded(fileName, config))
            {
                logger.LogTrace($"Excluded by VectorStoreConfig: {fileName}");
                return false;
            }

            // 2️⃣ Check file system validity (includes MimeTypeProvider.IsBinary check)
            if (!IsFileValid(filePath, outputPath: null))
            {
                logger.LogTrace($"Invalid or binary file: {fileName}");
                return false;
            }

            // All checks passed - file is eligible for export
            return true;
        }
    }
}