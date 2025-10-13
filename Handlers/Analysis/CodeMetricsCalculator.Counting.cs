using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace VecTool.Handlers.Analysis
{
    public partial class CodeMetricsCalculator
    {
        /// <summary>
        /// Counts total lines in the text, including whitespace and comments.
        /// Analogous to a quick "wc -l" in Unix—simple but effective for LOC.
        /// </summary>
        public static int CountLines(string text) // public for testing
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var lines = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') lines++;
            }
            return lines;
        }

        /// <summary>
        /// Counts class declarations using regex.
        /// Example 1: Matches "class MyClass" – straightforward for C#/Java.
        /// Example 2: Handles "public class Foo" or "internal class Bar".
        /// Step-by-step: Regex scan, count non-overlapping matches.
        /// </summary>
        public static int CountClasses(string text, string ext) // public for testing
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var classPattern = ext == ".cs"
                ? @"(public|private|internal|protected)?\s*class\s+[A-Za-z_][A-Za-z0-9_]*"
                : @"\bclass\s+[A-Za-z_][A-Za-z0-9_]*"; // Tailor per lang
            return Regex.Matches(text, classPattern).Count;
        }

        /// <summary>
        /// Counts methods exceeding a line threshold.
        /// Example: Threshold 40—flags those god-methods begging for a refactor.
        /// Step-by-step: Regex for method sig, brace-balance for body, line tally.
        /// Analogy: Like spotting overweight functions at a code gym.
        /// </summary>
        public static int CountLongMethods(string text, string ext, int thresholdLines = 40) // public for testing
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var methodMatches = Regex.Matches(text, ext == ".cs" ? @"[A-Za-z_][A-Za-z0-9_]*\s+[A-Za-z_][A-Za-z0-9_]*\s*(?=\{)" : @"def\s+\w+\s*:");
            var count = 0;
            foreach (Match m in methodMatches)
            {
                var bodyStart = m.Index + m.Length;
                var braceLevel = 0;
                var bodyLines = 0;
                var inBlock = ext == ".cs" ? "{" : ":";  // C# brace vs Python indent (simplified)
                bool isBody = false;
                for (int i = bodyStart; i < text.Length; i++)
                {
                    if (!isBody)
                    {
                        if (text[i] == inBlock[0]) isBody = true;
                        continue;
                    }
                    if (text[i] == '\n') bodyLines++;
                    if (ext == ".cs")
                    {
                        if (text[i] == '{') braceLevel++;
                        else if (text[i] == '}')
                        {
                            braceLevel--;
                            if (braceLevel == 0) break;
                        }
                    }
                    else
                    {
                        // Python: Approximate by indent levels—skip for now, or use dedent logic
                        if (text.Substring(i).StartsWith("    ")) { } else if (text[i] == '\n' && Regex.IsMatch(text.Substring(i), @"^\s*$")) break;
                    }
                }
                if (bodyLines > thresholdLines) count++;
            }
            return count;
        }
    }
}
