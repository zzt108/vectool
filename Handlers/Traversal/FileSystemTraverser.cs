// ✅ FULL FILE VERSION - CORRECTED

namespace VecTool.Handlers.Traversal
{
    using MAB.DotIgnore;
    using NLogShared;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using VecTool.Configuration;
    using VecTool.Configuration.Exclusion;

    /// <summary>
    /// Handles folder traversal and file enumeration with exclusion support.
    /// </summary>
    public sealed class FileSystemTraverser
    {
        private static readonly CtxLogger log = new();
        private readonly IUserInterface? ui;
        private readonly string _rootPath;
        private IIgnorePatternMatcher? _matcher;
        private IIgnorePatternMatcher? _fallbackMatcher;

        /// <summary>
        /// Initializes the traverser with repo root for pattern detection.
        /// Lazy-initializes the pattern matcher on first use.
        /// </summary>
        public FileSystemTraverser(IUserInterface? ui = null, string? rootPath = null)
        {
            this.ui = ui;
            _rootPath = rootPath ?? Environment.CurrentDirectory;
            _matcher = null; // Lazy-initialized
        }

        /// <summary>
        /// Initializes the exclusion pattern matcher lazily.
        /// Creates matcher on first call to ProcessFolder or EnumerateFilesRespectingExclusions.
        /// </summary>
        private void EnsureMatcherInitialized()
        {
            if (_matcher != null)
                return;

            try
            {
                _matcher = IgnoreMatcherFactory.Create(IgnoreLibraryType.MabDotIgnore, _rootPath);
                using var _ = new CtxLogger().Ctx.Set()
                    .Add("root_path", _rootPath)
                    .Add("library", "MabDotIgnore");
                log.Info($"Pattern matcher initialized for root: {_rootPath}");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Pattern matcher initialization failed, falling back to legacy config only: {_rootPath}");

                // Create fallback matcher that matches nothing (only legacy config filters)
                _fallbackMatcher = new LegacyOnlyIgnoreMatcher();
                _matcher = _fallbackMatcher;
            }
        }

        /// <summary>
        /// Recursively processes folders with custom processing logic.
        /// Pre-filters based on patterns BEFORE calling processFile delegate.
        /// </summary>
        public void ProcessFolder<T>(
            string folderPath,
            T context,
            VectorStoreConfig vectorStoreConfig,
            Action<string, T, VectorStoreConfig> processFile,
            Action<T, string> writeFolderName,
            Action<T>? writeFolderEnd = null)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            var folderName = new DirectoryInfo(folderPath).Name;

            // Pattern check FIRST (Layer 1) - before legacy config
            EnsureMatcherInitialized();
            if (_matcher != null && _matcher.IsIgnored(folderPath, isDirectory: true))
            {
                log.Trace($"Skipping excluded folder (pattern): {folderPath}");
                return;
            }

            // Legacy config fallback (Layer 1 fallback)
            if (FileValidator.IsFolderExcluded(folderName, vectorStoreConfig))
            {
                log.Trace($"Skipping excluded folder (legacy config): {folderPath}");
                return;
            }

            ui?.UpdateStatus($"Processing folder: {folderPath}");
            log.Debug($"Processing folder: {folderPath}");

            writeFolderName(context, folderName);

            // Process files - delegate has full responsibility
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
                    // Pattern check for file (Layer 1)
                    if (_matcher != null && _matcher.IsIgnored(file, isDirectory: false))
                    {
                        log.Trace($"Skipping excluded file (pattern): {file}");
                        continue;
                    }

                    // Invoke handler with pre-filtered file
                    processFile(file, context, vectorStoreConfig);
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Error processing file: {file}");
                }
            }

            // Process subfolders recursively
            string[] subfolders = Array.Empty<string>();
            try
            {
                subfolders = Directory.GetDirectories(folderPath);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to enumerate subdirectories in {folderPath}");
            }

            foreach (var sub in subfolders)
            {
                ProcessFolder(sub, context, vectorStoreConfig, processFile, writeFolderName, writeFolderEnd);
            }

            writeFolderEnd?.Invoke(context);
        }

        /// <summary>
        /// Enumerates all files in a folder tree respecting exclusions.
        /// Pre-filters patterns and legacy config BEFORE returning files.
        /// </summary>
        public IEnumerable<string> EnumerateFilesRespectingExclusions(string root, VectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(root))
                yield break;

            EnsureMatcherInitialized();

            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var folderName = new DirectoryInfo(current).Name;

                // Pattern check FIRST
                if (_matcher!.IsIgnored(current, isDirectory: true))
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
                    log.Error(ex, $"Failed to enumerate files in {current}");
                    continue;
                }

                foreach (var f in files)
                {
                    var fileName = Path.GetFileName(f);

                    // Pattern check for file
                    if (_matcher != null && _matcher.IsIgnored(f, isDirectory: false))
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
                        log.Trace($"Invalid or binary file: {f}");
                        continue;
                    }

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
    }
}