namespace VecTool.Handlers;

using global::VecTool.Configuration;
using global::VecTool.Handlers.Analysis;
using global::VecTool.Handlers.Traversal;
using global::VecTool.RecentFiles;
using Microsoft.Extensions.Logging;
using LogCtxShared;
using System;
using VecTool.Constants;

/// <summary>
/// Base class for all file format handlers (DOCX, MD, PDF, Git).
/// Delegates complex operations to specialized helpers following SRP.
/// </summary>
public abstract class FileHandlerBase
{
    protected readonly ILogger logger;
    protected readonly IUserInterface? Ui; // renamed from _ui to match AI faulti code generation logic
    protected readonly IRecentFilesManager? RecentFilesManager; // renamed from RecentFilesManager to match AI faulti code generation logic
    protected readonly AiContextGenerator AiContextGenerator;
    protected readonly IFileSystemTraverser FileSystemTraverser;

    protected FileHandlerBase(
        ILogger logger,
        IUserInterface? ui,
        IRecentFilesManager? recentFilesManager,
        IFileSystemTraverser? traverser = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    protected string GenerateTableOfContentsList(VectorStoreConfig config)
        => AiContextGenerator.GenerateTableOfContents(config);

    protected string GenerateCrossReferencesList(VectorStoreConfig config)
        => AiContextGenerator.GenerateCrossReferences(config);

    protected string GenerateCodeMetaInfoList(VectorStoreConfig config)
        => AiContextGenerator.GenerateCodeMetaInfo(config);

    protected void AddAIOptimizedContext<T>(
        VectorStoreConfig config,
        T context,
        Action<T, string> writeContent)
    {
        //var toc = GenerateTableOfContentsList(config);
        //if (!string.IsNullOrWhiteSpace(toc))
        //    writeContent(context, toc);

        //var xref = GenerateCrossReferencesList(config);
        //if (!string.IsNullOrWhiteSpace(xref))
        //    writeContent(context, xref);

        //var meta = GenerateCodeMetaInfoList(config);
        //if (!string.IsNullOrWhiteSpace(meta))
        //    writeContent(context, meta);

        writeContent(context, TestStrings.AiExportHeader);
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
        if (FileSystemTraverser is null)
        {
            using var _ = logger.SetContext(new Props()
                .Add(nameof(root), root)
                .Add("reason", "nulltraverser"));

            logger.LogWarning("FileSystemTraverser not initialized");
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