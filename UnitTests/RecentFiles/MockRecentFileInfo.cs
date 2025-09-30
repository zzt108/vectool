using DocXHandler.RecentFiles;

namespace UnitTests.UI.RecentFiles
{
    public partial class RecentFilesPanelTests
    {
        /// <summary>
        /// Direct MockRecentFileInfo constructor for precise control in tests.
        /// </summary>
        private class MockRecentFileInfo : RecentFileInfo
        {
            private readonly bool mockExists;

            public MockRecentFileInfo(MockFileInfo info)
                : base(info.Path, DateTimeOffset.UtcNow, info.Type, new List<string>(), info.Size)
            {
                mockExists = info.Exists;
            }

            // NEW: Direct constructor for test usage
            public MockRecentFileInfo(string path, RecentFileType type, long size, bool exists)
                : base(path, DateTimeOffset.UtcNow, type, new List<string>(), size)
            {
                mockExists = exists;
            }

            // Override the Exists property to return our mock value
            public override bool Exists => mockExists;
        }
    }
}
