namespace VecTool.Handlers;

using System;
using VecTool.Handlers.Analysis;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;
using NLogS;

/// <summary>
/// Base class for all file format handlers (DOCX, MD, PDF, Git).
/// Delegates complex operations to specialized helpers following SRP.
/// </summary>
public abstract class FileHandlerBase
{
    protected static readonly CtxLogger _log = new();
    
    protected readonly IUserInterface? _ui;
    protected readonly IRecentFilesManager? _recentFilesManager;
    protected readonly AiContextGenerator _aiContextGenerator;
    protected readonly FileSystemTraverser _fileSystemTraverser;

    protected FileHandlerBase(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
    {
        _ui = ui;
        _recentFilesManager = recentFilesManager;
        _aiContextGenerator = new AiContextGenerator();
        _fileSystemTraverser = new FileSystemTraverser(ui);
    }

    // ============================================================================
    // AI Context - Delegated to AiContextGenerator
    // ============================================================================

    protected string GenerateTableOfContentsList(List<string> folderPaths)
        => _aiContextGenerator.GenerateTableOfContents(folderPaths);

    protected string GenerateCrossReferencesList(List<string> folderPaths)
        => _aiContextGenerator.GenerateCrossReferences(folderPaths);

    protected string GenerateCodeMetaInfoList(List<string> folderPaths)
        => _aiContextGenerator.GenerateCodeMetaInfo(folderPaths);

    protected void AddAIOptimizedContext<T>(
        List<string> folderPaths,
        T context,
        Action<T, string> writeContent)
    {
        var toc = GenerateTableOfContentsList(folderPaths);
        if (!string.IsNullOrWhiteSpace(toc))
            writeContent(context, toc);

        var xref = GenerateCrossReferencesList(folderPaths);
        if (!string.IsNullOrWhiteSpace(xref))
            writeContent(context, xref);

        var meta = GenerateCodeMetaInfoList(folderPaths);
        if (!string.IsNullOrWhiteSpace(meta))
            writeContent(context, meta);
    }

    // ============================================================================
    // Traversal - Delegated to FileSystemTraverser
    // ============================================================================

    protected void ProcessFolder<T>(
        string folderPath,
        T context,
        VectorStoreConfig vectorStoreConfig,
        Action<string, T, VectorStoreConfig> processFile,
        Action<T, string> writeFolderName,
        Action<T>? writeFolderEnd = null)
    {
        _fileSystemTraverser.ProcessFolder(
            folderPath, context, vectorStoreConfig,
            processFile, writeFolderName, writeFolderEnd);
    }

    protected IEnumerable<string> EnumerateFilesRespectingExclusions(string root, VectorStoreConfig config)
        => _fileSystemTraverser.EnumerateFilesRespectingExclusions(root, config);

    // ============================================================================
    // Validation - Virtual for derived overrides
    // ============================================================================

    protected virtual bool IsFolderExcluded(string folderName, VectorStoreConfig config)
        => FileValidator.IsFolderExcluded(folderName, config);

    protected virtual bool IsFileExcluded(string fileName, VectorStoreConfig config)
        => FileValidator.IsFileExcluded(fileName, config);

    protected virtual bool IsFileValid(string path, string? outputPath)
        => FileValidator.IsFileValid(path, outputPath);

    // ============================================================================
    // Content helpers - Virtual for derived customization
    // ============================================================================

    protected virtual string GetFileContent(string filePath)
        => PathHelpers.SafeReadAllText(filePath);

    protected virtual string GetEnhancedFileContent(string file)
        => PathHelpers.SafeReadAllText(file);

    // ============================================================================
    // Legacy compatibility overloads
    // ============================================================================

    protected virtual void ProcessFile(string file, System.IO.StreamWriter writer, VectorStoreConfig vectorStoreConfig)
    {
        // Default no-op - derived handlers override when using StreamWriter context
    }

    protected virtual void WriteFolderName(System.IO.StreamWriter writer, string folderName)
    {
        // Default no-op - derived handlers override when using StreamWriter context
    }
}
