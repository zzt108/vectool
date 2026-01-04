using VecTool.Configuration.Helpers;

namespace VecTool.Core.RecentFiles
{
    public sealed class RecentFileItem
    {
        public string Path { get; }
        public string? VectorStoreId { get; }

        public bool IsLinked => !string.IsNullOrWhiteSpace(VectorStoreId);

        public RecentFileItem(string path, string? vectorStoreId)
        {
            Path = path.ThrowIfNull(nameof(path));
            VectorStoreId = vectorStoreId;
        }
    }
}