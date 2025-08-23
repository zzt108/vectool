using System;
using System.Text.RegularExpressions;

namespace GitIgnore.Models
{
    /// <summary>
    /// Represents a single .gitignore pattern with its matching logic
    /// </summary>
    public class GitIgnorePattern
    {
        public string OriginalPattern { get; }
        public bool IsNegation { get; }
        public bool IsDirectoryOnly { get; }
        public bool IsRootRelative { get; }
        public string ProcessedPattern { get; }
        public Regex? CompiledPattern { get; }
        public string SourceDirectory { get; }

        public GitIgnorePattern(string pattern, string sourceDirectory)
        {
            OriginalPattern = pattern?.Trim() ?? throw new ArgumentNullException(nameof(pattern));
            SourceDirectory = sourceDirectory ?? throw new ArgumentNullException(nameof(sourceDirectory));

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(pattern) || pattern.StartsWith("#"))
            {
                ProcessedPattern = string.Empty;
                return;
            }

            // Check for negation
            IsNegation = pattern.StartsWith("!");
            var workingPattern = IsNegation ? pattern.Substring(1) : pattern;

            // A pattern like `**/foo` is equivalent to `foo`. This simplifies the logic
            // by handling this special case before regex creation.
            if (workingPattern.StartsWith("**/") && workingPattern.IndexOfAny(new[] { '/', '\\' }, 3) < 0)
            {
                workingPattern = workingPattern.Substring(3);
            }

            // Check if directory only
            IsDirectoryOnly = workingPattern.EndsWith("/") && workingPattern.Length > 1;
            if (IsDirectoryOnly)
                workingPattern = workingPattern.TrimEnd('/');

            // Check if root relative. A pattern is root-relative if it starts with a `/`
            // or contains a `/`, unless it's a `**/` pattern.
            bool startsWithDoubleStar = workingPattern.StartsWith("**/");
            IsRootRelative = workingPattern.StartsWith("/") || (!startsWithDoubleStar && workingPattern.Contains("/"));
            if (workingPattern.StartsWith("/"))
                workingPattern = workingPattern.Substring(1);

            ProcessedPattern = workingPattern;
            CompiledPattern = CreateRegex(workingPattern);
        }

        private Regex? CreateRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return null;

            //var escapedPattern = pattern.Replace("/", @"\/");

            // 1) Escape all regex metachars
            string escaped = Regex.Escape(pattern);

            // 2) Replace the GitIgnore wildcards
            // Note: order matters—replace ** before *
            escaped = escaped.Replace(@"\*\*/", ".*");
            escaped = escaped.Replace(@"\*", @"[^/\\]*");
            escaped = escaped.Replace(@"\?", @"[^/\\]");

            // 3) Anchor
            bool startsWithDoubleStar = pattern.StartsWith("**/") || pattern.StartsWith(@"**\");
            if (IsRootRelative && !startsWithDoubleStar)
                escaped = "^" + escaped;
            else if (!startsWithDoubleStar)
                escaped = @"(^|[/\\])" + escaped;

            // 4) Anchor the end of the pattern. This is the crucial part.
            // A pattern that describes a directory should match the directory itself, or paths inside it.
            // A pattern that describes a file should match the path exactly.
            if (IsDirectoryOnly)
            {
                // Pattern was "build/". It should match "build" and "build/file".
                // The regex should match "build" followed by the end of the string or a slash.
                escaped += @"($|[/\\])";
            }
            else if (pattern.EndsWith("/**"))
            {
                // Pattern is "a/b/**". Regex is `^a/b/.*`.
                // This correctly matches everything in the directory. No end anchor is needed.
            }
            else if (IsRootRelative) // Covers "src/obj" and "src/**/*.tmp"
            {
                // Distinguish "src/obj" from "src/file.log".
                // If the last part has no wildcards or extension, treat as a directory.
                string lastSegment = pattern.Substring(pattern.LastIndexOf('/') + 1);
                bool hasWildcard = lastSegment.Contains('*') || lastSegment.Contains('?');
                // Heuristic: a dot not at the start suggests an extension.
                bool hasExtension = lastSegment.Contains('.') && lastSegment.LastIndexOf('.') > 0;

                escaped += (!hasWildcard && !hasExtension) ? @"($|[/\\])" : "$";
            }
            else
            {
                // Not root-relative, e.g., "*.log" or "**/temp". These must match the end of the string.
                escaped += "$";
            }

            return new Regex(escaped, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public bool IsMatch(string relativePath, bool isDirectory)
        {
            if (CompiledPattern == null || string.IsNullOrEmpty(ProcessedPattern))
                return false;

            // Normalize path separators
            var normalizedPath = relativePath.Replace('\\', '/');

            // For a directory-only pattern (e.g., "build/"), we must distinguish between
            // a file named "build" (no match) and a file inside, like "build/log.txt" (match).
            if (IsDirectoryOnly && !isDirectory)
            {
                // If the path we are checking is a file and does not contain a path separator,
                // it cannot be a file *within* the directory, so it's not a match.
                if (!normalizedPath.Contains('/'))
                    return false;
            }

            return CompiledPattern.IsMatch(normalizedPath);
        }

        public bool IsValid => CompiledPattern != null && !string.IsNullOrEmpty(ProcessedPattern);

        public override string ToString() => $"Pattern: {OriginalPattern}, Negation: {IsNegation}, DirOnly: {IsDirectoryOnly}";
    }
}