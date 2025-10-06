// ✅ FULL FILE VERSION
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

                string extension
