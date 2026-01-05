using VecTool.Configuration.Helpers;
using VecTool.Handlers.Metadata;

namespace VecTool.Handlers.Export;

public sealed class XmlMarkdownWriter : IMetadataWriter
{
    private readonly StreamWriter _writer;

    public XmlMarkdownWriter(StreamWriter writer)
    {
        _writer = writer.ThrowIfNull(nameof(writer));
    }

    public void WriteDocumentMetadata(Core.Metadata.ExportMetadata metadata)
    {
        // Write document-level XML wrapper
        _writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        _writer.WriteLine($"<codebase version=\"{metadata.Version}\" " +
                         $"totalFiles=\"{metadata.TotalFiles}\" " +
                         $"totalLoc=\"{metadata.TotalLoc}\" " +
                         $"exportDate=\"{metadata.ExportDate:O}\">");
    }

    public void WriteFileMetadata(Core.Metadata.FileMetadata metadata, string content)
    {
        // Write file-level XML with metadata attributes
        _writer.WriteLine($"<file path=\"{EscapeXml(metadata.Path)}\" " +
                         $"lines=\"1-{metadata.LinesOfCode}\" " +
                         $"loc=\"{metadata.LinesOfCode}\" " +
                         $"language=\"{metadata.Language}\" " +
                         $"modified=\"{metadata.Modified:O}\">");

        // Write content in CDATA with markdown fence
        _writer.WriteLine("  <content><![CDATA[");
        _writer.WriteLine($"```{metadata.Language}");
        _writer.WriteLine(content);
        _writer.WriteLine("```");
        _writer.WriteLine("]]></content>");
        _writer.WriteLine("</file>");
        _writer.WriteLine();
    }

    public void WriteDocumentFooter()
    {
        _writer.WriteLine("</codebase>");
    }

    private static string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}