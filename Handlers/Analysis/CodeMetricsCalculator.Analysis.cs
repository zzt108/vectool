// ✅ FULL FILE VERSION
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VecTool.Handlers.Analysis
{
    public partial class CodeMetricsCalculator
    {
        /// <summary>
        /// Estimates code complexity based on lines and methods.
        /// Analogy: Like a credit score for your code—too many methods/lines? High risk of bugs.
        /// Example 1: 100 lines, 5 methods → Medium (score ~1.25).
        /// Example 2: 500 lines, 10 methods → High (score ~3.0).
        /// Example 3: 50 lines, 2 methods → Low (score ~0.45).
        /// Step-by-step: Normalize lines ( /200 for files), methods ( /20 for classes), sum and tier.
        /// Sarcastic note: Because who needs formal metrics when we can wing it with ratios?
        /// </summary>
        public static string EstimateComplexity(int codeLines, int methods, string text) // public for testing
        {
            if (string.IsNullOrWhiteSpace(text)) return "Low";
            var lineScore = codeLines / 200.0;  // Arbitrary: 200 lines = 1 point (file-sized)
            var methodScore = methods / 20.0;   // 20 methods = 1 point (class-sized)
            var score = lineScore + methodScore;
            if (score < 1.0) return "Low";
            if (score < 2.5) return "Medium";
            return "High";  // Or "Abandon Hope" for score >5, but keep it civil
        }

        /// <summary>
        /// Detects common patterns like IDisposable, ILogger, etc.
        /// Example 1: Text with "IDisposable" → Returns "DisposePattern" (resource management win).
        /// Example 2: Contains "ILogger" or "NLog" → "Logging" (structured logs, per conventions).
        /// Example 3: "async Task" or "await" → "Async" (modern C# goodness).
        /// Step-by-step: String.Contains for quick hits, collect unique patterns, return as list.
        /// Analogy: Like scanning for tattoos at a job interview—reveals the code's personality.
        /// </summary>
        public static IEnumerable<string> DetectPatterns(string text, string ext) // public for testing
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            var hits = new HashSet<string>();  // Dedupe for cleanliness

            // Core C# patterns (extend for ext == ".py" etc.)
            if (ext == ".cs" || ext == ".net")
            {
                if (text.Contains("IDisposable", StringComparison.OrdinalIgnoreCase)) hits.Add("DisposePattern");
                if (text.Contains("IOptions<", StringComparison.OrdinalIgnoreCase)) hits.Add("Options");
                if (text.Contains("ILogger", StringComparison.OrdinalIgnoreCase) || text.Contains("LogCtx", StringComparison.OrdinalIgnoreCase) || text.Contains("NLog", StringComparison.OrdinalIgnoreCase)) hits.Add("Logging");
                if (text.Contains("async ", StringComparison.OrdinalIgnoreCase) || text.Contains("await ", StringComparison.OrdinalIgnoreCase)) hits.Add("Async");
                if (text.Contains("IServiceCollection", StringComparison.OrdinalIgnoreCase)) hits.Add("DependencyInjection");
                if (text.Contains("interface", StringComparison.OrdinalIgnoreCase) && text.Contains("I", StringComparison.OrdinalIgnoreCase)) hits.Add("Interfaces");
                // SOLID hints (from guide: explicit mentions or structures)
                if (text.Contains("SOLID", StringComparison.OrdinalIgnoreCase) || Regex.IsMatch(text, @"public interface I[A-Z]")) hits.Add("SOLID");
                // Gang of Four vibes (basic regex for patterns like Singleton)
                if (Regex.IsMatch(text, @"private static readonly.*Instance")) hits.Add("Singleton");
                if (Regex.IsMatch(text, @"virtual\s+.*override")) hits.Add("TemplateMethod");
            }
            else if (ext == ".py")
            {
                if (text.Contains("async def", StringComparison.OrdinalIgnoreCase)) hits.Add("Async");
                if (text.Contains("logging.", StringComparison.OrdinalIgnoreCase)) hits.Add("Logging");
                // Add more for Python
            }

            // Yield for IEnumerable—lazy if we expand to full scans
            foreach (var pattern in hits.OrderBy(p => p)) yield return pattern;
        }

        /// <summary>
        /// Calculates overall metric score (e.g., for sorting files).
        /// Example 1: High complexity + many catches → Score 80/100 (risky).
        /// Example 2: Low everything → 20/100 (boring but safe).
        /// Step-by-step: Weighted sum (complexity 40%, lines 20%, etc.), cap at 100.
        /// Note: Future-proof for LogCtx integration, e.g., log high-risk files.
        /// </summary>
        public static int CalculateOverallScore(FileMetrics metrics) // public for testing
        {
            var score = 0;
            switch (metrics.Complexity)
            {
                case "Low": score += 20; break;
                case "Medium": score += 50; break;
                case "High": score += 80; break;
            }
            score += (metrics.CodeLines / 100) * 20;  // Cap influence
            score += metrics.Catches * 5;  // Exception smells
            score += metrics.Todos * 3;  // Debt indicators
            score += metrics.LongMethods * 10;  // God method penalty
            return Math.Min(100, score);  // Don't overdo it
        }
    }
}
