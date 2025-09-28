// File: VecTool.Constants/Tags.cs  
// Path: VecTool.Constants/Tags.cs

namespace Constants
{
    /// <summary>
    /// Core XML tag constants used throughout VecTool for content generation and metadata.
    /// Centralized to eliminate magic strings and improve maintainability.
    /// </summary>
    public static class Tags
    {
        #region Metadata Tags
        /// <summary>File metadata tag with path information</summary>
        public const string FilePath = "file path=\"{0}\"";

        /// <summary>File name tag</summary> 
        public const string FileName = "file name=\"{0}\"";

        /// <summary>File extension tag</summary>
        public const string FileExtension = "file ext=\"{0}\"";

        /// <summary>File language tag</summary>
        public const string FileLanguage = "file lang=\"{0}\"";

        /// <summary>File size in bytes tag</summary>
        public const string FileSize = "file sizebytes=\"{0}\"";

        /// <summary>File last modified timestamp tag</summary>
        public const string FileLastModified = "file lastmodified=\"{0}\"";

        /// <summary>File properties wrapper tag</summary>
        public const string FileProperties = "FileProps";
        #endregion

        #region Structure Tags  
        /// <summary>Table of contents section tag</summary>
        public const string TableOfContents = "tableofcontents";

        /// <summary>Cross references section tag</summary>
        public const string CrossReferences = "crossreferences";

        /// <summary>Section name with identifier</summary>
        public const string SectionName = "section name=\"{0}\"";

        /// <summary>File content section wrapper</summary>
        public const string FileContent = "filecontent";
        #endregion

        #region Content Generation Tags
        /// <summary>AI guidance prompt tag</summary>
        public const string AIGuidance = "aiguidance";

        /// <summary>Project summary tag</summary> 
        public const string ProjectSummary = "projectsummary";

        /// <summary>Body content wrapper</summary>
        public const string BodyContent = "body";

        /// <summary>Content wrapper tag</summary>
        public const string Content = "content";
        #endregion
    }
}
