using Microsoft.Extensions.Logging;

using VecTool.Configuration;
using VecTool.Constants;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;
using LogCtxShared;

namespace VecTool.Handlers
{
    /// <summary>
    /// Markdown export handler for codebase documentation generation.
    /// Added parameter validation guards for null/empty folder lists (QF-003)
    /// </summary>
    public class MDHandler : FileHandlerBase
    {
        public MDHandler(ILogger logger, IUserInterface? ui, IRecentFilesManager? recentFilesManager) : base(logger, ui, recentFilesManager)
        {
        }

        public MDHandler(ILogger logger, IUserInterface? ui, IRecentFilesManager? recentFilesManager, IFileSystemTraverser traverser) : base(logger, ui, recentFilesManager, traverser)
        {
        }

        /// <summary>
        /// Async wrapper for ExportSelectedFolders to enable better parallelism.
        /// </summary>
        public Task ExportSelectedFoldersAsync(string outputPath, VectorStoreConfig vectorStoreConfig)
        {
            return Task.Run(() => ExportSelectedFolders(outputPath, vectorStoreConfig));
        }

        /// <summary>
        /// Export selected folders to a single markdown file with XML metadata wrapper and exclusions respected.
        /// Added validation guards to throw ArgumentException for invalid inputs.
        /// </summary>
        /// <param name="outputPath">Destination file path for markdown output (cannot be null/empty)</param>
        /// <param name="vectorStoreConfig">Configuration for file exclusion rules</param>
        /// <exception cref="ArgumentException">Thrown if outputPath is invalid or vectorStoreConfig is null</exception>
        /// <exception cref="IOException">Thrown if output file cannot be created or written</exception>
        public void ExportSelectedFolders(string outputPath, VectorStoreConfig vectorStoreConfig)
        {
            var folderPaths = vectorStoreConfig.FolderPaths;

            // Validation guards
            if (folderPaths == null || folderPaths.Count == 0)
                throw new ArgumentException("Folder list cannot be null or empty", nameof(folderPaths));
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

            using (Props p = ((ILogger)logger).SetContext()
                .Add("Operation", "MDHandler.ExportSelectedFolders")
                .Add("OutputPath", outputPath)
                .Add("FolderCount", folderPaths.Count))
            {
                try
                {
                    Ui?.WorkStart("Exporting to MD", folderPaths);

                    // 1. Collect all files and metadata first to calculate document totals
                    var collectedFiles = new List<(Core.Metadata.FileMetadata metadata, string content)>();
                    var metadataCollector = new Core.Metadata.MetadataCollector(logger);

                    foreach (string folderPath in folderPaths)
                    {
                        if (string.IsNullOrWhiteSpace(folderPath))
                        {
                            logger.LogWarning("Skipping null or empty folder path");
                            continue;
                        }

                        // Use existing exclusion-aware enumeration from FileHandlerBase
                        var files = EnumerateFilesRespectingExclusions(folderPath, vectorStoreConfig);

                        foreach (var file in files)
                        {
                            var content = GetFileContent(file);
                            var meta = metadataCollector.CollectFileMetadata(file, content);
                            collectedFiles.Add((meta, content));
                        }
                    }

                    // 2. Prepare writer
                    using var streamWriter = new StreamWriter(outputPath);
                    var metadataWriter = new Handlers.Export.XmlMarkdownWriter(streamWriter);

                    // 3. Write Document Metadata (Header)
                    var exportMetadata = new Core.Metadata.ExportMetadata
                    {
                        TotalFiles = collectedFiles.Count,
                        TotalLoc = collectedFiles.Sum(x => x.metadata.LinesOfCode),
                        Version = VersionInfo.DisplayVersion,
                        ExportDate = DateTime.Now
                    };

                    metadataWriter.WriteDocumentMetadata(exportMetadata);

                    // 4. Write each file with XML wrapper
                    foreach (var (metadata, content) in collectedFiles)
                    {
                        metadataWriter.WriteFileMetadata(metadata, content);
                    }

                    // 5. Close XML structure
                    metadataWriter.WriteDocumentFooter();

                    // 6. Register successful export
                    if (RecentFilesManager != null && File.Exists(outputPath))
                    {
                        var fileInfo = new FileInfo(outputPath);
                        RecentFilesManager.RegisterGeneratedFile(
                            outputPath,
                            RecentFileType.Codebase_Md,
                            folderPaths,
                            fileInfo.Length);
                    }

                    logger.LogInformation("Markdown export completed. {Count} files, {Loc} LOC.", exportMetadata.TotalFiles, exportMetadata.TotalLoc);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to export markdown to {Path}", outputPath);
                    throw;
                }
                finally
                {
                    Ui?.WorkFinish();
                }
            }
        }

        /// <summary>
        /// Counts total lines in text content.
        /// </summary>
        private static int CountLines(string content)
        {
            if (string.IsNullOrEmpty(content)) return 0;

            var lines = 1;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n') lines++;
            }
            return lines;
        }

        /// <summary>
        /// Process individual file and write to markdown stream.
        /// </summary>
        protected override void ProcessFile(string file, StreamWriter writer, VectorStoreConfig vectorStoreConfig)
        {
            string content = GetFileContent(file);
            DateTime lastModified = File.GetLastWriteTime(file);

            writer.WriteLine($"### File: {Path.GetFileName(file)} (Time:{lastModified})");
            writer.WriteLine($"```");
            writer.WriteLine(content);
            writer.WriteLine("```");
            writer.WriteLine();
        }

        /// <summary>
        /// Write folder header to markdown stream.
        /// </summary>
        protected override void WriteFolderName(StreamWriter writer, string folderName)
        {
            writer.WriteLine($"## Folder: {folderName}");
        }
    }
}