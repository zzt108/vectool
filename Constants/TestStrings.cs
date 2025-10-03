// Path: Constants/TestStrings.cs
using System;

namespace VecTool.Constants
{
    /// <summary>
    /// Test-only strings for unit and integration tests to avoid inlined literals.
    /// </summary>
    public static class TestStrings
    {
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
}
