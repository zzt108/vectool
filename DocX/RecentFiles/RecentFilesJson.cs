// File: DocXHandler/RecentFiles/RecentFilesJson.cs
// Purpose: JSON round-trip helpers for collections of RecentFileInfo (no IO in Step 2).

using System.Text.Json;

namespace DocXHandler.RecentFiles
{
    public static class RecentFilesJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true
        };

        public static string ToJson(IEnumerable<RecentFileInfo> items)
            => JsonSerializer.Serialize(items, Options);

        public static List<RecentFileInfo> FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<RecentFileInfo>();
            var result = JsonSerializer.Deserialize<List<RecentFileInfo>>(json, Options);
            return result ?? new List<RecentFileInfo>();
        }
    }
}
