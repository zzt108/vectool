using System;

namespace VecTool.Core.RecentFiles
{
    //public sealed class RecentFileItem
    //{
    //    public string Path { get; }
    //    public string? VectorStoreId { get; }

    //    public bool IsLinked => !string.IsNullOrWhiteSpace(VectorStoreId);

    //    public RecentFileItem(string path, string? vectorStoreId)
    //    {
    //        Path = path ?? throw new ArgumentNullException(nameof(path));
    //        VectorStoreId = vectorStoreId;
    //    }
    //}

    public sealed class RecentFileItem
    {
        public string FileName { get; }
        public string Path { get; }
        public string? LinkedStoreName { get; }
        public string? VectorStoreId { get; }

        public bool IsLinked => !string.IsNullOrWhiteSpace(VectorStoreId);

        public RecentFileItem(string path, string? linkedStoreName)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            FileName = System.IO.Path.GetFileName(path);
            LinkedStoreName = linkedStoreName;
        }
    }

}
