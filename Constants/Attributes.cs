// Path: Constants/Attributes.cs
using System;

namespace VecTool.Constants
{
    /// <summary>
    /// XML-like attribute name constants used throughout the payloads (name-only, not formatted fragments).
    /// </summary>
    public static class Attributes
    {
        /// <summary>Attribute name for a file system path.</summary>
        public const string Path = "path";

        /// <summary>Attribute name for a logical or file name.</summary>
        public const string Name = "name";

        /// <summary>Attribute name for a file extension.</summary>
        public const string Extension = "ext";

        /// <summary>Attribute name for programming or document language.</summary>
        public const string Language = "lang";

        /// <summary>Attribute name for file size in bytes.</summary>
        public const string Size = "sizebytes";

        /// <summary>Attribute name for last modified timestamp (ISO 8601).</summary>
        public const string LastModified = "lastmodified";

        /// <summary>Attribute name for lines of code metric.</summary>
        public const string LinesOfCode = "loc";

        /// <summary>Attribute name for code lines metric.</summary>
        public const string CodeLines = "codelines";

        /// <summary>Attribute name for classes count.</summary>
        public const string Classes = "classes";

        /// <summary>Attribute name for methods count.</summary>
        public const string Methods = "methods";
    }
}
