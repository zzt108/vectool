namespace VecTool.Handlers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VecTool.Configuration;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

/// <summary>
/// Handler for exporting folder structures to PDF format using QuestPDF.
/// </summary>
public sealed class PdfHandler : FileHandlerBase
{
    static PdfHandler()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.EnableDebugging = false;
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
    }

    public PdfHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        : base(ui, recentFilesManager)
    {
    }

    /// <summary>
    /// Converts selected folders to a single PDF file.
    /// </summary>
    public void ConvertSelectedFoldersToPdf(
        List<string> folderPaths,
        string outputPath,
        VectorStoreConfig vectorStoreConfig)
    {
        if (folderPaths == null || folderPaths.Count == 0)
            throw new ArgumentException("No folders provided", nameof(folderPaths));

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path required", nameof(outputPath));

        try
        {
            _ui?.UpdateStatus("Creating PDF document...");
            _log.Info($"Starting PDF conversion: {folderPaths.Count} folders -> {outputPath}");

            var columnList = new List<Action<ColumnDescriptor>>();

            foreach (var folder in folderPaths)
            {
                CollectFolderContent(folder, vectorStoreConfig, columnList);
            }

            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Margin(20);
                    page.Content().Column(column =>
                    {
                        column.Spacing(10);
                        foreach (var action in columnList)
                        {
                            action(column);
                        }
                    });
                });
            })
            .WithSettings(new DocumentSettings
            {
                PdfA = false,
                CompressDocument = true,
                ImageCompressionQuality = ImageCompressionQuality.Medium,
                ImageRasterDpi = 72
            })
            .GeneratePdf(outputPath);

            // Register in recent files
            if (_recentFilesManager != null)
            {
                var fi = new FileInfo(outputPath);
                _recentFilesManager.RegisterGeneratedFile(
                    outputPath,
                    RecentFileType.Pdf,
                    folderPaths,
                    fi.Exists ? fi.Length : 0);
            }

            _ui?.UpdateStatus($"PDF created: {outputPath}");
            _log.Info($"PDF conversion completed: {outputPath}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to convert folders to PDF: {outputPath}");
            throw;
        }
    }

    private void CollectFolderContent(
        string folderPath,
        VectorStoreConfig config,
        List<Action<ColumnDescriptor>> actions)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return;

        var folderName = new DirectoryInfo(folderPath).Name;

        if (FileValidator.IsFolderExcluded(folderName, config))
        {
            _log.Trace($"Skipping excluded folder: {folderPath}");
            return;
        }

        // Add folder header
        actions.Add(column => 
            column.Item().Text(folderName).FontSize(14).Bold().FontColor(Colors.Black));

        // Process files
        string[] files = Array.Empty<string>();
        try { files = Directory.GetFiles(folderPath); } catch { }

        foreach (var file in files)
        {
            ProcessFileToPdf(file, config, actions);
        }

        // Process subfolders recursively
        string[] subfolders = Array.Empty<string>();
        try { subfolders = Directory.GetDirectories(folderPath); } catch { }

        foreach (var sub in subfolders)
        {
            CollectFolderContent(sub, config, actions);
        }
    }

    private void ProcessFileToPdf(
        string file,
        VectorStoreConfig config,
        List<Action<ColumnDescriptor>> actions)
    {
        var fileName = Path.GetFileName(file);

        if (FileValidator.IsFileExcluded(fileName, config))
        {
            _log.Trace($"File excluded: {file}");
            return;
        }

        if (!FileValidator.IsFileValid(file, null))
        {
            _log.Trace($"File invalid: {file}");
            return;
        }

        try
        {
            var content = PathHelpers.SafeReadAllText(file);
            
            if (string.IsNullOrWhiteSpace(content))
            {
                _log.Debug($"Empty content for file: {file}");
                return;
            }

            var directoryName = Path.GetDirectoryName(file);
            if (string.IsNullOrEmpty(directoryName))
                directoryName = ".";

            var relativePath = Path.GetRelativePath(directoryName, file);
            var lastModified = File.GetLastWriteTime(file);

            // Add file header
            actions.Add(column =>
            {
                column.Item()
                    .PaddingLeft(10)
                    .DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black))
                    .Text(text =>
                    {
                        text.Span("File: ").SemiBold();
                        text.Span(relativePath).SemiBold()
                            .Style(TextStyle.Default.FontColor(Colors.Grey.Darken2));
                        text.Span(" Time: ").SemiBold();
                        text.Span($"{lastModified}").SemiBold()
                            .Style(TextStyle.Default.FontColor(Colors.Grey.Darken2));
                    });
            });

            // Add file content
            actions.Add(column =>
            {
                column.Item()
                    .PaddingLeft(15)
                    .Text(content)
                    .FontSize(10);
            });

            _log.Trace($"Processed file: {fileName}");
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Error processing file: {file}");
        }
    }
}
