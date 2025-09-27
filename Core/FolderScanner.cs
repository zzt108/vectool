using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            if (files is null) throw new ArgumentNullException(nameof(files));
            long total = 0;
            foreach (var file in files)
            {
                var length = new FileInfo(file).Length;
                total = checked(total + length);
            }
            return total;
        }
    }
}
