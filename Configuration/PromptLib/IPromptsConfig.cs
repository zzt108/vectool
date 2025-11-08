namespace VecTool.Configuration.PromptLib;

/// <summary>
/// Configuration interface for AI Prompts Library repository settings.
/// Follows existing <see cref="IVectorStoreConfig"/> pattern for testability.
/// </summary>
public interface IPromptsConfig
{
    /// <summary>Root directory containing prompt files (e.g., C:\prompts).</summary>
    string RepositoryPath { get; }

    /// <summary>Comma-separated file extensions to include (e.g., ".md,.txt,.yaml,.json").</summary>
    string FileExtensions { get; }

    /// <summary>Path to external LLM provider JSON configuration file.</summary>
    string LLMConfigPath { get; }

    /// <summary>Path to favorites JSON file (e.g., C:\prompts\.favorites.json).</summary>
    string FavoritesConfigPath { get; }
}
