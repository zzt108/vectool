// File: OaiUI/Config/PerVectorStoreSettings.cs

namespace oaiUI.Config
{
    /// <summary>
    /// Per-vector-store exclusion settings with inheritance from global app.config.
    /// Keeps persisted JSON shape owned by oaiVectorStore.VectorStoreConfig unchanged.
    /// </summary>
    public sealed class PerVectorStoreSettings
    {
        public string Name { get; }
        public bool UseCustomExcludedFiles { get; }
        public bool UseCustomExcludedFolders { get; }
        public List<string> CustomExcludedFiles { get; }
        public List<string> CustomExcludedFolders { get; }

        // Public ctor to match call sites (5 args)
        public PerVectorStoreSettings(
            string name,
            bool useCustomExcludedFiles,
            bool useCustomExcludedFolders,
            IEnumerable<string>? customExcludedFiles,
            IEnumerable<string>? customExcludedFolders)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Vector store name is required.", nameof(name));

            Name = name;
            UseCustomExcludedFiles = useCustomExcludedFiles;
            UseCustomExcludedFolders = useCustomExcludedFolders;
            CustomExcludedFiles = (customExcludedFiles ?? Enumerable.Empty<string>())
                .Select(Normalize).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            CustomExcludedFolders = (customExcludedFolders ?? Enumerable.Empty<string>())
                .Select(Normalize).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string Normalize(string s) => (s ?? string.Empty).Trim();

        public static PerVectorStoreSettings From(string name, VectorStoreConfig global, VectorStoreConfig? perOrNull)
        {
            var globalFiles = (global?.ExcludedFiles ?? new List<string>()).Select(Normalize).ToList();
            var globalFolders = (global?.ExcludedFolders ?? new List<string>()).Select(Normalize).ToList();

            var perFiles = (perOrNull?.ExcludedFiles ?? new List<string>()).Select(Normalize).ToList();
            var perFolders = (perOrNull?.ExcludedFolders ?? new List<string>()).Select(Normalize).ToList();

            bool filesDiffer = !SequenceEqualIgnoreOrder(perFiles, globalFiles);
            bool foldersDiffer = !SequenceEqualIgnoreOrder(perFolders, globalFolders);

            return new PerVectorStoreSettings(
                name,
                useCustomExcludedFiles: filesDiffer,
                useCustomExcludedFolders: foldersDiffer,
                customExcludedFiles: filesDiffer ? perFiles : globalFiles,
                customExcludedFolders: foldersDiffer ? perFolders : globalFolders
            );
        }

        public VectorStoreConfig ToEffectiveVectorStoreConfig(VectorStoreConfig global, VectorStoreConfig? existingPerOrNull)
        {
            var basePer = existingPerOrNull?.Clone() ?? new VectorStoreConfig();
            basePer.ExcludedFiles = (UseCustomExcludedFiles ? CustomExcludedFiles : global.ExcludedFiles).Select(Normalize).ToList();
            basePer.ExcludedFolders = (UseCustomExcludedFolders ? CustomExcludedFolders : global.ExcludedFolders).Select(Normalize).ToList();
            basePer.FolderPaths = existingPerOrNull?.FolderPaths?.ToList() ?? basePer.FolderPaths ?? new List<string>();
            return basePer;
        }

        public static void Save(Dictionary<string, VectorStoreConfig> all, PerVectorStoreSettings settings, VectorStoreConfig global)
        {
            if (all is null) throw new ArgumentNullException(nameof(all));
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (global is null) throw new ArgumentNullException(nameof(global));

            all[settings.Name] = settings.ToEffectiveVectorStoreConfig(
                global,
                all.TryGetValue(settings.Name, out var ex) ? ex : null
            );
        }

        private static bool SequenceEqualIgnoreOrder(List<string> a, List<string> b)
        {
            var aa = a?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(Normalize).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
            var bb = b?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(Normalize).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
            if (aa.Count != bb.Count) return false;
            for (int i = 0; i < aa.Count; i++)
                if (!string.Equals(aa[i], bb[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            return true;
        }
    }
}
