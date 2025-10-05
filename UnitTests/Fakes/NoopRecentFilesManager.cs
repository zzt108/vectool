// ✅ FULL FILE VERSION
using System;
using System.Collections.Generic;
using VecTool.RecentFiles;

namespace UnitTests.Fakes
{
    public sealed class NoopRecentFilesManager : IRecentFilesManager
    {
        public void Load() { }

        public void Save() { }

        // 🔄 MODIFY: Return int to match interface; no-op returns 0
        public int CleanupExpiredFiles(DateTime? nowUtc = null) => 0;

        // 🔄 MODIFY: Return type matches interface: IReadOnlyList<RecentFileInfo>
        public IReadOnlyList<RecentFileInfo> GetRecentFiles()
            => Array.Empty<RecentFileInfo>();

        public void RegisterGeneratedFile(
            string filePath,
            RecentFileType fileType,
            IReadOnlyList<string> sourceFolders,
            long fileSizeBytes,
            DateTime? generatedAtUtc = null)
        {
            // no-op
        }

        public void Remove(string filePath) { }

        public void Clear() { }
        
        /// <summary>
        /// No-op removal for tests; satisfies the interface without side effects.
        /// </summary>
        /// <param name="path">Absolute path to remove from the recent files list.</param>
        public void RemoveFile(string path)
        {
            // Intentionally no-op for unit tests
        }
    }
}
