// ✅ FULL FILE VERSION
// Path: RecentFiles/IRecentFilesManager.cs
// Phase 2, Option C: Recent Files integration with RemoveFile support

using System;
using System.Collections.Generic;

namespace VecTool.RecentFiles
{
    /// <summary>
    /// Interface for managing recent files tracking.
    /// </summary>
    public interface IRecentFilesManager
    {
        /// <summary>
        /// Gets all tracked recent files ordered by generation date (newest first).
        /// </summary>
        IReadOnlyList<RecentFileInfo> GetRecentFiles();

        /// <summary>
        /// Registers a newly generated file with metadata.
        /// </summary>
        /// <param name="filePath">Full path to the generated file</param>
        /// <param name="fileType">Type of the generated file</param>
        /// <param name="sourceFolders">Source folders used to generate the file</param>
        /// <param name="fileSizeBytes">Size of the file in bytes</param>
        /// <param name="generatedAt">Generation timestamp (defaults to now if null)</param>
        void RegisterGeneratedFile(
            string filePath,
            RecentFileType fileType,
            IReadOnlyList<string> sourceFolders,
            long fileSizeBytes = 0,
            DateTimeOffset? generatedAt = null);

        /// <summary>
        /// Removes expired files based on retention policy and enforces max count.
        /// </summary>
        /// <param name="now">Current time for comparison (defaults to now if null)</param>
        /// <returns>Number of items removed</returns>
        int CleanupExpiredFiles(DateTime? now = null);

        /// <summary>
        /// Persists current state to storage.
        /// </summary>
        void Save();

        /// <summary>
        /// Loads state from storage.
        /// </summary>
        void Load();

        /// <summary>
        /// Removes a specific file from the recent files list by path.
        /// </summary>
        /// <param name="filePath">Full path to the file to remove</param>
        void RemoveFile(string filePath);
    }
}
