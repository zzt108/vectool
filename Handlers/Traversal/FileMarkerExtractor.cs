namespace VecTool.Handlers.Traversal
{
    using LogCtxShared;
    using Microsoft.Extensions.Logging;
    using NLog.Extensions.Logging;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using VecTool.Constants;

    /// <summary>
    /// Extracts [VECTOOL:EXCLUDE:...] markers from file headers.
    /// Supports language-agnostic syntax: comments (C#, Python, JS, Go, Rust, XML)
    /// and JSON string values (__vectool_exclude).
    /// </summary>
    public class FileMarkerExtractor : IFileMarkerExtractor
    {
        private static readonly ILogger logger =
            LoggerFactory.Create(b => b.AddNLog()).CreateLogger<PromptSearchEngine>();

        public const string MarkerSigniture = "[VECTOOL:EXCLUDE:";

        /// <summary>
        /// Compiled regex for marker pattern (case-insensitive).
        /// Matches: [VECTOOL:EXCLUDE:reason:@reference] or [VECTOOL:EXCLUDE:reason:]
        /// Reason: alphanumeric + underscore (e.g., "generated_by_xsd", "vendor_library")
        /// Reference: optional @word-word (e.g., "@XSD-Docs", "@AI-Generated")
        /// </summary>
        private static readonly Regex MarkerRegex = new(
            pattern: @"\" + MarkerSigniture + @"(?<reason>[a-zA-Z0-9_\-]+)(?:(?<reference>@[\w\-\.]+))?\]",
            options: RegexOptions.Compiled | RegexOptions.IgnoreCase,
            matchTimeout: TimeSpan.FromMilliseconds(100)
        );

        /// <summary>
        /// Extracts file-level exclusion marker from first 1500 bytes / first 50 lines.
        /// Returns null if no marker found or file cannot be read.
        /// Logs audit trail to SEQ for all extraction attempts.
        /// </summary>
        public FileMarkerPattern? ExtractMarker(string filePath)
        {
            // Guard: validate input
            if (string.IsNullOrWhiteSpace(filePath))
            {
                using var ctx = logger.SetContext(
                    new Props().Add("error", "empty_file_path")
                );
                logger.LogWarning("ExtractMarker called with null/empty filePath");
                return null;
            }

            // 1. Read file header (1500 bytes max)
            string? header = ReadFileHeader(filePath, maxBytes: 1500);
            bool isVectoolExcude = header != null && header.Contains(MarkerSigniture);

            // 2. Handle read failures
            if (string.IsNullOrEmpty(header))
                return null;  // File doesn't exist or can't be read

            // 3. Take only first 50 lines (header section)
            var lines = header.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var headerLines = string.Join("\n", lines.Take(50));

            // 4. Apply regex pattern
            Match match = null!;
            try
            {
                match = MarkerRegex.Match(headerLines);
            }
            catch (RegexMatchTimeoutException ex)
            {
                using var ctx = logger.SetContext(new Props()
                    .Add("file_path", filePath)
                    .Add("error_type", "RegexMatchTimeoutException")
                );
                logger.LogWarning($"Regex timeout analyzing file: {ex.Message}");
                return null;
            }

            // 5. Validate match groups
            if (!match.Success)
            {
                if (isVectoolExcude)
                {
                    var markedLines = lines.Where(l => l.Contains(MarkerSigniture));
                    using var ctx = logger.SetContext(new Props()
                        .Add("file_path", filePath)
                        .AddJson("lines", markedLines));
                    logger.LogWarning($"Found:{lines.FirstOrDefault()}, but no match found in marker pattern");
                }
                return null;
            }

            // 6. Extract components
            var reason = match.Groups["reason"]?.Value;
            var spaceReference = match.Groups["reference"]?.Value;
            var lineNumber = DetermineLineNumber(headerLines, match.Index);

            // 7. Create marker pattern
            var markerPattern = new FileMarkerPattern
            {
                FilePath = filePath,
                Reason = reason ?? "unknown",
                SpaceReference = spaceReference,
                LineNumber = lineNumber,
                ExtractedAt = DateTime.UtcNow
            };

            // 8. Log successful extraction to SEQ
            using (var ctx = logger.SetContext(new Props()
                .Add("file_path", filePath)
                .Add("reason", reason)
                .Add("space_reference", spaceReference ?? Const.NA)
                .Add("line_number", lineNumber)
                .Add("marker_status", "extracted")))
            {
                logger.LogInformation("File marker extracted successfully");
            }

            return markerPattern;
        }

        /// <summary>
        /// Reads file header (first maxBytes) with proper encoding detection.
        /// Returns null if file doesn't exist or cannot be read.
        /// Logs failures to SEQ for troubleshooting.
        /// </summary>
        private static string? ReadFileHeader(string filePath, int maxBytes = 1500)
        {
            try
            {
                // Validate file exists before opening stream
                if (!File.Exists(filePath))
                {
                    using var ctx = logger.SetContext(new Props()
                        .Add("file_path", filePath)
                        .Add("error_type", "FileNotFoundException"));
                    logger.LogDebug("File does not exist");
                    return null;
                }

                using var fileStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096
                );

                using var reader = new StreamReader(
                    fileStream,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true
                );

                var buffer = new char[maxBytes / 2];  // Account for multi-byte chars
                int charsRead = reader.Read(buffer, 0, buffer.Length);

                return charsRead > 0
                    ? new string(buffer, 0, charsRead)
                    : null;
            }
            catch (UnauthorizedAccessException ex)
            {
                using var ctx = logger.SetContext(new Props()
                    .Add("file_path", filePath)
                    .Add("error_type", "UnauthorizedAccessException")
                    .Add("message", ex.Message));
                logger.LogWarning("Permission denied reading file header");
                return null;
            }
            catch (IOException ex)
            {
                using var ctx = logger.SetContext(new Props()
                    .Add("file_path", filePath)
                    .Add("error_type", "IOException")
                    .Add("message", ex.Message));
                logger.LogWarning($"I/O error reading file header: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                using var ctx = logger.SetContext(new Props()
                    .Add("file_path", filePath)
                    .Add("error_type", ex.GetType().Name)
                    .Add("message", ex.Message)
                    .Add("stack_trace", ex.StackTrace ?? "no_trace"));
                logger.LogError(ex, $"Unexpected error reading file header: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Determines line number where match occurs (1-indexed).
        /// Counts newline characters up to match position.
        /// </summary>
        private static int DetermineLineNumber(string headerContent, int matchIndex)
        {
            int lineNumber = 1;

            // Guard against invalid match index
            if (matchIndex < 0 || matchIndex > headerContent.Length)
                return 1;

            for (int i = 0; i < matchIndex && i < headerContent.Length; i++)
            {
                if (headerContent[i] == '\n')
                    lineNumber++;
            }

            return lineNumber;
        }
    }
}