namespace GitIgnore.Services
{
    /// <summary>
    /// Statistics about loaded .gitignore files
    /// </summary>
    public class GitIgnoreStatistics
    {
        public int GitIgnoreFileCount { get; set; }
        public int TotalPatterns { get; set; }
        public int NegationPatterns { get; set; }
        public int DirectoryOnlyPatterns { get; set; }
        public string RootDirectory { get; set; }

        public override string ToString()
        {
            return $"GitIgnore Stats - Files: {GitIgnoreFileCount}, Patterns: {TotalPatterns}, " +
                   $"Negations: {NegationPatterns}, Directory-Only: {DirectoryOnlyPatterns}";
        }

    }
}