#nullable enable
using System;

namespace VecTool.Core.Models
{
    /// <summary>
    /// Represents a prompt file with metadata, content, and favorites state.
    /// Immutable record type for thread-safe caching and indexing.
    /// </summary>
    public sealed record PromptFile
    {
        public string FullPath { get; init; } = string.Empty;
        public string RelativePath { get; init; } = string.Empty;
        public PromptMetadata Metadata { get; init; } = null!;
        public string Content { get; init; } = string.Empty;
        public bool IsFavorite { get; set; } // Mutable: updated by FavoritesManager
        public DateTime LastModified { get; init; }

        /// <summary>
        /// Create a PromptFile instance with required fields.
        /// </summary>
        public PromptFile(string fullPath, string relativePath, PromptMetadata metadata, string content, DateTime lastModified, bool isFavorite = false)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("Full path is required", nameof(fullPath));

            FullPath = fullPath;
            RelativePath = relativePath; 
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            Content = content ?? string.Empty;
            LastModified = lastModified;
            IsFavorite = isFavorite;
        }
    }
}
