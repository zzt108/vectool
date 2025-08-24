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
            // Corrected logic: If it starts with `**/`, it's never root relative.
            // Otherwise, if it starts with `/` or contains `/`, it's root relative.
            IsRootRelative = !OriginalPattern.StartsWith("**/") && (OriginalPattern.StartsWith("/") || OriginalPattern.Contains("/"));
            if (workingPattern.StartsWith("/"))
                workingPattern = workingPattern.Substring(1);

            ProcessedPattern = workingPattern;
            CompiledPattern = CreateRegex(workingPattern);
        }

        private Regex? CreateRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return null;

            string regexPattern = Regex.Escape(pattern); // Escape all regex metachars first

            // Replace GitIgnore wildcards with regex equivalents
            // Note: order matters—replace ** before *
            regexPattern = regexPattern.Replace(@"\*\*/", ".*"); // For Windows paths (escaped \ becomes \\)
            regexPattern = regexPattern.Replace(@"\*\*/", ".*");  // For Unix paths (escaped / becomes \/)

            // Replace * and ? with regex that matches any char except path separators
            // Here, we need to be careful with escaping.
            // The literal characters are / and \.
            // In a regex character class, \ needs to be \\.
            // So, [^/\\] means match any character except / or \
            // In a C# string, this would be "[^/\\]".
            regexPattern = regexPattern.Replace(@"\*", "[^/\\]*"); // Match any char except path separators
            regexPattern = regexPattern.Replace(@"\?", "[^/\\]");  // Match single char except path separators

            // Determine if the original pattern started with **/, which implies matching anywhere
            bool originalStartsWithDoubleStarSlash = OriginalPattern.StartsWith("**/" ) || OriginalPattern.StartsWith(@"**\");

            // Anchoring
            if (IsRootRelative)
            {
                regexPattern = "^" + regexPattern;
            }
            else if (originalStartsWithDoubleStarSlash)
            {
                // If original pattern was like "**/foo", it should match "foo", "dir/foo", "dir/subdir/foo"
                // This means it can be at the string or preceded by any path segments.
                // The `ProcessedPattern` already removed the `**/` prefix.
                // So, we need to add `(^|.*/)` to the beginning of the regex.
                regexPattern = @"(^|.*/)" + regexPattern;
            }
            else
            {
                // Not root-relative, not starting with **, e.g., "*.log" or "foo".
                // These should match at the start of a segment or after a slash.
                regexPattern = @"(^|[/\])" + regexPattern;
            }

            // End anchoring
            if (IsDirectoryOnly)
            {
                regexPattern += @"($|[/\])";
            }
            else if (pattern.EndsWith("/**"))
            {
                // Pattern is "a/b/**". Regex is `^a/b/.*`.
                // This correctly matches everything in the directory. No end anchor is needed.
            }
            else if (IsRootRelative)
            {
                // For root-relative patterns that don't end with /**, they should match the end of the string.
                // This covers "src/obj" and "src/file.log"
                regexPattern += "$";
            }
            else
            {
                // For patterns that are not root-relative and don't end with /**,
                // they should match the end of the string. This covers "*.log" or "foo".
                regexPattern += "$";
            }

            return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
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