using VecTool.RecentFiles;
using System.Collections.Generic;

namespace VecTool.Studio.Services
{
    /// <summary>
    /// No-operation implementation of IRecentFilesManager.
    /// Phase 2 Step 4 MVP: unblocks handler registration.
    /// Phase 3: will be replaced with real FileRecentFilesStore.
    /// </summary>
    public sealed class NoopRecentFilesManager : IRecentFilesManager
    {
        public IReadOnlyList<RecentFileInfo> GetRecentFiles()
            => new List<RecentFileInfo>();

        public void RegisterGeneratedFile(
            string filePath,
            RecentFileType fileType,
            IReadOnlyList<string> sourceFolders,
            long fileSizeBytes = 0,
            System.DateTime? generatedAtUtc = null)
        {
            // No-op: file is on disk but not tracked.
        }

        public int CleanupExpiredFiles(System.DateTime? nowUtc = null)
            => 0;

        public void Save()
        { }

        public void Load()
        { }

        public void RemoveFile(string path)
        { }
    }
}