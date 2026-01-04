// Path: Core/RepoLocator.cs
namespace VecTool.Core
{
    /// <summary>
    /// Locates Git repository roots and selects the best working directory for Git operations.
    /// </summary>
    public static class RepoLocator
    {
        /// <summary>
        /// Walks up from startPath to find the first directory that contains a .git marker.
        /// Returns the repo root if found, otherwise null.
        /// </summary>
        public static string? FindRepoRoot(string? startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return null;

            var dir = new DirectoryInfo(startPath);
            while (dir != null)
            {
                var gitDir = Path.Combine(dir.FullName, ".git");
                if (Directory.Exists(gitDir) || File.Exists(gitDir))
                    return dir.FullName;

                dir = dir.Parent;
            }
            return null;
        }

        /// <summary>
        /// From a list of folders, returns the first detected repo root if any; 
        /// otherwise returns the first existing folder; otherwise null.
        /// </summary>
        public static string? ResolvePreferredWorkingDirectory(IReadOnlyList<string> selectedFolders)
        {
            if (selectedFolders == null || selectedFolders.Count == 0)
                return null;

            string? firstExisting = null;
            foreach (var folder in selectedFolders)
            {
                if (string.IsNullOrWhiteSpace(folder))
                    continue;

                if (firstExisting is null && Directory.Exists(folder))
                    firstExisting = folder;

                var repoRoot = FindRepoRoot(folder);
                if (!string.IsNullOrWhiteSpace(repoRoot))
                    return repoRoot;
            }
            return firstExisting;
        }
    }
}
