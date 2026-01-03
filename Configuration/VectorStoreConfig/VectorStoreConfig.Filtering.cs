using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace VecTool.Configuration
{
    /// <summary>
    /// Filtering operations for VectorStoreConfig (exclusion rules).
    /// </summary>
    public partial class VectorStoreConfig
    {
        private static string WildcardToRegex(string pattern, bool forFolder)
        {
            // Handle 'no extension' intent: '*.' should match names without any dot
            if (string.Equals(pattern, "*.", StringComparison.Ordinal))
                return "^[^.]+$"; // no '.' anywhere => no extension [attached_file:2]

            // Escape regex, then restore wildcards
            var escaped = Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", "."); // support single-char wildcard [attached_file:2]

            // Folder/file names should match the whole string
            return $"^{escaped}$"; // anchor to avoid substring matches [attached_file:2]
        }

        /// <summary>
        /// Check if a file should be excluded based on patterns.
        /// </summary>
        public bool IsFileExcluded(string fileName)
        {
            var nameOnly = Path.GetFileName(fileName) ?? fileName; // robust matching [attached_file:2]
            foreach (var pattern in ExcludedFiles)
            {
                // Fast path for '*.' using API semantics
                if (string.Equals(pattern, "*.", StringComparison.Ordinal))
                {
                    if (!Path.HasExtension(nameOnly))
                    {
                        logger.LogTrace($"File {nameOnly} excluded by pattern {pattern}");
                        return true;
                    }
                    continue;
                }

                string regexPattern = WildcardToRegex(pattern, forFolder: false);
                if (Regex.IsMatch(nameOnly, regexPattern, RegexOptions.IgnoreCase))
                {
                    logger.LogTrace($"File {nameOnly} excluded by pattern {pattern}");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a folder should be excluded.
        /// </summary>
        public bool IsFolderExcluded(string folderName)
        {
            var nameOnly = folderName; // already a folder segment; adjust if full path is passed [attached_file:2]
            foreach (var pattern in ExcludedFolders)
            {
                var patternNormalized = pattern;
                if (pattern.EndsWith("/", StringComparison.Ordinal))
                    patternNormalized = pattern.Substring(0, pattern.Length - 1);
                string regexPattern = WildcardToRegex(patternNormalized, forFolder: true);
                if (Regex.IsMatch(nameOnly, regexPattern, RegexOptions.IgnoreCase))
                {
                    logger.LogTrace($"Folder {nameOnly} excluded by pattern {pattern}");
                    return true;
                }
            }
            return false;
        }
    }
}