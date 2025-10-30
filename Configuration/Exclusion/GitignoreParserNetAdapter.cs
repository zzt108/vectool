using GitignoreParserNet;
using LogCtxShared;
using NLogShared;

namespace VecTool.Configuration.Exclusion;

/// <summary>
/// Adapter for GitignoreParserNet library (v0.2.0.14).
/// Uses file-based loading with .gitignore and .vtignore support.
/// </summary>
public sealed class GitignoreParserNetAdapter : IIgnorePatternMatcher
{
    private static readonly CtxLogger _log = new();
    private GitignoreParser? _parser;
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
            _parser = null;
            return;
        }

        try
        {
            // GitignoreParserNet expects file-based input, so write to temp file
            var tempFile = Path.GetTempFileName();
            File.WriteAllLines(tempFile, patternsToLoad);

            _parser = new GitignoreParser(tempFile);
            _log.Info($"GitignoreParserNet loaded with {patternsToLoad.Count} patterns");

            // Clean up temp file
            File.Delete(tempFile);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to initialize GitignoreParserNet: {ex.Message}");
            _parser = null;
        }
    }

    public bool IsIgnored(string relativePath, bool isDirectory)
    {
        if (_parser == null)
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

            // GitignoreParserNet requires directory paths to end with /
            if (isDirectory && !normalizedPath.EndsWith('/'))
            {
                normalizedPath += '/';
            }

            var result = _parser.Denies(normalizedPath);

            if (result)
            {
                _log.Trace($"Ignored by GitignoreParserNet: {relativePath}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error checking ignore status for {relativePath}: {ex.Message}");
            return false; // Fail open - don't exclude on error
        }
    }
}