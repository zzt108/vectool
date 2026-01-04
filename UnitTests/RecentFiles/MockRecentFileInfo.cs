// ✅ FULL FILE VERSION
using VecTool.RecentFiles;

namespace UnitTests.RecentFiles
{
    /// <summary>
    /// Test helper exposing a controllable Exists flag without depending on other mocks.
    /// Removes dependency on a separate MockFileInfo type.
    /// </summary>
    public sealed class MockRecentFileInfo : RecentFileInfo
    {
        private readonly bool mockExists;

        public MockRecentFileInfo(string path, RecentFileType type, long size, bool exists)
            : base(path, DateTimeOffset.UtcNow, type, new List<string>(), size)
        {
            mockExists = exists;
        }

        public override bool Exists => mockExists;
    }
}
