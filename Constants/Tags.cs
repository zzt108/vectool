// Path: Constants/Tags.cs
using System;

namespace VecTool.Constants
{
    /// <summary>
    /// Core XML-like tag and attribute-format constants for document generation and metadata payloads.
    /// </summary>
    public static class Tags
    {
        /// <summary>Marker for table of contents section.</summary>
        public const string TableOfContents = "tableofcontents";

        /// <summary>Marker for a logical section grouping.</summary>
        public const string Section = "section";

        /// <summary>Marker for a file entry.</summary>
        public const string File = "file";

        /// <summary>Marker for cross-reference section.</summary>
        public const string CrossReferences = "crossreferences";

        /// <summary>Marker for code meta-information block.</summary>
        public const string CodeMetaInfo = "codemetainfo";

        /// <summary>Marker for file properties/metadata block.</summary>
        public const string FileProperties = "fileproperties";

        /// <summary>Marker for AI guidance/instructions block.</summary>
        public const string AIGuidance = "aiguidance";
        
        /// <summary>Marker for AI context block containing table of contents, cross-references, and code metadata.</summary>
        public const string AiContext = "aicontext";

        /// <summary>Attribute fragment for section name. Usage: string.Format(SectionName, value)</summary>
        public const string SectionName = "section name=\"{0}\"";

        /// <summary>Attribute fragment for file name. Usage: string.Format(FileName, value)</summary>
        public const string FileName = "file name=\"{0}\"";

        /// <summary>Attribute fragment for file path. Usage: string.Format(FilePath, value)</summary>
        public const string FilePath = "path=\"{0}\"";

        /// <summary>Attribute fragment for file extension. Usage: string.Format(FileExtension, value)</summary>
        public const string FileExtension = "ext=\"{0}\"";
        // (Folder element + attribute)
        public const string Folder = "folder";
        public const string FolderName = "folder name=\"{0}\"";

        /// <summary>Attribute fragment for dependencies (CSV).</summary>
        public const string DependsOn = "dependson=\"{0}\"";

        /// <summary>Attribute fragment for reverse dependencies (CSV).</summary>
        public const string UsedBy = "usedby=\"{0}\"";

        /// <summary>Attribute fragment for language code.</summary>
        public const string Language = "lang=\"{0}\"";

        /// <summary>Attribute fragment for file size in bytes.</summary>
        public const string SizeBytes = "sizebytes=\"{0}\"";

        /// <summary>Attribute fragment for last modified timestamp (ISO 8601).</summary>
        public const string LastModified = "lastmodified=\"{0:O}\"";

        /// <summary>Attribute fragment for lines of code metric.</summary>
        public const string LinesOfCode = "loc=\"{0}\"";

        /// <summary>Attribute fragment for code lines metric.</summary>
        public const string CodeLines = "codelines=\"{0}\"";

        /// <summary>Attribute fragment for classes count metric.</summary>
        public const string Classes = "classes=\"{0}\"";

        /// <summary>Attribute fragment for methods count metric.</summary>
        public const string Methods = "methods=\"{0}\"";

        /// <summary>Attribute fragment for complexity metric.</summary>
        public const string Complexity = "complexity=\"{0}\"";

        /// <summary>Attribute fragment for detected patterns list.</summary>
        public const string Patterns = "patterns=\"{0}\"";

        /// <summary>Attribute fragment for long methods count.</summary>
        public const string LongMethods = "longmethodscount=\"{0}\"";

        /// <summary>Attribute fragment for TODOs count.</summary>
        public const string Todos = "todoscount=\"{0}\"";

        /// <summary>Attribute fragment for catches count.</summary>
        public const string Catches = "catchescount=\"{0}\"";

        /// <summary>Attribute fragment indicating project has tests.</summary>
        public const string HasTests = "hastests=\"{0}\"";

        /// <summary>Attribute fragment listing tests or test files.</summary>
        public const string Tests = "tests=\"{0}\"";
    }
}
