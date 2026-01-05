#nullable enable

using LogCtxShared;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VecTool.Configuration;
using VecTool.Configuration.Logging;
using VecTool.Handlers;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;
using VecTool.Utils;

namespace VecTool.Export.Pdf;

/// <summary>
/// Exports folder structures to PDF using QuestPDF and FileSystemTraverser exclusion system.
/// Respects .gitignore, .vtignore, and VECTOOL_EXCLUDE file markers.
/// </summary>
public sealed class PdfExportHandler : FileHandlerBase, IPdfExporter
{
    private static readonly ILogger log = AppLogger.For<PdfExportHandler>();
    private readonly IFileSystemTraverser traverser;

    public PdfExportHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        : base(AppLogger.For<PdfExportHandler>(), ui, recentFilesManager, traverser: null)
    {
        // Use existing exclusion system: Layer 1 + Layer 2
        var markerExtractor = new FileMarkerExtractor();
        traverser = new FileSystemTraverser(ui, markerExtractor);
    }

    public void ConvertSelectedFoldersToPdf(
        List<string> folderPaths,
        string outputPath,
        VectorStoreConfig vectorStoreConfig)
    {
        using var ctx = log.SetContext()
            .Add("operation", "pdf_export")
            .Add("outputPath", outputPath)
            .Add("folderCount", folderPaths.Count);

        try
        {
            Ui?.WorkStart("Generating PDF...", folderPaths);
            log.LogInformation("PDF export started");

            // Configure QuestPDF license (Community for open-source projects)
            QuestPDF.Settings.License = LicenseType.Community;

            // Generate PDF document
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Courier New"));

                    page.Header()
                        .Text("VecTool Codebase Export")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            // Add folder list
                            column.Item().Text("Exported Folders:").Bold();
                            foreach (var folder in folderPaths)
                            {
                                column.Item().Text($"  - {folder}");
                            }

                            column.Item().PaddingTop(0.5f, Unit.Centimetre);

                            // Traverser handles ALL exclusions (.gitignore, .vtignore, markers)
                            foreach (var root in folderPaths)
                            {
                                traverser.ProcessFolder(
                                    folderPath: root,
                                    context: column,
                                    vectorStoreConfig: vectorStoreConfig,
                                    processFile: (filePath, col, cfg) => AddFileToPdf(filePath, col, cfg),
                                    writeFolderName: (col, name) => AddFolderHeader(col, name)
                                );
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf(outputPath);

            // Register in recent files
            if (RecentFilesManager != null)
            {
                var fi = new FileInfo(outputPath);
                RecentFilesManager.RegisterGeneratedFile(
                    outputPath,
                    RecentFileType.Pdf,
                    folderPaths,
                    fi.Exists ? fi.Length : 0
                );
            }

            Ui?.UpdateStatus($"PDF created: {outputPath}");
            log.LogInformation($"PDF export completed successfully for {folderPaths.Count} folders, {new FileInfo(outputPath).Length} bytes");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to convert folders to PDF");
            Ui?.ShowMessage($"PDF export failed: {ex.Message}", "Error", MessageType.Error);
            throw;
        }
        finally
        {
            Ui?.WorkFinish();
        }
    }

    private void AddFileToPdf(string filePath, ColumnDescriptor column, IVectorStoreConfig config)
    {
        // File already passed exclusion checks in traverser
        var content = PathHelpers.SafeReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            log.LogTrace($"Empty file skipped: {filePath}");
            return;
        }

        var rootPath = config.GetRootPath() ?? ".";
        var relativePath = Path.GetRelativePath(rootPath, filePath);

        // Add file header
        column.Item().PaddingTop(0.3f, Unit.Centimetre)
            .Text($"📄 File: {relativePath}")
            .Bold()
            .FontSize(11)
            .FontColor(Colors.Blue.Darken2);

        // Add file content (truncate if too long)
        var displayContent = content.Length > 5000
            ? content.Substring(0, 5000) + "\n\n[... truncated ...]"
            : content;

        column.Item()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5)
            .Text(displayContent)
            .FontSize(8)
            .FontFamily("Courier New");

        log.LogTrace($"File added to PDF: {relativePath}, contentLength={content.Length}");
    }

    private void AddFolderHeader(ColumnDescriptor column, string folderName)
    {
        column.Item().PaddingTop(0.5f, Unit.Centimetre)
            .Text($"📁 Folder: {folderName}")
            .Bold()
            .FontSize(14)
            .FontColor(Colors.Green.Darken1);
    }
}