namespace VecTool.Handlers.Traversal
{
    using global::VecTool.Configuration;
    using NLogShared;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Validates files and folders for processing based on exclusion rules.
    /// </summary>
    public static class FileValidator
    {
        private static readonly CtxLogger log = new();

        private static readonly string[] binaryExtensions =
        {
            ".dll", ".exe", ".pdb", ".obj", ".so", ".dylib",
            ".png", ".jpg", ".jpeg", ".gif", ".ico", ".bmp", ".svg",
            ".pdf", ".docx", ".xlsx", ".pptx",
            ".zip", ".7z", ".gz", ".tar", ".rar",
            ".bin", ".dat", ".db", ".sqlite",
            ".ttf", ".otf", ".woff", ".woff2", ".eot" 
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
                if (binaryExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
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

            return binaryExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        // ✅ NEW METHODS BELOW

        /// <summary>
        /// Determines if a file should be included in export (MD/DOCX) based on:
        /// 1. VectorStoreConfig exclusion rules (from app.config)
        /// 2. MIME type validation (text files only)
        /// 3. File system validity checks
        /// </summary>
        /// <remarks>
        /// ✅ USE THIS for both export handlers AND summary generators to ensure consistency.
        /// </remarks>
        public static bool ShouldIncludeInExport(string filePath, VectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var fileName = Path.GetFileName(filePath);

            // 1️⃣ Check config-based exclusions (app.config)
            if (IsFileExcluded(fileName, config))
            {
                log.Trace($"Excluded by config: {fileName}");
                return false;
            }

            // 2️⃣ Check file system validity
            if (!IsFileValid(filePath, outputPath: null))
            {
                log.Trace($"Invalid file: {fileName}");
                return false;
            }

            // 3️⃣ Check MIME type (text files only)
            if (!IsTextFile(filePath))
            {
                log.Trace($"Excluded by MIME type (binary): {fileName}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a file is text-based using MIME type detection.
        /// </summary>
        /// <remarks>
        /// Uses System.Web.MimeMapping and extension-based heuristics.
        /// NO HARDCODED EXCLUSIONS - relies on known text extensions.
        /// </remarks>
        public static bool IsTextFile(string filePath)
        {
            var mimeType = GetMimeType(filePath);

            // Text MIME types start with "text/"
            if (mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
                return true;

            // ✅ Config-driven text extensions (commonly recognized as text by development tools)
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var textExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".cs", ".csproj", ".sln", ".config", ".json", ".xml", ".md", ".txt",
                ".xaml", ".resx", ".props", ".targets", ".editorconfig", ".gitignore",
                ".yml", ".yaml", ".log", ".sql", ".css", ".js", ".ts", ".html", ".htm",
                ".sh", ".bat", ".ps1", ".py", ".rb", ".go", ".java", ".cpp", ".h", ".c"
            };

            return textExtensions.Contains(ext);
        }

        /// <summary>
        /// Gets MIME type for a file using System.Web.MimeMapping or extension fallback.
        /// </summary>
        private static string GetMimeType(string filePath)
        {
            try
            {
                // .NET Framework/Core built-in MIME mapping
                var mimeType = System.Web.MimeMapping.GetMimeMapping(filePath);
                return mimeType ?? "application/octet-stream";
            }
            catch
            {
                // Fallback for environments without System.Web
                return "application/octet-stream";
            }
        }
    }
}
