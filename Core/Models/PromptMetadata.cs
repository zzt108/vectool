#nullable enable
using System;
using System.IO;
using System.Linq;
using LogCtxShared;
using NLogShared;
using VecTool.Constants;

namespace VecTool.Core.Models
{
    /// <summary>
    /// Metadata parsed from prompt filename and path hierarchy.
    /// Naming convention: {TYPE}-{VERSION}-{NAME}.{ext}
    /// Example: PROMPT-1.0-analyzer.md → Type=PROMPT, Version=1.0, Name=analyzer
    /// Path: C:/work/vectortool/spaces/PROMPT-1.0-analyzer.md → Area=work, Project=vectortool, Category=spaces
    /// </summary>
    public sealed record PromptMetadata
    {
        private static readonly CtxLogger log = new();

        public string FileName { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty; // e.g., "1.0", "1.1"
        public string Name { get; init; } = string.Empty; // e.g., "analyzer", "git-integration"
        public string Type { get; init; } = string.Empty; // e.g., "PROMPT", "GUIDE", "SPACE"
        public string? Description { get; init; } // From first line/comment (optional)
        public string Area { get; init; } = string.Empty; // e.g., "work", "private", "development"
        public string Project { get; init; } = string.Empty; // e.g., "VecTool", "LINX", "AgileAI"
        public string Category { get; init; } = string.Empty; // e.g., "Spaces", "Guides"

        /// <summary>
        /// Parse filename and path into metadata.
        /// Returns null if filename doesn't match expected pattern (logs warning).
        /// </summary>
        public static PromptMetadata? Parse(string relativePath, string? firstLineContent = null)
        {
            using var ctx = LogCtx.Set(new Props()
                .Add("relativePath", relativePath)
                .Add("firstLine", firstLineContent?.Substring(0, Math.Min(50, firstLineContent?.Length ?? 0))));

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                log.Warn("Full path is null or empty");
                return null;
            }

            var fileName = Path.GetFileName(relativePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                log.Warn("File name could not be extracted from path");
                return null;
            }

            // Parse filename: {TYPE}-{VERSION}-{NAME}.{ext}
            var nameWithoutExt = fileName;
            string ext = Path.GetExtension(fileName);
            var allowedExtensions = PromptsConfig.DefaultFileExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (!string.IsNullOrWhiteSpace(ext) && allowedExtensions.FirstOrDefault(x => x == ext) is not null)
                nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

            var parts = nameWithoutExt.Split('-', StringSplitOptions.RemoveEmptyEntries);

            string type, version, name;

            if (parts.Length >= 3)
            {
                // Standard format: TYPE-VERSION-NAME
                type = parts[0].Trim();
                version = parts[1].Trim();
                name = string.Join("-", parts.Skip(2)).Trim();
            }
            else if (parts.Length == 2)
            {
                // TYPE-NAME format (missing version) → default version "0.0"
                type = parts[0].Trim();
                version = "0.0"; // Default version
                name = parts[1].Trim();
                log.Debug($"Filename missing version, using default: {fileName} → version={version}");
            }
            else if (parts.Length == 1)
            {
                // TYPE only (e.g., "PROMPT.md") → default version + name
                type = "Unknown";
                version = "0.0";
                name = parts[0].Trim();
                log.Debug($"Filename minimal format, using defaults: {fileName} → version={version}, name={name}");
            }
            else
            {
                // ❌ REMOVE: Reject completely invalid filenames
                log.Warn($"Filename does not match expected pattern (TYPE-VERSION-NAME or TYPE-NAME or TYPE): {fileName}");
                return null;
            }

            // Validate file extension (forgiving behavior only for recognized extensions)
            if (!allowedExtensions.Any(e => e.Trim().Equals(ext, StringComparison.OrdinalIgnoreCase)))
            {
                log.Warn($"File extension not in allowed list: {fileName} (ext={ext})");
                return null;
            }

            // Parse path hierarchy: /area/project/category/filename.md
            var (area, project, category) = ExtractHierarchy(relativePath);
            
            var description = firstLineContent?.Trim();
            if (!string.IsNullOrEmpty(description) && description.Length > 200)
            {
                description = description.Substring(0, 200) + "...";
            }
            
            var metadata = new PromptMetadata
            {
                FileName = fileName,
                Version = version,
                Name = name,
                Type = type,
                Description = description,
                Area = area,
                Project = project,
                Category = category
            };

            log.Debug($"Parsed metadata: Type={type}, Version={version}, Name={name}, Area={area}, Project={project}, Category={category}");
            return metadata;
        }

