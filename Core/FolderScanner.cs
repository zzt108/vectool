using VecTool.Configuration.Helpers;

namespace VecTool.Core
{
    /// <summary>
    /// Pure logic to enumerate files and summarize sizes; no Windows-specific APIs.
    /// </summary>
    public sealed class FolderScanner
    {
        public IReadOnlyList<string> GetFiles(string rootPath, string searchPattern = "*.*")
        {
            if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("Root path is required", nameof(rootPath));
            if (!Directory.Exists(rootPath)) throw new DirectoryNotFoundException(rootPath);
            return Directory.EnumerateFiles(rootPath, searchPattern, SearchOption.AllDirectories).ToList();
        }

        public long TotalSize(IEnumerable<string> files)
        {
            long total = 0;
            foreach (var file in files.ThrowIfNull(nameof(files), null, "Files is required."))
            {
                var length = new FileInfo(file).Length;
                total = checked(total + length);
            }
            return total;
        }
    }
}