#nullable enable

using LogCtxShared;
using Microsoft.Extensions.Logging;
using VecTool.Configuration;
using VecTool.Configuration.Logging;
using VecTool.Core.Models;
using VecTool.Handlers.Traversal;

namespace VecTool.Handlers
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
        private static readonly ILogger logger = AppLogger.For<PromptSearchEngine>();

        private readonly Dictionary<string, PromptFile> index = new(StringComparer.OrdinalIgnoreCase);
        private readonly IPromptsConfig? config;
        private const int ContentSearchLimit = 2000;

        public PromptSearchEngine(IPromptsConfig? config)
        {
            this.config = config;
        }

        /// <summary>
        /// Rebuilds the index from disk by traversing the repository path.
        /// Parses each file into PromptFile and adds to index.
        /// </summary>
        public void RebuildIndex()
        {
            using var ctx = logger.SetContext()
                .Add("repositoryPath", config?.RepositoryPath)
                .Add("operation", "RebuildIndex");

            index.Clear();

            if (string.IsNullOrWhiteSpace(config?.RepositoryPath) || !Directory.Exists(config.RepositoryPath))
            {
                logger.LogWarning($"Repository path is invalid or does not exist: {config?.RepositoryPath}");
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
                //foreach (var filePath in Directory.EnumerateFiles(config.RepositoryPath, "*.*", SearchOption.AllDirectories))
                //{
                //    filesProcessed++;

                //    var ext = Path.GetExtension(filePath);
                //    if (!extensions.Contains(ext))
                //    {
                //        logger.LogTrace($"Skipping file with non-matching extension: {filePath}");
                //        continue;
                //    }

                //    try
                //    {
                //        // Read file content
                //        var content = File.ReadAllText(filePath);
                //        var lastModified = File.GetLastWriteTime(filePath);
                //        var firstLine = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                //        var relativePath = Path.GetRelativePath(config.RepositoryPath, filePath).Replace('\\', '/');

                //        // Parse metadata
                //        var metadata = PromptMetadata.Parse(relativePath, firstLine);
                //        if (metadata == null)
                //        {
                //            logger.LogDebug($"Failed to parse metadata for file: {filePath}");
                //            continue;
                //        }

                //        // Create PromptFile
                //        var promptFile = new PromptFile(filePath, relativePath, metadata, content, lastModified, isFavorite: false);

                //        // Add to index
                //        index[filePath] = promptFile;
                //        filesIndexed++;

                //        logger.LogTrace($"Indexed file: {Path.GetFileName(filePath)}");
                //    }
                //    catch (Exception ex)
                //    {
                //        logger.Error(ex, $"Failed to index file: {filePath}");
                //    }
                //}

                // Traverse repository recursively using FileSystemTraverser to honor .vtignore/.gitignore [file:2]
                var traversalConfig = new VectorStoreConfig(config.RepositoryPath);  // ✅ NEW: configure root for traverser [file:1][file:2]
                                                                                     // Layer 2 marker exclusion (VECTOOLEXCLUDE...) is optional; enable for consistency with other features. [file:2]
                var fileMarkerExtractor = new FileMarkerExtractor();                 // ✅ NEW [file:2]
                var fileSystemTraverser = new FileSystemTraverser(ui: null, markerExtractor: fileMarkerExtractor); // ✅ NEW [file:2]

                // 🔄 MODIFY: use traverser.EnumerateFilesRespectingExclusions instead of Directory.EnumerateFiles [file:1][file:2]
                foreach (var filePath in fileSystemTraverser.EnumerateFilesRespectingExclusions(config.RepositoryPath, traversalConfig))
                {
                    filesProcessed++;

                    var ext = Path.GetExtension(filePath);
                    if (!extensions.Contains(ext))
                    {
                        logger.LogTrace($"Skipping file with non-matching extension: {filePath}");  // unchanged behavior [file:1]
                        continue;
                    }

                    try
                    {
                        // Read file content
                        var content = File.ReadAllText(filePath);  // unchanged [file:1]
                        var lastModified = File.GetLastWriteTime(filePath);  // unchanged [file:1]
                        var firstLine = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();  // unchanged [file:1]
                        var relativePath = Path.GetRelativePath(config.RepositoryPath, filePath).Replace('\\', '/');  // unchanged [file:1]

                        // Parse metadata
                        var metadata = PromptMetadata.Parse(relativePath, firstLine);  // unchanged API [file:1]
                        if (metadata == null)
                        {
                            logger.LogDebug($"Failed to parse metadata for file: {filePath}");  // unchanged [file:1]
                            continue;
                        }

                        // Create PromptFile
                        var promptFile = new PromptFile(filePath, relativePath, metadata, content, lastModified, isFavorite: false);  // unchanged [file:1]

                        // Add to index
                        index[filePath] = promptFile;
                        filesIndexed++;

                        logger.LogTrace($"Indexed file: {Path.GetFileName(filePath)}");  // unchanged [file:1]
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to index file: {filePath}");  // unchanged [file:1]
                    }
                }

                logger.LogInformation($"Index rebuilt successfully: {filesIndexed} files indexed out of {filesProcessed} processed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error during index rebuild: {ex.Message}");
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
            using var ctx = logger.SetContext()
                .Add("query", query)
                .Add("indexSize", index.Count);

            if (string.IsNullOrWhiteSpace(query))
            {
                // Empty search returns all files
                logger.LogDebug("Empty search query - returning all indexed files");
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

            logger.LogInformation($"Search completed: {results.Count} results found for query '{query}'");
            return results;
        }

        /// <summary>
        /// Filters indexed files by hierarchy properties (area/project/category).
        /// Null parameters are ignored (treated as wildcards).
        /// </summary>
        public List<PromptFile> GetByHierarchy(string? area, string? project, string? category)
        {
            using var ctx = logger.SetContext()
                .Add("area", area ?? "any")
                .Add("project", project ?? "any")
                .Add("category", category ?? "any")
                .Add("indexSize", index.Count);

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

            logger.LogInformation($"Hierarchy filter completed: {results.Count} files matched");
            return results;
        }

        /// <summary>
        /// Returns the total count of indexed files.
        /// </summary>
        public int GetIndexedFileCount() => index.Count;
    }
}