namespace VecTool.RecentFiles;

using System.Text.Json.Serialization;

/// <summary>
/// Enumerates known recent file types for filtering and UI.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecentFileType
{
    Unknown = 0,
    AllSourceDocx = 1,
    AllSourceMd = 2,
    AllSourcePdf = 3,
    GitChanges = 4,
    TestResults = 5,
    Plan,
    Guide
}
