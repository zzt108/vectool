// File: DocX/VecToolExtensions.cs - UPDATED VERSION
using oaiVectorStore;
using GitignoreParserNet;

namespace DocXHandler
{
    public static class VecToolExtensions
    {
        public static IEnumerable<string> EnumerateFilesRespectingGitIgnore(
            this string directoryPath,
            string searchPattern = "*.*",
            VectorStoreConfig _vectorStoreConfig = null)
        {
            if (!Directory.Exists(directoryPath))
                return Enumerable.Empty<string>();

            // Create GitignoreParser for the directory
            var (acceptedFiles, deniedFiles) = GitignoreParser.Parse(
                gitignorePath: Path.Combine(directoryPath, ".gitignore"),
                ignoreGitDirectory: true);

            // Also check for .vtignore files
            var vtIgnorePath = Path.Combine(directoryPath, ".vtignore");
            if (File.Exists(vtIgnorePath))
            {
                var (vtAccepted, vtDenied) = GitignoreParser.Parse(
                    gitignorePath: vtIgnorePath,
                    ignoreGitDirectory: false);

                // Combine results - vtignore takes precedence
                acceptedFiles = acceptedFiles.Except(vtDenied).Union(vtAccepted).ToList();
            }

            var result = new List<string>();
            foreach (var file in acceptedFiles)
            {
                if (IsMatchingPattern(Path.GetFileName(file), searchPattern))
                {
                    string extension = Path.GetExtension(file);

                    // Skip unknown and binary types
                    if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream")
                        continue;

                    if (MimeTypeProvider.IsBinary(extension))
                        continue;

                    result.Add(file);
                }
            }

            return result;
        }

        // Keep your existing helper methods
        private static bool IsMatchingPattern(string fileName, string pattern)
        {
            if (pattern == "*.*" || pattern == "*")
                return true;

            var regexPattern = "^" + pattern.Replace(".", @"\.").Replace("*", ".*").Replace("?", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
