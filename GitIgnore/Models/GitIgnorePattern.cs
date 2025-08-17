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
        public Regex CompiledPattern { get; }
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

            // Check if directory only
            IsDirectoryOnly = workingPattern.EndsWith("/") && workingPattern.Length > 1;
            if (IsDirectoryOnly)
                workingPattern = workingPattern.TrimEnd('/');

            // Check if root relative
            IsRootRelative = workingPattern.StartsWith("/") || workingPattern.Contains("/");
            if (workingPattern.StartsWith("/"))
                workingPattern = workingPattern.Substring(1);

            ProcessedPattern = workingPattern;
            CompiledPattern = CreateRegex(workingPattern);
        }

        private Regex CreateRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return null;

            // Escape special regex characters except our wildcards
            var escaped = Regex.Escape(pattern);
            
            // Handle ** (match zero or more directories)
            escaped = Regex.Replace(escaped, @"\\?\*\\?\*", ".*");
            
            // Handle * (match any characters except directory separator)
            escaped = escaped.Replace(@"\*", @"[^/\\]*");
            
            // Handle ? (match single character except directory separator)  
            escaped = escaped.Replace(@"\?", @"[^/\\]");

            // Make the pattern match from the beginning if it's root relative
            if (IsRootRelative)
                escaped = "^" + escaped;
            else
                escaped = "(^|[/\\\\])" + escaped;

            // Add end anchor
            escaped += "$";

            return new Regex(escaped, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public bool IsMatch(string relativePath, bool isDirectory)
        {
            if (CompiledPattern == null || string.IsNullOrEmpty(ProcessedPattern))
                return false;

            // If pattern is directory-only but item is not a directory, no match
            if (IsDirectoryOnly && !isDirectory)
                return false;

            // Normalize path separators
            var normalizedPath = relativePath.Replace('\\', '/');

            return CompiledPattern.IsMatch(normalizedPath);
        }

        public bool IsValid => CompiledPattern != null && !string.IsNullOrEmpty(ProcessedPattern);

        public override string ToString() => $"Pattern: {OriginalPattern}, Negation: {IsNegation}, DirOnly: {IsDirectoryOnly}";
    }
}