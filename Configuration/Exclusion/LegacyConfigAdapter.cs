namespace VecTool.Configuration.Exclusion;

using LogCtxShared;
using NLogShared;
using System;

/// <summary>
/// Adapter wrapping legacy VectorStoreConfig exclusion rules in IIgnorePatternMatcher interface.
/// Provides backward compatibility and unified fallback filtering.
/// </summary>
public sealed class LegacyConfigAdapter : IIgnorePatternMatcher
{
    private static readonly CtxLogger log = new();

    private IVectorStoreConfig? _config;
    private string? _loadedRootPath;

    public LegacyConfigAdapter(IVectorStoreConfig config)
    {
        SetConfig(config);
    }

    /// <summary>
    /// Initialize adapter with config (typically called via factory or directly).
    /// </summary>
    public void LoadFromRoot(string rootPath)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("rootPath", rootPath)
            .Add("adapterType", "LegacyConfig")
        );

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            log.Warn($"Root path invalid or does not exist: {rootPath}");
            _config = null;
            return;
        }

        _loadedRootPath = rootPath;

        // Legacy config is typically global, but this adapter supports per-root semantics.
        // In practice, VectorStoreConfig is loaded once and passed to traverser.
        // This method exists to satisfy IIgnorePatternMatcher interface contract.
        _config = VectorStoreConfig.FromAppConfig();

        if (_config?.ExcludedFiles?.Count > 0 || _config?.ExcludedFolders?.Count > 0)
        {
            log.Info(
                $"Legacy config loaded: {_config.ExcludedFiles?.Count ?? 0} file rules, {_config.ExcludedFolders?.Count ?? 0} folder rules");
        }
        else
        {
            log.Debug("No legacy config exclusion rules found");
        }
    }

    /// <summary>
    /// Sets config directly (alternative to LoadFromRoot for testing/DI).
    /// </summary>
    private void SetConfig(IVectorStoreConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        log.Debug($"Legacy config set directly: {_config.ExcludedFiles?.Count ?? 0} file rules, {_config.ExcludedFolders?.Count ?? 0} folder rules");
    }

    /// <summary>
    /// Checks if a path should be excluded based on legacy config rules.
    /// </summary>
    public bool IsIgnored(string relativePath, bool isDirectory)
    {
        if (_config == null)
        {
            log.Trace("Legacy config not initialized, no exclusion");
            return false;
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        try
        {
            if (isDirectory)
            {
                 var result = _config.IsFolderExcluded(relativePath);
                if (result)
                {
                    log.Trace($"Ignored by legacy config (folder): {relativePath}");
                }
                return result;
            }
            else
            {
                var pathName = Path.GetFileName(relativePath);

                var result = _config.IsFileExcluded(pathName);
                if (result)
                {
                    log.Trace($"Root path invalid or does not exist: {relativePath}");
                }
                return result;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Error checking legacy config exclusion for: {relativePath}");
            return false; // Fail open - don't exclude on error
        }
    }

    /// <summary>
    /// Cleanup (legacy config is typically static, no resources to release).
    /// </summary>
    public void Dispose()
    {
        _config = null;
        _loadedRootPath = null;
    }
}