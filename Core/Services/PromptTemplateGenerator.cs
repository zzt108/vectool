using LogCtxShared;
using Microsoft.Extensions.Logging;
using VecTool.Configuration.Logging;

namespace VecTool.Core.Services;

/// <summary>
/// Simple variable substitution engine for prompt templates.
/// Supports auto-provided variables: AREA, PROJECT, CATEGORY, VERSION, TIMESTAMP, AUTHOR, REPOROOT.
/// </summary>
public sealed class PromptTemplateGenerator
{
    private readonly ILogger logger = AppLogger.For<PromptTemplateGenerator>();

    /// <summary>
    /// Applies variable substitution to template content.
    /// </summary>
    /// <param name="content">Template content with {{VAR}} placeholders.</param>
    /// <param name="customVars">Custom variable values (optional).</param>
    /// <returns>Content with variables substituted.</returns>
    public string ApplyTemplateVariables(string content, Dictionary<string, string>? customVars = null)
    {
        using var lc = logger.SetContext(new Props().Add("content", content).Add("contentLength", content.Length));

        try
        {
            var result = content;
            var vars = new Dictionary<string, string>(customVars ?? new Dictionary<string, string>());

            // Auto-provided variables (if not already set)
            vars.TryAdd("TIMESTAMP", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            vars.TryAdd("AUTHOR", Environment.UserName);

            // Simple string.Replace for each variable
            foreach (var kvp in vars)
            {
                var placeholder = $"{{{{{kvp.Key}}}}}"; // {{VAR}}
                if (result.Contains(placeholder))
                {
                    result = result.Replace(placeholder, kvp.Value);
                    logger.LogDebug($"Substituted variable: {kvp.Key}");
                }
            }

            // Check for unresolved variables
            var unresolvedCount = System.Text.RegularExpressions.Regex.Matches(result, @"\{\{[A-Z_]+\}\}").Count;
            if (unresolvedCount > 0)
            {
                using var lc2 = logger.SetContext(new Props().Add("unresolvedCount", unresolvedCount));
                logger.LogWarning("Unresolved variables found");
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply template variables");
            return content; // Return original on error
        }
    }
}