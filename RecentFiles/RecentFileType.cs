namespace VecTool.RecentFiles;

using System.Text.Json.Serialization;

/// <summary>
/// Enumerates known recent file types for filtering and UI.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecentFileType
{
    Unknown = 0,
    Docx = 1,
    Md = 2,
    Pdf = 3,
    GitChanges = 4,
    TestResults = 5
}
