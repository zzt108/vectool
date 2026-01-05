namespace VecTool.Core.Metadata;

/// <summary>
/// Document-level metadata for the entire export.
/// </summary>
public sealed record ExportMetadata
{
    /// <summary>
    /// Total number of files exported.
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// Total lines of code across all files.
    /// </summary>
    public int TotalLoc { get; init; }

    /// <summary>
    /// Export generation timestamp.
    /// </summary>
    public DateTime ExportDate { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// VecTool version (e.g., "4.7.p4").
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// List of source folder paths included in export.
    /// </summary>
    public IReadOnlyList<string> SourceFolders { get; init; } = Array.Empty<string>();
}