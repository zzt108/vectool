namespace XmlStringDiscovery
{
    public enum TagContextCategory
    {
        Unknown = 0,
        Metadata = 1,
        Structure = 2,
        Content = 3
    }

    public sealed class TagInfo
    {
        public string Name { get; }
        public TagContextCategory Category { get; set; }
        public int Count { get; private set; }
        public HashSet<string> SourceFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool IsTestOnly { get; set; }

        public TagInfo(string name, TagContextCategory category)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Category = category;
        }

        public void AddOccurrence(string filePath)
        {
            Count++;
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                SourceFiles.Add(filePath);
            }
        }
    }

    public sealed class TagCatalog
    {
        public Dictionary<string, TagInfo> All { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, TagInfo> TestOnly { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, TagInfo> Production { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class ScanOptions
    {
        public string RootDirectory { get; set; } = AppContext.BaseDirectory;
        public Func<string, bool>? FileFilter { get; set; } // default: *.cs
    }
}
