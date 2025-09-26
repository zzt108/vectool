// File: DocXHandler/RecentFiles/RecentFileType.cs
// Purpose: Enumerates known recent file types for filtering and UI.
// Language: English-only identifiers and comments, per guidelines.

using System.Text.Json.Serialization;

namespace DocXHandler.RecentFiles
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RecentFileType
    {
        Unknown = 0,
        Docx = 1,
        Md = 2,
        Pdf = 3,
        GitChanges = 4
    }
}
