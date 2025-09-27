// File: DocX/IRecentFilesManager.cs
namespace DocXHandler.RecentFiles
{
    public interface IRecentFilesManager
    {
        IReadOnlyList<RecentFileInfo> GetRecentFiles();
        void RegisterGeneratedFile(string filePath, RecentFileType fileType, IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null);
        int CleanupExpiredFiles(DateTime? nowUtc = null);
    }
}
