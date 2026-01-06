namespace VecTool.Handlers.Metadata;

/// <summary>
/// Format-agnostic interface for writing metadata to export files.
/// Implementers: XmlMarkdownWriter, DocxMetadataWriter, PdfMetadataWriter.
/// </summary>
public interface IMetadataWriter
{
    /// <summary>
    /// Write document-level metadata (totals, version, date).
    /// </summary>
    void WriteDocumentMetadata(Core.Metadata.ExportMetadata metadata);

    /// <summary>
    /// Write file-level metadata before the file content.
    /// </summary>
    /// <param name="metadata">File metadata (path, LOC, language, etc.).</param>
    /// <param name="content">File content to write after metadata.</param>
    void WriteFileMetadata(Core.Metadata.FileMetadata metadata, string content);
}