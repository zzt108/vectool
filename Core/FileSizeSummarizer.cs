namespace VecTool.Core
{
    /// <summary>
    /// Pure business logic - sums file sizes safely.
    /// </summary>
    public static class FileSizeSummarizer
    {
        public static long ComputeTotalBytes(IEnumerable<long> sizes)
        {
            if (sizes is null) throw new ArgumentNullException(nameof(sizes));
            return sizes.Aggregate(0L, (acc, v) => checked(acc + v));
        }

        public static string ToHumanReadable(long bytes)
        {
            if (bytes < 0) throw new ArgumentOutOfRangeException(nameof(bytes));
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            return $"{size:0.##} {units[unitIndex]}";
        }
    }
}
