using NLogShared;
using VecTool.Core.AI;
using VecTool.Core.Models;

namespace VecTool.Core.Services;

/// <summary>
/// Generates concise git commit messages from diffs using AI.
/// </summary>
public sealed class GitCommitMessageGenerator
{
    private readonly ILlmProvider _llmProvider;
    private const int MaxCommitLength = 72;
    private CtxLogger _log => new();

    public GitCommitMessageGenerator(ILlmProvider llmProvider)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Generates a commit message from git diff output.
    /// </summary>
    public async Task<string?> GenerateAsync(string gitDiff, CommitContext context)
    {
        using var lc = LogCtxShared.LogCtx.Set()
            .Add("repo", context.Repo)
            .Add("diffLength", gitDiff.Length);

        try
        {
            if (string.IsNullOrWhiteSpace(gitDiff))
            {
                _log.Warn("Empty git diff provided");
                return null;
            }

            // Truncate diff if too large (max 2000 chars for efficiency)
            var diffPreview = gitDiff.Length > 2000
                ? gitDiff.Substring(0, 2000)
                : gitDiff;

            var prompt = $@"Git repository: {context.Repo}
{(context.Phase != null ? $"Phase: {context.Phase}" : "")}

Changes:
{diffPreview}

Generate a concise, professional commit message (max {MaxCommitLength} characters).
- Start with present tense verb (Add, Fix, Update, Remove, Refactor)
- Be specific but brief
- Single line only

Example: Add PromptCategorizer with AI-powered suggestions";

            _log.Info("Requesting AI commit message generation");
            var response = await _llmProvider.RequestAsync(prompt);

            var commitMessage = response.Trim();

            // Truncate if AI exceeded limit
            if (commitMessage.Length > MaxCommitLength)
            {
                commitMessage = commitMessage.Substring(0, MaxCommitLength);
                using var __ = LogCtxShared.LogCtx.Set().Add("length", commitMessage.Length);
                _log.Warn("Commit message truncated");
            }
            using var _ = LogCtxShared.LogCtx.Set().Add("message", commitMessage);
            _log.Info("Commit message generated");
            return commitMessage;
        }
        catch (Exception ex)
        {
            _log.Error(ex,"Failed to generate commit message");
            return null;
        }
    }
}
