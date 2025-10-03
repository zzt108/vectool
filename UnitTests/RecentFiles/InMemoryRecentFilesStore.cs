// Path: UnitTests/RecentFiles/InMemoryRecentFilesStore.cs
using VecTool.RecentFiles;

namespace UnitTests.RecentFiles
{
    public sealed class InMemoryRecentFilesStore : IRecentFilesStore
    {
        private string? _json;

        public string? Read() => _json;

        public void Write(string json) => _json = json;
    }
}
