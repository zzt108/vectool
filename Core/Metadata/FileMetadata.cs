namespace VecTool.Core.Metadata;

/// <summary>
/// Metadata for a single file in the export.
/// </summary>
public sealed record FileMetadata
{
    /// <summary>
    /// Relative path from repository root (e.g., "Configuration/IIgnorePatternMatcher.cs").
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Line range (e.g., "1-28").
    /// </summary>
    public string Lines { get; init; } = string.Empty;

    /// <summary>
    /// Total lines of code in the file.
    /// </summary>
    public int LinesOfCode { get; init; }

    /// <summary>
    /// Programming language detected from file extension.
    /// </summary>
    public string Language { get; init; } = string.Empty;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime Modified { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }
}