namespace VecTool.Handlers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using VecTool.Configuration;
using VecTool.Constants;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

/// <summary>
/// Handler for converting folder structures to DOCX format with AI-optimized metadata.
/// </summary>
public sealed class DocXHandler : FileHandlerBase
{
    public DocXHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        : base(ui, recentFilesManager)
    {
    }

    /// <summary>
    /// Converts selected folders to a single DOCX file with AI context.
    /// </summary>
    public void ConvertSelectedFoldersToDocx(
        List<string> folderPaths,
        string outputDocxPath,
        VectorStoreConfig vectorStoreConfig)
    {
        if (folderPaths == null || folderPaths.Count == 0)
            throw new ArgumentException("No folders provided", nameof(folderPaths));

        if (string.IsNullOrWhiteSpace(outputDocxPath))
            throw new ArgumentException("Output path required", nameof(outputDocxPath));

        try
        {
            _ui?.UpdateStatus("Creating DOCX document...");
            _log.Info($"Starting DOCX conversion: {folderPaths.Count} folders -> {outputDocxPath}");

            using (var doc = WordprocessingDocument.Create(outputDocxPath, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());
                var body = mainPart.Document.Body!;

                // Add AI-optimized context at the beginning
                AddAIOptimizedContext(folderPaths, body, (b, content) =>
                {
                    AppendFormattedXmlBlock(b, content);
                });

                // Process each folder
                foreach (var folder in folderPaths)
                {
                    ProcessFolder(
                        folder,
                        body,
                        vectorStoreConfig,
                        ProcessFileToDocx,
                        WriteFolderNameToDocx,
                        WriteFolderEndToDocx);
                }

                mainPart.Document.Save();
            }

            // Register in recent files
            if (_recentFilesManager != null)
            {
                var fi = new FileInfo(outputDocxPath);
                _recentFilesManager.RegisterGeneratedFile(
                    outputDocxPath,
                    RecentFileType.Docx,
                    folderPaths,
                    fi.Exists ? fi.Length : 0);
            }

            _ui?.UpdateStatus($"DOCX created: {outputDocxPath}");
            _log.Info($"DOCX conversion completed: {outputDocxPath}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to convert folders to DOCX: {outputDocxPath}");
            throw;
        }
    }

    private void ProcessFileToDocx(string file, Body body, VectorStoreConfig config)
    {
        var fileName = Path.GetFileName(file);
        
        if (IsFileExcluded(fileName, config))
        {
            _log.Trace($"File excluded: {file}");
            return;
        }

        if (!IsFileValid(file, null))
        {
            _log.Trace($"File invalid: {file}");
            return;
        }

        try
        {
            var content = PathHelpers.SafeReadAllText(file);
            var ext = Path.GetExtension(file);

            // File name tag
            body.Append(new Paragraph(new Run(new Text(
                TagBuilder.SelfClosing(
                    Tags.File,
                    TagBuilder.BuildFileNameTag(fileName),
                    TagBuilder.BuildExtensionTag(ext))))));

            // File content
            body.Append(new Paragraph(new Run(new Text(content))));

            _log.Trace($"Processed file: {fileName}");
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Error processing file: {file}");
        }
    }

    private void WriteFolderNameToDocx(Body body, string folderName)
    {
        body.Append(new Paragraph(new Run(new Text(
            TagBuilder.OpenWith(Tags.Folder, TagBuilder.BuildFolderNameTag(folderName))))));
    }

    private void WriteFolderEndToDocx(Body body)
    {
        body.Append(new Paragraph(new Run(new Text(TagBuilder.Close(Tags.Folder)))));
    }

    private void AppendFormattedXmlBlock(Body body, string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            return;

        foreach (var line in xmlContent.Split('\n'))
        {
            body.Append(new Paragraph(new Run(new Text(line.TrimEnd()))));
        }
    }
}
