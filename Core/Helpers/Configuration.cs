using VecTool.Configuration;

namespace VecTool.Core.Helpers
{
    public static class Configuration
    {
        public static string? ResolvePreferredWorkingDirectory(IReadOnlyList<string> folders)
        {
            if (folders == null || folders.Count == 0)
                return null;

            string? firstExisting = null;
            foreach (var folder in folders)
            {
                if (string.IsNullOrWhiteSpace(folder))
                    continue;

                if (firstExisting is null && Directory.Exists(folder))
                    firstExisting = folder;

                var root = RepoLocator.FindRepoRoot(folder);
                if (!string.IsNullOrWhiteSpace(root))
                    return root;
            }

            return firstExisting;
        }

        public static string[] FindSolutionFiles(VectorStoreConfig vsConfig)
        {
            // Guard and prepare containers
            if (vsConfig == null) return Array.Empty<string>();
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roots = new List<string>();

            // Collect candidate roots from configured folders and their repo roots
            foreach (var p in vsConfig.FolderPaths ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                if (!Directory.Exists(p)) continue;

                roots.Add(p);

                // Reuse the existing helper to locate the repo root if present
                var repoRoot = RepoLocator.FindRepoRoot(p);
                if (!string.IsNullOrWhiteSpace(repoRoot))
                    roots.Add(repoRoot!);
            }

            // De-duplicate candidate roots
            foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                // Walk upward from each root, collecting *.sln at each level
                var dir = new DirectoryInfo(root);
                while (dir != null)
                {
                    try
                    {
                        foreach (var sln in Directory.EnumerateFiles(dir.FullName, "*.sln", SearchOption.TopDirectoryOnly))
                            found.Add(Path.GetFullPath(sln));
                    }
                    catch
                    {
                        // Ignore probing errors and continue upward
                    }

                    dir = dir.Parent;
                }
            }

            // Return a stable, sorted list
            return found.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
        }

    }
}
