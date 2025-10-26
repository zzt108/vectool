using VecTool.RecentFiles;

namespace UnitTests.UI.RecentFiles
{
    public partial class RecentFilesPanelLayoutTests
    {
        public sealed class MockRecentFilesManager : IRecentFilesManager
        {
            public System.Collections.Generic.IReadOnlyList<RecentFileInfo> GetRecentFiles()
                => Array.Empty<RecentFileInfo>();

            public void RegisterGeneratedFile(string filePath, RecentFileType fileType, System.Collections.Generic.IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
            {
                // not needed in layout tests
            }

            public int CleanupExpiredFiles(DateTime? nowUtc = null) => 0;

            public void Save() { /* not used */ }

            public void Load() { /* not used */ }

            public void RemoveFile(string path)
            {
                // No-op for layout tests; interface compliance only
            }
        }
    }
}
