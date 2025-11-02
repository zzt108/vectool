using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using VecTool.Configuration;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;
using VecTool.Utils;

namespace VecTool.Handlers
{
    /// <summary>
    /// Markdown export handler for codebase documentation generation.
    /// Added parameter validation guards for null/empty folder lists (QF-003)
    /// </summary>
    public class MDHandler : FileHandlerBase
    {
        public MDHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager):base(ui, recentFilesManager)
        {
            
        }

        public MDHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager, IFileSystemTraverser traverser) : base(ui, recentFilesManager, traverser)
        {
        }

        /// <summary>
        /// Async wrapper for ExportSelectedFolders to enable better parallelism.
        /// </summary>
        public Task ExportSelectedFoldersAsync(List<string> folderPaths, string outputPath, VectorStoreConfig vectorStoreConfig)
        {
            return Task.Run(() => ExportSelectedFolders(folderPaths, outputPath, vectorStoreConfig));
        }

        /// <summary>
        /// Export selected folders to a single markdown file with exclusions respected.
        /// Added validation guards to throw ArgumentException for invalid inputs
        /// </summary>
        /// <param name="folderPaths">List of root folder paths to export (cannot be null or empty)</param>
        /// <param name="outputPath">Destination file path for markdown output (cannot be null/empty)</param>
        /// <param name="vectorStoreConfig">Configuration for file exclusion rules</param>
        /// <exception cref="ArgumentException">Thrown if folderPaths is null, empty, or outputPath is invalid</exception>
        /// <exception cref="IOException">Thrown if output file cannot be created or written</exception>
        public void ExportSelectedFolders(List<string> folderPaths, string outputPath, VectorStoreConfig vectorStoreConfig)
        {
            // Null check guard - prevents NullReferenceException
            if (folderPaths == null)
                throw new ArgumentException("Folder list cannot be null", nameof(folderPaths));

            // Empty check guard - validates business requirement
            if (folderPaths.Count == 0)
                throw new ArgumentException("Folder list cannot be empty", nameof(folderPaths));

            // Output path validation - defensive programming
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

            try
            {
                Ui?.WorkStart("Exporting to MD", folderPaths);

                using StreamWriter writer = new StreamWriter(outputPath);
                writer.WriteLine($"# Codebase for folder(s):");
                foreach (string folderPath in folderPaths)
                    writer.WriteLine($"- {folderPath}");
                writer.WriteLine();

                // ✅ Use FileSystemTraverser for exclusion-aware enumeration (ARCH-002 compliance)
                foreach (string folderPath in folderPaths)
                {
                    if (string.IsNullOrWhiteSpace(folderPath))
                    {
                        log.Warn($"Skipping null or empty folder path");
                        continue;
                    }

                    Ui?.UpdateStatus($"Enumerating files in {folderPath}");

                    var files = EnumerateFilesRespectingExclusions(folderPath, vectorStoreConfig).ToList();
                    log.Info($"Found {files.Count} files to export in {folderPath}");

                    // Group files by folder for structured output
                    var filesByFolder = files
                        .GroupBy(f => Path.GetDirectoryName(f) ?? string.Empty)
                        .OrderBy(g => g.Key);

                    foreach (var folderGroup in filesByFolder)
                    {
                        WriteFolderName(writer, new DirectoryInfo(folderGroup.Key).Name);

                        foreach (var file in folderGroup.OrderBy(f => Path.GetFileName(f)))
                        {
                            ProcessFile(file, writer, vectorStoreConfig);
                        }
                    }
                }

                // Register successful export in recent files
                if (RecentFilesManager != null && File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    RecentFilesManager.RegisterGeneratedFile(
                        outputPath,
                        RecentFileType.Codebase_Md,
                        folderPaths,
                        fileInfo.Length
                    );
                }
            }
            finally
            {
                Ui?.WorkFinish();
            }
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
