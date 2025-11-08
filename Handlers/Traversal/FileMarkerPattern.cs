// ✅ FULL FILE VERSION
namespace VecTool.Handlers.Traversal
{
    using System;

    /// <summary>
    /// Represents parsed file-level exclusion marker.
    /// </summary>
    public class FileMarkerPattern
    {
        /// <summary>Full path to file containing marker.</summary>
        public string FilePath { get; set; } = null!;

        /// <summary>Exclusion reason (e.g., "generated_by_xsd", "vendor_library").</summary>
        public string Reason { get; set; } = null!;

        /// <summary>Optional reference to documentation space (e.g., "@XSD-Schema-Docs"). Null if not specified.</summary>
        public string? SpaceReference { get; set; }

        /// <summary>Line number where marker was found (1-indexed).</summary>
        public int LineNumber { get; set; }

        /// <summary>UTC timestamp when marker was extracted.</summary>
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates summary for logging/display.
        /// Format: "{FilePath}:{LineNumber} → {Reason} [{SpaceReference}]"
        /// </summary>
        public override string ToString() =>
            $"{FilePath}:{LineNumber} → {Reason} [{SpaceReference ?? "no-docs"}]";
    }
}
