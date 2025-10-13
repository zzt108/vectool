// ✅ FULL FILE VERSION
// Path: Handlers/Analysis/CodeMetricsCalculator.cs
// Migrated from ILogCtxLogger to NLog with message-template logging per guide.

using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VecTool.Handlers.Traversal;

namespace VecTool.Handlers.Analysis
{
    /// <summary>
    /// Calculates code metrics (LOC, methods, complexity, etc.) for source files.
    /// </summary>
    public sealed partial class CodeMetricsCalculator
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public CodeMetricsCalculator()
        {
            // No dependencies required - removed ILogCtxLogger injection
        }

        /// <summary>
        /// Computes metrics for a single file and returns FileMetrics.
        /// </summary>
        public static FileMetrics Calculate(string filePath, IReadOnlyList<string> folderPaths)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                log.Warn("Invalid or missing file path {FilePath}", filePath ?? "null");
                return new FileMetrics(filePath ?? string.Empty, 0L, 0, 0, 0, "Low");
            }

            var ext = Path.GetExtension(filePath) ?? string.Empty;
            var text = PathHelpers.SafeReadAllText(filePath);
            var sizeBytes = new FileInfo(filePath).Length;

            // Use existing static counters from other partials.
            var loc = CountCodeLines(text, ext); // non-empty, non-comment code lines
            var methods = CountMethods(text, ext); // approximate number of methods
            var todos = CountTodos(text); // TODO occurrences

            // Derive codeLines for complexity estimation (matches ToXml: codeLines = LOC - Methods).
            var codeLines = Math.Max(0, loc - methods);
            var complexity = EstimateComplexity(codeLines, methods, text);

            log.Debug("Calculated metrics for {FilePath}: LOC={LOC}, Methods={Methods}, Complexity={Complexity}",
                filePath, loc, methods, complexity);

            return new FileMetrics(filePath, sizeBytes, loc, methods, todos, complexity);
        }

        /// <summary>
        /// Counts standalone TODO markers in the provided text using case-insensitive, word-boundary matching.
        /// </summary>
        /// <param name="text">Input source text.</param>
        /// <returns>Number of TODO occurrences.</returns>
        public static int CountTodos(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                log.Debug("CountTodos called with empty text");
                return 0;
            }

            // Case-insensitive, word-boundary match so "methodology" is not treated as TODO.
            var count = Regex.Matches(text, @"\bTODO\b", RegexOptions.IgnoreCase).Count;
            log.Debug("CountTodos found {Count} TODOs in {Length} chars", count, text.Length);
            return count;
        }

        /// <summary>
        /// Classifies an integer score into a human-readable complexity band.
        /// </summary>
        /// <param name="score">Complexity score.</param>
        /// <returns>"Low", "Medium", or "High".</returns>
        public static string EstimateComplexity(int score)
        {
            // Thresholds are intentionally simple and test-friendly:
            // 0..3 → Low
            // 4..6 → Medium
            // 7+   → High
            const int MediumStart = 4;
            const int HighStart = 7;

            // Normalize negatives to zero to keep mapping predictable.
            var normalized = Math.Max(0, score);

            string label;
            if (normalized >= HighStart)
            {
                label = "High";
            }
            else if (normalized >= MediumStart)
            {
                label = "Medium";
            }
            else
            {
                label = "Low";
            }

            log.Debug("EstimateComplexity mapped {Score} (normalized={Normalized}) to {Label}",
                score, normalized, label);
            return label;
        }

        /// <summary>
        /// Overload of EstimateComplexity that takes additional context for more sophisticated scoring.
        /// </summary>
        public static string EstimateComplexity(int codeLines, int methods, string text)
        {
            // Calculate a simple complexity score
            int score = codeLines / 10 + methods / 2;

            // 🔄 MODIFY: Count catch blocks and factor into complexity
            var catches = CountCatches(text);
            score += catches;

            return EstimateComplexity(score);
        }

        /// <summary>
        /// Counts code lines (non-empty, non-comment lines) in the provided text.
        /// </summary>
        public static int CountCodeLines(string? text, string ext)
        {
            if (string.IsNullOrEmpty(text))
            {
                log.Debug("CountCodeLines called with empty text");
                return 0;
            }

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var count = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Skip comment lines
                if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
                    continue;

                count++;
            }

            log.Debug("CountCodeLines found {Count} code lines in {Length} chars", count, text.Length);
            return count;
        }

        /// <summary>
        /// Counts approximate number of methods in the provided text.
        /// </summary>
        public static int CountMethods(string? text, string ext)
        {
            if (string.IsNullOrEmpty(text))
            {
                log.Debug("CountMethods called with empty text");
                return 0;
            }

            // Simple heuristic: count occurrences of common method patterns
            // This is a rough approximation and may need refinement
            var methodPattern = @"(public|private|protected|internal|static)\s+\w+\s+\w+\s*\(";
            var count = Regex.Matches(text, methodPattern).Count;

            log.Debug("CountMethods found {Count} methods in {Length} chars", count, text.Length);
            return count;
        }

        /// <summary>
        /// Counts catch blocks in C# code.
        /// </summary>
        /// <param name="text">Source code text.</param>
        /// <returns>Number of catch blocks found.</returns>
        public static int CountCatches(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                log.Debug("CountCatches called with empty text");
                return 0;
            }

            // 🔄 MODIFY: Improved regex pattern to match all catch block variations
            // Matches: catch(...){, catch{, catch (...) {, catch {...}
            // The pattern handles:
            // - Optional whitespace after 'catch'
            // - Optional exception specification in parentheses
            // - Optional whitespace before opening brace
            var catchPattern = @"\bcatch\s*(?:\([^)]*\))?\s*\{";
            var matches = Regex.Matches(text, catchPattern, RegexOptions.IgnoreCase);

            var count = matches.Count;
            log.Debug("CountCatches found {Count} catch blocks in {TextLength} chars", count, text.Length);
            return count;
        }
    }
}
