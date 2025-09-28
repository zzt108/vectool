// File: VecTool.Constants/TestStrings.cs
// Path: VecTool.Constants/TestStrings.cs

namespace Constants
{
    /// <summary>
    /// Constants specifically for unit tests to eliminate magic strings in test code.
    /// These should NEVER be used in production code.
    /// </summary>
    public static class TestStrings
    {
        #region Test File Paths
        /// <summary>Common test file path pattern</summary>
        public const string TestFilePath = "C:\\TestPath\\TestFile.cs";

        /// <summary>Test folder path pattern</summary>
        public const string TestFolderPath = "C:\\TestFolder";

        /// <summary>Test only XML tag for unit testing</summary>
        public const string TestOnlyTag = "testonlytag";
        #endregion

        #region Mock Data
        /// <summary>Sample test content for file processing</summary>
        public const string TestFileContent = "using System;\nclass TestClass {\n    void TestMethod() { }\n}";

        /// <summary>Test project name</summary>
        public const string TestProjectName = "VecTool.TestProject";
        #endregion

        #region Test Assertions
        /// <summary>Expected generated content marker</summary>
        public const string ExpectedContentMarker = "<!-- Generated Test Content -->";

        /// <summary>Test validation marker</summary> 
        public const string TestValidationMarker = "TEST_VALIDATION_PASSED";
        #endregion
    }
}
