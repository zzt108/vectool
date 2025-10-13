// Handlers/Traversal/FileSystemTraverser.cs
// Migrated from CtxLogger to NLog with message-template logging per guide.

using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using VecTool.Configuration;

namespace VecTool.Handlers.Traversal
{
    /// <summary>
    /// Handles folder traversal and file enumeration with exclusion support.
    /// </summary>
    public sealed class FileSystemTraverser
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IUserInterface? ui;

        public FileSystemTraverser(IUserInterface? ui)
        {
            this.ui = ui;
        }

        /// <summary>
        /// Recursively processes folders with custom processing logic.
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

            if (FileValidator.IsFolderExcluded(folderName, vectorStoreConfig))
            {
                log.Trace("Skipping excluded folder {FolderPath}", folderPath);
                return;
            }

            ui?.UpdateStatus($"Processing folder {folderPath}");
            log.Debug("Processing folder {FolderPath}", folderPath);

            writeFolderName(context, folderName);

            // Process files
            string[] files = Array.Empty<string>();
            try
            {
                files = Directory.GetFiles(folderPath);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to enumerate files in {FolderPath}", folderPath);
            }

            foreach (var file in files)
            {
                try
                {
                    processFile(file, context, vectorStoreConfig);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Error processing file {File}", file);
                    throw;
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
                log.Error(ex, "Failed to enumerate subdirectories in {FolderPath}", folderPath);
            }

            foreach (var sub in subfolders)
            {
                ProcessFolder(sub, context, vectorStoreConfig, processFile, writeFolderName, writeFolderEnd);
            }

            writeFolderEnd?.Invoke(context);
        }

        /// <summary>
        /// Enumerates all files in a folder tree respecting exclusions.
        /// </summary>
        public IEnumerable<string> EnumerateFilesRespectingExclusions(
            string root,
            VectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(root))
                yield break;

            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var folderName = new DirectoryInfo(current).Name;

                if (FileValidator.IsFolderExcluded(folderName, config))
                    continue;

                // Enumerate files
                string[] files = Array.Empty<string>();
                try
                {
                    files = Directory.GetFiles(current);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Failed to enumerate files in {Current}", current);
                    continue;
                }

                foreach (var f in files)
                {
                    var fileName = Path.GetFileName(f);
                    if (FileValidator.IsFileExcluded(fileName, config))
                        continue;

                    if (!FileValidator.IsFileValid(f, null))
                        continue;

                    yield return f;
                }

                // Enumerate subdirectories
                string[] subfolders = Array.Empty<string>();
                try
                {
                    subfolders = Directory.GetDirectories(current);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Failed to enumerate subdirectories in {Current}", current);
                    continue;
                }

                foreach (var sub in subfolders)
                {
                    stack.Push(sub);
                }
            }
        }
    }
}
