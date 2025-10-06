// ✅ FULL FILE VERSION
// Path: Core/Handlers/Analysis/CodeMetricsCalculator.cs

using System;
using System.Text.RegularExpressions;
using LogCtxShared;

namespace VecTool.Core.Handlers.Analysis
{
    /// <summary>
    /// Abstraction for code metrics calculation to enable unit testing and clean composition.
    /// </summary>
    public interface ICodeMetricsCalculator
    {
        /// <summary>
        /// Counts standalone TODO markers in the provided text using case-insensitive, word-boundary matching.
        /// </summary>
        /// <param name="text">Input source text.</param>
        /// <returns>Number of TODO occurrences.</returns>
        int CountTodos(string? text);

        /// <summary>
        /// Classifies an integer score into a human-readable complexity band.
        /// </summary>
        /// <param name="score">Complexity score.</param>
        /// <returns>"Low", "Medium", or "High".</returns>
        string EstimateComplexity(int score);
    }

    /// <summary>
    /// Default implementation of <see cref="ICodeMetricsCalculator"/> with structured logging via LogCtx.
    /// </summary>
    public sealed class CodeMetricsCalculator : ICodeMetricsCalculator
    {
        // Case-insensitive, word-boundary match so "methodology" is not treated as "TODO".
        private static readonly Regex TodoRegex = new Regex(@"\bTODO\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Thresholds are intentionally simple and test-friendly:
        //   0..3  => Low
        //   4..6  => Medium
        //   7+    => High
        private const int MediumStart = 4;
        private const int HighStart = 7;

        private readonly ILogCtxLogger _log;

        public CodeMetricsCalculator(ILogCtxLogger logger)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int CountTodos(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                using (_log.Ctx.Set(new Props { { "todo_count", 0 }, { "reason", "empty" } }))
                {
                    _log.Debug("CountTodos completed."); // structured: todo_count=0, reason=empty
                }
                return 0;
            }

            var count = TodoRegex.Matches(text).Count;

            using (_log.Ctx.Set(new Props { { "todo_count", count }, { "length", text.Length } }))
            {
                _log.Info("CountTodos completed."); // structured: todo_count, length
            }

            return count;
        }

        public string EstimateComplexity(int score)
        {
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

            using (_log.Ctx.Set(new Props { { "score", score }, { "normalized", normalized }, { "label", label } }))
            {
                _log.Debug("EstimateComplexity mapped score to label."); // structured: score, normalized, label
            }

            return label;
        }
    }
}
