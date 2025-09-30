namespace VecTool.RecentFiles;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

/// <summary>
/// Data model for tracking metadata of a generated recent file.
/// </summary>
public class RecentFileInfo
{
    /// <summary>
    /// Gets the full path to the generated file.
    /// </summary>
    [JsonPropertyName("filePath")]
    public string FilePath { get; init; }

    /// <summary>
    /// Gets the generation timestamp.
    /// </summary>
    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    /// Gets the type of the generated file.
    /// </summary>
    [JsonPropertyName("fileType")]
    public RecentFileType FileType { get; init; }

    /// <summary>
    /// Gets the list of source folders used to generate this file.
    /// </summary>
    [JsonPropertyName("sourceFolders")]
    public List<string> SourceFolders { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    [JsonPropertyName("fileSizeBytes")]
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Gets whether the file currently exists on disk.
    /// </summary>
    [JsonIgnore]
    public virtual bool Exists => SafeExists(FilePath);

    /// <summary>
    /// Gets the file name without path.
    /// </summary>
    [JsonIgnore]
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentFileInfo"/> class.
    /// </summary>
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
        FileSizeBytes = fileSizeBytes < 0 ? 0 : fileSizeBytes;
    }

    /// <summary>
    /// Creates a new instance from a file path with optional metadata.
    /// </summary>
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
        expanded = expanded.Replace('/', Path.DirectorySeparatorChar)
                          .Replace('\\', Path.DirectorySeparatorChar);
        return expanded;
    }

    private static bool SafeExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }
}
