using NLogShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VecTool.Configuration;
using VecTool.Handlers.Analysis;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    public class FileSizeSummaryHandler : FileHandlerBase
    {
        private static readonly CtxLogger log = new();

        public FileSizeSummaryHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
            : base(ui, recentFilesManager)
        {
        }

        public void GenerateFileSizeSummary(List<string> folderPaths, string outputPath, VectorStoreConfig config)
        {
            try
            {
                ui?.WorkStart("Generating file size report...", folderPaths);

                var fileSizesByType = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                var fileCountByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                int progress = 0;
                foreach (var folderPath in folderPaths)
                {
                    ui?.UpdateProgress(progress++);
                    ui?.UpdateStatus($"Analyzing folder {folderPath}");

                    CalculateFolderSizes(folderPath, config, fileSizesByType, fileCountByType);
                }

                WriteReportToFile(outputPath, folderPaths, fileSizesByType, fileCountByType);
                ui?.UpdateStatus("File size summary generated successfully.");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error generating file size summary: {ex.Message}");
                throw;
            }
            finally
            {
                ui?.WorkFinish();

                if (_recentFilesManager != null && File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    _recentFilesManager.RegisterGeneratedFile(
                        outputPath,
                        RecentFileType.AllSourceMd,
                        folderPaths,
                        fileInfo.Length);
                }
            }
        }

        private void CalculateFolderSizes(
            string folderPath,
            VectorStoreConfig config,
            Dictionary<string, long> fileSizesByType,
            Dictionary<string, int> fileCountByType)
        {
            if (IsFolderExcluded(Path.GetFileName(folderPath), config))
                return;

            try
            {
                foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    // ✅ Use centralized filter (ensures summary matches actual export)
                    if (!Traversal.FileValidator.ShouldIncludeInExport(file, config))
                        continue;

                    string extension = Path.GetExtension(file).ToLowerInvariant();
                    if (string.IsNullOrEmpty(extension))
                        extension = "no extension";

                    var fileInfo = new FileInfo(file);

                    if (!fileSizesByType.ContainsKey(extension))
                    {
                        fileSizesByType[extension] = 0;
                        fileCountByType[extension] = 0;
                    }

                    fileSizesByType[extension] += fileInfo.Length;
                    fileCountByType[extension]++;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error processing folder {folderPath}: {ex.Message}");
            }
        }

        private void WriteReportToFile(
            string outputPath,
            List<string> folderPaths,
            Dictionary<string, long> fileSizesByType,
            Dictionary<string, int> fileCountByType)
        {
            using var writer = new StreamWriter(outputPath);

            // ✅ Updated header to clarify this is exported files only
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
            writer.WriteLine("|-----------|-------|------------|--------------|");

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
        }

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
