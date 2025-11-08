namespace VecTool.Configuration.Exclusion;

/// <summary>
/// Identifies which .gitignore parser library to use.
/// </summary>
public enum IgnoreLibraryType
{
    /// <summary>
    /// GitignoreParserNet - Full .gitignore spec compliance.
    /// </summary>
    GitignoreParserNet = 0,

    /// <summary>
    /// MAB.DotIgnore - Simpler alternative parser.
    /// </summary>
    MabDotIgnore = 1,

    Auto = 999
}