// File: VecTool.Constants/Attributes.cs
// Path: VecTool.Constants/Attributes.cs

namespace Constants
{
    /// <summary>
    /// XML attribute name constants used in document generation.
    /// Provides type safety and prevents typos in attribute names.
    /// </summary>
    public static class Attributes
    {
        #region File Attributes
        /// <summary>Path attribute name</summary>
        public const string Path = "path";

        /// <summary>Name attribute name</summary>
        public const string Name = "name";

        /// <summary>Extension attribute name</summary>
        public const string Extension = "ext";

        /// <summary>Language attribute name</summary>
        public const string Language = "lang";

        /// <summary>Size attribute name</summary>
        public const string Size = "sizebytes";

        /// <summary>Last modified attribute name</summary>
        public const string LastModified = "lastmodified";
        #endregion

        #region Metrics Attributes
        /// <summary>Lines of code attribute</summary>
        public const string LinesOfCode = "loc";

        /// <summary>Code lines attribute</summary>
        public const string CodeLines = "codelines";

        /// <summary>Classes count attribute</summary>
        public const string Classes = "classes";

        /// <summary>Methods count attribute</summary>
        public const string Methods = "methods";
        #endregion
    }
}
