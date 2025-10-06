// ✅ FULL FILE VERSION
// File: Handlers/Analysis/CodeMetricsCalculator.Compute.cs

using System;
using System.Collections.Generic;
using System.IO;
using VecTool.Handlers.Traversal;

namespace VecTool.Handlers.Analysis
{
    // Partial class to extend existing analysis/counting helpers.
    public partial class CodeMetricsCalculator
    {
        /// <summary>
        /// Computes metrics for a single file and returns the current FileMetrics shape:
        /// FilePath, FileName, Extension, SizeBytes, LinesOfCode, Methods, TodoCount, Complexity.
        /// </summary>
        /// <param name="filePath">Absolute path to a source file.</param>
        /// <param name="folderPaths">Roots used for relative path resolution elsewhere; not required here.</param>
        /// <returns>Populated FileMetrics or an empty-low default when path is invalid.</returns>
        public FileMetrics Calculate(string filePath, IReadOnlyList<string> folderPaths)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return new FileMetrics(filePath ?? string.Empty, 0L, 0, 0, 0, "Low");
            }

            var ext = Path.GetExtension(filePath) ?? string.Empty;
            var text = PathHelpers.SafeReadAllText(filePath);
            var sizeBytes = new FileInfo(filePath).Length;

            // Use existing static counters from other partials.
            var loc = CountCodeLines(text, ext);     // non-empty, non-comment code lines
            var methods = CountMethods(text, ext);   // approximate number of methods
            var todos = CountTodos(text);            // TODO occurrences

            // Derive codeLines for complexity estimation (matches ToXml: codeLines = LOC - Methods).
            var codeLines = Math.Max(0, loc - methods);
            var complexity = EstimateComplexity(codeLines, methods, text);

            return new FileMetrics(filePath, sizeBytes, loc, methods, todos, complexity);
        }
    }
}
