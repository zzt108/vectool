using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using VecTool.Configuration;
using VecTool.Handlers.Analysis;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;
using VecTool.Utils;

namespace VecTool.Handlers
{
    /// <summary>
    /// Base class for all file format handlers (DOCX, MD, PDF, Git).
    /// Delegates complex operations to specialized helpers following SRP.
    /// </summary>
    public abstract class FileHandlerBase
    {
        // Unified logging per project standards (no wrappers).
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        protected readonly IUserInterface? ui;
        protected readonly IRecentFilesManager? recentFilesManager;

        // Delegated helpers
        protected readonly AiContextGenerator aiContextGenerator;
        protected readonly FileSystemTraverser fileSystemTraverser;

        protected FileHandlerBase(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        {
            this.ui = ui;
            this.recentFilesManager = recentFilesManager;

            aiContextGenerator = new AiContextGenerator();
            fileSystemTraverser = new FileSystemTraverser(ui);
        }

        #region AI Context (delegated to AiContextGenerator)

        protected string GenerateTableOfContents(List<string> folderPaths)
            => aiContextGenerator.GenerateTableOfContents(folderPaths);

        protected string GenerateCrossReferences(List<string> folderPaths)
            => aiContextGenerator.GenerateCrossReferences(folderPaths);

        protected string GenerateCodeMetaInfo(List<string> folderPaths)
            => aiContextGenerator.GenerateCodeMetaInfo(folderPaths);

        protected void AddAIOptimizedContext<T>(
            List<string> folderPaths,
            T context,
            Action<T, string> writeContent)
        {
            if (folderPaths == null || writeContent == null)
                return;

            var toc = GenerateTableOfContents(folderPaths);
            if (!string.IsNullOrWhiteSpace(toc))
                writeContent(context, toc);

            var xref = GenerateCrossReferences(folderPaths);
            if (!string.IsNullOrWhiteSpace(xref))
                writeContent(context, xref);

            var meta = GenerateCodeMetaInfo(folderPaths);
            if (!string.IsNullOrWhiteSpace(meta))
                writeContent(context, meta);
        }

        #endregion

        #region Traversal (delegated to FileSystemTraverser)

        protected void ProcessFolder<T>(
            string folderPath,
            T context,
            VectorStoreConfig vectorStoreConfig,
            Action<string, T, VectorStoreConfig> processFile,
            Action<T, string> writeFolderName,
            Action<T>? writeFolderEnd = null)
        {
            fileSystemTraverser.ProcessFolder(
                folderPath,
                context,
                vectorStoreConfig,
                processFile,
                writeFolderName,
                writeFolderEnd);
        }

        protected IEnumerable<string> EnumerateFilesRespectingExclusions(string root, VectorStoreConfig config)
            => fileSystemTraverser.EnumerateFilesRespectingExclusions(root, config);

        #endregion

        #region Validation (virtual for derived overrides)

        protected virtual bool IsFolderExcluded(string folderName, VectorStoreConfig config)
            => FileValidator.IsFolderExcluded(folderName, config);

        protected virtual bool IsFileExcluded(string fileName, VectorStoreConfig config)
            => FileValidator.IsFileExcluded(fileName, config);

        protected virtual bool IsFileValid(string path, string? outputPath)
            => FileValidator.IsFileValid(path, outputPath);

        #endregion

        #region Content helpers (virtual for derived customization)

        protected virtual string GetFileContent(string filePath)
            => PathHelpers.SafeReadAllText(filePath);

        protected virtual string GetEnhancedFileContent(string file)
            => PathHelpers.SafeReadAllText(file);

        #endregion

        #region Legacy compatibility overloads (StreamWriter context)

        // Default no-op - derived handlers override when using StreamWriter context
        protected virtual void ProcessFile(string file, StreamWriter writer, VectorStoreConfig vectorStoreConfig)
        {
        }

        // Default no-op - derived handlers override when using StreamWriter context
        protected virtual void WriteFolderName(StreamWriter writer, string folderName)
        {
        }

        #endregion
    }
}
