namespace VecTool.Handlers;

using global::VecTool.Configuration;
using global::VecTool.Handlers.Analysis;
using global::VecTool.Handlers.Traversal;
using global::VecTool.RecentFiles;
using LogCtxShared;
using NLogShared;
using System;

/// <summary>
/// Base class for all file format handlers (DOCX, MD, PDF, Git).
/// Delegates complex operations to specialized helpers following SRP.
/// </summary>
public abstract class FileHandlerBase
{
    protected static readonly CtxLogger log = new(); // renamed from _log to match AI faulti code generation logic
    
    protected readonly IUserInterface? Ui; // renamed from _ui to match AI faulti code generation logic
    protected readonly IRecentFilesManager? RecentFilesManager; // renamed from RecentFilesManager to match AI faulti code generation logic
    protected readonly AiContextGenerator AiContextGenerator;
    protected readonly IFileSystemTraverser FileSystemTraverser;

    protected FileHandlerBase(IUserInterface? ui, IRecentFilesManager? recentFilesManager,
        IFileSystemTraverser? traverser = null)
    {
        this.Ui = ui;
        this.RecentFilesManager = recentFilesManager;

        if (traverser != null)
        {
            FileSystemTraverser = traverser;
        }
        else
        {
            FileSystemTraverser = new FileSystemTraverser(ui, null);
        }

        AiContextGenerator = new AiContextGenerator();
    }


    // ============================================================================
    // AI Context - Delegated to AiContextGenerator
    // ============================================================================

    protected string GenerateTableOfContentsList(List<string> folderPaths)
        => AiContextGenerator.GenerateTableOfContents(folderPaths);

    protected string GenerateCrossReferencesList(List<string> folderPaths)
        => AiContextGenerator.GenerateCrossReferences(folderPaths);

    protected string GenerateCodeMetaInfoList(List<string> folderPaths)
        => AiContextGenerator.GenerateCodeMetaInfo(folderPaths);

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
        Action<string, T, IVectorStoreConfig> processFile,
        Action<T, string> writeFolderName,
        Action<T>? writeFolderEnd = null)
    {
        FileSystemTraverser.ProcessFolder(
            folderPath, context, vectorStoreConfig,
            processFile, writeFolderName, writeFolderEnd);
    }

    protected IEnumerable<string> EnumerateFilesRespectingExclusions(string root, VectorStoreConfig config)
    {
        if (FileSystemTraverser == null)
        {
            using var ctx = LogCtx.Set(new Props()
                .Add(nameof(root), root)
                .Add("reason", "null_traverser"));
            log.Warn("FileSystemTraverser not initialized");
            return Enumerable.Empty<string>();
        }

        return FileSystemTraverser.EnumerateFilesRespectingExclusions(root, config);
    }

    protected virtual string GetFileContent(string filePath)
        => PathHelpers.SafeReadAllText(filePath);

    protected virtual string GetEnhancedFileContent(string file)
        => PathHelpers.SafeReadAllText(file);

    protected virtual void ProcessFile(string file, System.IO.StreamWriter writer, VectorStoreConfig vectorStoreConfig)
    {
        // Default no-op - derived handlers override when using StreamWriter context
    }

    protected virtual void WriteFolderName(System.IO.StreamWriter writer, string folderName)
    {
        // Default no-op - derived handlers override when using StreamWriter context
    }
}
