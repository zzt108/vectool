// Path: RecentFiles/RecentFilesManager.cs
using VecTool.Configuration.Helpers;
using VecTool.Core.Configuration;

namespace VecTool.RecentFiles
{
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
            _config = config.ThrowIfNull(nameof(config));
            _store = store.ThrowIfNull(nameof(store));
            Load();
        }

        public IReadOnlyList<RecentFileInfo> GetRecentFiles()
        {
            lock (_gate)
            {
                return _items.OrderByDescending(f => f.GeneratedAt).ToList();
            }
        }

        public void RegisterGeneratedFile(string filePath, RecentFileType fileType, IReadOnlyList<string>? sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
        {
            var newItem = new RecentFileInfo(filePath, generatedAtUtc?.ToLocalTime() ?? DateTime.Now, fileType, sourceFolders?.ToList(), fileSizeBytes);

            lock (_gate)
            {
                _items.RemoveAll(i => i.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                _items.Add(newItem);
                EnforcePolicies_NoLock();
            }
            Save();
        }

        public int CleanupExpiredFiles(DateTime? nowUtc = null)
        {
            var now = nowUtc?.ToLocalTime() ?? DateTime.Now;
            int removedCount;

            lock (_gate)
            {
                var cutoff = now.AddDays(-_config.RetentionDays);
                removedCount = _items.RemoveAll(f => f.GeneratedAt < cutoff && _config.RetentionDays > 0);
            }

            if (removedCount > 0)
            {
                Save();
            }
            return removedCount;
        }

        private void EnforcePolicies_NoLock()
        {
            if (_items.Count > _config.MaxCount)
            {
                var trimmedList = _items.OrderByDescending(f => f.GeneratedAt)
                                        .Take(_config.MaxCount)
                                        .ToList();
                _items.Clear();
                _items.AddRange(trimmedList);
            }
        }

        public void Save()
        {
            string json;
            lock (_gate)
            {
                json = RecentFilesJson.ToJson(_items);
            }
            _store.Write(json);
        }

        public void Load()
        {
            var json = _store.Read();
            if (string.IsNullOrWhiteSpace(json)) return;

            var deserialized = RecentFilesJson.FromJson(json);
            lock (_gate)
            {
                _items.Clear();
                _items.AddRange(deserialized);
                EnforcePolicies_NoLock();
            }
        }

        public void RemoveFile(string path)
        {
            lock (_gate)
            {
                _items.RemoveAll(i => i.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
            Save();
        }
    }
}