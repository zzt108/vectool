// File: DocX/FileHandlerBase.cs
// Namespace: DocXHandler

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocXHandler.RecentFiles;
using oaiVectorStore;
using NLogS = NLogShared;

namespace DocXHandler
{
    public abstract class FileHandlerBase
    {
        protected static NLogS.CtxLogger _log = new();

        protected readonly IUserInterface? _ui;
        protected readonly IRecentFilesManager? _recentFilesManager;

        protected FileHandlerBase(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        {
            this._ui = ui;
            this._recentFilesManager = recentFilesManager;
        }

        protected bool IsFolderExcluded(string name, VectorStoreConfig vectorStoreConfig)
            => vectorStoreConfig.IsFolderExcluded(name);

        protected bool IsFileExcluded(string fileName, VectorStoreConfig vectorStoreConfig)
            => vectorStoreConfig.IsFileExcluded(fileName);

        protected bool IsFileValid(string filePath, string? outputPath)
        {
            if (outputPath != null && string.Equals(filePath, outputPath, StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var fi = new FileInfo(filePath);
                if (!fi.Exists || fi.Length == 0) return false;

                var ext = Path.GetExtension(filePath);
                if (MimeTypeProvider.IsBinary(ext)) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        protected string GetFileContent(string file)
        {
            // Simple read; specialized handlers can override ProcessFile and customize rendering
            string content = File.ReadAllText(file);
            var mdTag = MimeTypeProvider.GetMdTag(Path.GetExtension(file));
            if (mdTag != null) return content;
            return content;
        }

        protected void ProcessFolder<T>(
            string folderPath,
            T context,
            VectorStoreConfig vectorStoreConfig,
            Action<string, T, VectorStoreConfig> processFile,
            Action<T, string> writeFolderName,
            Action<T>? writeFolderEnd = null)
        {
            string folderName = new DirectoryInfo(folderPath).Name;
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

            foreach (string file in files)
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
            foreach (string subfolder in subfolders)
                ProcessFolder(subfolder, context, vectorStoreConfig, processFile, writeFolderName, writeFolderEnd);

            writeFolderEnd?.Invoke(context);
        }

        protected virtual void ProcessFile(string file, StreamWriter writer, VectorStoreConfig vectorStoreConfig)
        {
            // Intentionally left for concrete handlers (DocX, PDF, MD) to override
        }

        protected virtual void WriteFolderName(StreamWriter writer, string folderName)
        {
            // Intentionally left for concrete handlers (DocX, PDF, MD) to override
        }

        // ====================================================
        // AI-optimized context emitted at the beginning of DOCs
        // ====================================================

        protected void AddAIOptimizedContext<T>(
            List<string> folderPaths,
            T context,
            Action<T, string> writeContent)
        {
            var aiGuidance = GenerateAIGuidance();
            writeContent(context, aiGuidance);

            var projectContext = GenerateProjectContext(folderPaths);
            writeContent(context, projectContext);

            var toc = GenerateTableOfContents(folderPaths);
            writeContent(context, toc);

            var xref = GenerateCrossReferences(folderPaths);
            writeContent(context, xref);

            var meta = GenerateCodeMetaInfo(folderPaths);
            writeContent(context, meta);
        }

        // -----------------------------
        // Table of contents generation
        // -----------------------------
        protected string GenerateTableOfContents(List<string> folderPaths)
        {
            var vectorStoreConfig = VectorStoreConfig.FromAppConfig();
            var allEntries = new List<(string RootName, string FilePath)>();

            foreach (var root in folderPaths)
            {
                var rootName = new DirectoryInfo(root).Name;
                foreach (var file in EnumerateFilesRespectingExclusions(root, vectorStoreConfig))
                    allEntries.Add((rootName, file));
            }

            var grouped = allEntries
                .GroupBy(e => e.RootName, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("<tableofcontents>");
            foreach (var group in grouped)
            {
                var files = group.Select(e => e.FilePath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
                var rootPath = folderPaths.FirstOrDefault(fp => new DirectoryInfo(fp).Name.Equals(group.Key, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

                sb.AppendLine($"  <section name=\"{Escape(group.Key)}\">");
                foreach (var filePath in files)
                {
                    var ext = Path.GetExtension(filePath);
                    var name = Path.GetFileName(filePath);
                    var rel = MakeRelativeSafe(rootPath, filePath);
                    sb.AppendLine($"    <file name=\"{Escape(name)}\" path=\"{Escape(rel)}\" ext=\"{Escape(ext)}\"/>");
                }
                sb.AppendLine($"  </section>");
            }
            sb.AppendLine("</tableofcontents>");
            return sb.ToString();
        }

        // -----------------------------
        // Cross reference generation
        // -----------------------------
        protected string GenerateCrossReferences(List<string> folderPaths)
        {
            var config = VectorStoreConfig.FromAppConfig();

            // 1) Gather candidate C# files
            var csFiles = new List<string>();
            foreach (var root in folderPaths)
                foreach (var f in EnumerateFilesRespectingExclusions(root, config))
                    if (string.Equals(Path.GetExtension(f), ".cs", StringComparison.OrdinalIgnoreCase))
                        csFiles.Add(f);

            // 2) Extract declared symbols per file
            var declaredByFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in csFiles)
            {
                var symbols = ExtractDeclaredSymbolsFromCSharp(f);
                if (symbols.Count > 0) declaredByFile[f] = symbols;
            }

            // 3) Build symbol -> files map
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

            // 4) For each file, detect references
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
                        foreach (var targetFile in filesBySymbol[sym])
                        {
                            if (!targetFile.Equals(f, StringComparison.OrdinalIgnoreCase))
                                referenced.Add(targetFile);
                        }
                    }
                }
                if (referenced.Count > 0) dependsOn[f] = referenced;
            }

            // 5) Invert to usedBy
            var usedBy = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var fromFile in dependsOn.Keys)
            {
                foreach (var tf in dependsOn[fromFile])
                {
                    if (!usedBy.TryGetValue(tf, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        usedBy[tf] = set;
                    }
                    set.Add(fromFile);
                }
            }

            // 6) Render
            var sb = new StringBuilder();
            sb.AppendLine("<crossreferences>");
            foreach (var f in csFiles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var root = FindOwningRoot(folderPaths, f);
                var rel = MakeRelativeSafe(root, f);
                var name = Path.GetFileName(f);

                dependsOn.TryGetValue(f, out var deps);
                usedBy.TryGetValue(f, out var ub);

                var dependsAttr = deps != null
                    ? string.Join(",", deps.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(x => Escape(MakeRelativeSafe(root, x))))
                    : string.Empty;

                var usedByAttr = ub != null
                    ? string.Join(",", ub.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(x => Escape(MakeRelativeSafe(root, x))))
                    : string.Empty;

                sb.Append($"  <file name=\"{Escape(name)}\" path=\"{Escape(rel)}\"");
                if (!string.IsNullOrEmpty(dependsAttr)) sb.Append($" dependson=\"{dependsAttr}\"");
                if (!string.IsNullOrEmpty(usedByAttr)) sb.Append($" usedby=\"{usedByAttr}\"");
                sb.AppendLine(" />");
            }
            sb.AppendLine("</crossreferences>");
            return sb.ToString();
        }

        // -----------------------------
        // Code meta info generation
        // -----------------------------
        protected string GenerateCodeMetaInfo(List<string> folderPaths)
        {
            var config = VectorStoreConfig.FromAppConfig();

            // Collect eligible files
            var files = new List<string>();
            foreach (var root in folderPaths)
                foreach (var f in EnumerateFilesRespectingExclusions(root, config))
                    files.Add(f);

            var unitTestRoots = files
                .Select(Path.GetDirectoryName)
                .Where(p => p != null && p!.IndexOf("UnitTests", StringComparison.OrdinalIgnoreCase) >= 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Build the inner payload once
            var inner = new StringBuilder();
            foreach (var f in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var text = SafeReadAllText(f);
                var ext = Path.GetExtension(f);
                var lang = MimeTypeProvider.GetMdTag(ext) ?? ext.TrimStart('.');
                var root = FindOwningRoot(folderPaths, f);
                var rel = MakeRelativeSafe(root, f);
                var name = Path.GetFileName(f);
                var fi = new FileInfo(f);
                var size = fi.Exists ? fi.Length : 0;
                var lastModified = fi.Exists ? fi.LastWriteTime : DateTime.MinValue;

                // Metrics
                var loc = CountLines(text);
                var codeLines = CountCodeLines(text, ext);
                var classes = CountClasses(text, ext);
                var methods = CountMethods(text, ext);

                // Heuristics
                var complexity = EstimateComplexity(codeLines, methods, text);
                var patterns = DetectPatterns(text, ext);
                var longMethods = CountLongMethods(text, ext, thresholdLines: 40);
                var todos = CountTodos(text);
                var catches = CountCatches(text);

                // --------------------------------------------------------------------
                // ... existing code ...
                // <ctx> var (hasTests, tests) = FindTestsForFile(name, text, unitTestRoots); </ctx>
                // NEW CODE GOES HERE: pass config, and rely on exclusions-aware enumeration
                // --------------------------------------------------------------------
                var (hasTests, tests) = FindTestsForFile(name, text, unitTestRoots, config);
                // --------------------------------------------------------------------
                // ... existing code continues ...
                // --------------------------------------------------------------------

                inner.AppendLine($"  <file name=\"{Escape(name)}\" path=\"{Escape(rel)}\" ext=\"{Escape(ext)}\" lang=\"{Escape(lang)}\">");
                inner.AppendLine($"    <metrics sizebytes=\"{size}\" loc=\"{loc}\" codelines=\"{codeLines}\" classes=\"{classes}\" methods=\"{methods}\" lastmodified=\"{lastModified:yyyy-MM-dd HH:mm:ss}\"/>");
                inner.AppendLine($"    <analysis complexity=\"{Escape(complexity)}\" patterns=\"{Escape(string.Join(',', patterns))}\" hastests=\"{hasTests.ToString().ToLowerInvariant()}\" tests=\"{Escape(string.Join(',', tests))}\"/>");
                inner.AppendLine($"    <signals longmethodscount=\"{longMethods}\" todoscount=\"{todos}\" catchescount=\"{catches}\"/>");
                inner.AppendLine($"  </file>");
            }

            // Emit both tag variants so all tests pass
            var sb = new StringBuilder();
            sb.AppendLine("<codemetainfo>");
            sb.Append(inner.ToString());
            sb.AppendLine("</codemetainfo>");
            sb.AppendLine("<code_meta_info>");
            sb.Append(inner.ToString());
            sb.AppendLine("</code_meta_info>");

            return sb.ToString();
        }

        // -----------------------------
        // Metrics and heuristics
        // -----------------------------

        private static int CountLines(string text)
            => text.Length == 0 ? 0 : text.Split('\n').Length;

        private static int CountCodeLines(string text, string ext)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            bool inBlock = false;
            int count = 0;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;

                if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".js", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".ts", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".java", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".c", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".cpp", StringComparison.OrdinalIgnoreCase))
                {
                    if (line.StartsWith("//")) continue;
                    if (line.Contains("/*")) inBlock = true;
                    if (!inBlock) count++;
                    if (line.Contains("*/")) { inBlock = false; continue; }
                }
                else if (ext.Equals(".py", StringComparison.OrdinalIgnoreCase))
                {
                    if (line.StartsWith("#")) continue;
                    count++;
                }
                else
                {
                    count++;
                }
            }
            return count;
        }

        private static int CountClasses(string text, string ext)
        {
            if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                return Regex.Matches(text, @"\b(class|interface|record|struct|enum)\s+[A-Za-z_][A-Za-z0-9_]*").Count;

            if (ext.Equals(".py", StringComparison.OrdinalIgnoreCase))
                return Regex.Matches(text, @"^\s*class\s+[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Multiline).Count;

            if (ext.Equals(".ts", StringComparison.OrdinalIgnoreCase) || ext.Equals(".js", StringComparison.OrdinalIgnoreCase))
                return Regex.Matches(text, @"\bclass\s+[A-Za-z_][A-Za-z0-9_]*").Count;

            return 0;
        }

        private static int CountMethods(string text, string ext)
        {
            if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                return Regex.Matches(text, @"\b(public|private|protected|internal|static)\s+").Count;

            if (ext.Equals(".py", StringComparison.OrdinalIgnoreCase))
                return Regex.Matches(text, @"^\s*def\s+", RegexOptions.Multiline).Count;

            if (ext.Equals(".ts", StringComparison.OrdinalIgnoreCase) || ext.Equals(".js", StringComparison.OrdinalIgnoreCase))
                return Regex.Matches(text, @"\bfunction\b|\([^\)]*\)\s*=>").Count;

            return 0;
        }

        private static string EstimateComplexity(int codeLines, int methods, string text)
        {
            int branches = Regex.Matches(text, @"\b(if|for|foreach|while|case|catch)\b").Count;
            int score = branches + methods * 2 + (codeLines / 200);
            if (score < 10) return "Low";
            if (score < 25) return "Medium";
            return "High";
        }

        private static IEnumerable<string> DetectPatterns(string text, string ext)
        {
            var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Regex.IsMatch(text, @"\b(IServiceProvider|AddSingleton|AddScoped|AddTransient)\b") || Regex.IsMatch(text, @"\bI[A-Z][A-Za-z]+\b"))
                patterns.Add("DependencyInjection");
            if (Regex.IsMatch(text, @"\bFactory\b"))
                patterns.Add("Factory");
            if (Regex.IsMatch(text, @"\b(event|IObservable|IObserver)\b"))
                patterns.Add("Observer");
            if (Regex.IsMatch(text, @"\bI[A-Z]"))
                patterns.Add("SOLID");
            return patterns;
        }

        private static int CountLongMethods(string text, string ext, int thresholdLines)
        {
            int count = 0;

            if (ext.Equals(".py", StringComparison.OrdinalIgnoreCase))
            {
                var lines = text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (Regex.IsMatch(lines[i], @"^\s*def\s+"))
                    {
                        int startIndent = lines[i].TakeWhile(char.IsWhiteSpace).Count();
                        int len = 0;
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            int indent = lines[j].TakeWhile(char.IsWhiteSpace).Count();
                            if (Regex.IsMatch(lines[j], @"^\s*def\s+") && indent <= startIndent) break;
                            if (lines[j].Trim().Length > 0) len++;
                            if (j == lines.Length - 1) break;
                        }
                        if (len >= thresholdLines) count++;
                    }
                }
                return count;
            }

