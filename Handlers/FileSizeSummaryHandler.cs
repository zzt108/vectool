// File: Handlers/FileSizeSummaryHandler.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VecTool.Configuration;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Aggregates file sizes across selected folders and emits a Markdown summary table.
    /// Registers the generated report into Recent Files as TestResults.
    /// </summary>
    public sealed class FileSizeSummaryHandler : FileHandlerBase
    {
        public FileSizeSummaryHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
            : base(ui, recentFilesManager)
        {
        }

        /// <summary>
        /// Generate the file size summary report for the given folders and write it to outputPath.
        /// </summary>
        /// <param name="folderPaths">Folders to analyze.</param>
        /// <param name="outputPath">Destination .md file path.</param>
        /// <param name="config">Vector store configuration for exclusions; if null, an empty default is used.</param>
        public void GenerateFileSizeSummary(IReadOnlyList<string> folderPaths, string outputPath, VectorStoreConfig? config = null)
        {
            if (folderPaths is null || folderPaths.Count == 0)
                throw new ArgumentException("At least one folder is required.", nameof(folderPaths));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path is required.", nameof(outputPath));

            // Ensure non-null configuration for all downstream calls.
            var effectiveConfig = config ?? new VectorStoreConfig();

            try
            {
                ui?.WorkStart("Generating file size summary...", folderPaths.ToList());
                log.Info($"Starting file size summary for {folderPaths.Count} folders.");

                var fileSizesByType = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                var fileCountByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var folder in folderPaths)
                {
                    if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                    {
                        log.Warn($"Skipping non-existent or invalid folder '{folder}'.");
                        continue;
                    }

                    CalculateFolderSizes(folder, effectiveConfig, fileSizesByType, fileCountByType);
                }

                var outDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(outDir) && !Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                WriteReportToFile(outputPath, folderPaths, fileSizesByType, fileCountByType);
                ui?.UpdateStatus("File size summary generated successfully.");
                log.Info($"File size summary written to {outputPath}");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error generating file size summary: {ex.Message}");
                throw;
            }
            finally
            {
                ui?.WorkFinish();

                if (recentFilesManager != null && File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    // Register as TestResults to align with unit test expectations
                    recentFilesManager.RegisterGeneratedFile(
                        outputPath,
                        RecentFileType.TestResults,
                        folderPaths,
                        fileInfo.Length);

                    log.Debug($"Registered recent file '{outputPath}' ({fileInfo.Length:n0} bytes) as TestResults.");
                }
            }
        }

        private void CalculateFolderSizes(
            string folderPath,
            VectorStoreConfig config,
            Dictionary<string, long> fileSizesByType,
            Dictionary<string, int> fileCountByType)
        {
            // Exclude folder by name (leaf) if configured
            if (IsFolderExcluded(Path.GetFileName(folderPath), config))
                return;

            foreach (var file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);

                // Apply extension-aware pattern exclusions before other checks (e.g., ".log", "log", "*.log", "suffix.log")
                if (IsExcludedByPatterns(fileName, config.ExcludedFiles))
                    continue;

                if (IsFileExcluded(fileName, config) || !IsFileValid(file, null))
                    continue;

                string extension = Path.GetExtension(file);
                if (string.IsNullOrEmpty(extension))
                {
                    extension = "no-ext";
                }
                else
                {
                    extension = extension.ToLowerInvariant();
                }

                long size = 0L;
                try
                {
                    var fileInfo = new FileInfo(file);
                    size = fileInfo.Length;
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Skipping unreadable file '{file}': {ex.Message}");
                    continue;
                }

                if (!fileSizesByType.ContainsKey(extension))
                {
                    fileSizesByType[extension] = 0L;
                    fileCountByType[extension] = 0;
                }

                fileSizesByType[extension] += size;
                fileCountByType[extension] += 1;
            }
        }

        private void WriteReportToFile(
            string outputPath,
            IReadOnlyList<string> folderPaths,
            Dictionary<string, long> fileSizesByType,
            Dictionary<string, int> fileCountByType)
        {
            using var writer = new StreamWriter(outputPath);

            writer.WriteLine("# File Size Summary");
            writer.WriteLine();
            writer.WriteLine($"Generated on {DateTime.Now:yyyy-MM-dd HHmmss}");
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
            writer.WriteLine("|-----------|------:|-----------:|-------------:|");

            long totalSize = 0;
            int totalCount = 0;

            foreach (var kvp in fileSizesByType.OrderByDescending(kv => kv.Value))
            {
                var extension = kvp.Key;
                var size = kvp.Value;
                var count = fileCountByType.TryGetValue(extension, out var c) ? c : 0;

                long avgSize = count == 0 ? 0 : size / count;

                writer.WriteLine($"| {extension} | {count:N0} | {FormatFileSize(size)} | {FormatFileSize(avgSize)} |");

                totalSize += size;
                totalCount += count;
            }

            writer.WriteLine($"| Total | {totalCount:N0} | {FormatFileSize(totalSize)} | {FormatFileSize(totalCount == 0 ? 0 : totalSize / totalCount)} |");
        }

        private static bool IsExcludedByPatterns(string fileName, IReadOnlyCollection<string>? patterns)
        {
            if (patterns == null || patterns.Count == 0)
                return false;

            var ext = Path.GetExtension(fileName) ?? string.Empty;

            foreach (var p in patterns)
            {
                if (string.IsNullOrWhiteSpace(p))
                    continue;

                var pat = p.Trim();

                // Accept ".log", "log", "*.log", "suffix.log" styles
                if (pat.StartsWith("*.", StringComparison.Ordinal))
                {
                    // "*.log" -> compare extension without "*"
                    if (ext.Equals(pat.Substring(1), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else if (pat.StartsWith(".", StringComparison.Ordinal))
                {
                    // ".log" -> compare extension with dot
                    if (ext.Equals(pat, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else if (pat.StartsWith("*", StringComparison.Ordinal))
                {
                    // "*suffix.log" -> compare filename suffix
                    if (fileName.EndsWith(pat.Substring(1), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else
                {
                    // exact file name match
                    if (fileName.Equals(pat, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Formats bytes into human-readable units without rounding sub-1024 values up to KB.
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            // Use 1024 boundary to ensure, e.g., 600 stays "600.00 B"
            while (number >= 1024 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n2} {suffixes[counter]}";
        }
    }
}
