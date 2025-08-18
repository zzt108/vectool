using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NLogS = NLogShared;

namespace DocXHandler
{
    public class FileSizeSummaryHandler : FileHandlerBase
    {
        private readonly NLogS.CtxLogger _log = new();

        public FileSizeSummaryHandler(IUserInterface? ui) : base(ui)
        {
        }

        public void GenerateFileSizeSummary(List<string> folderPaths, string outputPath, VectorStoreConfig config)
        {
            try
            {
                _ui?.WorkStart("Generating file size report", folderPaths);
                int progress = 0;

                var fileSizesByType = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                var fileCountByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var folderPath in folderPaths)
                {
                    _ui?.UpdateProgress(progress++);
                    _ui?.UpdateStatus($"Analyzing folder: {folderPath}");

                    CalculateFolderSizes(folderPath, config, fileSizesByType, fileCountByType);
                }

                WriteReportToFile(outputPath, folderPaths, fileSizesByType, fileCountByType);

                _ui?.UpdateStatus("File size summary generated successfully.");
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error generating file size summary: {ex.Message}");
                throw;
            }
            finally
            {
                _ui?.WorkFinish();
            }
        }

        private void CalculateFolderSizes(string folderPath, VectorStoreConfig config,
            Dictionary<string, long> fileSizesByType, Dictionary<string, int> fileCountByType)
        {
            try
            {
                foreach (var file in GetProcessableFiles(folderPath, config))
                {

                    string extension = Path.GetExtension(file).ToLowerInvariant();
                    if (string.IsNullOrEmpty(extension))
                    {
                        extension = "(no extension)";
                    }

                    var fileInfo = new FileInfo(file);
                    if (!fileSizesByType.ContainsKey(extension))
                    {
                        fileSizesByType[extension] = 0;
                        fileCountByType[extension] = 0;
                    }

                    fileSizesByType[extension] += fileInfo.Length;
                    fileCountByType[extension]++;
                }

                // All subfolders are processed recursively already
                //foreach (var subDir in Directory.GetDirectories(folderPath))
                //{
                //    CalculateFolderSizes(subDir, config, fileSizesByType, fileCountByType);
                //}
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error processing folder {folderPath}: {ex.Message}");
            }
        }

        private void WriteReportToFile(string outputPath, List<string> folderPaths,
            Dictionary<string, long> fileSizesByType, Dictionary<string, int> fileCountByType)
        {
            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("# File Size Summary");
                writer.WriteLine();
                writer.WriteLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine();

                // Write analyzed folders
                writer.WriteLine("## Analyzed Folders");
                writer.WriteLine();
                foreach (var folder in folderPaths)
                {
                    writer.WriteLine($"- {folder}");
                }
                writer.WriteLine();

                // Write summary table
                writer.WriteLine("## Size by File Type");
                writer.WriteLine();
                writer.WriteLine("| File Type | Files | Total Size | Average Size |");
                writer.WriteLine("|-----------|-------|------------|--------------|");

                long totalSize = 0;
                int totalCount = 0;

                // Sort by total size (largest first)
                foreach (var kvp in fileSizesByType.OrderByDescending(kv => kv.Value))
                {
                    string extension = kvp.Key;
                    long size = kvp.Value;
                    int count = fileCountByType[extension];

                    writer.WriteLine($"| {extension} | {count:N0} | {FormatFileSize(size)} | {FormatFileSize(count > 0 ? size / count : 0)} |");

                    totalSize += size;
                    totalCount += count;
                }

                // Add total row
                writer.WriteLine($"| **Total** | **{totalCount:N0}** | **{FormatFileSize(totalSize)}** | **{FormatFileSize(totalCount > 0 ? totalSize / totalCount : 0)}** |");
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }

            return string.Format("{0:n2} {1}", Math.Round(number, 2), suffixes[counter]);
        }
    }
}
