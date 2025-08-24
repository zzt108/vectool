// File: DocX/GitIgnoreWrapper.cs - NEW FILE
using GitignoreParserNet;
using NLogS = NLogShared;

namespace DocXHandler
{
    /// <summary>
    /// Wrapper around GitignoreParserNet to provide VecTool-specific functionality
    /// </summary>
    public class GitIgnoreWrapper : IDisposable
    {
        private static readonly NLogS.CtxLogger _log = new();
        private readonly string _rootDirectory;
        private readonly Dictionary<string, (List<string> accepted, List<string> denied)> _cache;
        private readonly bool _enableCache;

        public GitIgnoreWrapper(string rootDirectory, bool enableCache = true)
        {
            _rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            _enableCache = enableCache;
            _cache = new Dictionary<string, (List<string>, List<string>)>();
        }

        /// <summary>
        /// Gets all non-ignored files from a directory, supporting both .gitignore and .vtignore
        /// </summary>
        public IEnumerable<string> GetNonIgnoredFiles(string directoryPath, bool recursive = true)
        {
            if (!Directory.Exists(directoryPath))
                return Enumerable.Empty<string>();

            var cacheKey = $"{directoryPath}|{recursive}";

            if (_enableCache && _cache.ContainsKey(cacheKey))
            {
                return _cache[cacheKey].accepted;
            }

            try
            {
                var acceptedFiles = new List<string>();
                var deniedFiles = new List<string>();

                // Process .gitignore
                var gitignorePath = FindGitignoreFile(directoryPath);
                if (!string.IsNullOrEmpty(gitignorePath))
                {
                    var (gitAccepted, gitDenied) = GitignoreParser.Parse(
                        gitignorePath: gitignorePath,
                        ignoreGitDirectory: true);

                    acceptedFiles.AddRange(gitAccepted);
                    deniedFiles.AddRange(gitDenied);
                }

                // Process .vtignore files
                var vtIgnoreFiles = FindVtignoreFiles(directoryPath);
                foreach (var vtIgnorePath in vtIgnoreFiles)
                {
                    var (vtAccepted, vtDenied) = GitignoreParser.Parse(
                        gitignorePath: vtIgnorePath,
                        ignoreGitDirectory: false);

                    // VTIgnore takes precedence over gitignore
                    acceptedFiles = acceptedFiles.Except(vtDenied).Union(vtAccepted).ToList();
                }

                // Cache results
                if (_enableCache)
                {
                    _cache[cacheKey] = (acceptedFiles, deniedFiles);
                }

                return acceptedFiles;
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error processing gitignore files for {directoryPath}");
                // Fallback: return all files
                return Directory.GetFiles(directoryPath, "*",
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
        }

        /// <summary>
        /// Checks if a specific file should be ignored
        /// </summary>
        public bool ShouldIgnore(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath) ?? _rootDirectory;
            var nonIgnoredFiles = GetNonIgnoredFiles(directory, true);
            return !nonIgnoredFiles.Contains(filePath);
        }

        private string FindGitignoreFile(string startDirectory)
        {
            var current = startDirectory;
            while (current != null && current.StartsWith(_rootDirectory))
            {
                var gitignorePath = Path.Combine(current, ".gitignore");
                if (File.Exists(gitignorePath))
                    return gitignorePath;

                current = Path.GetDirectoryName(current);
            }
            return null;
        }

        private IEnumerable<string> FindVtignoreFiles(string startDirectory)
        {
            var vtIgnoreFiles = new List<string>();
            var current = startDirectory;

            while (current != null && current.StartsWith(_rootDirectory))
            {
                var vtIgnorePattern = Path.Combine(current, "*.vtignore");
                vtIgnoreFiles.AddRange(Directory.GetFiles(current, "*.vtignore"));

                current = Path.GetDirectoryName(current);
            }

            return vtIgnoreFiles;
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public void Dispose()
        {
            _cache.Clear();
        }
    }
}