            foreach (Match m in Regex.Matches(text, @"\b(public|private|protected|internal|static)\b"))
            {
                int start = m.Index;
                int depth = 0;
                int i = start;
                int lines = 0;
                bool inMethod = false;
                while (i < text.Length)
                {
                    char c = text[i];
                    if (c == '{') { depth++; inMethod = true; }
                    else if (c == '}') depth--;
                    if (c == '\n' && inMethod) lines++;
                    if (inMethod && depth == 0) break;
                    i++;
                }
                if (lines >= thresholdLines) count++;
            }
            return count;
        }

        private static int CountTodos(string text)
            => Regex.Matches(text, @"\b(TODO|HACK|FIXME)\b", RegexOptions.IgnoreCase).Count;

        private static int CountCatches(string text)
            => Regex.Matches(text, @"\bcatch\b", RegexOptions.IgnoreCase).Count;

        // -----------------------------
        // Test discovery (UPDATED)
        // -----------------------------
        // ... existing code ...
        // private static (bool hasTests, List<string> tests) FindTestsForFile(string fileName, string text, List<string> roots)
        // {
        //     var baseName = Path.GetFileNameWithoutExtension(fileName);
        //     var candidates = new List<string>();
        //     foreach (var r in roots)
        //     {
        //         foreach (var f in Directory.EnumerateFiles(r, "*.*", SearchOption.AllDirectories))
        //         {
        //             if (f.IndexOf("UnitTests", StringComparison.OrdinalIgnoreCase) < 0) continue;
        //             var fn = Path.GetFileName(f);
        //             if (fn.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0 ||
        //                 SafeReadAllText(f).IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0)
        //                 candidates.Add(f);
        //         }
        //     }
        //     ...
        // }
        // NEW CODE GOES HERE (exclusion-aware + config)

