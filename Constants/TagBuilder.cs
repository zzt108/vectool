// File: VecTool.Constants/TagBuilder.cs
// Path: VecTool.Constants/TagBuilder.cs

using Constants;

namespace Constants
{
    /// <summary>
    /// Helper class for building formatted XML tags with proper escaping.
    /// Provides type-safe tag construction and reduces formatting errors.
    /// </summary>
    public static class TagBuilder
    {
        /// <summary>
        /// Builds a file path tag with proper formatting
        /// </summary>
        /// <param name="path">File path to include</param>
        /// <returns>Formatted file path tag</returns>
        public static string BuildFilePathTag(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            return string.Format(Tags.FilePath, EscapeXmlAttribute(path));
        }

        /// <summary>
        /// Builds a file name tag with proper formatting
        /// </summary>
        /// <param name="fileName">File name to include</param>
        /// <returns>Formatted file name tag</returns>
        public static string BuildFileNameTag(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            return string.Format(Tags.FileName, EscapeXmlAttribute(fileName));
        }

        /// <summary>
        /// Builds a section name tag with proper formatting
        /// </summary>
        /// <param name="sectionName">Section name to include</param>
        /// <returns>Formatted section name tag</returns>
        public static string BuildSectionNameTag(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

            return string.Format(Tags.SectionName, EscapeXmlAttribute(sectionName));
        }

        /// <summary>
        /// Escapes XML attribute values to prevent malformed XML
        /// </summary>
        /// <param name="value">Value to escape</param>
        /// <returns>Escaped XML attribute value</returns>
        private static string EscapeXmlAttribute(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
