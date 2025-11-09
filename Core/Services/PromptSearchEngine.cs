#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LogCtxShared;
using NLogShared;
using VecTool.Core.Models;

namespace VecTool.Core.Services
{
    /// <summary>
    /// In-memory search engine for prompt files.
    /// Uses Dictionary-based indexing and LINQ queries.
    /// Simplicity decisions:
    /// - In-memory Dictionary indexed by file path
    /// - LINQ queries for search (no Lucene complexity)
    /// - Search scope: filename, description, first 2000 chars of content
    /// - No ranking/scoring - boolean match only
    /// - RebuildIndex called on app startup + FileSystemWatcher events
    /// </summary>
    public sealed class PromptSearchEngine
    {
        private static readonly CtxLogger log = new();
        private readonly Dictionary<string, PromptFile> index = new(StringComparer.OrdinalIgnoreCase);
        private readonly IPromptsConfig config;
        private const int ContentSearchLimit = 2000;

        public PromptSearchEngine(IPromptsConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Rebuilds the index from disk by traversing the repository path.
        /// Parses each file into PromptFile and adds to index.
        /// </summary>
        public void RebuildIndex()
        {
            using var ctx = log.Ctx.Set(new Props()
                .Add("repositoryPath", config.RepositoryPath)
                .Add("operation", "RebuildIndex"));

            index.Clear();

            if (string.IsNullOrWhiteSpace(config.RepositoryPath) || !Directory.Exists(config.RepositoryPath))
            {
                log.Warn($"Repository path is invalid or does not exist: {config.RepositoryPath}");
                return;
            }

            var extensions = config.FileExtensions
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var filesProcessed = 0;
            var filesIndexed = 0;

            try
            {
                // Traverse repository recursively
                foreach (var filePath in Directory.EnumerateFiles(config.RepositoryPath, "*.*", SearchOption.AllDirectories))
                {
                    filesProcessed++;

                    var ext = Path.GetExtension(filePath);
                    if (!extensions.Contains(ext))
                    {
                        log.Trace($"Skipping file with non-matching extension: {filePath}");
                        continue;
                    }

                    try
                    {
                        // Read file content
                        var content = File.ReadAllText(filePath);
                        var lastModified = File.GetLastWriteTime(filePath);
                        var firstLine = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                        // Parse metadata
                        var metadata = PromptMetadata.Parse(filePath, firstLine);
                        if (metadata == null)
                        {
                            log.Debug($"Failed to parse metadata for file: {filePath}");
                            continue;
                        }

                        // Create PromptFile
                        var promptFile = new PromptFile(filePath, metadata, content, lastModified, isFavorite: false);

                        // Add to index
                        index[filePath] = promptFile;
                        filesIndexed++;

                        log.Trace($"Indexed file: {Path.GetFileName(filePath)}");
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, $"Failed to index file: {filePath}");
                    }
                }

                log.Info($"Index rebuilt successfully: {filesIndexed} files indexed out of {filesProcessed} processed");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error during index rebuild: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Searches indexed files by query string.
        /// Matches on filename, description, and first 2000 chars of content.
        /// Boolean match only - no ranking/scoring.
        /// </summary>
        public List<PromptFile> Search(string query)
        {
            using var ctx = log.Ctx.Set(new Props()
                .Add("query", query)
                .Add("indexSize", index.Count));

            if (string.IsNullOrWhiteSpace(query))
            {
                // Empty search returns all files
                log.Debug("Empty search query - returning all indexed files");
                return index.Values.ToList();
            }

            var queryLower = query.ToLowerInvariant();

            var results = index.Values
                .Where(p =>
                {
                    // Match on filename
                    if (p.Metadata.FileName.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
                        return true;

                    // Match on description
                    if (!string.IsNullOrWhiteSpace(p.Metadata.Description) &&
                        p.Metadata.Description.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
                        return true;

                    // Match on first 2000 chars of content
                    var searchableContent = p.Content.Length > ContentSearchLimit
                        ? p.Content.Substring(0, ContentSearchLimit)
                        : p.Content;

                    if (searchableContent.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
                        return true;

                    return false;
                })
                .ToList();

            log.Info($"Search completed: {results.Count} results found for query '{query}'");
            return results;
        }

        /// <summary>
        /// Filters indexed files by hierarchy properties (area/project/category).
        /// Null parameters are ignored (treated as wildcards).
        /// </summary>
        public List<PromptFile> GetByHierarchy(string? area, string? project, string? category)
        {
            using var ctx = log.Ctx.Set(new Props()
                .Add("area", area ?? "any")
                .Add("project", project ?? "any")
                .Add("category", category ?? "any")
                .Add("indexSize", index.Count));

            var results = index.Values
                .Where(p =>
                {
                    if (area != null && !p.Metadata.Area.Equals(area, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (project != null && !p.Metadata.Project.Equals(project, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (category != null && !p.Metadata.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                        return false;

                    return true;
                })
                .ToList();

            log.Info($"Hierarchy filter completed: {results.Count} files matched");
            return results;
        }

        /// <summary>
        /// Returns the total count of indexed files.
        /// </summary>
        public int GetIndexedFileCount() => index.Count;
    }
}
