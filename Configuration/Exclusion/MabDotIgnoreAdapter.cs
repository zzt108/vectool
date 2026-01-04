using LogCtxShared;
using MAB.DotIgnore;
using Microsoft.Extensions.Logging;
using VecTool.Configuration.Logging;

namespace VecTool.Configuration.Exclusion;

/// <summary>
/// Adapter for MAB.DotIgnore library (v3.0.2).
/// Uses in-memory pattern loading with .gitignore and .vtignore support.
/// </summary>
public sealed class MabDotIgnoreAdapter : IIgnorePatternMatcher
{
    private static readonly ILogger logger = AppLogger.For<MabDotIgnoreAdapter>();

    private IgnoreList? _ignoreList;
    private string? _loadedRootPath;

    public void LoadFromRoot(string rootPath)
    {
        using var _ = logger.SetContext()
            .Add("RootPath", rootPath);

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            logger.LogWarning($"Root path invalid or does not exist: {rootPath}");
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
                logger.LogDebug($"Loaded {lines.Length} lines from .gitignore");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to read .gitignore: {ex.Message}");
            }
        }

        // Load .vtignore patterns (higher priority)
        if (File.Exists(vtignorePath))
        {
            try
            {
                var lines = File.ReadAllLines(vtignorePath);
                patternsToLoad.AddRange(lines);
                logger.LogDebug($"Loaded {lines.Length} lines from .vtignore");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to read .vtignore: {ex.Message}");
            }
        }

        if (patternsToLoad.Count == 0)
        {
            var ex = new InvalidOperationException("No ignore patterns found in .gitignore or .vtignore");
            logger.LogError(ex, "No ignore patterns found in .gitignore or .vtignore");
            _ignoreList = null;
            throw ex;
        }

        try
        {
            // MAB.DotIgnore supports in-memory pattern loading
            _ignoreList = new IgnoreList(patternsToLoad);
            logger.LogInformation($"MAB.DotIgnore loaded with {patternsToLoad.Count} patterns");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to initialize MAB.DotIgnore: {ex.Message}");
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
                logger.LogTrace($"Ignored by MAB.DotIgnore: {relativePath}");
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"LogError checking ignore status for {relativePath}: {ex.Message}");
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