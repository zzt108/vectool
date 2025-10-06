// Path: Handlers/Analysis/FileMetrics.cs

using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace VecTool.Handlers.Analysis
{
    /// <summary>
    /// Immutable metrics captured for a single source file, including size, LOC, method count, TODOs, and a complexity label.
    /// </summary>
    public sealed class FileMetrics
    {
        public string FilePath { get; }
        public string FileName { get; }
        public string Extension { get; }
        public long SizeBytes { get; }
        public int LinesOfCode { get; }
        public int Methods { get; }
        public int TodoCount { get; }
        public string Complexity { get; }

        public FileMetrics(
            string filePath,
            long sizeBytes,
            int linesOfCode,
            int methods,
            int todoCount,
            string complexity)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            FileName = Path.GetFileName(filePath);
            Extension = Path.GetExtension(filePath) ?? string.Empty;
            SizeBytes = sizeBytes;
            LinesOfCode = Math.Max(0, linesOfCode);
            Methods = Math.Max(0, methods);
            TodoCount = Math.Max(0, todoCount);
            Complexity = string.IsNullOrWhiteSpace(complexity) ? "Low" : complexity;
        }

        /// <summary>
        /// Serializes the metrics to a compact XML fragment used by reporting/export handlers.
        /// </summary>
        public string ToXml()
        {
            // Using attributes for compactness; child nodes for expandable items.
            // Example:
            // <file name="Program.cs" ext=".cs" sizeBytes="1234" loc="200" methods="5" codeLines="195">
            //   <todo count="2" />
            //   <complexity level="Medium" />
            // </file>
            var codeLines = Math.Max(0, LinesOfCode - Methods);

            var element = new XElement(
                "file",
                new XAttribute("name", FileName),
                new XAttribute("ext", Extension),
                new XAttribute("sizeBytes", SizeBytes.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("loc", LinesOfCode.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("methods", Methods.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("codeLines", codeLines.ToString(CultureInfo.InvariantCulture)),
                new XElement("todo", new XAttribute("count", TodoCount.ToString(CultureInfo.InvariantCulture))),
                new XElement("complexity", new XAttribute("level", Complexity))
            );

            return element.ToString(SaveOptions.DisableFormatting);
        }
    }
}
