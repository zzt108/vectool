// Path: Constants/TestStrings.cs
using System;

namespace VecTool.Constants
{
    /// <summary>
    /// Test-only strings for unit and integration tests to avoid inlined literals.
    /// </summary>
    public static class TestStrings
    {
        /// <summary>
        /// Header text for AI context in exported markdown files.
        /// </summary>
        public const string AiExportHeader = @"# Codebase Export Format

**Line Numbering Convention:**
- Each file section starts with `### File: <filename> (Time:<timestamp>)`
- Line numbers RESET to 1 at the start of each fenced code block (```)
- Line numbers END at the closing fence (```)
- Example: If you see line 5 in `MainActivity.kt` and line 5 in `TimerService.kt`, these are DIFFERENT lines in DIFFERENT files

**Usage for AI:**
When referencing code changes, always specify:
1. Full file path (e.g., `app/src/main/java/.../MainActivity.kt`)
2. Line number relative to the file's code fence (1-indexed)
3. Context snippet (2-3 lines before/after)
";

        /// <summary>Sample absolute folder path used in tests.</summary>
        public const string SampleFolder = @"C:\Projects\VecTool\src";

        /// <summary>Sample file name used in tests.</summary>
        public const string SampleFileName = "Program.cs";

        /// <summary>Sample relative path used in tests.</summary>
        public const string SampleRelativePath = @"src\Core\Program.cs";

        /// <summary>Sample file extension used in tests.</summary>
        public const string SampleExtension = ".cs";

        /// <summary>Sample section name used in tests.</summary>
        public const string SampleSection = "Core";

        /// <summary>Intentionally dangerous value for escaping tests.</summary>
        public const string DangerousValue = "a&b<\"c\">'d";

        /// <summary>Expected escaped form of <see cref="DangerousValue"/>.</summary>
        public const string EscapedDangerousValue = "a&amp;b&lt;&quot;c&quot;&gt;&apos;d";

        /// <summary>Sample CSV dependency list.</summary>
        public const string DependsList = @"A.cs,B.cs";

        /// <summary>Sample CSV reverse dependency list.</summary>
        public const string UsedByList = @"Main.cs,App.cs";
    }

    public static class Const
    {
        public const string NA = "N/A";
        public const string All = "All";
    }
}
