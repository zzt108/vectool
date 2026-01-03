using VecTool.RecentFiles;

namespace UnitTests
{
    public class MockRecentFilesManager : IRecentFilesManager
    {
        public List<(string FilePath, RecentFileType FileType, IReadOnlyList<string> SourceFolders, long FileSizeBytes)> RegisteredFiles { get; } = new();

        public IReadOnlyList<RecentFileInfo> GetRecentFiles()
        {
            throw new NotImplementedException();
        }

        public void RegisterGeneratedFile(string filePath, RecentFileType fileType, IReadOnlyList<string>? sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
        {
            RegisteredFiles.Add((filePath, fileType, sourceFolders, fileSizeBytes));
        }

        public int CleanupExpiredFiles(DateTime? nowUtc = null)
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        public void RemoveFile(string path)
        {
            // No-op for these tests; not required by the current assertions
        }
    }
}