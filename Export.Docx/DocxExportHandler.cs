using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LogCtxShared;
using Microsoft.Extensions.Logging;
using VecTool.Configuration;
using VecTool.Configuration.Logging;
using VecTool.Handlers;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

namespace VecTool.Export.Docx;

/// <summary>
/// Exports folder structures to DOCX using FileSystemTraverser exclusion system.
/// Respects .gitignore, .vtignore, and VECTOOL_EXCLUDE file markers.
/// </summary>
public sealed class DocxExportHandler : FileHandlerBase, IDocxExporter
{
    private static readonly ILogger _log = AppLogger.For<DocxExportHandler>();
    private readonly IFileSystemTraverser _traverser;

    public DocxExportHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        : base(AppLogger.For<DocxExportHandler>(), ui, recentFilesManager, traverser: null)
    {
        // ✅ Use existing exclusion system (Layer 1 + Layer 2)
        var markerExtractor = new FileMarkerExtractor();
        _traverser = new FileSystemTraverser(ui, markerExtractor);
    }

    public void ConvertSelectedFoldersToDocx(
        List<string> folderPaths,
        string outputPath,
        VectorStoreConfig vectorStoreConfig)
    {
        using var ctx = _log.SetContext()
            .Add("operation", "docx_export")
            .Add("outputPath", outputPath)
            .Add("folderCount", folderPaths.Count);

        try
        {
            Ui?.WorkStart("Generating DOCX...", folderPaths);
            _log.LogInformation("DOCX export started");

            using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            // ✅ Traverser handles ALL exclusions (.gitignore + .vtignore + markers)
            foreach (var root in folderPaths)
            {
                _traverser.ProcessFolder(
                    root,
                    context: body,
                    vectorStoreConfig,
                    processFile: AddFileToDocument,
                    writeFolderName: AddFolderHeader
                );
            }

            mainPart.Document.Save();

            // Register in recent files
            if (RecentFilesManager != null)
            {
                var fi = new FileInfo(outputPath);
                RecentFilesManager.RegisterGeneratedFile(
                    outputPath,
                    RecentFileType.Docx,
                    folderPaths,
                    fi.Exists ? fi.Length : 0
                );
            }

            Ui?.UpdateStatus($"DOCX created: {outputPath}");
            _log.LogInformation("DOCX export completed successfully for {folderCount} folders", new FileInfo(outputPath).Length);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to convert folders to DOCX");
            Ui?.ShowMessage($"DOCX export failed: {ex.Message}", "Error", MessageType.Error);
            throw;
        }
        finally
        {
            Ui?.WorkFinish();
        }
    }

    private void AddFileToDocument(string filePath, Body body, IVectorStoreConfig config)
    {
        // ✅ File already passed exclusion checks in traverser
        var content = PathHelpers.SafeReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            _log.LogTrace("Empty file skipped: {}", filePath);
            return;
        }

        var rootPath = config.GetRootPath() ?? ".";
        var relativePath = Path.GetRelativePath(rootPath, filePath);

        // Add file header
        body.AppendChild(new Paragraph(new Run(new Text($"File: {relativePath}")
        {
            Space = SpaceProcessingModeValues.Preserve
        })));

        // Add file content
        body.AppendChild(new Paragraph(new Run(new Text(content)
        {
            Space = SpaceProcessingModeValues.Preserve
        })));

        _log.LogTrace("File added to DOCX {relativePath} {contentLength}", relativePath, content.Length);
    }

    private void AddFolderHeader(Body body, string folderName)
    {
        body.AppendChild(new Paragraph(new Run(new Text($"Folder: {folderName}")
        {
            Space = SpaceProcessingModeValues.Preserve
        })));
    }
}