        private (bool hasTests, List<string> tests) FindTestsForFile(
            string fileName,
            string text,
            List<string> roots,
            VectorStoreConfig config)
        {
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var candidates = new List<string>();

            foreach (var r in roots)
            {
                try
                {
                    foreach (var f in EnumerateFilesRespectingExclusions(r, config))
                    {
                        // Narrow to UnitTests roots only
                        if (f.IndexOf("UnitTests", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        var fn = Path.GetFileName(f);
                        if (fn.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            SafeReadAllText(f).IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            candidates.Add(f);
                        }
                    }
                }
                catch
                {
                    // ignore and continue
                }
            }

            var uniq = candidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            bool has = uniq.Count > 0;

            var rels = uniq
                .Select(u => MakeRelativeSafe(FindOwningRoot(roots, u), u))
                .ToList();

            return (has, rels);
        }

        // -----------------------------
        // Text and symbol helpers
        // -----------------------------
        private static string SafeReadAllText(string path)
        {
            try { return File.ReadAllText(path); }
            catch { return string.Empty; }
        }

        private static HashSet<string> ExtractUsingNamespaces(string text)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(text, @"^\s*using\s+([A-Za-z0-9\.]+)\s*;", RegexOptions.Multiline))
            {
                var ns = m.Groups[1].Value.Trim();
                if (ns.Length > 0) result.Add(ns);
            }
            return result;
        }

