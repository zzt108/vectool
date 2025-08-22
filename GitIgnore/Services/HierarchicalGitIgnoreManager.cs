// File: ExtendedHierarchicalIgnoreManager.cs
using GitIgnore.Models;

namespace GitIgnore.Services
{
    public class HierarchicalIgnoreManager : IDisposable
    {
        private readonly Dictionary<string, GitIgnoreFile> _ignoreFiles;
        private readonly string _rootDirectory;
        private readonly bool _cacheEnabled;
        private readonly string[] _additionalIgnorePatterns = new[] {".gitignore", "*.vtignore" };

        public HierarchicalIgnoreManager(string rootDirectory, bool enableCache = true)
        {
            _rootDirectory = Path.GetFullPath(rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory)));
            _cacheEnabled = enableCache;
            _ignoreFiles = new Dictionary<string, GitIgnoreFile>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(_rootDirectory))
                throw new DirectoryNotFoundException($"Root directory not found: {_rootDirectory}");

            LoadIgnoreFiles();
        }

        private void LoadIgnoreFiles()
        {
            // Discover both .gitignore and *.vtignore
            var patterns = _additionalIgnorePatterns;
            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(_rootDirectory, pattern, SearchOption.AllDirectories);
                foreach (var filePath in files)
                {
                    var dirKey = NormalizePath(filePath);
                    if (!_ignoreFiles.ContainsKey(dirKey))
                    {
                        _ignoreFiles[dirKey] = new GitIgnoreFile(filePath);
                    }
                }
            }
        }

        public void RefreshCache()
        {
            if (!_cacheEnabled) return;
            foreach (var igf in _ignoreFiles.Values)
                igf.RefreshIfNeeded();

            // Check for newly added ignore files
            LoadIgnoreFiles();
        }

        public bool ShouldIgnore(string fullPath, bool isDirectory)
        {
            if (string.IsNullOrEmpty(fullPath)) return false;

            var normalized = NormalizePath(fullPath);
            if (!normalized.StartsWith(NormalizePath(_rootDirectory), StringComparison.OrdinalIgnoreCase))
                return false;

            var relative = GetRelativePath(_rootDirectory, normalized);
            var containingDir = isDirectory ? normalized : Path.GetDirectoryName(normalized)!;

            var sut = new IgnoreFileResolver(_rootDirectory, _ignoreFiles);

            var applicable = new List<GitIgnoreFile>(sut.GetApplicableIgnoreFiles(containingDir)).OrderBy(f => f.Directory.Length);

            var result = IgnoreResult.NotMatched;
            foreach (var igf in applicable)
            {
                var relToIgnore = GetRelativePath(igf.Directory, normalized);
                var check = igf.CheckPath(relToIgnore, isDirectory);
                if (check != IgnoreResult.NotMatched)
                    result = check;
            }

            return result == IgnoreResult.Ignored;
        }

        public void Dispose()
        {
            _ignoreFiles.Clear();
        }

        /// <summary>
        /// replaces slashes (/) with backslashes (\), removes last slash
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string NormalizePath(string path) => string.IsNullOrEmpty(path.Trim()) ? path :
            Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar).Replace('/', '\\');

        private static string GetRelativePath(string basePath, string targetPath)
        {
            var baseUri = new Uri(NormalizePath(basePath) + Path.DirectorySeparatorChar);
            var targetUri = new Uri(NormalizePath(targetPath));
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(targetUri).ToString())
                      .Replace('/', '\\');
        }

        /// <summary>
        /// Gets all files and directories that should NOT be ignored in the given directory
        /// </summary>
        /// <param name="directoryPath">Directory to scan</param>
        /// <param name="recursive">Whether to scan recursively</param>
        /// <returns>List of non-ignored paths</returns>
        public IEnumerable<string> GetNonIgnoredPaths(string directoryPath, bool recursive = true)
        {
            if (!Directory.Exists(directoryPath))
                yield break;

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // Get all files
            foreach (var filePath in Directory.GetFiles(directoryPath, "*", searchOption))
            {
                if (!ShouldIgnore(filePath, false))
                    yield return filePath;
            }

            // Get all directories
            foreach (var dirPath in Directory.GetDirectories(directoryPath, "*", searchOption))
            {
                if (!ShouldIgnore(dirPath, true))
                    yield return dirPath;
            }
        }

        /// <summary>
        /// Gets statistics about loaded .gitignore files
        /// </summary>
        public GitIgnoreStatistics GetStatistics()
        {
            var totalPatterns = _ignoreFiles.Values.Sum(f => f.Patterns.Count);
            var negationPatterns = _ignoreFiles.Values.Sum(f => f.Patterns.Count(p => p.IsNegation));
            var directoryOnlyPatterns = _ignoreFiles.Values.Sum(f => f.Patterns.Count(p => p.IsDirectoryOnly));

            return new GitIgnoreStatistics
            {
                GitIgnoreFileCount = _ignoreFiles.Count,
                TotalPatterns = totalPatterns,
                NegationPatterns = negationPatterns,
                DirectoryOnlyPatterns = directoryOnlyPatterns,
                RootDirectory = _rootDirectory
            };
        }

    }
}
