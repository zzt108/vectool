namespace VecTool.RecentFiles;

using System.Text.Json.Serialization;

/// <summary>
/// Enumerates known recent file types for filtering and UI.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecentFileType
{
    Unknown = 0,
    Codebase_Docx = 1,
    Codebase_Md = 2,
    Codebase_Pdf = 3,
    Git_Md = 4,
    TestResults_Md = 5,
    Plan,
    Guide
}

/// <summary>
/// Extension methods for RecentFileType enum.
/// </summary>
public static class RecentFileTypeExtensions
{
    /// <summary>
    /// Converts enum value to file suffix with extension.
    /// Replaces underscore with dot: TestResultsMd -> _test-results.md
    /// </summary>
    public static string ToFileSuffix(this RecentFileType fileType)
    {
        return fileType switch
        {
            RecentFileType.Codebase_Docx => "_codebase.docx",
            RecentFileType.Codebase_Md => "_codebase.md",
            RecentFileType.Codebase_Pdf => "_codebase.pdf",
            RecentFileType.Git_Md => "_git.md",
            RecentFileType.TestResults_Md => "_test-results.md",
            RecentFileType.Plan => "_plan.md",
            RecentFileType.Guide => "_guide.md",
            RecentFileType.Unknown => ".txt",
            _ => ".txt"
        };
    }
}
