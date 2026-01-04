// Path: Constants/TagBuilder.cs
using System.Text;

namespace VecTool.Constants
{
    /// <summary>
    /// Helper for building formatted tags with proper XML-attribute escaping.
    /// </summary>
    public static class TagBuilder
    {
        /// <summary>
        /// Builds a path attribute fragment with correct XML escaping.
        /// </summary>
        /// <param name="path">File or directory path.</param>
        /// <returns>Formatted attribute fragment, e.g., path="...".</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is null or empty.</exception>
        public static string BuildFilePathTag(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            return string.Format(Tags.FilePath, EscapeXmlAttribute(path));
        }
        public static string BuildFolderNameTag(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentException("Folder name cannot be null or empty.", nameof(folderName));
            return string.Format(Tags.FolderName, EscapeXmlAttribute(folderName));
        }

        /// <summary>
        /// Builds a file name attribute fragment with correct XML escaping.
        /// </summary>
        /// <param name="fileName">File name without path.</param>
        /// <returns>Formatted attribute fragment, e.g., file name="...".</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="fileName"/> is null or empty.</exception>
        public static string BuildFileNameTag(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            return string.Format(Tags.FileName, EscapeXmlAttribute(fileName));
        }

        /// <summary>
        /// Builds a section name attribute fragment with correct XML escaping.
        /// </summary>
        /// <param name="sectionName">Logical section name.</param>
        /// <returns>Formatted attribute fragment, e.g., section name="...".</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="sectionName"/> is null or empty.</exception>
        public static string BuildSectionNameTag(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

            return string.Format(Tags.SectionName, EscapeXmlAttribute(sectionName));
        }

        /// <summary>
        /// Builds an extension attribute fragment with correct XML escaping.
        /// </summary>
        public static string BuildExtensionTag(string extension)
            => string.Format(Tags.FileExtension, EscapeXmlAttribute(extension ?? string.Empty));

        /// <summary>
        /// Builds a depends-on CSV attribute fragment with correct XML escaping.
        /// </summary>
        public static string BuildDependsOnTag(string csvList)
            => string.Format(Tags.DependsOn, EscapeXmlAttribute(csvList ?? string.Empty));

        /// <summary>
        /// Builds a used-by CSV attribute fragment with correct XML escaping.
        /// </summary>
        public static string BuildUsedByTag(string csvList)
            => string.Format(Tags.UsedBy, EscapeXmlAttribute(csvList ?? string.Empty));

        /// <summary>
        /// Builds a language attribute fragment with correct XML escaping.
        /// </summary>
        public static string BuildLanguageTag(string language)
            => string.Format(Tags.Language, EscapeXmlAttribute(language ?? string.Empty));

        // Element helpers for XML-like rendering
        public static string Open(string tag) => $"<{tag}>";
        public static string Close(string tag) => $"</{tag}>";
        public static string OpenWith(string tag, params string[] attributes)
            => attributes is { Length: > 0 } ? $"<{tag} {string.Join(" ", attributes)}>" : $"<{tag}>";
        public static string SelfClosing(string tag, params string[] attributes)
            => attributes is { Length: > 0 } ? $"<{tag} {string.Join(" ", attributes)}/>" : $"<{tag}/>";

        /// <summary>
        /// Builds a size-in-bytes attribute fragment.
        /// </summary>
        public static string BuildSizeBytesTag(long sizeBytes)
            => string.Format(Tags.SizeBytes, sizeBytes);

        /// <summary>
        /// Builds a last-modified attribute fragment in ISO 8601 (UTC).
        /// </summary>
        public static string BuildLastModifiedTag(DateTime dtUtc)
            => string.Format(Tags.LastModified, dtUtc.ToUniversalTime());

        /// <summary>Builds a lines-of-code metric attribute fragment.</summary>
        public static string BuildLinesOfCodeTag(int value) => string.Format(Tags.LinesOfCode, value);

        /// <summary>Builds a code-lines metric attribute fragment.</summary>
        public static string BuildCodeLinesTag(int value) => string.Format(Tags.CodeLines, value);

        /// <summary>Builds a classes-count metric attribute fragment.</summary>
        public static string BuildClassesTag(int value) => string.Format(Tags.Classes, value);

        /// <summary>Builds a methods-count metric attribute fragment.</summary>
        public static string BuildMethodsTag(int value) => string.Format(Tags.Methods, value);

        /// <summary>
        /// Escapes a string for safe inclusion in XML attribute values.
        /// </summary>
        /// <param name="value">Raw value.</param>
        /// <returns>Escaped value with &amp;, &lt;, &gt;, &quot;, &apos; replaced.</returns>
        public static string EscapeXmlAttribute(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var sb = new StringBuilder(value.Length + 16);
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&apos;"); break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }
    }
}
