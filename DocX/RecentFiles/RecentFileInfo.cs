// File: DocXHandler/RecentFiles/RecentFileInfo.cs
// Purpose: Data model for tracking metadata of a generated "recent file" (Step 2).

using System.Text.Json.Serialization;

namespace DocXHandler.RecentFiles
{
    public class RecentFileInfo
    {
        // JSON-contract properties
        [JsonPropertyName("filePath")]
        public string FilePath { get; init; }

        [JsonPropertyName("generatedAt")]
        public DateTimeOffset GeneratedAt { get; init; }

        [JsonPropertyName("fileType")]
        public RecentFileType FileType { get; init; }

        [JsonPropertyName("sourceFolders")]
        public List<string> SourceFolders { get; init; }

        [JsonPropertyName("fileSizeBytes")]
        public long FileSizeBytes { get; init; }

        // Derived, non-persisted
        [JsonIgnore]
        public virtual bool Exists => SafeExists(FilePath);

        [JsonIgnore]
        public string FileName => Path.GetFileName(FilePath);

        [JsonConstructor]
        public RecentFileInfo(
            string filePath,
            DateTimeOffset generatedAt,
            RecentFileType fileType,
            List<string>? sourceFolders,
            long fileSizeBytes)
        {
            FilePath = NormalizePathOrThrow(filePath);
            GeneratedAt = generatedAt == default ? DateTimeOffset.UtcNow : generatedAt;
            FileType = fileType;
            SourceFolders = sourceFolders?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList()
                            ?? new List<string>();
            FileSizeBytes = fileSizeBytes >= 0 ? fileSizeBytes : 0;
        }

        public static RecentFileInfo FromPath(
            string filePath,
            RecentFileType fileType,
            IEnumerable<string> sourceFolders,
            DateTimeOffset? generatedAt = null)
        {
            var normalized = NormalizePathOrThrow(filePath);
            long size = 0;
            try
            {
                var fi = new FileInfo(normalized);
                size = fi.Exists ? fi.Length : 0;
            }
            catch
            {
                size = 0;
            }

            return new RecentFileInfo(
                normalized,
                generatedAt ?? DateTimeOffset.UtcNow,
                fileType,
                sourceFolders?.ToList() ?? new List<string>(),
                size);
        }

        private static string NormalizePathOrThrow(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("filePath must not be null or whitespace.", nameof(path));

            var expanded = Environment.ExpandEnvironmentVariables(path);
            expanded = expanded.Replace('\\', Path.DirectorySeparatorChar)
                               .Replace('/', Path.DirectorySeparatorChar);
            return expanded;
        }

        private static bool SafeExists(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            try { return File.Exists(path); } catch { return false; }
        }
    }
}
