// File: DocX/FileHandlerBase.cs
// Purpose: Shared generation utilities for DocX handlers (TOC, CrossReferences, CodeMetaInfo)
// Notes:
// - All XML-like tags via Constants.TagBuilder + Constants.Tags.
// - Provides 6-arg ProcessFolder<T>(...) to match DocXHandler/PdfHandler calls.
// - Adds _ui/_recentFilesManager/_log compatibility fields used by derived handlers.
// - Avoids CustomExcluded* fields; uses VectorStoreConfig API instead.
// - Adds AddAIOptimizedContext and GetEnhancedFileContent used by DocXHandler.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// Centralized tag/attribute constants and builders
using Constants;

// Recent files contracts
using DocXHandler.RecentFiles;

// Logging alias as used in the repo
using NLogS = NLogShared;

namespace DocXHandler
{
    public abstract class FileHandlerBase
    {
        // Compatibility fields (derived handlers reference these names)
        protected static NLogS.CtxLogger _log = new();

        protected readonly IUserInterface? _ui;
        protected readonly IRecentFilesManager? _recentFilesManager;

        // Also keep non-underscore variants if other codepaths reference them
        protected readonly IUserInterface? ui;
        protected readonly IRecentFilesManager? recentFilesManager;

        protected FileHandlerBase(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        {
            this._ui = ui;
            this._recentFilesManager = recentFilesManager;
            this.ui = ui;
            this.recentFilesManager = recentFilesManager;
        }

        // --------------------------------------------------------------------------------
        // Step 4: XML-like block generators using Tags + TagBuilder
        // --------------------------------------------------------------------------------

        // <tableofcontents> <section name="..."> <file .../>* </section>* </tableofcontents>
        protected string GenerateTableOfContentsList(List<string> folderPaths)
        {
            if (folderPaths == null || folderPaths.Count == 0)
                return string.Empty;

            var config = VectorStoreConfig.FromAppConfig();

            var entries = new List<(string RootName, string FilePath)>();
            foreach (var root in folderPaths)
            {
                var rootName = SafeDirectoryName(root);
                foreach (var file in EnumerateFilesRespectingExclusions(root, config))
                {
                    entries.Add((rootName, file));
                }
            }

            var grouped = entries
                .GroupBy(e => e.RootName, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine(TagBuilder.Open(Tags.TableOfContents));

            foreach (var group in grouped)
            {
                sb.AppendLine(TagBuilder.OpenWith(
                    Tags.Section,
                    string.Format(Tags.SectionName, TagBuilder.EscapeXmlAttribute(group.Key))
                ));

                foreach (var filePath in group.Select(e => e.FilePath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                {
                    var ext = Path.GetExtension(filePath);
                    var name = Path.GetFileName(filePath);
                    var root = FindOwningRoot(folderPaths, filePath);
                    var rel = MakeRelativeSafe(root, filePath);

                    sb.AppendLine(TagBuilder.SelfClosing(
                        Tags.File,
                        TagBuilder.BuildFileNameTag(name),
                        TagBuilder.BuildFilePathTag(rel),
                        TagBuilder.BuildExtensionTag(ext)
                    ));
                }

                sb.AppendLine(TagBuilder.Close(Tags.Section));
            }

            sb.AppendLine(TagBuilder.Close(Tags.TableOfContents));
            return sb.ToString();
        }

        // <crossreferences> <file name="..." path="..." dependson="a,b" usedby="c,d"/>* </crossreferences>
        protected string GenerateCrossReferencesList(List<string> folderPaths)
        {
            if (folderPaths == null || folderPaths.Count == 0)
                return string.Empty;

            var config = VectorStoreConfig.FromAppConfig();

            // Collect C# files honoring exclusions
            var csFiles = new List<string>();
            foreach (var root in folderPaths)
                foreach (var f in EnumerateFilesRespectingExclusions(root, config))
                    if (string.Equals(Path.GetExtension(f), ".cs", StringComparison.OrdinalIgnoreCase))
                        csFiles.Add(f);

            // file -> declared symbols
            var declaredByFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in csFiles)
            {
                var symbols = ExtractDeclaredSymbolsFromCSharp(f);
                if (symbols.Count > 0) declaredByFile[f] = symbols;
            }

            // symbol -> files
            var filesBySymbol = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in declaredByFile)
            {
                foreach (var sym in kv.Value)
                {
                    if (!filesBySymbol.TryGetValue(sym, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        filesBySymbol[sym] = set;
                    }
                    set.Add(kv.Key);
                }
            }

            // dependencies: file -> referenced files
            var dependsOn = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in csFiles)
            {
                var text = SafeReadAllText(f);
                if (text.Length == 0) continue;

                var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var sym in filesBySymbol.Keys)
                {
                    if (IsLikelyOwnDeclaration(sym, f, declaredByFile)) continue;
                    if (WordExists(text, sym))
                    {
                        foreach (var target in filesBySymbol[sym])
                        {
                            if (!string.Equals(target, f, StringComparison.OrdinalIgnoreCase))
                                referenced.Add(target);
                        }
                    }
                }

                if (referenced.Count > 0)
                    dependsOn[f] = referenced;
            }

            // invert: used-by map
            var usedBy = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var src in dependsOn.Keys)
            {
                foreach (var tgt in dependsOn[src])
                {
                    if (!usedBy.TryGetValue(tgt, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        usedBy[tgt] = set;
                    }
                    set.Add(src);
                }
            }

            // render
            var sb = new StringBuilder();
            sb.AppendLine(TagBuilder.Open(Tags.CrossReferences));

            foreach (var f in csFiles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var root = FindOwningRoot(folderPaths, f);
                var rel = MakeRelativeSafe(root, f);
                var name = Path.GetFileName(f);

                dependsOn.TryGetValue(f, out var deps);
                usedBy.TryGetValue(f, out var ub);

                var dependsCsv = deps is { Count: > 0 }
                    ? string.Join(",", deps.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                           .Select(x => TagBuilder.EscapeXmlAttribute(MakeRelativeSafe(root, x))))
                    : string.Empty;

                var usedByCsv = ub is { Count: > 0 }
                    ? string.Join(",", ub.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                         .Select(x => TagBuilder.EscapeXmlAttribute(MakeRelativeSafe(root, x))))
                    : string.Empty;

                var attrs = new List<string>
                {
                    TagBuilder.BuildFileNameTag(name),
                    TagBuilder.BuildFilePathTag(rel)
                };
                if (!string.IsNullOrEmpty(dependsCsv)) attrs.Add(TagBuilder.BuildDependsOnTag(dependsCsv));
                if (!string.IsNullOrEmpty(usedByCsv)) attrs.Add(TagBuilder.BuildUsedByTag(usedByCsv));

                sb.AppendLine(TagBuilder.SelfClosing(Tags.File, attrs.ToArray()));
            }

            sb.AppendLine(TagBuilder.Close(Tags.CrossReferences));
            return sb.ToString();
        }

        // two <codemetainfo> blocks for legacy consumers
        protected string GenerateCodeMetaInfoList(List<string> folderPaths)
        {
            if (folderPaths == null || folderPaths.Count == 0)
                return string.Empty;

            var config = VectorStoreConfig.FromAppConfig();

            var files = new List<string>();
            foreach (var root in folderPaths)
                foreach (var f in EnumerateFilesRespectingExclusions(root, config))
                    files.Add(f);

            // discover unit test roots
            var unitTestRoots = files
                .Select(Path.GetDirectoryName)
                .Where(p => !string.IsNullOrEmpty(p) && p!.IndexOf("UnitTests", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(p => p!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var inner = new StringBuilder();

            foreach (var f in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var text = SafeReadAllText(f);
                var ext = Path.GetExtension(f);
                var lang = ext.TrimStart('.'); // keep local, no external dep
                var root = FindOwningRoot(folderPaths, f);
                var rel = MakeRelativeSafe(root, f);
                var name = Path.GetFileName(f);
                var fi = new FileInfo(f);

                var size = fi.Exists ? fi.Length : 0L;
                var lastModified = fi.Exists ? fi.LastWriteTime : DateTime.MinValue;

                var loc = CountLines(text);
                var codeLines = CountCodeLines(text, ext);
                var classes = CountClasses(text, ext);
                var methods = CountMethods(text, ext);

                var complexity = EstimateComplexity(codeLines, methods, text);
                var patterns = DetectPatterns(text, ext).ToList();
                var longMethods = CountLongMethods(text, ext, 40);
                var todos = CountTodos(text);
                var catches = CountCatches(text);

                var (hasTests, tests) = FindTestsForFile(name, text, unitTestRoots, config);

                inner.AppendLine(TagBuilder.SelfClosing(
                    Tags.File,
                    TagBuilder.BuildFileNameTag(name),
                    TagBuilder.BuildFilePathTag(rel),
                    TagBuilder.BuildExtensionTag(ext),
                    TagBuilder.BuildLanguageTag(lang)
                ));

                inner.AppendLine(TagBuilder.SelfClosing(
                    "metrics",
                    TagBuilder.BuildSizeBytesTag(size),
                    TagBuilder.BuildLinesOfCodeTag(loc),
                    TagBuilder.BuildCodeLinesTag(codeLines),
                    TagBuilder.BuildClassesTag(classes),
                    TagBuilder.BuildMethodsTag(methods),
                    TagBuilder.BuildLastModifiedTag(lastModified)
                ));

                inner.AppendLine(TagBuilder.SelfClosing(
                    "analysis",
                    string.Format(Tags.Complexity, TagBuilder.EscapeXmlAttribute(complexity)),
                    string.Format(Tags.Patterns, TagBuilder.EscapeXmlAttribute(string.Join(",", patterns))),
                    string.Format(Tags.HasTests, hasTests.ToString().ToLowerInvariant()),
                    string.Format(Tags.Tests, TagBuilder.EscapeXmlAttribute(string.Join(",", tests)))
                ));

                inner.AppendLine(TagBuilder.SelfClosing(
                    "signals",
                    string.Format(Tags.LongMethods, longMethods),
                    string.Format(Tags.Todos, todos),
                    string.Format(Tags.Catches, catches)
                ));
            }

            var sb = new StringBuilder();
            sb.AppendLine(TagBuilder.Open(Tags.CodeMetaInfo));
            sb.Append(inner.ToString());
            sb.AppendLine(TagBuilder.Close(Tags.CodeMetaInfo));

            sb.AppendLine(TagBuilder.Open(Tags.CodeMetaInfo));
            sb.Append(inner.ToString());
            sb.AppendLine(TagBuilder.Close(Tags.CodeMetaInfo));

            return sb.ToString();
        }

        // --------------------------------------------------------------------------------
        // Traversal helpers used by DocX/MD/PDF
        // --------------------------------------------------------------------------------

        // Generic traversal used by DocXHandler/PdfHandler (6-arg overload)
        protected void ProcessFolder<T>(
            string folderPath,
            T context,
            VectorStoreConfig vectorStoreConfig,
            Action<string, T, VectorStoreConfig> processFile,
            Action<T, string> writeFolderName,
            Action<T>? writeFolderEnd = null)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            var folderName = new DirectoryInfo(folderPath).Name;
            if (IsFolderExcluded(folderName, vectorStoreConfig))
            {
                _log.Trace($"Skipping excluded folder: {folderPath}");
                return;
            }

            _ui?.UpdateStatus($"Processing folder: {folderPath}");
            _log.Debug($"Processing folder: {folderPath}");

            writeFolderName(context, folderName);

            string[] files = Array.Empty<string>();
            try { files = Directory.GetFiles(folderPath); } catch { /* ignore */ }

            foreach (var file in files)
            {
                try
                {
                    processFile(file, context, vectorStoreConfig);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Error processing file: {file}");
                    throw;
                }
            }

            string[] subfolders = Array.Empty<string>();
            try { subfolders = Directory.GetDirectories(folderPath); } catch { /* ignore */ }

            foreach (var sub in subfolders)
            {
                ProcessFolder(sub, context, vectorStoreConfig, processFile, writeFolderName, writeFolderEnd);
            }

            writeFolderEnd?.Invoke(context);
        }

        // Legacy compatibility overload used by some handlers/tests
        protected virtual void ProcessFile(string file, StreamWriter writer, VectorStoreConfig vectorStoreConfig)
        {
            // Default no-op; derived handlers override when using StreamWriter context
        }

        protected virtual void WriteFolderName(StreamWriter writer, string folderName)
        {
            // Default no-op; derived handlers override when using StreamWriter context
        }

        // --------------------------------------------------------------------------------
        // AI context + content helpers used by DocXHandler
        // --------------------------------------------------------------------------------

        protected virtual string GetEnhancedFileContent(string file)
        {
            // Default: plain content; derived can enrich
            return SafeReadAllText(file);
        }

        protected void AddAIOptimizedContext<T>(
            List<string> folderPaths,
            T context,
            Action<T, string> writeContent)
        {
            // Keep it simple: just emit TOC, XRefs, Meta as preface blocks
            var toc = GenerateTableOfContentsList(folderPaths);
            if (!string.IsNullOrWhiteSpace(toc))
                writeContent(context, toc);

            var xref = GenerateCrossReferencesList(folderPaths);
            if (!string.IsNullOrWhiteSpace(xref))
                writeContent(context, xref);

            var meta = GenerateCodeMetaInfoList(folderPaths);
            if (!string.IsNullOrWhiteSpace(meta))
                writeContent(context, meta);
        }

        // --------------------------------------------------------------------------------
        // Enumeration and exclusion helpers
        // --------------------------------------------------------------------------------

        protected IEnumerable<string> EnumerateFilesRespectingExclusions(string root, VectorStoreConfig config)
        {
            if (string.IsNullOrWhiteSpace(root))
                yield break;

            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var folderName = new DirectoryInfo(current).Name;

                if (IsFolderExcluded(folderName, config))
                    continue;

                string[] files = Array.Empty<string>();
                try { files = Directory.GetFiles(current); } catch { /* ignore */ }

                foreach (var f in files)
                {
                    var fileName = Path.GetFileName(f);
                    if (IsFileExcluded(fileName, config)) continue;
                    if (!IsFileValid(f, null)) continue;
                    yield return f;
                }

                string[] subfolders = Array.Empty<string>();
                try { subfolders = Directory.GetDirectories(current); } catch { /* ignore */ }

                foreach (var sub in subfolders)
                    stack.Push(sub);
            }
        }

        protected virtual bool IsFolderExcluded(string folderName, VectorStoreConfig config)
        {
            try { return config.IsFolderExcluded(folderName); }
            catch { return false; }
        }

        protected virtual bool IsFileExcluded(string fileName, VectorStoreConfig config)
        {
            try { return config.IsFileExcluded(fileName); }
            catch { return false; }
        }

        protected virtual bool IsFileValid(string path, string? outputPath)
        {
            if (!string.IsNullOrEmpty(outputPath) &&
                string.Equals(path, outputPath, StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var fi = new FileInfo(path);
                if (!fi.Exists) return false;
                if (fi.Length == 0) return false;

                // Quick binary-ish extension screening without external deps
                var ext = Path.GetExtension(path);
                var binExt = new[]
                {
                    ".dll", ".exe", ".pdb", ".obj", ".so", ".dylib", ".png", ".jpg", ".jpeg", ".gif", ".ico",
                    ".pdf", ".docx", ".xlsx", ".zip", ".7z", ".gz", ".tar"
                };
                if (binExt.Contains(ext, StringComparer.OrdinalIgnoreCase)) return false;

                return true;
            }
            catch { return false; }
        }

        // --------------------------------------------------------------------------------
        // Path/text helpers
        // --------------------------------------------------------------------------------

        protected static string SafeReadAllText(string path)
        {
            try { return File.ReadAllText(path); }
            catch { return string.Empty; }
        }

        protected static string SafeDirectoryName(string path)
        {
            try { return new DirectoryInfo(path).Name; }
            catch { return path ?? string.Empty; }
        }

        protected static string? FindOwningRoot(List<string> roots, string file)
        {
            string? best = null;
            foreach (var r in roots)
            {
                if (file.StartsWith(r, StringComparison.OrdinalIgnoreCase))
                {
                    if (best == null || r.Length > best.Length)
                        best = r;
                }
            }
            return best ?? Path.GetDirectoryName(file);
        }

        protected static string MakeRelativeSafe(string? root, string full)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
                    return Path.GetRelativePath(root!, full);
            }
            catch { /* ignore */ }
            return full;
        }

        // --------------------------------------------------------------------------------
        // Symbol/analysis helpers (heuristic)
        // --------------------------------------------------------------------------------

        protected static HashSet<string> ExtractDeclaredSymbolsFromCSharp(string filePath)
        {
            var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var text = SafeReadAllText(filePath);
            if (text.Length == 0) return symbols;

            var typeRegex = new Regex(@"\b(class|struct|interface|enum)\s+(?<id>[A-Za-z_][A-Za-z0-9_]*)\b", RegexOptions.Multiline);
            foreach (Match m in typeRegex.Matches(text))
                symbols.Add(m.Groups["id"].Value);

            var methodRegex = new Regex(@"\b(public|internal|protected|private)\s+[\w\<\>\[\],\s]+\s+(?<id>[A-Za-z_][A-Za-z0-9_]*)\s*\(", RegexOptions.Multiline);
            foreach (Match m in methodRegex.Matches(text))
                symbols.Add(m.Groups["id"].Value);

            return symbols;
        }

        protected static bool IsLikelyOwnDeclaration(string symbol, string file, Dictionary<string, HashSet<string>> declaredByFile)
        {
            return declaredByFile.TryGetValue(file, out var set) && set.Contains(symbol);
        }

        protected static bool WordExists(string text, string symbol)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(symbol))
                return false;

