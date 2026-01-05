using LogCtxShared;
using Microsoft.Extensions.Logging;
using VecTool.Utils;

namespace VecTool.Core.Metadata;

/// <summary>
/// Collects metadata from files for export.
/// </summary>
public sealed class MetadataCollector
{
    private readonly ILogger _logger;

    public MetadataCollector(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Collect metadata for a single file.
    /// </summary>
    /// <param name="filePath">Full path to file.</param>
    /// <param name="content">File content (for LOC calculation).</param>
    /// <returns>File metadata.</returns>
    public FileMetadata CollectFileMetadata(string filePath, string content)
    {
        using var ctx = _logger.SetContext()
            .Add("filePath", filePath);

        try
        {
            var fileInfo = new FileInfo(filePath);
            var extension = Path.GetExtension(filePath);
            var language = DetectLanguage(extension);
            var lines = CountLines(content);

            var metadata = new FileMetadata
            {
                Path = filePath,
                Lines = $"1-{lines}",
                LinesOfCode = lines,
                Language = language,
                Modified = fileInfo.LastWriteTime,
                SizeBytes = fileInfo.Length
            };

            _logger.LogDebug($"Collected metadata: {lines} LOC, {language}");
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to collect metadata for file");

            // Return minimal metadata on error
            return new FileMetadata
            {
                Path = filePath,
                Language = "unknown",
                Modified = DateTime.MinValue
            };
        }
    }

    /// <summary>
    /// Collect document-level metadata from multiple files.
    /// </summary>
    /// <param name="files">Collection of (path, content) tuples.</param>
    /// <param name="folderPaths">Source folder paths.</param>
    /// <param name="version">VecTool version.</param>
    /// <returns>Export metadata.</returns>
    public ExportMetadata CollectExportMetadata(
        IReadOnlyList<(string path, string content)> files,
        IReadOnlyList<string> folderPaths,
        string version)
    {
        using var ctx = _logger.SetContext()
            .Add("fileCount", files.Count)
            .Add("folderCount", folderPaths.Count);

        var totalLoc = 0;
        foreach (var (_, content) in files)
        {
            totalLoc += CountLines(content);
        }

        var metadata = new ExportMetadata
        {
            TotalFiles = files.Count,
            TotalLoc = totalLoc,
            ExportDate = DateTime.UtcNow,
            Version = version,
            SourceFolders = folderPaths
        };

        _logger.LogInformation($"Export metadata collected: {metadata.TotalFiles} files, {metadata.TotalLoc} LOC");
        return metadata;
    }

    /// <summary>
    /// Count non-empty lines in content.
    /// </summary>
    private static int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        return content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Detect programming language from file extension.
    /// </summary>
    private static string DetectLanguage(string extension)
    {
        // Use existing MimeTypeProvider if available
        var mdTag = MimeTypeProvider.GetMdTag(extension);
        if (!string.IsNullOrEmpty(mdTag))
            return mdTag;

        // Fallback to extension without dot
        return string.IsNullOrEmpty(extension)
            ? "plaintext"
            : extension.TrimStart('.');
    }
}