using GitIgnore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitIgnore.Services
{
    public sealed class IgnoreFileResolver
    {
        private readonly string _rootDirectory;
        // Key: full ignore file path; Value: parsed ignore file
        private readonly IReadOnlyDictionary<string, GitIgnoreFile> _ignoreFiles;

        public IgnoreFileResolver(string rootDirectory, IReadOnlyDictionary<string, GitIgnoreFile> ignoreFiles)
        {
            _rootDirectory = NormalizePath(rootDirectory);
            _ignoreFiles = ignoreFiles ?? throw new ArgumentNullException(nameof(ignoreFiles));
        }

        // PUBLIC API
        public IEnumerable<GitIgnoreFile> GetApplicableIgnoreFiles(string directoryPath)
        {
            var dir = NormalizePath(directoryPath);
            var root = _rootDirectory;

            // If dir is not equal to root and not beneath root, nothing applies
            if (!IsSameOrDescendant(dir, root))
                yield break;

            // Walk upwards: dir, parent(dir), ..., root
            var current = dir;
            while (true)
            {
                // Collect all ignore files whose parent directory == current
                foreach (var igf in GetIgnoreFilesInDirectory(current))
                    yield return igf;

                if (string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
                    break;

                var parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent))
                    break;

                current = NormalizePath(parent);
            }
        }

        // Returns ignore files where the file’s parent directory equals the given directory
        private IEnumerable<GitIgnoreFile> GetIgnoreFilesInDirectory(string directory)
        {
            foreach (var kvp in _ignoreFiles)
            {
                var filePath = NormalizePath(kvp.Key);
                var parentDir = NormalizePath(Path.GetDirectoryName(filePath)!);

                if (string.Equals(parentDir, directory, StringComparison.OrdinalIgnoreCase))
                    yield return kvp.Value;
            }
        }

        // Helpers

        private static string NormalizePath(string path)
        {
            // FullPath resolves relative/.. segments; we also normalize directory separators
            var full = Path.GetFullPath(path);
            // Ensure trailing separator is removed (compare by segments, not suffix)
            return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        // dir is same as root or is a descendant of root (segment-safe)
        private static bool IsSameOrDescendant(string dir, string root)
        {
            if (string.Equals(dir, root, StringComparison.OrdinalIgnoreCase))
                return true;

            // Add directory separator to enforce boundary, e.g., c:\a\ vs c:\aa\
            var rootWithSep = EnsureTrailingSeparator(root);
            var dirWithSep = EnsureTrailingSeparator(dir);

            return dirWithSep.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase);
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase))
                return path;
            return path + Path.DirectorySeparatorChar;
        }
    }
}
