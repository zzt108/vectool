namespace VecTool.Handlers.Traversal
{
    using global::VecTool.Configuration;
    using global::VecTool.Utils; // ✅ NEW - for MimeTypeProvider
    using NLogShared;
    using System;
    using System.IO;

    /// <summary>
    /// Validates files and folders for processing based on exclusion rules.
    /// </summary>
    public static class FileValidator
    {
        private static readonly CtxLogger log = new();

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

                // ✅ Use MimeTypeProvider instead of hardcoded array
                var ext = Path.GetExtension(path);
                if (MimeTypeProvider.IsBinary(ext))
                {
                    log.Trace($"File marked as binary by MimeTypeProvider: {path}");
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
        /// Determines if a file extension is binary.
        /// ✅ Uses MimeTypeProvider (loads from mdTags.json).
        /// </summary>
        public static bool IsBinaryExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return false;

            // ✅ Use MimeTypeProvider.IsBinary (loads from Config/mdTags.json)
            return MimeTypeProvider.IsBinary(extension);
        }

        // ✅ NEW METHODS BELOW

        /// <summary>
        /// Determines if a file should be included in export (MD/DOCX) based on:
        /// 1. VectorStoreConfig exclusion rules (from app.config)
        /// 2. MimeTypeProvider binary detection (from mdTags.json)
        /// 3. File system validity checks
        /// </summary>
        /// <remarks>
        /// ✅ USE THIS for both export handlers AND summary generators to ensure consistency.
        /// ✅ NO HARDCODED STRINGS - uses VectorStoreConfig + MimeTypeProvider.
        /// </remarks>
        public static bool ShouldIncludeInExport(string filePath, VectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var fileName = Path.GetFileName(filePath);

            // 1️⃣ Check config-based exclusions (app.config)
            if (IsFileExcluded(fileName, config))
            {
                log.Trace($"Excluded by VectorStoreConfig: {fileName}");
                return false;
            }

            // 2️⃣ Check file system validity (includes MimeTypeProvider.IsBinary check)
            if (!IsFileValid(filePath, outputPath: null))
            {
                log.Trace($"Invalid or binary file: {fileName}");
                return false;
            }

            // All checks passed - file is eligible for export
            return true;
        }
    }
}
