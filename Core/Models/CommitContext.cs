namespace VecTool.Core.Models;

/// <summary>
/// Context information for generating git commit messages.
/// </summary>
public sealed record CommitContext
{
    /// <summary>
    /// Repository name (e.g., "VecTool").
    /// </summary>
    public string Repo { get; init; } = string.Empty;

    /// <summary>
    /// Optional branch name for context.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Optional feature/phase identifier (e.g., "4.6.1.5").
    /// </summary>
    public string? Phase { get; init; }
}
