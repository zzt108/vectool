namespace VecTool.Configuration.Exclusion;

/// <summary>
/// Facade interface for .gitignore/.vtignore pattern matching.
/// Allows swappable implementations (GitignoreParserNet, MAB.DotIgnore).
/// </summary>
public interface IIgnorePatternMatcher
{
    /// <summary>
    /// Loads ignore patterns from .gitignore and .vtignore files in the root path.
    /// .vtignore patterns take precedence if both files exist.
    /// </summary>
    /// <param name="rootPath">Repository root directory to scan for ignore files.</param>
    void LoadFromRoot(string rootPath);

    /// <summary>
    /// Checks if a path should be ignored based on loaded patterns.
    /// </summary>
    /// <param name="relativePath">Path relative to root (forward slashes).</param>
    /// <param name="isDirectory">True if path is a directory.</param>
    /// <returns>True if path matches any ignore pattern.</returns>
    bool IsIgnored(string relativePath, bool isDirectory);
}