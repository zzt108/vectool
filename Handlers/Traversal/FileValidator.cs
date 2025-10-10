// Path: Handlers/Traversal/FileValidator.cs
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using VecTool.Configuration;

namespace VecTool.Handlers.Traversal
{
    /// <summary>
    /// Validates files and folders for processing based on exclusion rules.
    /// </summary>
    public static class FileValidator
    {
        // Centralized binary extension set; case-insensitive.
        private static readonly HashSet<string> BinaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".dll", ".exe", ".pdb", ".obj", ".so", ".dylib",
            ".png", ".jpg", ".jpeg", ".gif", ".ico", ".bmp", ".svg",
            ".pdf", ".docx", ".xlsx", ".pptx",
            ".zip", ".7z", ".gz", ".tar", ".rar",
            ".bin", ".dat", ".db", ".sqlite"
        };

        /// <summary>
        /// Determines if a folder should be excluded from processing, based on config.ExcludedFolders.
        /// Supports exact name, prefix/suffix wildcards, and "*" catch-all.
        /// </summary>
        public static bool IsFolderExcluded(string folderName, VectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(folderName)) return true;

            if (config?.ExcludedFolders == null || config.ExcludedFolders.Count == 0)
                return false;

            var name = folderName.Trim();

            foreach (var pat in config.ExcludedFolders)
            {
                if (string.IsNullOrWhiteSpace(pat)) continue;
                if (MatchesPattern(name, pat, isFile: false))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a file should be excluded from processing, based on config.ExcludedFiles.
        /// Supports:
        /// - Exact name: "test.txt"
        /// - Prefix glob: "test.*"
        /// - Suffix glob: "*.txt"
        /// - Extension-only: ".log"
        /// - "*" catch-all
        /// </summary>
        public static bool IsFileExcluded(string fileName, VectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return true;

            if (config?.ExcludedFiles == null || config.ExcludedFiles.Count == 0)
                return false;

            var name = fileName.Trim();

            foreach (var pat in config.ExcludedFiles)
            {
                if (string.IsNullOrWhiteSpace(pat)) continue;
                if (MatchesPattern(name, pat, isFile: true))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Validates if a file is suitable for processing.
        /// Rules:
        /// - Path must exist and have non-zero length
        /// - Must not be the output file
        /// - Must not be a binary extension (see BinaryExtensions)
        /// </summary>
        public static bool IsFileValid(string path, string? outputPath)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            // Do not process the output file itself
            if (!string.IsNullOrEmpty(outputPath) &&
                string.Equals(path, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                var fi = new FileInfo(path);

                // File must exist
                if (!fi.Exists) return false;

                // File must have content
                if (fi.Length == 0) return false;

                // Skip binary files
                var ext = Path.GetExtension(path) ?? string.Empty;
                if (IsBinaryExtension(ext)) return false;

                return true;
            }
            catch
            {
                // Defensive: treat IO errors as invalid
                return false;
            }
        }

        /// <summary>
        /// Determines if a file extension is treated as binary.
        /// </summary>
        public static bool IsBinaryExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return false;
            return BinaryExtensions.Contains(extension);
        }

        // -----------------------
        // Pattern matching helper
        // -----------------------

        // Supports common, allocation-light patterns:
        // Files:
        //   - Exact name: "test.txt"
        //   - Prefix glob: "test.*"
        //   - Suffix glob: "*.txt"
        //   - Extension-only: ".log" (matches Path.GetExtension(input))
        //   - "*" catch-all
        // Folders:
        //   - Exact name, "prefix*", "*suffix", "*" catch-all
        private static bool MatchesPattern(string input, string pattern, bool isFile)
        {
            var p = pattern.Trim();
            if (p.Length == 0) return false;

            // Catch-all
            if (p == "*") return true;

            // Extension-only form for files: ".ext"
            if (isFile && p.StartsWith(".", StringComparison.Ordinal) && !p.Contains('*'))
            {
                var ext = Path.GetExtension(input) ?? string.Empty;
                // Match with or without leading dot tolerance
                if (ext.Equals(p, StringComparison.OrdinalIgnoreCase)) return true;
                if (ext.StartsWith(".", StringComparison.Ordinal))
                {
                    var extNoDot = ext.Substring(1);
                    var pNoDot = p.TrimStart('.');
                    if (extNoDot.Equals(pNoDot, StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }

            // Simple leading star: "*suffix"
            if (p.Length > 1 && p[0] == '*' && p.IndexOf('*', 1) < 0)
            {
                var suffix = p.Substring(1);
                return input.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            // Simple trailing star: "prefix*"
            if (p.Length > 1 && p[^1] == '*' && p.AsSpan(0, p.Length - 1).IndexOf('*') < 0)
            {
                var prefix = p.Substring(0, p.Length - 1);
                return input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            // Common file patterns:
            // - "name.*" => prefix + any extension
            // - "*.ext" => any name with given extension
            if (isFile && p.EndsWith(".*", StringComparison.Ordinal) && p.IndexOf('*') == p.Length - 1)
            {
                var prefix = p.Substring(0, p.Length - 2);
                return input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            if (isFile && p.StartsWith("*.", StringComparison.Ordinal) && p.IndexOf('*') == 0 && p.LastIndexOf('*') == 0)
            {
                var ext = Path.GetExtension(input) ?? string.Empty;
                return ext.Equals(p.Substring(1), StringComparison.OrdinalIgnoreCase);
            }

            // Fallback: translate limited glob to regex
            // Escape all regex chars, then expand '*' => '.*'
            string EscapeGlob(string s) => Regex.Escape(s).Replace(@"\*", ".*");
            var regex = "^" + EscapeGlob(p) + "$";
            return Regex.IsMatch(input, regex, RegexOptions.IgnoreCase);
        }
    }
}