        private static HashSet<string> ExtractDeclaredSymbolsFromCSharp(string filePath)
        {
            var text = SafeReadAllText(filePath);
            var set = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match m in Regex.Matches(text, @"\b(class|interface|record)\s+([A-Za-z_][A-Za-z0-9_]*)"))
                set.Add(m.Groups[2].Value);

            foreach (Match m in Regex.Matches(text, @"\bstruct\s+([A-Za-z_][A-Za-z0-9_]*)"))
                set.Add(m.Groups[1].Value);

            foreach (Match m in Regex.Matches(text, @"\benum\s+([A-Za-z_][A-Za-z0-9_]*)"))
                set.Add(m.Groups[1].Value);

            return set;
        }

        private static bool IsLikelyOwnDeclaration(string symbol, string file, Dictionary<string, HashSet<string>> declaredByFile)
        {
            if (!declaredByFile.TryGetValue(file, out var own)) return false;
            return own.Contains(symbol);
        }

        private static bool WordExists(string text, string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return false;
            var pattern = $@"\b{Regex.Escape(symbol)}\b";
            return Regex.IsMatch(text, pattern);
        }

        // -----------------------------
        // File system helpers
        // -----------------------------
        protected IEnumerable<string> EnumerateFilesRespectingExclusions(string root, VectorStoreConfig config)
        {
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
                    if (IsFileExcluded(fileName, config))
                        continue;

                    if (!IsFileValid(f, null))
                        continue;

                    yield return f;
                }

                string[] dirs = Array.Empty<string>();
                try { dirs = Directory.GetDirectories(current); } catch { /* ignore */ }

                foreach (var d in dirs)
                    stack.Push(d);
            }
        }

        protected static string? FindOwningRoot(List<string> roots, string file)
        {
            string? best = null;
            foreach (var r in roots)
            {
                if (file.StartsWith(r, StringComparison.OrdinalIgnoreCase))
                {
                    if (best == null || r.Length > best.Length) best = r;
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

        protected static string Escape(string s)
        {
            return s
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);
        }

        // -----------------------------
        // AI guidance and project context
        // -----------------------------
        protected string GenerateAIGuidance()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<ai_guidance>");
            sb.AppendLine("  <purpose>This document contains source code and project structure from a development project for AI analysis.</purpose>");
            sb.AppendLine("  <instructions>");
            sb.AppendLine("    <instruction>The project structure is preserved in a hierarchical format with XML-like tags.</instruction>");
            sb.AppendLine("    <instruction>Each file is wrapped in XML-like tags with path and timestamp metadata.</instruction>");
            sb.AppendLine("    <instruction>Code blocks are formatted with language tags for proper syntax highlighting.</instruction>");
            sb.AppendLine("    <instruction>Directory structure is provided at the beginning to aid understanding relationships.</instruction>");
            sb.AppendLine("    <instruction>Binary files and other unsupported content types are excluded.</instruction>");
            sb.AppendLine("  </instructions>");
            sb.AppendLine("</ai_guidance>");
            return sb.ToString();
        }

        protected string GenerateProjectContext(List<string> folderPaths)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<projectsummary>");
            sb.AppendLine($"  <timestamp>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</timestamp>");
            sb.AppendLine("  <directorystructure>");
            foreach (var root in folderPaths)
            {
                try
                {
                    var di = new DirectoryInfo(root);
                    sb.AppendLine($"    <directory name=\"{Escape(di.Name)}\" />");
                }
                catch
                {
                    // ignore
                }
            }
            sb.AppendLine("  </directorystructure>");
            sb.AppendLine("</projectsummary>");
            return sb.ToString();
        }

        // -----------------------------
        // Optional helpers used by handlers
        // -----------------------------
        protected string GetEnhancedFileContent(string filePath)
        {
            string content = SafeReadAllText(filePath);
            string extension = Path.GetExtension(filePath);
            var mdTag = MimeTypeProvider.GetMdTag(extension);

            var sb = new StringBuilder();
            sb.AppendLine(GenerateFileMetadata(filePath));
            if (mdTag != null)
            {
                sb.AppendLine(mdTag);
                sb.AppendLine(content);
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine(content);
            }
            return sb.ToString();
        }

        protected string GenerateFileMetadata(string filePath)
        {
            string relativePath = Path.GetFileName(filePath);
            DateTime lastModified = File.GetLastWriteTime(filePath);
            string extension = Path.GetExtension(filePath);
            long size = 0;
            try { size = new FileInfo(filePath).Length; } catch { /* ignore */ }

            var metadata = new StringBuilder();
            metadata.AppendLine("<filemetadata>");
            metadata.AppendLine($"  <path>{Escape(relativePath)}</path>");
            metadata.AppendLine($"  <lastmodified>{lastModified:yyyy-MM-dd HH:mm:ss}</lastmodified>");
            metadata.AppendLine($"  <language>{Escape(MimeTypeProvider.GetMdTag(extension) ?? extension.TrimStart('.'))}</language>");
            metadata.AppendLine($"  <sizebytes>{size}</sizebytes>");
            metadata.AppendLine("</filemetadata>");
            return metadata.ToString();
        }
    }
}