            var pattern = $@"\b{Regex.Escape(symbol)}\b";
            return Regex.IsMatch(text, pattern);
        }

        // --------------------------------------------------------------------------------
        // Metrics/heuristics
        // --------------------------------------------------------------------------------

        protected static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var lines = 1;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n') lines++;
            return lines;
        }

        protected static int CountCodeLines(string text, string ext)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var lines = text.Split('\n');
            return lines.Count(l =>
            {
                var s = l.Trim();
                if (s.Length == 0) return false;
                if (s.StartsWith("//")) return false;
                if (s.StartsWith("/*") || s.StartsWith("*") || s.StartsWith("*/")) return false;
                return true;
            });
        }

        protected static int CountClasses(string text, string ext)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return Regex.Matches(text, @"\bclass\b").Count;
        }

        protected static int CountMethods(string text, string ext)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return Regex.Matches(text, @"\b[A-Za-z_][A-Za-z0-9_]*\s*\([^;]*\)\s*\{").Count;
        }

        protected static string EstimateComplexity(int codeLines, int methods, string text)
        {
            var score = codeLines / 200.0 + methods / 20.0;
            if (score < 1.0) return "Low";
            if (score < 2.0) return "Medium";
            return "High";
        }

        protected static IEnumerable<string> DetectPatterns(string text, string ext)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var hits = new List<string>();
            if (text.Contains("IDisposable", StringComparison.Ordinal)) hits.Add("DisposePattern");
            if (text.Contains("IOptions<", StringComparison.Ordinal)) hits.Add("Options");
            if (text.Contains("ILogger", StringComparison.Ordinal) || text.Contains("NLog", StringComparison.Ordinal)) hits.Add("Logging");
            if (text.Contains("async ", StringComparison.Ordinal)) hits.Add("Async");
            if (text.Contains("IServiceCollection", StringComparison.Ordinal)) hits.Add("DependencyInjection");
            if (text.Contains("SOLID", StringComparison.Ordinal)) hits.Add("SOLID");
            return hits;
        }

        protected static int CountLongMethods(string text, string ext, int thresholdLines)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var methodMatches = Regex.Matches(text, @"\b[A-Za-z_][A-Za-z0-9_]*\s*\([^;]*\)\s*\{(?<body>[\s\S]*?)\}");
            var count = 0;
            foreach (Match m in methodMatches)
            {
                var body = m.Groups["body"].Value;
                var lines = CountLines(body);
                if (lines >= thresholdLines) count++;
            }
            return count;
        }

        protected static int CountTodos(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return Regex.Matches(text, @"\bTODO\b", RegexOptions.IgnoreCase).Count;
        }

        protected static int CountCatches(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return Regex.Matches(text, @"\bcatch\b").Count;
        }

        // --------------------------------------------------------------------------------
        // Test linkage (heuristic)
        // --------------------------------------------------------------------------------

        protected static (bool hasTests, List<string> tests) FindTestsForFile(
            string fileName,
            string text,
            List<string> unitTestRoots,
            VectorStoreConfig config)
        {
            var stem = Path.GetFileNameWithoutExtension(fileName);
            var list = new List<string>();

            foreach (var root in unitTestRoots)
            {
                try
                {
                    foreach (var f in Directory.EnumerateFiles(root!, "*.cs", SearchOption.AllDirectories))
                    {
                        var name = Path.GetFileName(f);
                        if (name.IndexOf(stem, StringComparison.OrdinalIgnoreCase) >= 0 &&
                            name.IndexOf("Test", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            list.Add(name);
                        }
                    }
                }
                catch { /* ignore */ }
            }

            list = list.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            return (list.Count > 0, list);
        }
    }
}
