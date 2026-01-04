namespace VecTool.RecentFiles;

using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// JSON round-trip helpers for collections of RecentFileInfo (no IO).
/// </summary>
public static class RecentFilesJson
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    /// <summary>
    /// Serializes a collection of RecentFileInfo to JSON string.
    /// </summary>
    public static string ToJson(IEnumerable<RecentFileInfo> items)
        => JsonSerializer.Serialize(items, _options);

    /// <summary>
    /// Deserializes JSON string to a list of RecentFileInfo.
    /// </summary>
    public static List<RecentFileInfo> FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<RecentFileInfo>();

        try
        {   
            return JsonSerializer.Deserialize<List<RecentFileInfo>>(json, _options);
        }
        catch
        {
            return new List<RecentFileInfo>();
        }
    }
}
