// DocX/FileHandlerBase.cs
using DocXHandler.RecentFiles;
using oaiVectorStore;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
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
            _ui = ui;
            _recentFilesManager = recentFilesManager;
        }

        protected bool IsFolderExcluded(string name, VectorStoreConfig vectorStoreConfig)
        {
            return vectorStoreConfig.IsFolderExcluded(name);
        }

        protected bool IsFileExcluded(string fileName, VectorStoreConfig vectorStoreConfig)
        {
            return vectorStoreConfig.IsFileExcluded(fileName);
        }

        protected bool IsFileValid(string filePath, string? outputPath)
        {
            if (outputPath != null && filePath == outputPath) return false;
            try
            {
                var fi = new FileInfo(filePath);
                if (fi.Length == 0) return false;
                var ext = Path.GetExtension(filePath);
                if (MimeTypeProvider.IsBinary(ext)) return false;
                return true;
            }
            catch { return false; }
        }

        protected string GetFileContent(string file)
        {
            string content = File.ReadAllText(file);
            var mdTag = MimeTypeProvider.GetMdTag(Path.GetExtension(file));
            if (mdTag != null) content = $"``````";
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
                try { processFile(file, context, vectorStoreConfig); }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Error processing file: {file}");
                    throw;
                }
            }

            string[] subfolders = Array.Empty<string>();
            try { subfolders = Directory.GetDirectories(folderPath); } catch { /* ignore */ }

            foreach (string subfolder in subfolders)
            {
                ProcessFolder(subfolder, context, vectorStoreConfig, processFile, writeFolderName, writeFolderEnd);
            }

            writeFolderEnd?.Invoke(context);
        }

        protected virtual void ProcessFile(string file, StreamWriter writer, VectorStoreConfig vectorStoreConfig) { }
        protected virtual void WriteFolderName(StreamWriter writer, string folderName) { }

        #region AI-Optimized Context and Metadata

        /// <summary>
        /// Adds project summary, AI guidance, a generated table of contents, and cross references to the beginning of output.
        /// </summary>
        protected void AddAIOptimizedContext<T>(List<string> folderPaths, T context, Action<T, string> writeContent)
        {
            // Generate AI guidance
            string aiGuidance = GenerateAIGuidance();
            writeContent(context, aiGuidance);

            // Generate project context
            string projectContext = GenerateProjectContext(folderPaths);
            writeContent(context, projectContext);

            // Generate table of contents (grouped by top-level folder)
            string toc = GenerateTableOfContents(folderPaths);
            writeContent(context, toc);

            // Generate cross references (symbol dependencies map)
            string xref = GenerateCrossReferences(folderPaths);
            writeContent(context, xref);
        }

        /// <summary>
        /// Generates a table-of-contents from the selected folders, honoring exclusion rules and skipping binaries.
        /// </summary>
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
            sb.AppendLine("<table_of_contents>");
            foreach (var group in grouped)
            {
                var files = group.Select(e => e.FilePath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
                var rootPath = folderPaths.FirstOrDefault(fp => new DirectoryInfo(fp).Name.Equals(group.Key, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

                sb.AppendLine($"  <section name=\"{Escape(group.Key)}\" files=\"{files.Count}\" path=\"{Escape(rootPath)}\">");
                foreach (var filePath in files)
                {
                    var ext = Path.GetExtension(filePath);
                    var name = Path.GetFileName(filePath);
                    var rel = MakeRelativeSafe(rootPath, filePath);
                    sb.AppendLine($"    <file name=\"{Escape(name)}\" path=\"{Escape(rel)}\" ext=\"{Escape(ext)}\" />");
                }
                sb.AppendLine("  </section>");
            }
            sb.AppendLine("</table_of_contents>");
            return sb.ToString();
        }

        /// <summary>
        /// Generates cross-references between files based on declared and referenced C# symbols.
        /// </summary>
        protected string GenerateCrossReferences(List<string> folderPaths)
        {
            var config = VectorStoreConfig.FromAppConfig();

            // 1) Gather candidate C# files
            var csFiles = new List<string>();
            foreach (var root in folderPaths)
            {
                foreach (var f in EnumerateFilesRespectingExclusions(root, config))
                {
                    if (string.Equals(Path.GetExtension(f), ".cs", StringComparison.OrdinalIgnoreCase))
                        csFiles.Add(f);
                }
            }

            // 2) Extract declared symbols per file
            var declaredByFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in csFiles)
            {
                var symbols = ExtractDeclaredSymbolsFromCSharp(f);
                if (symbols.Count > 0)
                    declaredByFile[f] = symbols;
            }

            // 3) Build a symbol -> files map
            var filesBySymbol = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
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

            // 4) For each file, detect symbol references to other files
            var dependsOn = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in csFiles)
            {
                var text = SafeReadAllText(f);
                if (text.Length == 0) continue;

                // Optimize candidates by "using" and simple heuristics
                var usingNamespaces = ExtractUsingNamespaces(text);

                var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Rough symbol reference scan: whole word match for each declared symbol
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

                if (referenced.Count > 0)
                    dependsOn[f] = referenced;
            }

            // 5) Invert map to get used_by
            var usedBy = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (fromFile, targets) in dependsOn)
            {
                foreach (var tf in targets)
                {
                    if (!usedBy.TryGetValue(tf, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        usedBy[tf] = set;
                    }
                    set.Add(fromFile);
                }
            }

            // 6) Render XML-like output
            var sb = new StringBuilder();
            sb.AppendLine("<cross_references>");
            foreach (var f in csFiles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var root = FindOwningRoot(folderPaths, f);
                var rel = MakeRelativeSafe(root, f);
                var name = Path.GetFileName(f);

                dependsOn.TryGetValue(f, out var deps);
                usedBy.TryGetValue(f, out var ub);

                var dependsAttr = deps != null ? string.Join(',', deps.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(x => Escape(MakeRelativeSafe(root, x)))) : "";
                var usedByAttr = ub != null ? string.Join(',', ub.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(x => Escape(MakeRelativeSafe(root, x)))) : "";

                sb.Append("  <file");
                sb.Append($" name=\"{Escape(name)}\"");
                sb.Append($" path=\"{Escape(rel)}\"");

                if (!string.IsNullOrEmpty(dependsAttr))
                    sb.Append($" depends_on=\"{dependsAttr}\"");

                if (!string.IsNullOrEmpty(usedByAttr))
                    sb.Append($" used_by=\"{usedByAttr}\"");

                sb.AppendLine(" />");
            }
            sb.AppendLine("</cross_references>");
            return sb.ToString();
        }

        private static string FindOwningRoot(List<string> roots, string file)
        {
            foreach (var r in roots)
            {
                if (file.StartsWith(r, StringComparison.OrdinalIgnoreCase)) return r;
            }
            return Path.GetDirectoryName(file) ?? "";
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

        private static string SafeReadAllText(string path)
        {
            try { return File.ReadAllText(path); } catch { return string.Empty; }
        }

        private static HashSet<string> ExtractUsingNamespaces(string text)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(text, @"^\s*using\s+([A-Za-z0-9_\.]+)\s*;", RegexOptions.Multiline))
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

            // class / interface / record names
            foreach (Match m in Regex.Matches(text, @"\b(class|interface|record)\s+([A-Za-z_][A-Za-z0-9_]*)\b"))
            {
                set.Add(m.Groups[2].Value);
            }

            // structs too, because why not
            foreach (Match m in Regex.Matches(text, @"\bstruct\s+([A-Za-z_][A-Za-z0-9_]*)\b"))
            {
                set.Add(m.Groups[1].Value);
            }

            // public enum
            foreach (Match m in Regex.Matches(text, @"\benum\s+([A-Za-z_][A-Za-z0-9_]*)\b"))
            {
                set.Add(m.Groups[1].Value);
            }

            return set;
        }

        private IEnumerable<string> EnumerateFilesRespectingExclusions(string root, VectorStoreConfig config)
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
                string[] dirs = Array.Empty<string>();

                try { files = Directory.GetFiles(current); } catch { /* ignore */ }
                foreach (var f in files)
                {
                    var fileName = Path.GetFileName(f);
                    if (IsFileExcluded(fileName, config)) continue;
                    if (!IsFileValid(f, null)) continue;
                    yield return f;
                }

                try { dirs = Directory.GetDirectories(current); } catch { /* ignore */ }
                foreach (var d in dirs) stack.Push(d);
            }
        }

        private static string MakeRelativeSafe(string? root, string full)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
                    return Path.GetRelativePath(root, full);
            }
            catch { /* ignore */ }
            return full;
        }

        private static string Escape(string s)
        {
            return s
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);
        }

        protected string GenerateAIGuidance()
        {
            var guidance = new StringBuilder();
            guidance.AppendLine("<ai_guidance>");
            guidance.AppendLine("  <purpose>This document contains source code and project structure from a development project for AI analysis.</purpose>");
            guidance.AppendLine("  <instructions>");
            guidance.AppendLine("    <instruction>The project structure is preserved in a hierarchical format with XML-like tags.</instruction>");
            guidance.AppendLine("    <instruction>Each file is wrapped in XML-like tags with path and timestamp metadata.</instruction>");
            guidance.AppendLine("    <instruction>Code blocks are formatted with language tags for proper syntax highlighting.</instruction>");
            guidance.AppendLine("    <instruction>Directory structure is provided at the beginning to aid understanding relationships.</instruction>");
            guidance.AppendLine("    <instruction>Binary files and other unsupported content types are excluded.</instruction>");
            guidance.AppendLine("  </instructions>");
            guidance.AppendLine("  <suggested_analysis>");
            guidance.AppendLine("    <task>Analyze the code architecture and design patterns</task>");
            guidance.AppendLine("    <task>Identify potential improvements, technical debt, or bugs</task>");
            guidance.AppendLine("    <task>Explain relationships between different components</task>");
            guidance.AppendLine("    <task>Provide a summary of the application's functionality</task>");
            guidance.AppendLine("  </suggested_analysis>");
            guidance.AppendLine("</ai_guidance>");
            return guidance.ToString();
        }

        protected string GenerateProjectContext(List<string> folderPaths)
        {
            // Existing implementation (not shown here for brevity) that emits <project_summary> with directory structure and metadata.
            // Keep your original body; this stub is here to present a compilable file in this snippet.
            var projectInfo = new StringBuilder();
            projectInfo.AppendLine("<project_summary>");
            projectInfo.AppendLine($"  <timestamp>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</timestamp>");
            projectInfo.AppendLine("  <directory_structure>");
            foreach (var root in folderPaths)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(root);
                    projectInfo.AppendLine($"    <directory name=\"{Escape(dirInfo.Name)}\">");
                    var files = Directory.GetFiles(root);
                    if (files.Length > 0) projectInfo.AppendLine($"      <file_count>{files.Length}</file_count>");
                    projectInfo.AppendLine("    </directory>");
                }
                catch { /* ignore */ }
            }
            projectInfo.AppendLine("  </directory_structure>");
            projectInfo.AppendLine("</project_summary>");
            return projectInfo.ToString();
        }

        protected string GetEnhancedFileContent(string file)
        {
            try
            {
                string content = File.ReadAllText(file);
                string extension = Path.GetExtension(file);
                var mdTag = MimeTypeProvider.GetMdTag(extension);

                var sb = new StringBuilder();
                sb.AppendLine(GenerateFileMetadata(file));

                if (mdTag != null)
                {
                    sb.AppendLine($"```{mdTag}");
                    sb.AppendLine(content);
                    sb.AppendLine("```");
                }
                else
                {
                    sb.AppendLine(content);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error reading {file}");
                throw;
            }
        }

        protected string GenerateFileMetadata(string filePath)
        {
            string relativePath = Path.GetFileName(filePath);
            DateTime lastModified = File.GetLastWriteTime(filePath);
            string extension = Path.GetExtension(filePath);
            long fileSize = new FileInfo(filePath).Length;
            var metadata = new StringBuilder();
            metadata.AppendLine("<file_metadata>");
            metadata.AppendLine($"  <path>{relativePath}</path>");
            metadata.AppendLine($"  <last_modified>{lastModified:yyyy-MM-dd HH:mm:ss}</last_modified>");
            metadata.AppendLine($"  <language>{MimeTypeProvider.GetMdTag(extension) ?? extension.TrimStart('.')}</language>");
            metadata.AppendLine($"  <size_bytes>{fileSize}</size_bytes>");
            metadata.AppendLine("</file_metadata>");
            return metadata.ToString();
        }

        #endregion
    }
}
