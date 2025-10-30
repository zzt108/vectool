// ✅ FULL FILE VERSION

using LogCtxShared;
using NLogShared;

namespace VecTool.Configuration.Exclusion;

/// <summary>
/// Factory for creating IIgnorePatternMatcher instances based on library type.
/// </summary>
public static class IgnoreMatcherFactory
{
    private static readonly CtxLogger _log = new();

    /// <summary>
    /// Creates a matcher instance for the specified library type.
    /// </summary>
    /// <param name="libraryType">Which library to use.</param>
    /// <param name="rootPath">Repository root to load patterns from (optional - can call LoadFromRoot later).</param>
    /// <returns>Configured matcher instance.</returns>
    public static IIgnorePatternMatcher Create(IgnoreLibraryType libraryType, string? rootPath = null)
    {
        using var _ = _log.Ctx.Set(new Props()
            .Add("LibraryType", libraryType.ToString())
            .Add("RootPath", rootPath ?? "(deferred)"));

        IIgnorePatternMatcher matcher = libraryType switch
        {
            IgnoreLibraryType.GitignoreParserNet => new GitignoreParserNetAdapter(),
            IgnoreLibraryType.MabDotIgnore => new MabDotIgnoreAdapter(),
            _ => throw new ArgumentException($"Unknown library type: {libraryType}", nameof(libraryType))
        };

        _log.Debug($"Created matcher: {matcher.GetType().Name}");

        // Optionally load patterns immediately if root path provided
        if (!string.IsNullOrWhiteSpace(rootPath))
        {
            matcher.LoadFromRoot(rootPath);
        }

        return matcher;
    }
}