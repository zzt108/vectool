using LogCtxShared;
using MAB.DotIgnore;
using NLogShared;

namespace VecTool.Configuration.Exclusion;

/// <summary>
/// Adapter for MAB.DotIgnore library (v3.0.2).
/// Uses in-memory pattern loading with .gitignore and .vtignore support.
/// </summary>
public sealed class MabDotIgnoreAdapter : IIgnorePatternMatcher
{
    private static readonly CtxLogger _log = new();
    private IgnoreList? _ignoreList;
    private string? _loadedRootPath;

    public void LoadFromRoot(string rootPath)
    {
        using var _ = _log.Ctx.Set(new Props()
            .Add("RootPath", rootPath));

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            _log.Warn($"Root path invalid or does not exist: {rootPath}");
            return;
        }

        _loadedRootPath = rootPath;

        var gitignorePath = Path.Combine(rootPath, ".gitignore");
        var vtignorePath = Path.Combine(rootPath, ".vtignore");

        var patternsToLoad = new List<string>();

        // Load .gitignore patterns first
        if (File.Exists(gitignorePath))
        {
            try
            {
                var lines = File.ReadAllLines(gitignorePath);
                patternsToLoad.AddRange(lines);
                _log.Debug($"Loaded {lines.Length} lines from .gitignore");
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to read .gitignore: {ex.Message}");
            }
        }

        // Load .vtignore patterns (higher priority)
        if (File.Exists(vtignorePath))
        {
            try
            {
                var lines = File.ReadAllLines(vtignorePath);
                patternsToLoad.AddRange(lines);
                _log.Debug($"Loaded {lines.Length} lines from .vtignore");
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to read .vtignore: {ex.Message}");
            }
        }

        if (patternsToLoad.Count == 0)
        {
            _log.Info("No ignore patterns found in .gitignore or .vtignore");
            _ignoreList = null;
            return;
        }

        try
        {
            // MAB.DotIgnore supports in-memory pattern loading
            _ignoreList = new IgnoreList(patternsToLoad);
            _log.Info($"MAB.DotIgnore loaded with {patternsToLoad.Count} patterns");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to initialize MAB.DotIgnore: {ex.Message}");
            _ignoreList = null;
        }
    }

    public bool IsIgnored(string relativePath, bool isDirectory)
    {
        if (_ignoreList == null)
        {
            return false; // No patterns loaded
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        try
        {
            // Normalize path separators to forward slashes (Git convention)
            var normalizedPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

            // MAB.DotIgnore expects directory paths to end with /
            if (isDirectory && !normalizedPath.EndsWith('/'))
            {
                normalizedPath += '/';
            }

            var result = _ignoreList.IsIgnored(normalizedPath, isDirectory);

            if (result)
            {
                _log.Trace($"Ignored by MAB.DotIgnore: {relativePath}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error checking ignore status for {relativePath}: {ex.Message}");
            return false; // Fail open - don't exclude on error
        }
    }

    /// <summary>
    /// MAB.DotIgnore uses in-memory patterns, no cleanup needed.
    /// </summary>
    public void Dispose()
    {
        _ignoreList = null;
        _loadedRootPath = null;
    }
}