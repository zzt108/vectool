#nullable enable

using LogCtxShared;
using Microsoft.Extensions.Logging;
using VecTool.Configuration.Logging;

namespace VecTool.Core.Models;

/// <summary>
/// Prompts repository settings loaded from app.config with validation.
/// Follows <see cref="VectorStoreConfig"/> pattern: factory method + defaults.
/// </summary>
public sealed class PromptsConfig : IPromptsConfig
{
    private static readonly ILogger logger = AppLogger.For<PromptsConfig>();

    private const string KEY_REPO_PATH = "promptsRepositoryPath";
    private const string KEY_FILE_EXTENSIONS = "promptsFileExtensions";
    private const string KEY_LLM_CONFIG_PATH = "llmProviderConfig";
    private const string KEY_FAVORITES_PATH = "favoritesConfigPath";

    public const string DefaultFileExtensions = ".md,.txt,.yaml,.json";
    public const string DefaultFavoritesFileName = ".favorites.json";

    public string RepositoryPath { get; }
    public string FileExtensions { get; }
    public string LLMConfigPath { get; }
    public string FavoritesConfigPath { get; }

    /// <summary>
    /// Construct with explicit values (testable constructor).
    /// </summary>
    public PromptsConfig(string repositoryPath, string fileExtensions, string llmConfigPath, string favoritesConfigPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            throw new ArgumentException("RepositoryPath is required.", nameof(repositoryPath));

        if (string.IsNullOrWhiteSpace(fileExtensions))
            throw new ArgumentException("FileExtensions is required.", nameof(fileExtensions));

        if (string.IsNullOrWhiteSpace(llmConfigPath))
            throw new ArgumentException("LLMConfigPath is required.", nameof(llmConfigPath));

        if (string.IsNullOrWhiteSpace(favoritesConfigPath))
            throw new ArgumentException("FavoritesConfigPath is required.", nameof(favoritesConfigPath));

        RepositoryPath = repositoryPath;
        FileExtensions = fileExtensions;
        LLMConfigPath = llmConfigPath;
        FavoritesConfigPath = favoritesConfigPath;
    }

    /// <summary>
    /// Factory method to load configuration from app.config with defaults and validation.
    /// Similar to <see cref="VectorStoreConfig.FromAppConfig"/>.
    /// </summary>
    public static PromptsConfig? FromAppConfig(IAppSettingsReader? reader = null)
    {
        reader ??= new ConfigurationManagerAppSettingsReader();

        using var ctx = logger.SetContext(new Props().Add("source", "app.config")); //

        var repoPath = reader.Get(KEY_REPO_PATH);
        var extensions = reader.Get(KEY_FILE_EXTENSIONS) ?? DefaultFileExtensions;
        var llmConfigPath = reader.Get(KEY_LLM_CONFIG_PATH);
        var favoritesPath = reader.Get(KEY_FAVORITES_PATH);

        logger.SetContext(new Props().Add(KEY_REPO_PATH, repoPath)
            .Add(KEY_FILE_EXTENSIONS, extensions)
            .Add(KEY_LLM_CONFIG_PATH, llmConfigPath)
            .Add(KEY_FAVORITES_PATH, favoritesPath));

        // Validate required settings
        if (string.IsNullOrWhiteSpace(repoPath))
        {
            var ex = new InvalidOperationException($"Missing required app.config key: {KEY_REPO_PATH}");
            logger.LogError(ex, "Prompts repository path not configured");
            return null;
        }

        if (string.IsNullOrWhiteSpace(llmConfigPath))
        {
            var ex = new InvalidOperationException($"Missing required app.config key: {KEY_LLM_CONFIG_PATH}");
            logger.LogError(ex, "LLM provider config path not configured");
            return null;
        }

        // Auto-generate favorites path if not specified
        if (string.IsNullOrWhiteSpace(favoritesPath))
        {
            favoritesPath = Path.Combine(repoPath, DefaultFavoritesFileName);
            logger.LogDebug($"Favorites path not configured, using default: {favoritesPath}");
        }

        // LogWarning if paths don't exist (non-fatal)
        if (!Directory.Exists(repoPath))
            logger.LogWarning($"Prompts repository path does not exist: {repoPath}");

        if (!File.Exists(llmConfigPath))
            logger.LogWarning($"LLM provider config file does not exist: {llmConfigPath}");

        logger.LogInformation($"Prompts config loaded: repo={repoPath}, extensions={extensions}");
        return new PromptsConfig(repoPath, extensions, llmConfigPath, favoritesPath);
    }
}