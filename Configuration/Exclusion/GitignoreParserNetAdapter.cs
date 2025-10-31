// ✅ FULL FILE VERSION
// File: Configuration/Exclusion/GitignoreParserNetAdapter.cs

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
    private string? _tempFilePath; // Track temp file for cleanup

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
            // Create persistent temp file (library needs it to exist for parsing)
            // Clean up any previous temp file
            if (_tempFilePath != null && File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }

            _tempFilePath = Path.GetTempFileName();
            File.WriteAllLines(_tempFilePath, patternsToLoad);

            _parser = new GitignoreParser(_tempFilePath);
            _log.Info($"GitignoreParserNet loaded {patternsToLoad.Count} patterns from temp file: {_tempFilePath}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to initialize GitignoreParserNet: {ex.Message}");
            _parser = null;

            // Clean up temp file on error
            if (_tempFilePath != null && File.Exists(_tempFilePath))
            {
                try { File.Delete(_tempFilePath); } catch { /* Ignore cleanup errors */ }
                _tempFilePath = null;
            }
        }
    }

    // ✅ DUAL-PATH DIRECTORY TESTING for robust pattern matching
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

            // ✅ Remove leading slashes if present (GitignoreParserNet expects relative paths)
            normalizedPath = normalizedPath.TrimStart('/');

            // ✅ Dual-path testing for directories
            // Test BOTH "bin/" and "bin" forms to handle all GitignoreParserNet pattern edge cases
            if (isDirectory)
            {
                var withSlash = normalizedPath.EndsWith('/') ? normalizedPath : normalizedPath + '/';
                var withoutSlash = normalizedPath.TrimEnd('/');

                // Test both forms; pattern matches if EITHER form matches
                var result = _parser.Denies(withSlash) || _parser.Denies(withoutSlash);

                if (result)
                {
                    _log.Trace($"Ignored by GitignoreParserNet: {relativePath} (normalized: {withSlash} | alt: {withoutSlash})");
                }
                else
                {
                    _log.Trace($"NOT ignored by GitignoreParserNet: {relativePath} (normalized: {withSlash} | alt: {withoutSlash})");
                }

                return result;
            }

            // File semantics (no trailing slash)
            var filePath = normalizedPath.TrimEnd('/');

            var fileResult = _parser.Denies(filePath);

            if (fileResult)
            {
                _log.Trace($"Ignored by GitignoreParserNet: {relativePath} (normalized: {filePath})");
            }
            else
            {
                _log.Trace($"NOT ignored by GitignoreParserNet: {relativePath} (normalized: {filePath})");
            }

            return fileResult;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error checking ignore status for {relativePath}: {ex.Message}");
            return false; // Fail open - don't exclude on error
        }
    }

    /// <summary>
    /// Cleans up the temporary .gitignore file used by GitignoreParserNet.
    /// </summary>
    public void Dispose()
    {
        if (_tempFilePath != null && File.Exists(_tempFilePath))
        {
            try
            {
                File.Delete(_tempFilePath);
                _log.Debug($"Cleaned up temp gitignore file: {_tempFilePath}");
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to delete temp file {_tempFilePath}: {ex.Message}");
            }
            finally
            {
                _tempFilePath = null;
            }
        }

        _parser = null;
    }
}