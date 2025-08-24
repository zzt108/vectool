// File: DocX/VecToolExtensions.cs - UPDATED VERSION
using GitignoreParserNet;
using oaiVectorStore;
using System.Text;

namespace DocXHandler
{
    public static class VecToolExtensions
    {
        public static IEnumerable<string> EnumerateFilesRespectingGitIgnore(
            this string directoryPath,
            VectorStoreConfig config)
        {
            // 1. Find all ignore files from root to `directoryPath`
            var ignoreFiles = config.FolderPaths
                   .Where(root => directoryPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                   .SelectMany(root => TraverseUp(directoryPath, root))
                   .Where(f => Path.GetFileName(f) is ".gitignore" or ".vtignore")
                    .OrderBy(f => f.Length)  // root first, child later
                    .ToList();

            // 2. Initialize allowed list with all files under `directoryPath`
            var allFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories).ToList();
            var allowed = new HashSet<string>(allFiles, StringComparer.OrdinalIgnoreCase);

            // 3. Apply each ignore file in sequence
            foreach (var ignoreFile in ignoreFiles)
            {
                (var accept, var deny) = GitignoreParser.Parse(ignoreFile, Encoding.UTF8);
                // Remove denied, then re-add any accepted (negations)
                foreach (var d in deny) allowed.Remove(d);
                foreach (var a in accept) allowed.Add(a);
            }

            return allowed;
        }

        /// <summary>
        /// Enumerates each directory from <paramref name="startDirectory"/> up to (and including)
        /// <paramref name="rootDirectory"/>, stopping when <paramref name="rootDirectory"/> is reached
        /// or when the file system root is encountered.
        /// </summary>
        /// <param name="startDirectory">The starting directory path.</param>
        /// <param name="rootDirectory">The directory at which to stop (inclusive). Comparison is case-insensitive.</param>
        /// <returns>An IEnumerable of directory paths from start up to root.</returns>
        public static IEnumerable<string> TraverseUp(string startDirectory, string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(startDirectory))
                throw new ArgumentException("Start directory must not be null or empty.", nameof(startDirectory));
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException("Root directory must not be null or empty.", nameof(rootDirectory));

            var current = Path.GetFullPath(startDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var root = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Ensure the start is under the root
            if (!current.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"The start directory '{current}' is not a subdirectory of root '{root}'.");

            while (true)
            {
                yield return current;

                if (string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
                    yield break;

                var parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent))
                    yield break; // reached filesystem root without matching the specified rootDirectory

                current = parent;
            }
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
