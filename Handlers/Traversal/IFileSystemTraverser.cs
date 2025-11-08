namespace VecTool.Handlers.Traversal
{
    using System;
    using System.Collections.Generic;
    using VecTool.Configuration;

    public interface IFileSystemTraverser
    {
        /// <summary>
        /// Enumerates all folders (directories) in the specified root, respecting exclusion rules.
        /// Returns only folders that should be included based on pattern matching and legacy config.
        /// Folders are yielded even if they contain no exportable files (e.g., folders with only .git).
        /// Used by Git handler to discover repositories.
        /// </summary>
        IEnumerable<string> EnumerateFoldersRespectingExclusions(string root, VectorStoreConfig config);

        /// <summary>
        /// Enumerates all files in a folder tree respecting exclusions.
        /// Pre-filters patterns and legacy config BEFORE returning files.
        /// </summary>
        IEnumerable<string> EnumerateFilesRespectingExclusions(string root, IVectorStoreConfig config);

        IEnumerable<string> EnumerateFilesRespectingExclusions(IVectorStoreConfig config);

        /// <summary>
        /// Recursively processes folders with custom processing logic.
        /// Pre-filters based on patterns BEFORE calling processFile delegate.
        /// </summary>
        void ProcessFolder<T>(string folderPath, T context, IVectorStoreConfig vectorStoreConfig, Action<string, T, IVectorStoreConfig> processFile, Action<T, string> writeFolderName, Action<T>? writeFolderEnd = null);
    }
}