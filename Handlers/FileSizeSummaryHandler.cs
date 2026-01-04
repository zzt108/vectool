using LogCtxShared;
using Microsoft.Extensions.Logging;
using System.Text;
using VecTool.Configuration;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Generates file size summaries for selected folders using traverser for exclusive file enumeration.
    /// ✅ Uses FileSystemTraverser.EnumerateFilesRespectingExclusions() for all file access
    /// </summary>
    public class FileSizeSummaryHandler : FileHandlerBase
    {
        //private static readonly ILogger logger;

        // Injected traverser for exclusive authority
        private readonly IFileSystemTraverser _fileSystemTraverser;

        /// <summary>
        // Constructor with dependency injection for traverser
        /// </summary>
        /// <param name="ui">Optional UI interface for progress updates</param>
        /// <param name="recentFilesManager">Optional recent files manager</param>
        /// <param name="fileSystemTraverser">Traverser for file enumeration (required for exclusive authority)</param>
        public FileSizeSummaryHandler(ILogger logger,
            IUserInterface? ui,
            IRecentFilesManager? recentFilesManager,
            IFileSystemTraverser? fileSystemTraverser = null)
            : base(logger, ui, recentFilesManager)
        {
            // ✅ DI pattern: accept injection or create default
            _fileSystemTraverser = fileSystemTraverser ?? new FileSystemTraverser(ui);
        }

        /// <summary>
        /// Generates a file size summary report for the specified folders.
        /// Reports only on files that would be exported (respects exclusions).
        /// </summary>
        public void GenerateFileSizeSummary(
            List<string> folderPaths,
            string outputPath,
            VectorStoreConfig config)
        {
            // ✅ Defensive: Validate inputs
            if (folderPaths == null || folderPaths.Count == 0)
                throw new ArgumentException("Folder list cannot be null or empty", nameof(folderPaths));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

            try
            {
                Ui?.WorkStart("Generating file size report...", folderPaths);

                var fileSizesByType = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                var fileCountByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int progress = 0;

                foreach (var folderPath in folderPaths)
                {
                    Ui?.UpdateProgress(progress++);
                    Ui?.UpdateStatus($"Analyzing folder {folderPath}");

                    // ✅ Delegates to method that uses traverser
                    CalculateFolderSizes(folderPath, config, fileSizesByType, fileCountByType);
                }

                WriteReportToFile(outputPath, folderPaths, fileSizesByType, fileCountByType);
                Ui?.UpdateStatus("File size summary generated successfully.");
            }
            catch (Exception ex)
            {
                using (var ctx = logger.SetContext()
                    .Add("outputPath", outputPath)
                    .Add("folderCount", folderPaths.Count))
                {
                    logger.LogError(ex, "LogError generating file size summary");
                }
                throw;
            }
            finally
            {
                Ui?.WorkFinish();

                // ✅ Register output with recent files manager if available
                if (RecentFilesManager != null && File.Exists(outputPath))
                {
                    try
                    {
                        var fileInfo = new FileInfo(outputPath);
                        RecentFilesManager.RegisterGeneratedFile(
                            outputPath,
                            RecentFileType.Summary_Md,
                            folderPaths,
                            fileInfo.Length);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to register generated file in recent files");
                        // Don't throw—report generation succeeded
                    }
                }
            }
        }

        /// <summary>
        /// ✅ REFACTORED: Uses FileSystemTraverser exclusively for file enumeration.
        /// Traverser already applies:
        ///   - Pattern matching (.gitignore / .vtignore)
        ///   - Legacy config exclusions (ExcludedFolders, ExcludedFiles)
        ///   - FileValidator filters
        /// Handler just processes what traverser gives us (exclusive authority pattern).
        /// </summary>
        private void CalculateFolderSizes(
            string folderPath,
            VectorStoreConfig config,
            Dictionary<string, long> fileSizesByType,
            Dictionary<string, int> fileCountByType)
        {
            // ✅ Validate folder
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                using (var ctx = logger.SetContext()
                    .Add("folderPath", folderPath)
                    .Add("exists", Directory.Exists(folderPath)))
                {
                    logger.LogDebug($"Skipping invalid folder path");
                }
                return;
            }

            try
            {
                // ✅ CRITICAL FIX: Use traverser for ALL file enumeration
                // This replaces Directory.GetFiles() which was bypassing mock setup in tests
                var files = _fileSystemTraverser
                    .EnumerateFilesRespectingExclusions(folderPath, config)
                    .ToList();

                using (var ctx = logger.SetContext()
                    .Add("folderPath", folderPath)
                    .Add("fileCount", files.Count)
                    .Add("source", "traverser"))
                {
                    logger.LogInformation($"Enumerating {files.Count} files for size summary");
                }

                // ✅ Process all files provided by traverser (already filtered)
                foreach (var file in files)
                {
                    try
                    {
                        string extension = Path.GetExtension(file).ToLowerInvariant();
                        if (string.IsNullOrEmpty(extension))
                            extension = "(no extension)";

                        var fileInfo = new FileInfo(file);

                        if (!fileSizesByType.ContainsKey(extension))
                        {
                            fileSizesByType[extension] = 0;
                            fileCountByType[extension] = 0;
                        }

                        fileSizesByType[extension] += fileInfo.Length;
                        fileCountByType[extension]++;

                        using (var ctx = logger.SetContext()
                            .Add("file", Path.GetFileName(file))
                            .Add("size", fileInfo.Length)
                            .Add("extension", extension))
                        {
                            logger.LogDebug($"Added to summary: {Path.GetFileName(file)} ({fileInfo.Length:N0} bytes)");
                        }
                    }
                    catch (Exception ex)
                    {
                        using (var ctx = logger.SetContext()
                            .Add("file", file))
                        {
                            logger.LogError(ex, "LogError processing file for summary");
                        }
                        // Continue processing other files
                    }
                }
            }
            catch (Exception ex)
            {
                using (var ctx = logger.SetContext()
                    .Add("folderPath", folderPath))
                {
                    logger.LogError(ex, "LogError calculating folder sizes");
                }
                // Continue with other folders
            }
        }

        /// <summary>
        /// Writes the file size summary as a Markdown report.
        /// </summary>
        private void WriteReportToFile(
            string outputPath,
            List<string> folderPaths,
            Dictionary<string, long> fileSizesByType,
            Dictionary<string, int> fileCountByType)
        {
            try
            {
                using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);

                // ✅ Clear header indicating this is exported files only
                writer.WriteLine("## File Size Summary - Exported Files");
                writer.WriteLine();
                writer.WriteLine($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine();

                writer.WriteLine("### Analyzed Folders");
                foreach (var folder in folderPaths)
                {
                    writer.WriteLine($"- {folder}");
                }
                writer.WriteLine();

                writer.WriteLine("### Size by File Type");
                writer.WriteLine();
                writer.WriteLine("| File Type | Files | Total Size | Average Size |");
                writer.WriteLine("|-----------|-------|------------|-----------------|");

                long totalSize = 0;
                int totalCount = 0;

                foreach (var kvp in fileSizesByType.OrderByDescending(kv => kv.Value))
                {
                    string extension = kvp.Key;
                    long size = kvp.Value;
                    int count = fileCountByType[extension];
                    long avgSize = count > 0 ? size / count : 0;

                    writer.WriteLine($"| {extension} | {count:N0} | {FormatFileSize(size)} | {FormatFileSize(avgSize)} |");

                    totalSize += size;
                    totalCount += count;
                }

                writer.WriteLine($"| **Total** | **{totalCount:N0}** | **{FormatFileSize(totalSize)}** | **{FormatFileSize(totalCount > 0 ? totalSize / totalCount : 0)}** |");
                writer.WriteLine();
                writer.WriteLine($"*Summary includes {totalCount:N0} files across {fileSizesByType.Count} file types.*");

                using (var ctx = logger.SetContext()
                    .Add("outputPath", outputPath)
                    .Add("fileCount", totalCount)
                    .Add("totalSize", totalSize))
                {
                    logger.LogInformation($"File size summary written: {outputPath}");
                }
            }
            catch (Exception ex)
            {
                using (var ctx = logger.SetContext()
                    .Add("outputPath", outputPath))
                {
                    logger.LogError(ex, "LogError writing file size summary report");
                }
                throw;
            }
        }

        /// <summary>
        /// Formats byte size into human-readable format (B, KB, MB, GB, TB).
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n2} {suffixes[counter]}";
        }
    }
}