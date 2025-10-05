// ✅ FULL FILE VERSION
// Path: tests/UnitTests/RecentFiles/RecentFilesPanelDataBindingTests.cs

using System;
using VecTool.RecentFiles;

namespace UnitTests.Fakes
{
    // Fake manager to simulate data
    public class FakeRecentFilesManager : IRecentFilesManager
    {
        public int CleanupExpiredFiles(DateTime? nowUtc = null)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<RecentFileInfo> GetRecentFiles() => new[]
        {
            new RecentFileInfo("file1.txt", DateTime.Now, RecentFileType.Unknown,  new List<string> { "source1", "source2" },100),
            new RecentFileInfo("missing.png",  DateTime.Now, RecentFileType.Pdf, new List<string> { "source1", "source2" },220)
            };

        public void Load()
        {
            throw new NotImplementedException();
        }

        public void RegisterGeneratedFile(string filePath, RecentFileType fileType, IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
        {
            throw new NotImplementedException();
        }

        public void RemoveFile(string path)
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }
    };
}

