// ✅ FULL FILE VERSION
using System;
using System.Collections.Generic;
using System.Configuration;  // If using app.config for thresholds, etc.
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VecTool.Handlers.Traversal;

namespace VecTool.Handlers.Analysis
{
    /// <summary>
    /// Main entry point for code metrics calculation.
    /// Orchestrates counting and analysis via partials—SRP achieved!
    /// Sarcastic aside: No more monolithic mess; this is the conductor, not the whole orchestra.
    /// </summary>
    public partial class CodeMetricsCalculator
    {
        /// <summary>
        /// Calculates metrics for a single file, integrating counting and analysis.
        /// Step-by-step: Read text, extract basics (path/ext/size), call partials for deep dives, build FileMetrics.
        /// Example 1: Simple .cs file → Low complexity, basic patterns.
        /// Example 2: Monolith with 1000 lines → High complexity, many catches/todos.
        /// Example 3: Test file → HasTests: true (future: scan for [Test] attrs).
        /// Analogy: Like a doctor's checkup—vitals (lines), scans (patterns), diagnosis (complexity).
        /// </summary>
        public FileMetrics Calculate(string filePath, List<string> folderPaths)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                // Log warning via LogCtx if injected, but keep simple
                return new FileMetrics();  // Empty metrics for invalid path
            }

            var text = PathHelpers.SafeReadAllText(filePath);
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var name = Path.GetFileName(filePath);
            var root = PathHelpers.FindOwningRoot(folderPaths, filePath);
            var rel = PathHelpers.MakeRelativeSafe(root, filePath);
            var fi = new FileInfo(filePath);
            var size = fi.Length;
            var lastModified = fi.LastWriteTime;

            // Delegate to partials—zero refactoring needed on call sites!
            var loc = CountLines(text);
            var codeLines = CountCodeLines(text, ext);
            var classes = CountClasses(text, ext);
            var methods = CountMethods(text, ext);
            var longMethods = CountLongMethods(text, ext, 40);  // Default threshold from conventions
            var todos = CountTodos(text);
            var catches = CountCatches(text);
            var complexity = EstimateComplexity(codeLines, methods, text);
            var patterns = DetectPatterns(text, ext).ToList();

            // Bonus: Overall score from Analysis partial (new addition for richer metrics)
            var metrics = new FileMetrics(
                Name: name,
                Path: rel,
                Ext: ext,
                Lang: ext.TrimStart('.'),
                SizeBytes: size,
                Loc: loc,
                CodeLines: codeLines,
                Classes: classes,
                Methods: methods,
                LastModified: lastModified,
                Complexity: complexity,
                Patterns: patterns,
                HasTests: text.Contains("[Test]") || text.Contains("Should."),  // Basic heuristic; enhance with Roslyn later
                Tests: DetectTestMethods(text),  // Placeholder: Implement if needed
                LongMethods: longMethods,
                Todos: todos,
                Catches: catches
            );
            metrics.Score = CalculateOverallScore(metrics);  // From Analysis partial

