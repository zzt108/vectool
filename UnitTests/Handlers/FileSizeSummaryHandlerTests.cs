using LogCtxShared;
using NLogShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VecTool.Configuration;
using VecTool.Core;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Generates a file size summary report for exported files.
    /// Uses FileSystemTraverser to ensure exclusions are respected.
    /// Handler is completely exclusion-unaware—traverser handles all filtering.
    /// </summary>
    public class FileSizeSummaryHandler : FileHandlerBase
    {
        private static readonly CtxLogger log = new();

        /// <summary>
        /// Lazy-initialized traverser for exclusion-aware file enumeration.
        /// </summary>
        private readonly IFileSystemTraverser _traverser;

        private readonly string _rootPath;

        public FileSizeSummaryHandler(
            IUserInterface? ui,
            IRecentFilesManager? recentFilesManager,
            IFileSystemTraverser? traverser = null,
            string? rootPath = null)
            : base(ui, recentFilesManager)
        {
            // ✅ NEW: Accept optional traverser for testing
            _traverser = traverser ?? new FileSystemTraverser(ui, rootPath);
            _rootPath = rootPath ?? Environment.CurrentDirectory;
        }

        /// <summary>
        /// Generates a file size summary report for the selected folders.
        /// Uses FileSystemTraverser for exclusion-aware enumeration.
        /// </summary>
        public void GenerateFileSizeSummary(
            List<string> folderPaths,
            string outputPath,
            VectorStoreConfig config)
        {
            try
            {
                Ui?.WorkStart("Generating file size report...", folderPaths);

                var fileSizesByType = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                var fileCountByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int progress = 0;

                foreach (var folderPath in folderPaths)
                {
                    Ui?.UpdateProgress(progress);
                    Ui?.UpdateStatus($"Analyzing folder: {folderPath}");

                    // ✅ REFACTORED: Use traverser instead of direct enumeration
                    CalculateFolderSizes(folderPath, config, fileSizesByType, fileCountByType);
                    progress++;
                }

                WriteReportToFile(outputPath, folderPaths, fileSizesByType, fileCountByType);

                Ui?.UpdateStatus("File size summary generated successfully.");

                // ✅ Register with recent files
                if (RecentFilesManager != null && File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    RecentFilesManager.RegisterGeneratedFile(
                        outputPath,
                        RecentFileType.Summary_Md,
                        folderPaths,
                        fileInfo.Length
                    );
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error generating file size summary: {ex.Message}");
                throw;
            }
            finally
            {
                Ui?.WorkFinish();
            }
        }

        /// <summary>
        /// Calculates file sizes and counts for a folder.
        /// Uses FileSystemTraverser to get only non-excluded files.
        /// ✅ NEW: Handler does NOT check IsFileExcluded—traverser handles all decisions.
        /// </summary>
        private void CalculateFolderSizes(
            string folderPath,
            VectorStoreConfig config,
            Dictionary<string, long> fileSizesByType,
            Dictionary<string, int> fileCountByType)
        {
            if (!Directory.Exists(folderPath))
            {
                log.Warn($"Folder does not exist: {folderPath}");
                return;
            }

            try
            {
                // ✅ CRITICAL: Use traverser for enumeration
                // Traverser already applies all exclusion rules (patterns, legacy config, etc.)
                var files = _traverser
                    .EnumerateFilesRespectingExclusions(folderPath, config)
                    .ToList();

                using (var ctx = log.Ctx.Set(
                    new Props()
                        .Add("folderPath", folderPath)
                        .Add("fileCount", files.Count)))
                {
                    log.Info($"Found {files.Count} files to include in summary");

                    foreach (var file in files)
                    {
                        try
                        {
                            // ✅ Handler just processes what traverser gave us
                            var extension = Path.GetExtension(file).ToLowerInvariant();
                            if (string.IsNullOrEmpty(extension))
                            {
                                extension = "[no extension]";
                            }

                            var fileInfo = new FileInfo(file);

                            if (!fileSizesByType.ContainsKey(extension))
                            {
                                fileSizesByType[extension] = 0;
                                fileCountByType[extension] = 0;
                            }

                            fileSizesByType[extension] += fileInfo.Length;
                            fileCountByType[extension]++;
                            using var _ = log.Ctx.Set()
                                    .Add("file", file)
                                    .Add("size", fileInfo.Length)
                                    .Add("extension", extension);
                            log.Debug(
                                $"Added to summary: {Path.GetFileName(file)} ({fileInfo.Length} bytes)");
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex, $"Error processing file {file}: {ex.Message}");
                            // ✅ Continue processing other files
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error processing folder {folderPath}: {ex.Message}");
                // ✅ Continue with other folders
            }
        }

        /// <summary>
        /// Writes the size summary report to the output file.
        /// </summary>
        private void WriteReportToFile(
            string outputPath,
            List<string> folderPaths,
            Dictionary<string, long> fileSizesByType,
            Dictionary<string, int> fileCountByType)
        {
            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("# File Size Summary - Exported Files");
                writer.WriteLine();
                writer.WriteLine($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine();

                writer.WriteLine("## Analyzed Folders");
                foreach (var folder in folderPaths)
                {
                    writer.WriteLine($"- {folder}");
                }
                writer.WriteLine();

                writer.WriteLine("## Size by File Type");
                writer.WriteLine();
                writer.WriteLine("| File Type | Files | Total Size | Average Size |");
                writer.WriteLine("|-----------|-------|------------|--------------|");

                long totalSize = 0;
                int totalCount = 0;

                foreach (var kvp in fileSizesByType.OrderByDescending(kv => kv.Value))
                {
                    string extension = kvp.Key;
                    long size = kvp.Value;
                    int count = fileCountByType[extension];
                    long avgSize = count > 0 ? size / count : 0;

                    writer.WriteLine(
                        $"| {extension} | {count:N0} | {FormatFileSize(size)} | {FormatFileSize(avgSize)} |"
                    );

                    totalSize += size;
                    totalCount += count;
                }

                writer.WriteLine("|-----------|-------|------------|--------------|");
                writer.WriteLine(
                    $"| **Total** | **{totalCount:N0}** | **{FormatFileSize(totalSize)}** | **{FormatFileSize(totalCount > 0 ? totalSize / totalCount : 0)}** |"
                );
            }
        }

        /// <summary>
        /// Formats bytes as human-readable size (B, KB, MB, GB, TB).
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

            return $"{number:N2} {suffixes[counter]}";
        }
    }
}