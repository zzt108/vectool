using LogCtxShared;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using VecTool.Core.AI;
using VecTool.Core.Models;

namespace VecTool.Core.Services;

/// <summary>
/// AI-powered service that suggests categorization paths for prompt files.
/// </summary>
public sealed class PromptCategorizer
{
    private readonly ILlmProvider _llmProvider;

    private static readonly ILogger logger =
        LoggerFactory.Create(b => b.AddNLog()).CreateLogger<PromptCategorizer>();

    public PromptCategorizer(ILlmProvider llmProvider)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Suggests area/project/category based on prompt content.
    /// </summary>
    public async Task<CategorySuggestion?> SuggestCategoryAsync(string content)
    {
        using var lc = logger.SetContext(new Props().Add("content", content).Add("contentLength", content.Length));

        try
        {
            // Truncate content for efficiency (first 1000 chars)
            var contentPreview = content.Length > 1000
                ? content.Substring(0, 1000)
                : content;

            var prompt = $@"This is a prompt file content:

{contentPreview}

Based on this content, suggest where it should be organized. Respond ONLY with the path in format: AREA/PROJECT/CATEGORY

Areas: private, work, development
Projects: VecTool, LINX, AgileAI, DevTools
Categories: Spaces, Guides, Templates, Scripts

Example response: work/VecTool/Spaces";

            logger.LogInformation("Requesting AI categorization");
            var response = await _llmProvider.RequestAsync(prompt);
            using var ctx = logger.SetContext(new Props().Add("prompt", prompt).Add("response", response));
            logger.LogInformation("AI response received");

            // Parse response: "work/VecTool/Spaces"
            var parts = response.Trim().Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
            {
                using var _ = logger.SetContext(new Props().Add("prompt", prompt).Add("response", response));
                logger.LogWarning("Invalid AI response format");
                return null;
            }

            return new CategorySuggestion
            {
                Area = parts[0],
                Project = parts[1],
                Category = parts[2]
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to categorize prompt");
            return null;
        }
    }
}