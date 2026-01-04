namespace VecTool.Handlers.Traversal
{
    using System;

    /// <summary>
    /// Static helper methods for creating  Properties related to file exclusion audit trail.
    /// Enables consistent structured logging across Layer 1 (patterns) and Layer 2 (markers).
    /// </summary>
    public static class ExclusionProps
    {
        public class Properties : Dictionary<string, object>
        {
            public new Properties Add(string key, object value)
            {
                this[key] = value;
                return this;
            }
        }

        /// <summary>
        /// Creates  Properties for pattern-based exclusion (Layer 1).
        /// Used when .gitignore/.vtignore patterns exclude a file or directory.
        /// </summary>
        /// <param name="itemPath">Full path to excluded item (file or directory)</param>
        /// <param name="pattern">Matching pattern from ignore file (e.g., "*.generated.cs")</param>
        /// <param name="sourceFile">Name of ignore file that matched (e.g., ".vtignore" or ".gitignore")</param>
        /// <returns>Properties object with exclusion context</returns>
        /// <example>
        /// using (var ctx = logger.Ctx.Set(ExclusionProps.CreatePatternProps(
        ///     itemPath: "/path/to/file.g.cs",
        ///     pattern: "*.g.cs",
        ///     sourceFile: ".vtignore")))
        /// {
        ///     logger.LogInformation("File excluded by pattern matching");
        /// }
        /// </example>
        public static Properties CreatePatternProps(string itemPath, string pattern, string sourceFile)
        {
            return new Properties()
                .Add("exclusion_layer", "layer_1_pattern")
                .Add("item_path", itemPath)
                .Add("pattern", pattern)
                .Add("source_file", sourceFile)
                .Add("timestamp_utc", DateTime.UtcNow.ToString("O"));
        }

        /// <summary>
        /// Creates  Properties for marker-based exclusion (Layer 2).
        /// Used when file contains [VECTOOL:EXCLUDE:...] marker in header.
        /// </summary>
        /// <param name="filePath">Full path to file with exclusion marker</param>
        /// <param name="reason">Reason from marker (e.g., "generated_by_xsd")</param>
        /// <param name="spaceReference">Optional @reference from marker (e.g., "@XSD-Docs")</param>
        /// <param name="lineNumber">Line number where marker was found (1-indexed)</param>
        /// <returns>Properties object with marker context</returns>
        /// <example>
        /// using (var ctx = logger.Ctx.Set(ExclusionProps.CreateMarkerProps(
        ///     filePath: "/path/to/GeneratedClass.cs",
        ///     reason: "generated_by_xsd",
        ///     spaceReference: "@XSD-Schema-Docs",
        ///     lineNumber: 3)))
        /// {
        ///     logger.LogInformation("File excluded by marker");
        /// }
        /// </example>
        public static Properties CreateMarkerProps(
            string filePath,
            string reason,
            string? spaceReference,
            int lineNumber)
        {
            return new Properties()
                .Add("exclusion_layer", "layer_2_marker")
                .Add("file_path", filePath)
                .Add("reason", reason)
                .Add("space_reference", string.IsNullOrWhiteSpace(spaceReference) ? "no space reference" : spaceReference)
                .Add("line_number", lineNumber)
                .Add("timestamp_utc", DateTime.UtcNow.ToString("O"));
        }

        /// <summary>
        /// Creates  Properties for marker extraction errors.
        /// Used when file marker extraction fails (read error, regex timeout, etc.).
        /// </summary>
        /// <param name="filePath">Full path to file where extraction was attempted</param>
        /// <param name="errorType">Exception type name (e.g., "UnauthorizedAccessException")</param>
        /// <param name="errorMessage">LogError message for debugging</param>
        /// <returns>Properties object with error context</returns>
        /// <example>
        /// using (var ctx = logger.Ctx.Set(ExclusionProps.CreateMarkerErrorProps(
        ///     filePath: "/path/to/file.cs",
        ///     errorType: "UnauthorizedAccessException",
        ///     errorMessage: "Access to the path is denied")))
        /// {
        ///     logger.LogWarning("Marker extraction failed");
        /// }
        /// </example>
        public static Properties CreateMarkerErrorProps(
            string filePath,
            string errorType,
            string errorMessage)
        {
            return new Properties()
                .Add("exclusion_layer", "layer_2_marker_error")
                .Add("file_path", filePath)
                .Add("error_type", errorType)
                .Add("error_message", errorMessage)
                .Add("timestamp_utc", DateTime.UtcNow.ToString("O"));
        }

        /// <summary>
        /// Creates  Properties for FileSystemTraverser exclusion summary.
        /// Used at end of ProcessFolder() for aggregate statistics.
        /// </summary>
        /// <param name="filesProcessed">Total files processed</param>
        /// <param name="filesExcludedByPattern">Files excluded by Layer 1 patterns</param>
        /// <param name="filesExcludedByMarker">Files excluded by Layer 2 markers</param>
        /// <param name="markerExtractionErrors">Files where marker extraction failed</param>
        /// <returns>Properties object with summary statistics</returns>
        /// <example>
        /// using (var ctx = logger.Ctx.Set(ExclusionProps.CreateSummaryProps(
        ///     filesProcessed: 1500,
        ///     filesExcludedByPattern: 425,
        ///     filesExcludedByMarker: 12,
        ///     markerExtractionErrors: 2)))
        /// {
        ///     logger.LogInformation("Folder traversal completed");
        /// }
        /// </example>
        public static Properties CreateSummaryProps(
            int filesProcessed,
            int filesExcludedByPattern,
            int filesExcludedByMarker,
            int markerExtractionErrors)
        {
            return new Properties()
                .Add("operation", "folder_traversal_summary")
                .Add("files_processed", filesProcessed)
                .Add("files_excluded_layer_1_pattern", filesExcludedByPattern)
                .Add("files_excluded_layer_2_marker", filesExcludedByMarker)
                .Add("marker_extraction_errors", markerExtractionErrors)
                .Add("timestamp_utc", DateTime.UtcNow.ToString("O"));
        }

        /// <summary>
        /// Creates  Properties for directory-level exclusions.
        /// Used when entire directory is excluded by pattern matching (Layer 1).
        /// </summary>
        /// <param name="directoryPath">Full path to excluded directory</param>
        /// <param name="pattern">Matching pattern (e.g., "node_modules", "bin/**")</param>
        /// <param name="itemsSkipped">Count of items skipped due to directory exclusion</param>
        /// <returns>Properties object with directory exclusion context</returns>
        /// <example>
        /// using (var ctx = logger.Ctx.Set(ExclusionProps.CreateDirectoryExclusionProps(
        ///     directoryPath: "/path/to/node_modules",
        ///     pattern: "node_modules",
        ///     itemsSkipped: 1247)))
        /// {
        ///     logger.LogInformation("Directory excluded (subtree skipped)");
        /// }
        /// </example>
        public static Properties CreateDirectoryExclusionProps(
            string directoryPath,
            string pattern,
            int itemsSkipped)
        {
            return new Properties()
                .Add("exclusion_layer", "layer_1_directory")
                .Add("directory_path", directoryPath)
                .Add("pattern", pattern)
                .Add("items_skipped", itemsSkipped)
                .Add("timestamp_utc", DateTime.UtcNow.ToString("O"));
        }
    }
}