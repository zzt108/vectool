using GitIgnore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitIgnore.Services
{
    /// <summary>
    /// Manages hierarchical .gitignore files and provides ignore checking functionality
    /// Following Git's precedence rules: child .gitignore patterns override parent patterns
    /// </summary>
    public class HierarchicalGitIgnoreManager: IDisposable
    {
        private readonly Dictionary<string, GitIgnoreFile> _gitIgnoreFiles;
        private readonly string _rootDirectory;
        private readonly bool _cacheEnabled;

        public HierarchicalGitIgnoreManager(string rootDirectory, bool enableCache = true)
        {
            _rootDirectory = Path.GetFullPath(rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory)));
            _cacheEnabled = enableCache;
            _gitIgnoreFiles = new Dictionary<string, GitIgnoreFile>(StringComparer.OrdinalIgnoreCase);
            
            if (!Directory.Exists(_rootDirectory))
                throw new DirectoryNotFoundException($"Root directory not found: {_rootDirectory}");

            LoadGitIgnoreFiles();
        }

        /// <summary>
        /// Discovers and loads all .gitignore files in the directory hierarchy
        /// </summary>
        private void LoadGitIgnoreFiles()
        {
            try
            {
                var gitIgnoreFiles = Directory.GetFiles(_rootDirectory, ".gitignore", SearchOption.AllDirectories);
                
                foreach (var filePath in gitIgnoreFiles)
                {
                    var directory = Path.GetDirectoryName(filePath);
                    var normalizedDir = NormalizePath(directory);
                    
                    if (!_gitIgnoreFiles.ContainsKey(normalizedDir))
                    {
                        _gitIgnoreFiles[normalizedDir] = new GitIgnoreFile(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load .gitignore files from hierarchy", ex);
            }
        }

        /// <summary>
        /// Refreshes all cached .gitignore files if caching is enabled
        /// </summary>
        public void RefreshCache()
        {
            if (!_cacheEnabled)
                return;

            foreach (var gitIgnoreFile in _gitIgnoreFiles.Values)
            {
                gitIgnoreFile.RefreshIfNeeded();
            }

            // Check for new .gitignore files
            LoadGitIgnoreFiles();
        }

        /// <summary>
        /// Determines if a file or directory should be ignored based on hierarchical .gitignore rules
        /// </summary>
        /// <param name="fullPath">Full path to the file or directory</param>
        /// <param name="isDirectory">Whether the path represents a directory</param>
        /// <returns>True if the path should be ignored</returns>
        public bool ShouldIgnore(string fullPath, bool isDirectory)
        {
            if (string.IsNullOrEmpty(fullPath))
                return false;

            var normalizedPath = NormalizePath(fullPath);
            
            // Path must be within our root directory
            if (!normalizedPath.StartsWith(NormalizePath(_rootDirectory), StringComparison.OrdinalIgnoreCase))
                return false;

            var relativePath = GetRelativePath(_rootDirectory, normalizedPath);
            var pathDirectory = isDirectory ? normalizedPath : Path.GetDirectoryName(normalizedPath);

            // Get all applicable .gitignore files in order (root to specific)
            var applicableGitIgnores = GetApplicableGitIgnoreFiles(pathDirectory);

            var result = IgnoreResult.NotMatched;

            // Process .gitignore files from root to most specific (child overrides parent)
            foreach (var gitIgnoreFile in applicableGitIgnores.OrderBy(g => g.Directory.Length))
            {
                var relativeToGitIgnore = GetRelativePath(gitIgnoreFile.Directory, normalizedPath);
                var checkResult = gitIgnoreFile.CheckPath(relativeToGitIgnore, isDirectory);

                if (checkResult != IgnoreResult.NotMatched)
                {
                    result = checkResult;
                }
            }

            return result == IgnoreResult.Ignored;
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
        /// Gets .gitignore files that apply to the given directory path
        /// </summary>
        private List<GitIgnoreFile> GetApplicableGitIgnoreFiles(string directoryPath)
        {
            var applicableFiles = new List<GitIgnoreFile>();
            var currentDir = NormalizePath(directoryPath);
            var rootDir = NormalizePath(_rootDirectory);

            // Walk up the directory tree to find applicable .gitignore files
            while (currentDir.StartsWith(rootDir, StringComparison.OrdinalIgnoreCase))
            {
                if (_gitIgnoreFiles.TryGetValue(currentDir, out var gitIgnoreFile))
                {
                    applicableFiles.Add(gitIgnoreFile);
                }

                if (string.Equals(currentDir, rootDir, StringComparison.OrdinalIgnoreCase))
                    break;

                var parentDir = Path.GetDirectoryName(currentDir);
                if (string.IsNullOrEmpty(parentDir) || parentDir == currentDir)
                    break;

                currentDir = NormalizePath(parentDir);
            }

            return applicableFiles;
        }

        /// <summary>
        /// Normalizes path separators and ensures consistent formatting
        /// </summary>
        private string NormalizePath(string path)
        {
            if (!string.IsNullOrEmpty(path.Trim()))
            {
                return Path.GetFullPath(path).Replace('/', '\\');
            }

            return path;
        }

        /// <summary>
        /// Gets relative path from base to target
        /// </summary>
        private string GetRelativePath(string basePath, string targetPath)
        {
            var baseUri = new Uri(NormalizePath(basePath) + "\\");
            var targetUri = new Uri(NormalizePath(targetPath));
            
            return baseUri.MakeRelativeUri(targetUri).ToString().Replace('/', '\\');
        }

        /// <summary>
        /// Gets statistics about loaded .gitignore files
        /// </summary>
        public GitIgnoreStatistics GetStatistics()
        {
            var totalPatterns = _gitIgnoreFiles.Values.Sum(f => f.Patterns.Count);
            var negationPatterns = _gitIgnoreFiles.Values.Sum(f => f.Patterns.Count(p => p.IsNegation));
            var directoryOnlyPatterns = _gitIgnoreFiles.Values.Sum(f => f.Patterns.Count(p => p.IsDirectoryOnly));

            return new GitIgnoreStatistics
            {
                GitIgnoreFileCount = _gitIgnoreFiles.Count,
                TotalPatterns = totalPatterns,
                NegationPatterns = negationPatterns,
                DirectoryOnlyPatterns = directoryOnlyPatterns,
                RootDirectory = _rootDirectory
            };
        }

        public void Dispose()
        {
            _gitIgnoreFiles?.Clear();
        }
    }
}