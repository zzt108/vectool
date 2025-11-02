namespace VecTool.Handlers.Traversal
{
    using System;
    using System.Collections.Generic;
    using VecTool.Configuration;
    public interface IFileSystemTraverser
    {
        /// <summary>
        /// Enumerates all files in a folder tree respecting exclusions.
        /// Pre-filters patterns and legacy config BEFORE returning files.
        /// </summary>
        IEnumerable<string> EnumerateFilesRespectingExclusions(string root, IVectorStoreConfig config);

        /// <summary>
        /// Recursively processes folders with custom processing logic.
        /// Pre-filters based on patterns BEFORE calling processFile delegate.
        /// </summary>
        void ProcessFolder<T>(string folderPath, T context, IVectorStoreConfig vectorStoreConfig, Action<string, T, IVectorStoreConfig> processFile, Action<T, string> writeFolderName, Action<T>? writeFolderEnd = null);
    }
}