using System;
using System.Linq;

namespace GitIgnore.Models
{
    /// <summary>
    /// Represents a single .gitignore file and its patterns
    /// </summary>
    public class GitIgnoreFile
    {
        public string FilePath { get; }
        public string Directory { get; }
        public List<GitIgnorePattern> Patterns { get; }
        public DateTime LastModified { get; private set; }

        public GitIgnoreFile(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Directory = Path.GetDirectoryName(filePath);
            Patterns = new List<GitIgnorePattern>();
            LoadPatterns();
        }

        public GitIgnoreFile(string filePath, IEnumerable<string> patterns):this(filePath)
        {
            foreach (var line in patterns)
            {
                var pattern = new GitIgnorePattern(line, Directory);
                if (pattern.IsValid)
                    Patterns.Add(pattern);
            }
        }

        /// <summary>
        /// Loads patterns from the .gitignore file
        /// </summary>
        private void LoadPatterns()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return;

                var fileInfo = new FileInfo(FilePath);
                LastModified = fileInfo.LastWriteTime;

                var lines = File.ReadAllLines(FilePath);
                foreach (var line in lines)
                {
                    var pattern = new GitIgnorePattern(line, Directory);
                    if (pattern.IsValid)
                        Patterns.Add(pattern);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load .gitignore file: {FilePath}", ex);
            }
        }

        /// <summary>
        /// Checks if patterns have been modified and reloads if necessary
        /// </summary>
        public void RefreshIfNeeded()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    Patterns.Clear();
                    return;
                }

                var fileInfo = new FileInfo(FilePath);
                if (fileInfo.LastWriteTime > LastModified)
                {
                    Patterns.Clear();
                    LoadPatterns();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - graceful degradation
                Console.WriteLine($"Warning: Could not refresh .gitignore file {FilePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines ignore status for a path relative to this .gitignore's directory
        /// </summary>
        public IgnoreResult CheckPath(string relativePath, bool isDirectory)
        {
            if (string.IsNullOrEmpty(relativePath))
                return IgnoreResult.NotMatched;

            var result = IgnoreResult.NotMatched;

            // Process patterns in order - later patterns override earlier ones
            foreach (var pattern in Patterns)
            {
                if (pattern.IsMatch(relativePath, isDirectory))
                {
                    result = pattern.IsNegation ? IgnoreResult.NotIgnored : IgnoreResult.Ignored;
                }
            }

            return result;
        }

        public override string ToString() => $"GitIgnore: {FilePath} ({Patterns.Count} patterns)";
    }

    /// <summary>
    /// Result of checking if a path should be ignored
    /// </summary>
    public enum IgnoreResult
    {
        NotMatched,   // No pattern matched
        Ignored,      // Path should be ignored
        NotIgnored    // Path explicitly not ignored (negation pattern)
    }
}