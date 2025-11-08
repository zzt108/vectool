namespace VecTool.RecentFiles;

using System.Text.Json.Serialization;

/// <summary>
/// Enumerates known recent file types for filtering and UI.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecentFileType
{
    Unknown = 0,
    Codebase_Docx = 1,
    Codebase_Md = 2,
    Codebase_Pdf = 3,
    Git_Md = 4,
    TestResults_Md = 5,
    Summary_Md = 6,
    Repomix_Xml = 7,
    Plan,
    Guide
}

/// <summary>
/// Extension methods for RecentFileType enum.
/// </summary>
public static class RecentFileTypeExtensions
{
    /// <summary>
    /// Converts enum value to file suffix with extension.
    /// Replaces underscore with dot: TestResultsMd -> _test-results.md
    /// </summary>
    public static string ToFileSuffix(this RecentFileType fileType)
    {
        return fileType switch
        {
            RecentFileType.Codebase_Docx => "_codebase.docx",
            RecentFileType.Codebase_Md => "_codebase.md",
            RecentFileType.Codebase_Pdf => "_codebase.pdf",
            RecentFileType.Git_Md => "_git.md",
            RecentFileType.TestResults_Md => "_test-results.md",
            RecentFileType.Summary_Md => "_summary.md",
            RecentFileType.Repomix_Xml => "_repomix.xml",
            RecentFileType.Plan => "_plan.md",
            RecentFileType.Guide => "_guide.md",
            RecentFileType.Unknown => ".txt",
            _ => ".txt"
        };
    }

    // Improve type inference to consider the last word of the filename and map via RecentFileType enum suffixes
    public static RecentFileType MapExtensionToType(this RecentFileType fileType, string? ext, string? fileName)
    {
        var e = (ext ?? string.Empty).Trim('.').ToLowerInvariant();
        var baseName = string.IsNullOrWhiteSpace(fileName) ? string.Empty : Path.GetFileNameWithoutExtension(fileName);
        var last = GetLastToken(baseName).ToLowerInvariant();

        // First: map by the last token to enum-defined suffix bases
        if (!string.IsNullOrWhiteSpace(last))
        {
            // Plan / Guide / Summary
            if (last.Equals(Path.GetFileNameWithoutExtension(RecentFileType.Plan.ToFileSuffix()), StringComparison.OrdinalIgnoreCase))
            {
                return RecentFileType.Plan;
            }
            if (last.Equals(Path.GetFileNameWithoutExtension(RecentFileType.Guide.ToFileSuffix()), StringComparison.OrdinalIgnoreCase))
            {
                return RecentFileType.Guide;
            }
            if (last.Equals(Path.GetFileNameWithoutExtension(RecentFileType.Summary_Md.ToFileSuffix()), StringComparison.OrdinalIgnoreCase))
            {
                return RecentFileType.Summary_Md;
            }

            // Test results: support "test-results", "testresults"
            var testBase = Path.GetFileNameWithoutExtension(RecentFileType.TestResults_Md.ToFileSuffix());
            if (last.Equals(testBase, StringComparison.OrdinalIgnoreCase) || last.Equals(testBase.Replace("-", ""), StringComparison.OrdinalIgnoreCase))
            {
                return RecentFileType.TestResults_Md;
            }

            // Git: support "git", "git-changes", "gitchanges"
            var gitBase = Path.GetFileNameWithoutExtension(RecentFileType.Git_Md.ToFileSuffix());
            if (last.Equals(gitBase, StringComparison.OrdinalIgnoreCase) || last.Equals("git-changes", StringComparison.OrdinalIgnoreCase) || last.Equals("git", StringComparison.OrdinalIgnoreCase))
            {
                return RecentFileType.Git_Md;
            }

            // Codebase family: pick exact type by extension
            var codebaseBase = Path.GetFileNameWithoutExtension(RecentFileType.Codebase_Md.ToFileSuffix());
            if (codebaseBase.EndsWith(last, StringComparison.OrdinalIgnoreCase))
            {
                return e switch
                {
                    "md" or "markdown" => RecentFileType.Codebase_Md,
                    "docx" => RecentFileType.Codebase_Docx,
                    "pdf" => RecentFileType.Codebase_Pdf,
                    _ => RecentFileType.Codebase_Md
                };
            }
        }

        // Fallbacks: extension-first, then legacy name patterns
        if (e is "md" or "markdown")
        {
            var upper = (fileName ?? string.Empty).ToUpperInvariant();
            if (upper.Contains("GIT-CHANGES") || upper.Contains(".GIT."))
            {
                return RecentFileType.Git_Md;
            }
            if (upper.Contains("TEST-RESULTS") || upper.Contains("TESTRESULTS"))
            {
                return RecentFileType.TestResults_Md;
            }
            if (upper.Contains("SUMMARY"))
            {
                return RecentFileType.Summary_Md;
            }
            if (upper.Contains("PLAN"))
            {
                return RecentFileType.Plan;
            }
            if (upper.Contains("GUIDE"))
            {
                return RecentFileType.Guide;
            }

            return RecentFileType.Unknown;
        }

        if (e == "docx")
        {
            return RecentFileType.Codebase_Docx;
        }
        if (e == "pdf")
        {
            return RecentFileType.Codebase_Pdf;
        }
        return RecentFileType.Unknown;
    }

    // robust last token extraction without altering underscores/slashes on disk
    private static string GetLastToken(string baseName)
    {
        if (string.IsNullOrWhiteSpace(baseName)) return string.Empty;
        // split on space, underscore, and hyphen; keep parsing-only, do not mutate filenames
        var parts = baseName.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? string.Empty : parts[^1];
    }
}