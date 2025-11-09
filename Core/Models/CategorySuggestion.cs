namespace VecTool.Core.Models;

/// <summary>
/// Represents an AI-generated suggestion for categorizing a prompt file.
/// </summary>
public sealed record CategorySuggestion
{
    /// <summary>
    /// Suggested area (e.g., "work", "private", "development").
    /// </summary>
    public string Area { get; init; } = string.Empty;

    /// <summary>
    /// Suggested project (e.g., "VecTool", "LINX", "AgileAI").
    /// </summary>
    public string Project { get; init; } = string.Empty;

    /// <summary>
    /// Suggested category (e.g., "Spaces", "Guides", "Templates").
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Confidence score from AI (0.0 to 1.0), if provided.
    /// </summary>
    public double? Confidence { get; init; }

    /// <summary>
    /// Full suggested path combining area/project/category.
    /// </summary>
    public string SuggestedPath => $"{Area}/{Project}/{Category}";
}
