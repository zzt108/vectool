// ✅ FULL FILE VERSION
// File: Handlers/Analysis/CodeMetricsCalculator.Analysis.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VecTool.Handlers.Analysis
{
    // Partial class housing analysis/scoring helpers.
    public partial class CodeMetricsCalculator
    {
        /// <summary>
        /// Estimates complexity from derived code lines and method count.
        /// Rule of thumb:
        /// - Low: score < 1.0
        /// - Medium: score <= 2.5
        /// - High: score > 2.5
        /// Where score = (codeLines / 200.0) + (methods / 20.0).
        /// </summary>
        public static string EstimateComplexity(int codeLines, int methods, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "Low";
            }

            var lineScore = codeLines / 200.0;   // 200 lines ~ 1 point
            var methodScore = methods / 20.0;    // 20 methods ~ 1 point
            var score = lineScore + methodScore;

            if (score < 1.0) return "Low";
            if (score <= 2.5) return "Medium";
            return "High";
        }

        /// <summary>
        /// Fast pattern detector for common C#/.NET idioms.
        /// Returns a small set of unique, order-stable tokens like "DisposePattern", "Logging", "Async", etc.
        /// </summary>
        public static IEnumerable<string> DetectPatterns(string text, string ext)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var hits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Core C# patterns; can be extended per language via 'ext'.
            if (string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ext, ".net", StringComparison.OrdinalIgnoreCase))
            {
                if (text.Contains("IDisposable", StringComparison.OrdinalIgnoreCase))
                    hits.Add("DisposePattern");

                if (text.Contains("ILogger", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("LogCtx", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("NLog", StringComparison.OrdinalIgnoreCase))
                    hits.Add("Logging");

                if (text.Contains("async ", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("await ", StringComparison.OrdinalIgnoreCase))
                    hits.Add("Async");

                if (text.Contains("IServiceCollection", StringComparison.OrdinalIgnoreCase))
                    hits.Add("DependencyInjection");

                if (text.Contains("interface", StringComparison.OrdinalIgnoreCase))
                    hits.Add("Interfaces");

                // Quick SOLID hinting
                if (text.Contains("SOLID", StringComparison.OrdinalIgnoreCase) ||
                    Regex.IsMatch(text, @"public\s+interface\s+I[A-Z]", RegexOptions.IgnoreCase))
                    hits.Add("SOLID");

                // Basic GoF vibe checks
                if (Regex.IsMatch(text, @"private\s+static\s+readonly\s+.*\bInstance\b", RegexOptions.IgnoreCase))
                    hits.Add("Singleton");

                if (Regex.IsMatch(text, @"\bvirtual\b.*\boverride\b", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                    hits.Add("TemplateMethod");
            }
            else if (string.Equals(ext, ".py", StringComparison.OrdinalIgnoreCase))
            {
                if (text.Contains("async def", StringComparison.OrdinalIgnoreCase))
                    hits.Add("Async");

                if (text.Contains("logging.", StringComparison.OrdinalIgnoreCase))
                    hits.Add("Logging");
            }

            foreach (var pattern in hits.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                yield return pattern;
        }

        /// <summary>
        /// Produces a simple 0..100 score from current FileMetrics shape.
        /// Uses: Complexity (band), derived codeLines, and TodoCount.
        /// Mapping:
        /// - Complexity Low/Medium/High => base 20/50/80
        /// - codeLines = max(0, LinesOfCode - Methods); adds up to +20 (scaled per 100 lines)
        /// - TodoCount => +3 each
        /// Final score capped at 100.
        /// </summary>
        public static int CalculateOverallScore(FileMetrics metrics)
        {
            if (metrics is null)
                return 0;

            var baseScore = metrics.Complexity switch
            {
                "Low" => 20,
                "Medium" => 50,
                "High" => 80,
                _ => 20
            };

            var codeLines = Math.Max(0, metrics.LinesOfCode - metrics.Methods);
            var codeLinesContribution = (int)Math.Min(20, (codeLines / 100.0) * 20);

            var todoContribution = metrics.TodoCount * 3;

            var score = baseScore + codeLinesContribution + todoContribution;

            // Legacy properties (Catches, LongMethods) removed from FileMetrics in refactor.
            return Math.Max(0, Math.Min(100, score));
        }
    }
}
