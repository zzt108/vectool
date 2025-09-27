// File: DocX/RecentFiles/RecentFilesManager.cs
namespace DocXHandler.RecentFiles
{
    // Assumptions:
    // - RecentFileInfo exists with properties:
    //   string FilePath, DateTimeOffset GeneratedAt, RecentFileType FileType,
    //   List<string> SourceFolders, long FileSizeBytes
    // - RecentFilesJson exists with:
    //   static string ToJson(IEnumerable<RecentFileInfo> items)
    //   static List<RecentFileInfo> FromJson(string json)
    // - RecentFilesConfig exists with properties:
    //   int MaxCount, int RetentionDays, string OutputPath

    public interface IRecentFilesStore
    {
        // Pure text persistence; File-based implementation comes in Phase 1 Step 3.
        string? Read();
        void Write(string json);
    }

    public sealed class InMemoryRecentFilesStore : IRecentFilesStore
    {
        private string? _json;
        public string? Read() => _json;
        public void Write(string json) => _json = json;
    }

    public sealed class RecentFilesManager: IRecentFilesManager
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

        // Registers a generated file; no filesystem IO here (size can be provided by caller).
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
                        fileSizeBytes
                    );
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
                        fileSizeBytes
                    );
                    _items.Add(info);
                }

                // NOTE: Only enforce MaxCount here to keep retention removal deterministic in tests.
                EnforcePolicies_NoLock();
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

        // Explicit cleanup that applies retention (and trims max count if needed).
        public int CleanupExpiredFiles(DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            int removed = 0;

            lock (_gate)
            {
                // Remove by retention
                if (_config.RetentionDays > 0)
                {
                    var cutoff = now.AddDays(-_config.RetentionDays);
                    removed += _items.RemoveAll(f => f.GeneratedAt < cutoff);
                }

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
                        removed++;
                    }
                }
            }

            return removed;
        }

        // Serialize current state to store (JSON round-trip via RecentFilesJson).
        public void Save()
        {
            string json;
            lock (_gate)
            {
                json = RecentFilesJson.ToJson(_items);
            }
            _store.Write(json);
        }

        // Load state from store, replacing in-memory list.
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
                // Keep only MaxCount trimming here; retention is explicit via CleanupExpiredFiles(...)
                EnforcePolicies_NoLock();
            }
        }

        private void EnforcePolicies_NoLock()
        {
            // Only enforce max count here; retention is handled exclusively by CleanupExpiredFiles(...)
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
}
