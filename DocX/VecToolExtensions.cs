// File: DocX/VecToolExtensions.cs - UPDATED VERSION
using oaiVectorStore;
using GitignoreParserNet;

namespace DocXHandler
{
    public static class VecToolExtensions
    {
        public static IEnumerable<string> EnumerateFilesRespectingGitIgnore(
            this string directoryPath,
            VectorStoreConfig _vectorStoreConfig,
            string searchPattern = "*.*")
        {
            if (!Directory.Exists(directoryPath))
                return Enumerable.Empty<string>();

            var gitignorePath = Path.Combine(directoryPath, ".gitignore");
            IEnumerable<string> acceptedFiles = new List<string>();

            // Only parse if .gitignore exists
            if (File.Exists(gitignorePath))
            {
                (acceptedFiles, var deniedFiles) = GitignoreParser.Parse(
                    gitignorePath: gitignorePath, System.Text.Encoding.UTF8);
            }
            else
            {
                // No .gitignore = accept all files in directory
                acceptedFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories).ToList();
            }

            // Rest of the method unchanged...
            var vtIgnorePath = Path.Combine(directoryPath, ".vtignore");
            if (File.Exists(vtIgnorePath))
            {
                (var vtAccepted, var vtDenied) = GitignoreParser.Parse(
                    gitignorePath: vtIgnorePath, System.Text.Encoding.UTF8);
                acceptedFiles = acceptedFiles.Except(vtDenied).Union(vtAccepted).ToList();
            }

            var result = new List<string>();
            foreach (var file in acceptedFiles)
            {
                if (IsMatchingPattern(Path.GetFileName(file), searchPattern))
                {
                    string extension = Path.GetExtension(file);
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
