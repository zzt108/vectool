// ✅ FULL FILE VERSION
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
        /// Counts non-empty, non-comment code lines based on extension.
        /// Example: Skips "//" or multi-line "/* */" comments for real code density.
        /// Step-by-step: Split lines, trim, filter out blanks/comments.
        /// </summary>
        //public static int CountCodeLines(string text, string ext) // public for testing
        //{
        //    if (string.IsNullOrWhiteSpace(text)) return 0;
        //    var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        //    return lines.Count(l =>
        //    {
        //        var s = l.Trim();
        //        if (s.Length == 0) return false;
        //        if (s.StartsWith("//")) return false;
        //        if (s.StartsWith("/*") || s.Contains("*/")) return false;  // Basic multi-line check
        //        // Extend for lang-specific (e.g., Python #, JS //) if ext != ".cs"
        //        if (ext == ".py" && s.StartsWith("#")) return false;
        //        return true;
        //    });
        //}

        /// <summary>
        /// Counts class declarations using regex.
        /// Example 1: Matches "class MyClass" – straightforward for C#/Java.
        /// Example 2: Handles "public class Foo" or "internal class Bar".
        /// Step-by-step: Regex scan, count non-overlapping matches.
        /// </summary>
        public static int CountClasses(string text, string ext) // public for testing
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var classPattern = ext == ".cs" ? @"(public|private|internal|protected)?\s*class\s+\w+" : @"class\s+\w+";  // Tailor per lang
            return Regex.Matches(text, classPattern).Count;
        }

        /// <summary>
        /// Counts method declarations using regex.
        /// Example 1: "void DoStuff()" or "int Calculate(int x)".
        /// Example 2: Ignores lambdas or local funcs for now—focus on top-level.
        /// Sarcastic note: Methods are the heart; counting them reveals the beast within.
        /// </summary>
        //public static int CountMethods(string text, string ext) // public for testing
        //{
        //    if (string.IsNullOrWhiteSpace(text)) return 0;
        //    var methodPattern = ext == ".cs" ? @"[A-Za-z_][A-Za-z0-9_]*\s+[A-Za-z_][A-Za-z0-9_]*\s*\(" : @"def\s+\w+\(";  // C# vs Python example
        //    return Regex.Matches(text, methodPattern).Count;
        //}

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

        /// <summary>
        /// Counts TODO comments.
        /// Example: "// TODO: Fix this mess" or "/* TODO: Refactor */".
        /// Quick win for tech debt tracking—every TODO is a future headache.
        /// </summary>
        //public static int CountTodos(string text) // public for testing
        //{
        //    if (string.IsNullOrWhiteSpace(text)) return 0;
        //    return Regex.Matches(text, @"TODO", RegexOptions.IgnoreCase | RegexOptions.Multiline).Count;
        //}

        /// <summary>
        /// Counts catch blocks.
        /// Example: "catch (Exception ex)" – smell of defensive programming.
        /// Step-by-step: Regex for "catch (" patterns, count per block.
        /// </summary>
        //public static int CountCatches(string text) // public for testing
        //{
        //    if (string.IsNullOrWhiteSpace(text)) return 0;
        //    return Regex.Matches(text, @"catch\s*\(", RegexOptions.IgnoreCase).Count;
        //}
    }
}
