//```C#
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VecTool.Configuration
{
    /// <summary>
    /// Root path resolution for vector store folder sets.
    /// </summary>
    public partial class VectorStoreConfig
    {
        /// <summary>
        /// Returns the common root of all FolderPaths; if multiple ancestor roots are possible,
        /// returns the nearest ancestor that contains a ".git" directory or one or more folders
        /// starting with "."; returns null if FolderPaths is empty or no common ancestor exists.
        /// </summary>
        public string? GetRootPath()
        {
            var paths = (FolderPaths ?? new List<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(NormalizePath)
            .Distinct(PathComparer)
            .ToList();

            if (paths.Count == 0)
                return null;

            if (paths.Count == 1)
                return TrimDirectorySeparator(paths.First());

            // Compute deepest common directory via segment-wise LCP
            var segmentsList = paths.Select(SplitSegments).ToList();
            var minLen = segmentsList.Min(s => s.Length);
            var common = new List<string>();

            for (int i = 0; i < minLen; i++)
            {
                var candidate = segmentsList[0][i];
                if (segmentsList.All(s => StringEquals(s[i], candidate)))
                {
                    common.Add(candidate);
                }
                else
                {
                    break;
                }
            }

            if (common.Count == 0)
                return null;

            var commonPath = Recombine(common);
            // If multiple ancestor roots are possible, prefer one with .git or dot-folders
            var promoted = PromoteToRepoRoot(commonPath);
            return TrimDirectorySeparator(promoted ?? commonPath);
        }

        private static string NormalizePath(string path)
        {
            // Normalize to full path and standard separators
            var full = Path.GetFullPath(path);
            return TrimDirectorySeparator(full);
        }

        private static string TrimDirectorySeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string[] SplitSegments(string path)
        {
            // Handle drive roots and UNC paths robustly by using DirectoryInfo
            // But to keep things simple and fast, split on both separators
            var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            // Keep leading "\\" or drive root semantics intact by not removing empty head segments
            return normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string Recombine(IEnumerable<string> segments)
        {
            var combined = string.Join(Path.DirectorySeparatorChar, segments);
            // Prepend root if needed (Windows drive letter or Unix root)
            if (OperatingSystem.IsWindows())
            {
                // If first segment has "C:"-like drive, ensure it stays as drive-rooted
                var first = segments.FirstOrDefault();
                if (!string.IsNullOrEmpty(first) && first.EndsWith(":", StringComparison.Ordinal))
                {
                    return first + Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar, segments.Skip(1));
                }
            }
            else
            {
                // On Unix, ensure leading separator for absolute paths
                return Path.DirectorySeparatorChar + combined;
            }

            // Fallback for already absolute inputs
            return Path.GetFullPath(combined);
        }

        private static bool StringEquals(string a, string b)
            => OperatingSystem.IsWindows()
                ? string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
                : string.Equals(a, b, StringComparison.Ordinal);

        private static readonly IEqualityComparer<string> PathComparer =
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

        private static bool ContainsRepoMarkers(string dir)
        {
            try
            {
                var gitDir = Path.Combine(dir, ".git");
                if (Directory.Exists(gitDir))
                    return true;

                foreach (var sub in Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(sub);
                    if (!string.IsNullOrEmpty(name) && name.StartsWith(".", StringComparison.Ordinal))
                        return true;
                }
            }
            catch
            {
                // Fail open: ignore IO errors and just return false
            }
            return false;
        }

        private static string? PromoteToRepoRoot(string commonPath)
        {
            try
            {
                var current = new DirectoryInfo(commonPath);
                DirectoryInfo? candidateWithMarkers = null;

                // Walk upwards; pick nearest ancestor that has repo markers
                while (current != null)
                {
                    if (ContainsRepoMarkers(current.FullName))
                    {
                        candidateWithMarkers = current;
                        break; // nearest preferred
                    }
                    current = current.Parent;
                }

                return candidateWithMarkers?.FullName;
            }
            catch
            {
                return null;
            }
        }
    }
}

//```