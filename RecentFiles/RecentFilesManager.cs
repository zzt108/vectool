// ✅ FULL FILE VERSION
// Path: RecentFiles/RecentFilesManager.cs
// Phase 2, Option C: Recent Files integration with RemoveFile support

using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using VecTool.Configuration;

namespace VecTool.RecentFiles
{
    /// <summary>
    /// Manages recent files: registration, retrieval, cleanup, persistence.
    /// Thread-safe for concurrent access.
    /// </summary>
    public sealed class RecentFilesManager : IRecentFilesManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly RecentFilesConfig config;
        private readonly IRecentFilesStore store;
        private readonly object sync = new();
        private readonly List<RecentFileInfo> files = new();

        public RecentFilesManager(RecentFilesConfig config, IRecentFilesStore store)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// Gets all tracked recent files ordered by generation date (newest first).
        /// </summary>
        public IReadOnlyList<RecentFileInfo> GetRecentFiles()
        {
            lock (sync)
            {
                return files
                    .OrderByDescending(f => f.GeneratedAt)
                    .ToList();
            }
        }

        /// <summary>
        /// Registers a newly generated file with metadata.
        /// </summary>
        public void RegisterGeneratedFile(
            string filePath,
            RecentFileType fileType,
            IReadOnlyList<string> sourceFolders,
            long fileSizeBytes = 0,
            DateTimeOffset? generatedAt = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty", nameof(filePath));
            }

            lock (sync)
            {
                // Remove existing entry for the same path (update scenario)
                var existing = files.FirstOrDefault(f =>
                    string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    files.Remove(existing);
                }

                // Create new entry
                var newFile = new RecentFileInfo(
                    filePath,
                    generatedAt ?? DateTimeOffset.UtcNow,
                    fileType,
                    sourceFolders?.ToList() ?? new List<string>(),
                    fileSizeBytes);

                files.Add(newFile);

                Log.Info("Registered recent file: {Path} (type: {Type}, size: {Size} bytes)",
                    filePath, fileType, fileSizeBytes);

                // Enforce max count
                EnforceMaxCount();
            }
        }

        /// <summary>
        /// Removes a specific file from the recent files list by path.
        /// </summary>
        public void RemoveFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty", nameof(filePath));
            }

            lock (sync)
            {
                var existing = files.FirstOrDefault(f =>
                    string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    files.Remove(existing);
                    Log.Debug("Removed file from recent list: {Path}", filePath);
                }
                else
                {
                    Log.Debug("File not found in recent list: {Path}", filePath);
                }
            }
        }

        /// <summary>
        /// Removes expired files based on retention policy and enforces max count.
        /// </summary>
        public int CleanupExpiredFiles(DateTime? now = null)
        {
            var cutoff = (now ?? DateTime.UtcNow).AddDays(-config.RetentionDays);
            int removedCount = 0;

            lock (sync)
            {
                // Remove expired files
                var expired = files.Where(f => f.GeneratedAt.UtcDateTime < cutoff).ToList();
                foreach (var file in expired)
                {
                    files.Remove(file);
                    removedCount++;
                }

                // Enforce max count
                removedCount += EnforceMaxCount();

                if (removedCount > 0)
                {
                    Log.Info("Cleanup removed {Count} files (retention: {Days} days, max: {Max})",
                        removedCount, config.RetentionDays, config.MaxCount);
                }
            }

            return removedCount;
        }

        /// <summary>
        /// Persists current state to storage.
        /// </summary>
        public void Save()
        {
            lock (sync)
            {
                try
                {
                    var json = RecentFilesJson.ToJson(files);
                    store.Write(json);

                    Log.Debug("Saved {Count} recent files to storage", files.Count);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to save recent files");
                    throw;
                }
            }
        }

        /// <summary>
        /// Loads state from storage.
        /// </summary>
        public void Load()
        {
            lock (sync)
            {
                try
                {
                    var json = store.Read();
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Log.Debug("No recent files JSON found; starting fresh");
                        files.Clear();
                        return;
                    }

                    var loaded = RecentFilesJson.FromJson(json);
                    files.Clear();
                    files.AddRange(loaded);

                    Log.Info("Loaded {Count} recent files from storage", files.Count);

                    // Clean up on load
                    CleanupExpiredFiles();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to load recent files; starting fresh");
                    files.Clear();
                }
            }
        }

        /// <summary>
        /// Enforces max count policy by removing oldest entries.
        /// Must be called within lock(sync).
        /// </summary>
        private int EnforceMaxCount()
        {
            if (files.Count <= config.MaxCount)
                return 0;

            var ordered = files.OrderBy(f => f.GeneratedAt).ToList();
            int toRemove = files.Count - config.MaxCount;
            int removed = 0;

            for (int i = 0; i < toRemove && i < ordered.Count; i++)
            {
                files.Remove(ordered[i]);
                removed++;
            }

            if (removed > 0)
            {
                Log.Debug("Enforced max count: removed {Count} oldest files", removed);
            }

            return removed;
        }
    }
}
