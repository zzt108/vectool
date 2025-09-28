using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace XmlStringDiscovery
{
    public sealed class XmlTagScanner
    {
        // Regex for normal and interpolated string literals: "..."
        private static readonly Regex NormalStringRegex =
            new("\"((?:\\\\\"|[^\"])*)\"", RegexOptions.Compiled);

        // Regex for verbatim string literals: @"..."
        private static readonly Regex VerbatimStringRegex =
            new("@\"((?:\"\"|[^\"])*)\"", RegexOptions.Compiled);

        // Heuristic patterns for "XML-ish"
        private static readonly Regex LowerTokenPattern =
            new("^[a-z][a-z0-9_-]{2,}$", RegexOptions.Compiled);

        private static readonly Regex HeaderLikePattern =
            new("^[a-z][a-z0-9_-]*(?:\\s+[a-z][a-z0-9_-]*=)", RegexOptions.Compiled);

        private static readonly string[] MetadataHints =
            { "file", "path", "ext", "language", "size", "lastmodified", "timestamp", "usedby", "dependson", "directory", "section name", "name=" };

        private static readonly string[] StructureHints =
            { "tableofcontents", "crossreferences", "section", "file" };

        private static readonly string[] ContentHints =
            { "aiguidance", "projectsummary" };

        public TagCatalog Scan(ScanOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var catalog = new TagCatalog();

            IEnumerable<string> files = Directory.EnumerateFiles(options.RootDirectory, "*.cs", SearchOption.AllDirectories);
            if (options.FileFilter != null)
            {
                files = files.Where(options.FileFilter);
            }

            foreach (var file in files)
            {
                var isTest = file.IndexOf("UnitTests", StringComparison.OrdinalIgnoreCase) >= 0;
                string text;

                try
                {
                    text = File.ReadAllText(file);
                }
                catch
                {
                    // Skip unreadable files to keep the scanner resilient
                    continue;
                }

                foreach (var literal in ExtractStringLiterals(text))
                {
                    var token = literal.Trim();

                    if (!LooksLikeXmlish(token))
                        continue;

                    var cat = Classify(token);
                    var info = GetOrAdd(catalog, token, cat, isTest);
                    info.AddOccurrence(file);
                }
            }

            // Split to Production vs TestOnly maps
            foreach (var kv in catalog.All)
            {
                if (kv.Value.IsTestOnly)
                    catalog.TestOnly[kv.Key] = kv.Value;
                else
                    catalog.Production[kv.Key] = kv.Value;
            }

            return catalog;
        }

        private static IEnumerable<string> ExtractStringLiterals(string text)
        {
            foreach (Match m in NormalStringRegex.Matches(text))
            {
                yield return UnescapeNormal(m.Groups[1].Value);
            }

            foreach (Match m in VerbatimStringRegex.Matches(text))
            {
                yield return UnescapeVerbatim(m.Groups[1].Value);
            }
        }

        private static string UnescapeNormal(string s) => s.Replace("\\\"", "\"");
        private static string UnescapeVerbatim(string s) => s.Replace("\"\"", "\"");

        private static bool LooksLikeXmlish(string s)
        {
            // Accept plain lower tokens or header-like lines with key=value
            if (LowerTokenPattern.IsMatch(s))
                return true;

            if (HeaderLikePattern.IsMatch(s))
                return true;

            // Also accept “section name=...”, “file name=...”
            if (s.StartsWith("section ", StringComparison.OrdinalIgnoreCase)) return true;
            if (s.StartsWith("file ", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private static TagContextCategory Classify(string token)
        {
            var t = token.ToLowerInvariant();

            if (ContentHints.Any(h => t.Contains(h)))
                return TagContextCategory.Content;

            if (StructureHints.Any(h => t.StartsWith(h, StringComparison.OrdinalIgnoreCase) || t.Equals(h, StringComparison.OrdinalIgnoreCase)))
                return TagContextCategory.Structure;

            if (MetadataHints.Any(h => t.Contains(h)))
                return TagContextCategory.Metadata;

            return TagContextCategory.Unknown;
        }

        private static TagInfo GetOrAdd(TagCatalog catalog, string name, TagContextCategory cat, bool isTest)
        {
            if (!catalog.All.TryGetValue(name, out var info))
            {
                info = new TagInfo(name, cat) { IsTestOnly = isTest };
                catalog.All[name] = info;
            }
            else
            {
                // Merge test-only flag if any occurrence is under tests
                info.IsTestOnly |= isTest;
                // Upgrade category if previously Unknown and now we have a better hint
                if (info.Category == TagContextCategory.Unknown && cat != TagContextCategory.Unknown)
                    info.Category = cat;
            }

            return info;
        }
    }
}
