using GitIgnore.Services;
using oaiVectorStore;

namespace DocXHandler
{
    /// <summary>
    /// Extension methods to integrate GitIgnore functionality with existing VecTool components
    /// </summary>
    public static class VecToolExtensions
    {
        /// <summary>
        /// Extends existing file enumeration to respect .gitignore patterns
        /// </summary>
        /// <param name="directoryPath">Directory to scan</param>
        /// <param name="searchPattern">File search pattern (e.g., "*.*")</param>
        /// <param name="respectGitIgnore">Whether to respect .gitignore files</param>
        /// <returns>Enumerable of non-ignored files</returns>
        public static IEnumerable<string> EnumerateFilesRespectingGitIgnore(
            this string directoryPath,
            string searchPattern = "*.*",
            bool respectGitIgnore = true, VectorStoreConfig _vectorStoreConfig = null)
        {
            IEnumerable<string>? files = null;

            if (!Directory.Exists(directoryPath))
                return Enumerable.Empty<string>();

            if (!respectGitIgnore)
            {
                files = Directory.EnumerateFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
            }

            using var processor = new GitIgnoreAwareFileProcessor(directoryPath);
            files = processor.GetNonIgnoredFiles(directoryPath)
                .Where(f => IsMatchingPattern(f.Name, searchPattern))
                .Select(f => f.FullName);

            var result = new List<string>();
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                // Check MIME type and binary
                string extension = Path.GetExtension(file);
                if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream") // Skip unknown types
                {
                    continue;
                }

                if (MimeTypeProvider.IsBinary(extension)) // non text types should be uploaded separately
                {
                    continue;
                }
                result.Add(file);

            }
            return result;

        }

        /// <summary>
        /// Filters a collection of file paths based on .gitignore patterns
        /// </summary>
        /// <param name="filePaths">Collection of file paths to filter</param>
        /// <param name="rootDirectory">Root directory containing .gitignore files</param>
        /// <returns>Filtered collection of non-ignored paths</returns>
        public static IEnumerable<string> FilterByGitIgnore(
            this IEnumerable<string> filePaths,
            string rootDirectory)
        {
            if (filePaths == null || !Directory.Exists(rootDirectory))
                return Enumerable.Empty<string>();

            using var manager = new HierarchicalIgnoreManager(rootDirectory);

            return filePaths.Where(path =>
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                var isDirectory = Directory.Exists(path);
                return !manager.ShouldIgnore(path, isDirectory);
            });
        }

        /// <summary>
        /// Creates a GitIgnore-aware file processor for batch operations
        /// </summary>
        /// <param name="rootDirectory">Root directory</param>
        /// <param name="configureOptions">Optional configuration action</param>
        /// <returns>Configured GitIgnoreAwareFileProcessor</returns>
        public static GitIgnoreAwareFileProcessor CreateGitIgnoreProcessor(
            this string rootDirectory,
            Action<FileProcessorOptions> configureOptions = null)
        {
            var options = new FileProcessorOptions();
            configureOptions?.Invoke(options);

            return new GitIgnoreAwareFileProcessor(rootDirectory, options);
        }

        /// <summary>
        /// Checks if a single file should be ignored based on .gitignore patterns
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <param name="rootDirectory">Root directory containing .gitignore files</param>
        /// <returns>True if file should be ignored</returns>
        public static bool IsIgnoredByGit(this string filePath, string rootDirectory)
        {
            if (string.IsNullOrEmpty(filePath) || !Directory.Exists(rootDirectory))
                return false;

            using var manager = new HierarchicalIgnoreManager(rootDirectory);
            var isDirectory = Directory.Exists(filePath);
            return manager.ShouldIgnore(filePath, isDirectory);
        }

        /// <summary>
        /// Gets GitIgnore statistics for a directory
        /// </summary>
        /// <param name="rootDirectory">Root directory to analyze</param>
        /// <returns>GitIgnore statistics</returns>
        public static GitIgnoreStatistics GetGitIgnoreStats(this string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
                throw new DirectoryNotFoundException($"Directory not found: {rootDirectory}");

            using var manager = new HierarchicalIgnoreManager(rootDirectory);
            return manager.GetStatistics();
        }

        /// <summary>
        /// Processes files in a directory with custom action, respecting .gitignore
        /// </summary>
        /// <param name="directoryPath">Directory to process</param>
        /// <param name="fileAction">Action to perform on each file</param>
        /// <param name="options">Processing options</param>
        public static void ProcessFilesRespectingGitIgnore(
            this string directoryPath,
            Action<FileInfo> fileAction,
            FileProcessorOptions options = null)
        {
            if (!Directory.Exists(directoryPath) || fileAction == null)
                return;

            using var processor = new GitIgnoreAwareFileProcessor(directoryPath, options);
            processor.ProcessDirectory(directoryPath, fileAction);
        }

        /// <summary>
        /// Simple pattern matching for file names (supports * and ? wildcards)
        /// </summary>
        private static bool IsMatchingPattern(string fileName, string pattern)
        {
            if (pattern == "*.*" || pattern == "*")
                return true;

            // Convert simple wildcards to regex
            var regexPattern = "^" + pattern.Replace(".", @"\.").Replace("*", ".*").Replace("?", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }

    /// <summary>
    /// Fluent builder for configuring GitIgnore processing options
    /// </summary>
    public static class GitIgnoreOptionsBuilder
    {
        public static FileProcessorOptions Create()
        {
            return new FileProcessorOptions();
        }

        public static FileProcessorOptions WithCache(this FileProcessorOptions options, bool enableCache = true)
        {
            options.EnableCache = enableCache;
            return options;
        }

        public static FileProcessorOptions WithAutoRefresh(this FileProcessorOptions options, bool autoRefresh = true)
        {
            options.AutoRefreshCache = autoRefresh;
            return options;
        }

        public static FileProcessorOptions IncludeHiddenFiles(this FileProcessorOptions options, bool include = true)
        {
            options.IncludeHiddenFiles = include;
            return options;
        }

        public static FileProcessorOptions IncludeHiddenDirectories(this FileProcessorOptions options, bool include = true)
        {
            options.IncludeHiddenDirectories = include;
            return options;
        }

        public static FileProcessorOptions ContinueOnErrors(this FileProcessorOptions options, bool continueOnError = true)
        {
            options.ContinueOnError = continueOnError;
            return options;
        }
    }
}