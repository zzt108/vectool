// ✅ FULL FILE VERSION
namespace VecTool.Handlers.Traversal
{
    using LogCtxShared;
    using MAB.DotIgnore;
    using NLogShared;
    using System;
    using System.Collections.Concurrent; // ✅ NEW - For thread-safe collections
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using VecTool.Configuration;
    using VecTool.Configuration.Exclusion;

    /// <summary>
    /// Handles folder traversal and file enumeration with exclusion support.
    /// Thread-safe for concurrent enumeration scenarios.
    /// </summary>
    public class FileSystemTraverser : IFileSystemTraverser
    {
        private static readonly CtxLogger log = new();
        private readonly IUserInterface? ui;
        private readonly string rootPath;

        // ✅ MODIFIED - Removed shared state (no longer a field)
        // Thread safety: Each call creates its own result collection
        private IIgnorePatternMatcher? primaryMatcher = null;
        private IIgnorePatternMatcher? fallbackMatcher = null;

        /// <summary>
        /// Initializes the traverser with repo root for pattern detection.
        /// Lazy-initializes the pattern matcher on first use.
        /// </summary>
        public FileSystemTraverser(IUserInterface? ui = null, string? rootPath = null)
        {
            this.ui = ui;
            this.rootPath = rootPath ?? Environment.CurrentDirectory;
        }

        /// <summary>
        /// Initializes the exclusion pattern matcher lazily.
        /// Creates matcher on first call to ProcessFolder or EnumerateFilesRespectingExclusions.
        /// </summary>
        private void EnsureMatchersInitialized(IVectorStoreConfig config)
        {
            if (primaryMatcher != null) return;

            try
            {
                // LAYER 1 PRIMARY: Create pattern matcher (.gitignore/.vtignore)
                primaryMatcher = IgnoreMatcherFactory.Create(IgnoreLibraryType.Auto,rootPath);

                using var _ = log.Ctx.Set(new Props()
                    .Add("rootpath", rootPath)
                    .Add("library", "MabDotIgnore"));

                log.Info($"Pattern matcher initialized for root: {rootPath}");

                // LAYER 2 FALLBACK: Create fallback matcher for legacy config
                fallbackMatcher = new LegacyConfigAdapter(config);

                using var __ = log.Ctx.Set(new Props()
                    .Add("primary", primaryMatcher?.GetType().Name ?? "null")
                    .Add("fallback", fallbackMatcher?.GetType().Name ?? "null"));

                log.Info("Exclusion matcher chain ready");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Pattern matcher initialization failed, falling back to legacy config only: {rootPath}");

                // Create fallback matcher that matches nothing - only legacy config filters
                fallbackMatcher = new LegacyConfigAdapter(config);
                primaryMatcher = fallbackMatcher;

                log.Info($"Exclusion matcher chain ready (Fallback only): primary={primaryMatcher?.GetType().Name ?? "null"}");
            }
        }

        /// <summary>
        /// Single validation check - replaces dual code path.
        /// Checks both pattern matcher and legacy config.
        /// </summary>
        private bool ShouldExcludePath(string path, bool isDirectory, IVectorStoreConfig config)
        {
            EnsureMatchersInitialized(config);

            // Layer 1: Try primary matcher (patterns)
            if (primaryMatcher != null && primaryMatcher.IsIgnored(path, isDirectory))
            {
                log.Trace($"Path excluded by primary matcher: {path}");
                return true;
            }

            // Layer 1 Fallback: Try legacy config
            if (fallbackMatcher != null && fallbackMatcher.IsIgnored(path, isDirectory))
            {
                log.Trace($"Path excluded by fallback matcher: {path}");
                return true;
            }

            return false; // Not excluded
        }

        /// <summary>
        /// Recursively processes folders with custom processing logic.
        /// Pre-filters based on patterns BEFORE calling processFile delegate.
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

            ui?.UpdateStatus($"Processing folder: {folderPath}");
            log.Debug($"Processing folder: {folderPath}");

            writeFolderName(context, folderName);

            // Process files
            string[] files = Array.Empty<string>();
            try
            {
                files = Directory.GetFiles(folderPath);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to enumerate files in: {folderPath}");
            }

            foreach (var file in files)
            {
                try
                {
                    // Single check for files - UNIFIED CODE PATH
                    if (ShouldExcludePath(file, isDirectory: false, vectorStoreConfig))
                    {
                        log.Trace($"Skipping excluded file: {file}");
                        continue;
                    }

                    processFile(file, context, vectorStoreConfig);
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Error processing file: {file}");
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
                log.Error(ex, $"Failed to enumerate subdirectories in: {folderPath}");
            }

            foreach (var subfolder in subfolders)
            {
                ProcessFolder(subfolder, context, vectorStoreConfig, processFile, writeFolderName, writeFolderEnd);
            }

            writeFolderEnd?.Invoke(context);
        }

        /// <summary>
        /// Enumerates all files in a folder tree respecting exclusions.
        /// Pre-filters patterns and legacy config BEFORE returning files.
        /// THREAD-SAFE: Each call creates its own result collection.
        /// </summary>
        public IEnumerable<string> EnumerateFilesRespectingExclusions(string root, IVectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(root))
                yield break;

            EnsureMatchersInitialized(config);

            // ✅ THREAD-SAFE: Use stack-based iteration (no shared state)
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var folderName = new DirectoryInfo(current).Name;
                using var _ = log.Ctx.Set().Add("current", current).Add("folderName", folderName);

                // Layer 1: Pattern check FIRST
                if (primaryMatcher!.IsIgnored(current, isDirectory: true))
                {
                    log.Trace($"Skipping excluded folder (pattern): {current}");
                    continue;
                }

                // Legacy config fallback
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
                    log.Error(ex, $"Failed to enumerate files in: {current}");
                    continue;
                }

                foreach (var f in files)
                {
                    var fileName = Path.GetFileName(f);

                    // Pattern check for file
                    if (primaryMatcher != null && primaryMatcher.IsIgnored(f, isDirectory: false))
                    {
                        log.Trace($"Skipping excluded file (pattern): {f}");
                        continue;
                    }

                    // Legacy config check
                    if (FileValidator.IsFileExcluded(fileName, config))
                    {
                        log.Trace($"Skipping excluded file (legacy): {f}");
                        continue;
                    }

                    // File system validity check
                    if (!FileValidator.IsFileValid(f, null))
                    {
                        _.Add("content", f);
                        log.Trace($"Invalid or binary file: {f}");
                        continue;
                    }

                    // ✅ THREAD-SAFE: yield return (no shared collection)
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
                    log.Error(ex, $"Failed to enumerate subdirectories in: {current}");
                    continue;
                }

                foreach (var sub in subdirs)
                {
                    stack.Push(sub);
                }
            }
        }
    }
}
