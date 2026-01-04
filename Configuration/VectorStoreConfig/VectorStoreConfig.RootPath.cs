//```C#
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

            if (paths.Count == 0) return null;
            if (paths.Count == 1) return TrimDirectorySeparator(paths.First());

            // Compute deepest common directory via segment-wise LCP
            var segmentsList = paths.Select(SplitSegments).ToList();
            var minLen = segmentsList.Min(s => s.Length);
            var common = new List<string>();

            for (int i = 0; i < minLen; i++)
            {
                var candidate = segmentsList[0][i];
                if (segmentsList.All(s => StringEquals(s[i], candidate)))
                    common.Add(candidate);
                else
                    break;
            }

            if (common.Count == 0) return null;

            var commonPath = Recombine(common);

            // Find the shortest (shallowest) configured path as the boundary
            // PromoteToRepoRoot should not walk above this boundary
            var shortestPath = paths
                .OrderBy(p => SplitSegments(p).Length)
                .First();

            // If multiple ancestor roots are possible, prefer one with .git or dot-folders
            // but only within the boundary of configured paths
            var promoted = PromoteToRepoRoot(commonPath, shortestPath);
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

        // Preserve drive letters and handle path splitting correctly
        private static string[] SplitSegments(string path)
        {
            // Normalize separators
            var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            // On Windows, handle drive letters specially
            if (OperatingSystem.IsWindows() && normalized.Length >= 2 && normalized[1] == ':')
            {
                // Extract drive (e.g., "C:") as first segment
                var drive = normalized.Substring(0, 2);
                var rest = normalized.Length > 3
                    ? normalized.Substring(3) // Skip "C:\"
                    : string.Empty;

                if (string.IsNullOrEmpty(rest))
                    return new[] { drive };

                var restSegments = rest.Split(new[] { Path.DirectorySeparatorChar },
                                              StringSplitOptions.RemoveEmptyEntries);

                return new[] { drive }.Concat(restSegments).ToArray();
            }

            // Unix or relative paths
            return normalized.Split(new[] { Path.DirectorySeparatorChar },
                                   StringSplitOptions.RemoveEmptyEntries);
        }

        // Fixed Windows path reconstruction
        private static string Recombine(IEnumerable<string> segments)
        {
            var segmentList = segments.ToList();
            if (segmentList.Count == 0) return string.Empty;

            // On Windows, check if first segment is a drive (e.g., "C:")
            if (OperatingSystem.IsWindows())
            {
                var first = segmentList[0];
                if (!string.IsNullOrEmpty(first) && first.EndsWith(":", StringComparison.Ordinal))
                {
                    // Drive letter present - reconstruct with proper separator
                    if (segmentList.Count == 1)
                        return first + Path.DirectorySeparatorChar;

                    return first + Path.DirectorySeparatorChar +
                           string.Join(Path.DirectorySeparatorChar, segmentList.Skip(1));
                }

                // No drive letter - relative path, use Path.GetFullPath
                var combined = string.Join(Path.DirectorySeparatorChar, segmentList);
                return Path.GetFullPath(combined);
            }
            else
            {
                // Unix: ensure leading separator for absolute paths
                var combined = string.Join(Path.DirectorySeparatorChar, segmentList);
                return Path.DirectorySeparatorChar + combined;
            }
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

        // boundary parameter to prevent walking above vector store scope
        private static string? PromoteToRepoRoot(string commonPath, string boundaryPath)
        {
            try
            {
                var current = new DirectoryInfo(commonPath);
                var boundary = new DirectoryInfo(boundaryPath);
                DirectoryInfo? candidateWithMarkers = null;

                // Walk upwards; pick nearest ancestor that has repo markers
                // BUT do not go above the boundary (shortest configured path)
                while (current != null)
                {
                    // Stop if we've reached or gone above the boundary
                    if (!IsAtOrBelow(current.FullName, boundary.FullName))
                        break;

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

        //Helper to check if a path is at or below another path
        private static bool IsAtOrBelow(string childPath, string parentPath)
        {
            try
            {
                var childFull = Path.GetFullPath(childPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var parentFull = Path.GetFullPath(parentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // On Windows, use case-insensitive comparison; on Unix, case-sensitive
                var comparison = OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

                // Child is at or below parent if it equals parent or starts with parent + separator
                return childFull.Equals(parentFull, comparison) ||
                       childFull.StartsWith(parentFull + Path.DirectorySeparatorChar, comparison);
            }
            catch
            {
                return false; // Defensive: treat path comparison errors as "not below"
            }
        }
    }
}

//```