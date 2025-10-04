using System.Collections.Generic;

namespace VecTool.Core.RecentFiles
{
    public interface IRecentFilesService
    {
        IReadOnlyList<RecentFileItem> GetRecentFiles();
        IReadOnlyList<RecentFileItem> GetRecentFiles(VectorStoreLinkFilter filter, string? storeId = null);
    }
}
