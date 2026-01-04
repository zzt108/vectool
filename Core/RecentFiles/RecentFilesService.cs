namespace VecTool.Core.RecentFiles
{
    public sealed class RecentFilesService : IRecentFilesService
    {
        private readonly IReadOnlyList<RecentFileItem> _items;

        public RecentFilesService(IReadOnlyList<RecentFileItem> items)
        {
            _items = items ?? Array.Empty<RecentFileItem>();
        }

        public IReadOnlyList<RecentFileItem> GetRecentFiles()
        {
            return _items;
        }

        public IReadOnlyList<RecentFileItem> GetRecentFiles(VectorStoreLinkFilter filter, string? storeId = null)
        {
            return filter switch
            {
                VectorStoreLinkFilter.All => _items,
                VectorStoreLinkFilter.Linked => _items.Where(i => i.IsLinked).ToList(),
                VectorStoreLinkFilter.Unlinked => _items.Where(i => !i.IsLinked).ToList(),
                VectorStoreLinkFilter.SpecificStore => string.IsNullOrWhiteSpace(storeId)
                    ? Array.Empty<RecentFileItem>()
                    : _items.Where(i => string.Equals(i.VectorStoreId, storeId, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => _items
            };
        }
    }
}
