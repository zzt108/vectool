namespace VecTool.Handlers.Traversal
{
    using DocumentFormat.OpenXml.Bibliography;
    using LogCtxShared;
    using MAB.DotIgnore;
    using NLogShared;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using VecTool.Configuration;
    using VecTool.Configuration.Exclusion;
    using static System.Runtime.InteropServices.JavaScript.JSType;

    /// <summary>
    /// Handles folder traversal and file enumeration with exclusion support (Layer 1 + Layer 2).
    /// Thread-safe for concurrent enumeration scenarios.
    /// </summary>
    public class FileSystemTraverser : IFileSystemTraverser
    {
        private static readonly CtxLogger log = new();

        private readonly IUserInterface? ui;
        // private readonly string rootPath;

        /// <summary>
        /// NEW - Optional Layer 2 file marker extractor (injected via constructor).
        /// If null, Layer 2 marker checks are skipped.
        /// </summary>
        private readonly IFileMarkerExtractor? markerExtractor;

        // Thread safety: Each call creates its own result collection
        private IIgnorePatternMatcher? primaryMatcher = null;

        private IIgnorePatternMatcher? fallbackMatcher = null;

        /// <summary>
        /// Initializes the traverser with repo root for pattern detection.
        /// Lazy-initializes the pattern matcher on first use.
        /// Constructor WITHOUT Layer 2 support (backward compatible).
        /// </summary>
        public FileSystemTraverser(IUserInterface? ui = null)
        {
            this.ui = ui;
            this.markerExtractor = null;  // Layer 2 disabled
        }

        /// <summary>
        /// NEW - Initializes traverser WITH Layer 2 file marker support.
        /// Optional marker extractor: if provided, enables marker-based exclusions.
        /// </summary>
        public FileSystemTraverser(
            IUserInterface? ui,
            IFileMarkerExtractor? markerExtractor)
        {
            this.ui = ui;
            //this.rootPath = rootPath ?? Environment.CurrentDirectory;
            this.markerExtractor = markerExtractor;  // Layer 2 enabled (if provided)

            using var ctx = LogCtx.Set(new Props()
                .Add("layer_2_enabled", markerExtractor != null ? "yes" : "no"));
            log.Info("FileSystemTraverser initialized");
        }

        /// <summary>
        /// Initializes the exclusion pattern matcher lazily.
        /// Creates matcher on first call to ProcessFolder or EnumerateFilesRespectingExclusions.
        /// </summary>
        private void EnsureMatchersInitialized(IVectorStoreConfig config)
        {
            if (primaryMatcher != null)
                return;

            var rootPath = config.GetRootPath() ?? Environment.CurrentDirectory;

            try
            {
                // LAYER 1: PRIMARY - Create pattern matcher .gitignore/.vtignore
                primaryMatcher = IgnoreMatcherFactory.Create(
                    IgnoreLibraryType.Auto,
                    rootPath
                );

                using var ctx = LogCtx.Set(new Props()
                    .Add("rootpath", rootPath)
                    .Add("library", "MabDotIgnore"));
                log.Info("Pattern matcher initialized for root");

                // LAYER 2: FALLBACK - Create fallback matcher for legacy config
                fallbackMatcher = new LegacyConfigAdapter(config);

                using var ctx2 = LogCtx.Set(new Props()
                    .Add("primary", primaryMatcher?.GetType().Name ?? "null")
                    .Add("fallback", fallbackMatcher?.GetType().Name ?? "null"));
                log.Info("Exclusion matcher chain ready");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Pattern matcher initialization failed, falling back to legacy config only {rootPath}");

                // Create fallback matcher that matches nothing - only legacy config filters
                fallbackMatcher = new LegacyConfigAdapter(config);
                primaryMatcher = fallbackMatcher;

                log.Info("Exclusion matcher chain ready (Fallback only)");
            }
        }

        /// <summary>
        /// NEW - Checks Layer 2 file marker exclusions.
        /// Returns true if file contains [VECTOOL:EXCLUDE:...] marker.
        /// Called AFTER Layer 1 pattern check succeeds.
        /// </summary>
        private bool ShouldExcludeByMarker(string filePath, IVectorStoreConfig config)
        {
            // Layer 2 only applies to files, not directories
            if (markerExtractor == null)
                return false;  // Layer 2 disabled

            try
            {
                var marker = markerExtractor.ExtractMarker(filePath);

                if (marker != null)
                {
                    // File excluded by marker
                    using var ctx = LogCtx.Set(ExclusionProps.CreateMarkerProps(
                        marker.FilePath,
                        marker.Reason,
                        marker.SpaceReference,
                        marker.LineNumber));
                    log.Info("File excluded (Layer 2 marker)");
                    return true;
                }

                return false;  // No marker found
            }
            catch (Exception ex)
            {
                using var ctx = LogCtx.Set(ExclusionProps.CreateMarkerErrorProps(
                    filePath,
                    ex.GetType().Name,
                    ex.Message));
                log.Warn("Marker extraction error (continuing)");
                return false;  // Error during extraction: don't exclude
            }
        }

        /// <summary>
        /// Single validation check - replaces dual code path.
        /// Checks both pattern matcher (Layer 1) and legacy config (Layer 1 fallback).
        /// NEW - Also checks Layer 2 file markers.
        /// </summary>
        private bool ShouldExcludePath(string path, bool isDirectory, IVectorStoreConfig config)
        {
            EnsureMatchersInitialized(config);

            // LAYER 1: Try primary matcher patterns
            if (primaryMatcher != null && primaryMatcher.IsIgnored(path, isDirectory))
            {
                log.Trace($"Path excluded by primary matcher: {path}");
                return true;
            }

            // LAYER 1: Fallback - Try legacy config
            if (fallbackMatcher != null && fallbackMatcher.IsIgnored(path, isDirectory))
            {
                log.Trace($"Path excluded by fallback matcher: {path}");
                return true;
            }

            // LAYER 2: NEW - File marker check (files only)
            if (!isDirectory && ShouldExcludeByMarker(path, config))
            {
                return true;  // Already logged inside ShouldExcludeByMarker
            }

            return false;  // Not excluded
        }

        /// <summary>
        /// Recursively processes folders with custom processing logic.
        /// Pre-filters based on patterns and markers BEFORE calling processFile delegate.
        /// </summary>
        public void ProcessFolder<T>(
            string folderPath,
            T context,
            IVectorStoreConfig vectorStoreConfig,
            Action<string, T, IVectorStoreConfig> processFile,
            Action<T, string> writeFolderName,
            Action<T>? writeFolderEnd = null)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            var folderName = new DirectoryInfo(folderPath).Name;

            // Single check for folders
            if (ShouldExcludePath(folderPath, isDirectory: true, vectorStoreConfig))
            {
                log.Trace($"Skipping excluded folder: {folderPath}");
                return;
            }

            ui?.UpdateStatus($"Processing folder {folderPath}");
            log.Debug($"Processing folder: {folderPath}");

            // Process files
            string[] files = Array.Empty<string>();
            try
            {
                files = Directory.GetFiles(folderPath);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to enumerate files in {folderPath}");
            }

            foreach (var file in files)
            {
                try
                {
                    // Single check for files - UNIFIED CODE PATH (Layer 1 + Layer 2)
                    if (ShouldExcludePath(file, isDirectory: false, vectorStoreConfig))
                    {
                        log.Trace($"Skipping excluded file: {file}");
                        continue;
                    }

                    processFile(file, context, vectorStoreConfig);
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Error processing file {file}");
                }
            }

            // Recurse into subdirectories
            string[] subfolders = Array.Empty<string>();
            try
            {
                subfolders = Directory.GetDirectories(folderPath);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to enumerate subdirectories in {folderPath}");
            }

            foreach (var subfolder in subfolders)
            {
                ProcessFolder(subfolder, context, vectorStoreConfig, processFile, writeFolderName, writeFolderEnd);
            }

            writeFolderEnd?.Invoke(context);
        }

        /// <summary>
        /// gets root path from VectorStoreConfig and calls EnumerateFilesRespectingExclusions
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public IEnumerable<string> EnumerateFilesRespectingExclusions(IVectorStoreConfig config) => EnumerateFilesRespectingExclusions(config.GetRootPath(), config);

        /// <summary>
        /// Enumerates all files in a folder tree respecting exclusions (Layer 1 + Layer 2).
        /// Pre-filters patterns and markers BEFORE returning files.
        /// THREAD-SAFE: Each call creates its own result collection.
        /// </summary>
        public IEnumerable<string> EnumerateFilesRespectingExclusions(
            string root,
            IVectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(root))
                yield break;

            EnsureMatchersInitialized(config);

            // THREAD-SAFE: Use stack-based iteration, no shared state
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var folderName = new DirectoryInfo(current).Name;

                using var ctx = LogCtx.Set(new Props()
                    .Add("current", current)
                    .Add("folderName", folderName));

                // LAYER 1: Pattern check FOLDER FIRST (requires relative path)
                if (primaryMatcher != null)
                {
                    var relativePath = Path.GetRelativePath(root, current);
                    if (primaryMatcher.IsIgnored(relativePath, isDirectory: true))
                    {
                        using var _ = LogCtx.Set(new Props()
                            .Add("excludedDir", relativePath)
                            .Add("fullPath", current));
                        log.Trace($"Skipping excluded folder (pattern): {relativePath}");
                        continue;
                    }
                }

                // LAYER 1: Legacy config fallback
                if (FileValidator.IsFolderExcluded(folderName, config))
                {
                    log.Trace($"Skipping excluded folder (legacy): {current}");
                    continue;
                }

                // Enumerate files
                string[] files = Array.Empty<string>();
                try
                {
                    files = Directory.GetFiles(current);
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Failed to enumerate files in {current}");
                    continue;
                }

                foreach (var f in files)
                {
                    var fileName = Path.GetFileName(f);

                    // Exclude .gitignore and .vtignore files themselves
                    if (fileName.Equals(".gitignore", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals(".vtignore", StringComparison.OrdinalIgnoreCase))
                    {
                        log.Trace($"Skipping ignore file: {f}");
                        continue;
                    }

                    // LAYER 1: Pattern check for file (requires relative path)
                    if (primaryMatcher != null)
                    {
                        var relativePath = Path.GetRelativePath(root, f);
                        if (primaryMatcher.IsIgnored(relativePath, isDirectory: false))
                        {
                            using var _ = LogCtx.Set(new Props()
                                .Add("excludedFile", relativePath)
                                .Add("fullPath", f));
                            log.Trace($"Skipping excluded file (pattern): {relativePath}");
                            continue;
                        }
                    }

                    // LAYER 1: Legacy config check
                    if (FileValidator.IsFileExcluded(fileName, config))
                    {
                        log.Trace($"Skipping excluded file (legacy): {f}");
                        continue;
                    }

                    // File system validity check
                    if (!FileValidator.IsFileValid(f, outputPath: null))
                    {
                        using var ctxValid = LogCtx.Set(new Props()
                            .Add("content", f));
                        log.Trace("Invalid or binary file");
                        continue;
                    }

                    // LAYER 2: NEW - File marker check
                    if (ShouldExcludeByMarker(f, config))
                    {
                        continue;  // Already logged in ShouldExcludeByMarker
                    }

                    // THREAD-SAFE: yield return (no shared collection)
                    yield return f;
                }

                // Enumerate subdirectories
                string[] subdirs = Array.Empty<string>();
                try
                {
                    subdirs = Directory.GetDirectories(current);
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Failed to enumerate subdirectories in {current}");
                    continue;
                }

                foreach (var sub in subdirs)
                {
                    stack.Push(sub);
                }
            }
        }

        /// <summary>
        /// Yields directories even if they contain only excluded files (important for Git detection).
        /// Uses helper method to avoid CS1626 (yield in try-catch).
        /// </summary>
        public IEnumerable<string> EnumerateFoldersRespectingExclusions(string root, VectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                yield break;

            // Delegate to helper that handles exceptions
            foreach (var folder in EnumerateFoldersRespectingExclusionsInternal(root, config))
            {
                yield return folder;
            }
        }

        private IEnumerable<string> EnumerateFoldersRespectingExclusionsInternal(string root, VectorStoreConfig config)
        {
            var dirStack = new Stack<string>();
            dirStack.Push(root);

            while (dirStack.Count > 0)
            {
                var currentDir = dirStack.Pop();

                // Layer 1: Check if directory itself should be excluded by patterns
                if (!ShouldIncludeDirectory(currentDir, root, config))
                    continue;

                // ✅ Yield this folder (even if it has no valid files)
                yield return currentDir;

                // ✅ FIXED: Get subdirectories without try-catch in iterator
                // Errors are logged inside the helper, stack building happens outside yield
                var subdirs = GetSubdirectoriesSafe(currentDir);
                foreach (var subDir in subdirs)
                {
                    dirStack.Push(subDir);
                }
            }
        }

        /// <summary>
        /// Helper method to safely enumerate subdirectories.
        /// Handles errors locally, returns safe collection for iteration in iterator.
        /// Separated to avoid try-catch in yield method.
        /// </summary>
        private IEnumerable<string> GetSubdirectoriesSafe(string currentDir)
        {
            try
            {
                return Directory.EnumerateDirectories(currentDir).ToList();
            }
            catch (UnauthorizedAccessException ex)
            {
                using var ctx = LogCtx.Set(new Props { { "path", currentDir }, { "error", ex.GetType().Name } });
                log?.Error(ex, "Access denied to directory");
                return Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                using var ctx = LogCtx.Set(new Props { { "path", currentDir }, { "error", ex.GetType().Name } });
                log?.Error(ex, "Error enumerating subdirectories");
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Determines if a directory should be included based on exclusion rules.
        /// Uses same Layer 1 and Layer 2 filtering as file enumeration.
        /// </summary>
        private bool ShouldIncludeDirectory(string dirPath, string rootPath, VectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(dirPath))
                return false;

            // Layer 1: Pattern matching (gitignore/_vtignore)
            if (primaryMatcher != null)
            {
                var relativePath = Path.GetRelativePath(rootPath, dirPath);

                // Check if directory matches exclusion patterns
                if (primaryMatcher.IsIgnored(relativePath + "/", true) || primaryMatcher.IsIgnored(relativePath, true))
                    return false;
            }

            // Layer 2: Legacy config ExcludedFolders
            if (config?.ExcludedFolders != null && config.ExcludedFolders.Count > 0)
            {
                var dirName = Path.GetFileName(dirPath);
                if (config.ExcludedFolders.Any(ex => ex.Equals(dirName, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            return true;
        }

    }
}