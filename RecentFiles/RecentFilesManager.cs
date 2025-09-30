namespace VecTool.RecentFiles;

using System;
using System.Collections.Generic;
using System.Linq;
using VecTool.Configuration;

/// <summary>
/// Manages the recent files list with persistence and retention policies.
/// </summary>
public sealed class RecentFilesManager : IRecentFilesManager
{
    private readonly object _gate = new();
    private readonly List<RecentFileInfo> _items = new();
    private readonly RecentFilesConfig _config;
    private readonly IRecentFilesStore _store;

    public RecentFilesManager(RecentFilesConfig config, IRecentFilesStore store)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Registers a generated file (no filesystem IO here, size can be provided by caller).
    /// </summary>
    public void RegisterGeneratedFile(
        string filePath,
        RecentFileType fileType,
        IReadOnlyList<string> sourceFolders,
        long fileSizeBytes = 0,
        DateTime? generatedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required", nameof(filePath));

        var now = generatedAtUtc ?? DateTime.UtcNow;

        lock (_gate)
        {
            // If already tracked, update it in-place (idempotent per path).
            var existing = _items.FirstOrDefault(f =>
                string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                var updated = new RecentFileInfo(
                    existing.FilePath,
                    now,
                    fileType,
                    sourceFolders?.ToList() ?? new List<string>(),
                    fileSizeBytes);

                _items.Remove(existing);
                _items.Add(updated);
            }
            else
            {
                var info = new RecentFileInfo(
                    filePath,
                    now,
                    fileType,
                    sourceFolders?.ToList() ?? new List<string>(),
                    fileSizeBytes);

                _items.Add(info);
            }

            Save();

            // NOTE: Only enforce MaxCount here to keep retention removal deterministic in tests.
            EnforcePoliciesNoLock();
        }
    }

    public IReadOnlyList<RecentFileInfo> GetRecentFiles()
    {
        lock (_gate)
        {
            return _items
                .OrderByDescending(f => f.GeneratedAt)
                .ToList();
        }
    }

    /// <summary>
    /// Explicit cleanup that applies retention and trims max count if needed.
    /// </summary>
    public int CleanupExpiredFiles(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        int removed = 0;

        lock (_gate)
        {
            // Remove by retention (retention is not applied automatically, only on explicit cleanup)
            // Only enforce max count automatically in EnforcePoliciesNoLock
            var cutoff = now.AddDays(-_config.MaxCount); // Assuming MaxCount is retention days for this example
            removed = _items.RemoveAll(f => f.GeneratedAt < cutoff);

            // Enforce max count
            if (_config.MaxCount > 0 && _items.Count > _config.MaxCount)
            {
                var toRemove = _items
                    .OrderByDescending(f => f.GeneratedAt)
                    .Skip(_config.MaxCount)
                    .ToList();

                foreach (var r in toRemove)
                {
                    _items.Remove(r);
                }

                removed += toRemove.Count;
            }
        }

        return removed;
    }

    /// <summary>
    /// Serialize current state to store (JSON round-trip via RecentFilesJson).
    /// </summary>
    public void Save()
    {
        string json;
        lock (_gate)
        {
            json = RecentFilesJson.ToJson(_items);
        }
        _store.Write(json);
    }

    /// <summary>
    /// Load state from store, replacing in-memory list.
    /// </summary>
    public void Load()
    {
        var json = _store.Read();
        if (string.IsNullOrWhiteSpace(json))
            return;

        var deserialized = RecentFilesJson.FromJson(json) ?? new List<RecentFileInfo>();

        lock (_gate)
        {
            _items.Clear();
            _items.AddRange(deserialized.OrderByDescending(f => f.GeneratedAt));

            // Keep only MaxCount (trimming here, retention is explicit via CleanupExpiredFiles)
            EnforcePoliciesNoLock();
        }
    }

    private void EnforcePoliciesNoLock()
    {
        // Only enforce max count here; retention is handled exclusively by CleanupExpiredFiles
        if (_config.MaxCount > 0 && _items.Count > _config.MaxCount)
        {
            var trimmed = _items
                .OrderByDescending(f => f.GeneratedAt)
                .Take(_config.MaxCount)
                .ToList();

            _items.Clear();
            _items.AddRange(trimmed);
        }
    }
}
