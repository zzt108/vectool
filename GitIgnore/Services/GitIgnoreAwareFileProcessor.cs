namespace GitIgnore.Services
{
    /// <summary>
    /// Processes files and directories while respecting .gitignore patterns
    /// Integrates with existing VecTool file processing pipeline
    /// </summary>
    public class GitIgnoreAwareFileProcessor : IDisposable
    {
        private readonly HierarchicalIgnoreManager _gitIgnoreManager;
        private readonly FileProcessorOptions _options;

        public GitIgnoreAwareFileProcessor(string rootDirectory, FileProcessorOptions options = null)
        {
            _gitIgnoreManager = new HierarchicalIgnoreManager(rootDirectory, options?.EnableCache ?? true);
            _options = options ?? new FileProcessorOptions();
        }

        /// <summary>
        /// Processes directory recursively, respecting .gitignore patterns
        /// </summary>
        /// <param name="directoryPath">Root directory to process</param>
        /// <param name="fileProcessor">Action to perform on each non-ignored file</param>
        /// <param name="directoryProcessor">Optional action to perform on each non-ignored directory</param>
        public void ProcessDirectory(
            string directoryPath, 
            Action<FileInfo> fileProcessor,
            Action<DirectoryInfo> directoryProcessor = null)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            if (fileProcessor == null)
                throw new ArgumentNullException(nameof(fileProcessor));

            ProcessDirectoryRecursive(new DirectoryInfo(directoryPath), fileProcessor, directoryProcessor);
        }

        /// <summary>
        /// Recursively processes directory contents
        /// </summary>
        private void ProcessDirectoryRecursive(
            DirectoryInfo directory, 
            Action<FileInfo> fileProcessor,
            Action<DirectoryInfo> directoryProcessor)
        {
            // Refresh .gitignore cache if needed
            if (_options.EnableCache && _options.AutoRefreshCache)
                _gitIgnoreManager.RefreshCache();

            try
            {
                if (_gitIgnoreManager.ShouldIgnore(directory.FullName, true))
                    return;
                    
                // Process files in current directory
                foreach (var file in directory.GetFiles())
                {
                    if (_options.IncludeHiddenFiles || !IsHidden(file))
                    {
                        if (!_gitIgnoreManager.ShouldIgnore(file.FullName, false))
                        {
                            try
                            {
                                fileProcessor(file);
                                ProcessedFileCount++;
                            }
                            catch (Exception ex) when (_options.ContinueOnError)
                            {
                                OnErrorOccurred(new FileProcessingError
                                {
                                    FilePath = file.FullName,
                                    Exception = ex,
                                    ErrorType = ProcessingErrorType.FileProcessing
                                });
                            }
                        }
                    }
                }

                // Process subdirectories
                foreach (var subDir in directory.GetDirectories())
                {
                    if (_options.IncludeHiddenDirectories || !IsHidden(subDir))
                    {
                        if (!_gitIgnoreManager.ShouldIgnore(subDir.FullName, true))
                        {
                            try
                            {
                                // Process the directory itself if processor provided
                                directoryProcessor?.Invoke(subDir);
                                ProcessedDirectoryCount++;

                                // Recurse into subdirectory
                                ProcessDirectoryRecursive(subDir, fileProcessor, directoryProcessor);
                            }
                            catch (Exception ex) when (_options.ContinueOnError)
                            {
                                OnErrorOccurred(new FileProcessingError
                                {
                                    FilePath = subDir.FullName,
                                    Exception = ex,
                                    ErrorType = ProcessingErrorType.DirectoryProcessing
                                });
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex) when (_options.ContinueOnError)
            {
                OnErrorOccurred(new FileProcessingError
                {
                    FilePath = directory.FullName,
                    Exception = ex,
                    ErrorType = ProcessingErrorType.AccessDenied
                });
            }
        }

        /// <summary>
        /// Gets all non-ignored files in the specified directory
        /// </summary>
        public IEnumerable<FileInfo> GetNonIgnoredFiles(string directoryPath, bool recursive = true)
        {
            return _gitIgnoreManager.GetNonIgnoredPaths(directoryPath, recursive)
                .Where(path => File.Exists(path))
                .Select(path => new FileInfo(path));
        }

        /// <summary>
        /// Gets all non-ignored directories in the specified directory
        /// </summary>
        public IEnumerable<DirectoryInfo> GetNonIgnoredDirectories(string directoryPath, bool recursive = true)
        {
            return _gitIgnoreManager.GetNonIgnoredPaths(directoryPath, recursive)
                .Where(path => Directory.Exists(path))
                .Select(path => new DirectoryInfo(path));
        }

        /// <summary>
        /// Checks if a specific path should be ignored
        /// </summary>
        public bool IsPathIgnored(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var isDirectory = Directory.Exists(path);
            return _gitIgnoreManager.ShouldIgnore(path, isDirectory);
        }

        /// <summary>
        /// Gets processing statistics
        /// </summary>
        public ProcessingStatistics GetStatistics()
        {
            var gitIgnoreStats = _gitIgnoreManager.GetStatistics();
            return new ProcessingStatistics
            {
                GitIgnoreStats = gitIgnoreStats,
                ProcessedFiles = ProcessedFileCount,
                ProcessedDirectories = ProcessedDirectoryCount,
                ErrorCount = ErrorCount
            };
        }

        private static bool IsHidden(FileSystemInfo item)
        {
            return (item.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                   item.Name.StartsWith(".");
        }

        // Event handling for errors
        public event EventHandler<FileProcessingError> ErrorOccurred;

        protected virtual void OnErrorOccurred(FileProcessingError error)
        {
            ErrorOccurred?.Invoke(this, error);
            ErrorCount++;
        }

        // Statistics tracking
        public int ProcessedFileCount { get; private set; }
        public int ProcessedDirectoryCount { get; private set; }
        public int ErrorCount { get; private set; }

        public void Dispose()
        {
            _gitIgnoreManager?.Dispose();
        }
    }

    /// <summary>
    /// Configuration options for file processing
    /// </summary>
    public class FileProcessorOptions
    {
        public bool EnableCache { get; set; } = true;
        public bool AutoRefreshCache { get; set; } = true;
        public bool IncludeHiddenFiles { get; set; } = false;
        public bool IncludeHiddenDirectories { get; set; } = false;
        public bool ContinueOnError { get; set; } = true;
    }

    /// <summary>
    /// Represents an error that occurred during file processing
    /// </summary>
    public class FileProcessingError
    {
        public string FilePath { get; set; }
        public Exception Exception { get; set; }
        public ProcessingErrorType ErrorType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"[{ErrorType}] {FilePath}: {Exception?.Message}";
        }
    }

    public enum ProcessingErrorType
    {
        FileProcessing,
        DirectoryProcessing,
        AccessDenied,
        GitIgnoreError
    }

    /// <summary>
    /// Combined processing and .gitignore statistics
    /// </summary>
    public class ProcessingStatistics
    {
        public GitIgnoreStatistics GitIgnoreStats { get; set; }
        public int ProcessedFiles { get; set; }
        public int ProcessedDirectories { get; set; }
        public int ErrorCount { get; set; }

        public override string ToString()
        {
            return $"{GitIgnoreStats} | Processed - Files: {ProcessedFiles}, Dirs: {ProcessedDirectories}, Errors: {ErrorCount}";
        }
    }
}