            // Log if high risk? E.g., LogCtx.Debug($"High complexity in {name}: {complexity}");
            return metrics;
        }

        /// <summary>
        /// Placeholder for detecting test methods—extend as needed.
        /// Example: Returns list of method names like "TestAddition".
        /// </summary>
        public static List<string> DetectTestMethods(string text) // public for testing
        {
            // Simple regex for NUnit-style: [Test] public void TestXxx()
            var testMatches = System.Text.RegularExpressions.Regex.Matches(text, @"\[Test\][^{]*void\s+([A-Za-z0-9_]+)\s*\(");
            return testMatches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        }

        /// <summary>
        /// FileMetrics model—immutable record for output (per Clean Code: data classes simple).
        /// All properties public, with defaults for safety.
        /// Note: Added Score property to leverage the new CalculateOverallScore.
        /// </summary>
        public record FileMetrics
        {
            public FileMetrics() { }  // Parameterless for invalid paths

            public FileMetrics(
                string Name,
                string Path,
                string Ext,
                string Lang,
                long SizeBytes,
                int Loc,
                int CodeLines,
                int Classes,
                int Methods,
                DateTime LastModified,
                string Complexity,
                List<string> Patterns,
                bool HasTests,
                List<string> Tests,
                int LongMethods,
                int Todos,
                int Catches)
            {
                this.Name = Name;
                this.Path = Path;
                this.Ext = Ext;
                this.Lang = Lang;
                this.SizeBytes = SizeBytes;
                this.Loc = Loc;
                this.CodeLines = CodeLines;
                this.Classes = Classes;
                this.Methods = Methods;
                this.LastModified = LastModified;
                this.Complexity = Complexity;
                this.Patterns = Patterns ?? new List<string>();
                this.HasTests = HasTests;
                this.Tests = Tests ?? new List<string>();
                this.LongMethods = LongMethods;
                this.Todos = Todos;
                this.Catches = Catches;
                this.Score = 0;  // Computed separately
            }

            public string Name { get; init; } = string.Empty;
            public string Path { get; init; } = string.Empty;
            public string Ext { get; init; } = string.Empty;
            public string Lang { get; init; } = string.Empty;
            public long SizeBytes { get; init; }
            public int Loc { get; init; }
            public int CodeLines { get; init; }
            public int Classes { get; init; }
            public int Methods { get; init; }
            public DateTime LastModified { get; init; } = DateTime.MinValue;
            public string Complexity { get; init; } = "Low";
            public List<string> Patterns { get; init; } = new();
            public bool HasTests { get; init; }
            public List<string> Tests { get; init; } = new();
            public int LongMethods { get; init; }
            public int Todos { get; init; }
            public int Catches { get; init; }
            public int Score { get; set; }  // Overall risk score (0-100)

            /// <summary>
            /// Serializes FileMetrics to XML for AI context (e.g., prompt injection).
            /// Example 1: Basic file → <FileMetrics><Name>Test.cs</Name><Loc>100</Loc>...</FileMetrics>
            /// Example 2: With patterns → <Patterns><Pattern>Logging</Pattern></Patterns>
            /// Example 3: High complexity → <Complexity>High</Complexity><Score>75</Score>
            /// Step-by-step: XElement root, add simple props as elements, lists as sub-nodes, return outer XML.
            /// Sarcastic note: Because JSON is too hip for AI—XML keeps it old-school structured.
            /// </summary>
            public string ToXml()
            {
                var root = new System.Xml.Linq.XElement("FileMetrics");
                root.Add(new System.Xml.Linq.XElement("Name", Name ?? ""));
                root.Add(new System.Xml.Linq.XElement("Path", Path ?? ""));
                root.Add(new System.Xml.Linq.XElement("Ext", Ext ?? ""));
                root.Add(new System.Xml.Linq.XElement("Lang", Lang ?? ""));
                root.Add(new System.Xml.Linq.XElement("SizeBytes", SizeBytes));
                root.Add(new System.Xml.Linq.XElement("Loc", Loc));
                root.Add(new System.Xml.Linq.XElement("CodeLines", CodeLines));
                root.Add(new System.Xml.Linq.XElement("Classes", Classes));
                root.Add(new System.Xml.Linq.XElement("Methods", Methods));
                root.Add(new System.Xml.Linq.XElement("LastModified", LastModified.ToString("O")));  // ISO format
                root.Add(new System.Xml.Linq.XElement("Complexity", Complexity ?? "Low"));
                root.Add(new System.Xml.Linq.XElement("Score", Score));

                var patterns = new System.Xml.Linq.XElement("Patterns");
                foreach (var p in Patterns ?? new List<string>())
                {
                    patterns.Add(new System.Xml.Linq.XElement("Pattern", p));
                }
                root.Add(patterns);

                root.Add(new System.Xml.Linq.XElement("HasTests", HasTests));
                var tests = new System.Xml.Linq.XElement("Tests");
                foreach (var t in Tests ?? new List<string>())
                {
                    tests.Add(new System.Xml.Linq.XElement("Test", t));
                }
                root.Add(tests);

                root.Add(new System.Xml.Linq.XElement("LongMethods", LongMethods));
                root.Add(new System.Xml.Linq.XElement("Todos", Todos));
                root.Add(new System.Xml.Linq.XElement("Catches", Catches));

                return root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);  // Compact XML
            }
        }
    }
}
