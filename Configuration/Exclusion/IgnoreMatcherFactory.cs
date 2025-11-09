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
    public static IIgnorePatternMatcher? Create(IgnoreLibraryType libraryType, string? rootPath = null)
    {
        using var ctx = _log.Ctx.Set(new Props()
            .Add("LibraryType", libraryType.ToString())
            .Add("RootPath", rootPath ?? "deferred"));

        // Default to MAB.DotIgnore when libraryType is unspecified
        if (libraryType == IgnoreLibraryType.Auto)  // Placeholder for "auto" selection
        {
            libraryType = IgnoreLibraryType.MabDotIgnore;
            _log.Info("Library type not specified; defaulting to MAB.DotIgnore");
        }

        IIgnorePatternMatcher matcher = libraryType switch
        {
            IgnoreLibraryType.MabDotIgnore => new MabDotIgnoreAdapter(),
            _ => throw new ArgumentException($"Unknown library type: {libraryType}", nameof(libraryType))
        };

        _log.Debug($"Created matcher: {matcher.GetType().Name}");

        if (!string.IsNullOrWhiteSpace(rootPath))
        {
            try
            {
                matcher.LoadFromRoot(rootPath);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to load patterns from {rootPath}; matcher will not be used");
                throw;
            }
        }

        return matcher;
    }
}