        /// <summary>
        /// Extract area/project/category from filesystem path.
        /// Example: C:/work/vectortool/spaces/PROMPT-1.0-analyzer.md → area=work, project=vectortool, category=spaces
        /// Root-level files return empty strings for all fields.
        /// </summary>
        private static (string Area, string Project, string Category) ExtractHierarchy(string relativePath)
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(relativePath);
                if (string.IsNullOrWhiteSpace(directoryPath))
                    return (Const.NA, Const.NA, Const.NA);

                // Split path into segments (handle both / and \)
                var segments = directoryPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                segments = [.. segments.Where(s => !s.EndsWith(':'))];

                // Extract last 3 segments as category, project, area (in reverse order)
                var category = segments.Length >= 3 ? segments[2] : Const.NA;
                var project = segments.Length >= 2 ? segments[1] : Const.NA;
                var area = segments.Length >= 1 ? segments[0] : Const.NA;

                return (area, project, category);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to extract hierarchy from path");
                return (string.Empty, string.Empty, string.Empty);
            }
        }

        public IReadOnlyList<string> GetDisplayHierarchy()
        {
            var segments = new List<string>();

            // Skip unknown area
            if (!string.IsNullOrWhiteSpace(Area) &&
                !string.Equals(Area, Const.NA, StringComparison.OrdinalIgnoreCase))
            {
                segments.Add(Area);
            }

            // Skip unknown project
            if (!string.IsNullOrWhiteSpace(Project) &&
                !string.Equals(Project, Const.NA, StringComparison.OrdinalIgnoreCase))
            {
                segments.Add(Project);
            }

            // Skip unknown category
            if (!string.IsNullOrWhiteSpace(Category) &&
                !string.Equals(Category, Const.NA, StringComparison.OrdinalIgnoreCase))
            {
                segments.Add(Category);
            }

            // Fallback: if everything above was NA/empty but Category is a real folder,
            // place the file directly under that folder as a top level.
            if (segments.Count == 0 &&
                !string.IsNullOrWhiteSpace(Category) &&
                !string.Equals(Category, Const.NA, StringComparison.OrdinalIgnoreCase))
            {
                segments.Add(Category);
            }

            return segments;
        }

        /// <summary>
        /// Build a standard filename using the prompt naming convention.
        /// Pattern: TYPE-VERSION-NAME.ext (or TYPE-NAME.ext if version is empty).
        /// </summary>
        /// <param name="type">Prompt type, e.g. PROMPT, GUIDE, SPACE.</param>
        /// <param name="version">Version string, e.g. 1.0, 1.1 (may be empty).</param>
        /// <param name="name">Prompt name slug, e.g. analyzer, git-integration.</param>
        /// <param name="extension">
        /// File extension including or excluding leading dot, e.g. ".md" or "md".
        /// </param>
        /// <returns>Standardized filename.</returns>
        public static string BuildFileName(string type, string version, string name, string extension)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            type = type.Trim();
            version = version?.Trim() ?? string.Empty;
            name = name.Trim();

            if (type.Length == 0)
            {
                throw new ArgumentException("Type is required.", nameof(type));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("Name is required.", nameof(name));
            }

            var core = string.IsNullOrWhiteSpace(version)
                ? $"{type}-{name}"
                : $"{type}-{version}-{name}";

            if (string.IsNullOrWhiteSpace(extension))
            {
                return core;
            }

            extension = extension.Trim();
            if (!extension.StartsWith(".", StringComparison.Ordinal))
            {
                extension = "." + extension;
            }

            return core + extension;
        }
    }
}


