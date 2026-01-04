#nullable enable

using LogCtxShared;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VecTool.Configuration.Helpers;
using VecTool.Configuration.Logging;

namespace VecTool.Core.Helpers
{
    /// <summary>
    /// Manages loading and saving of favorite prompt file paths to JSON.
    /// </summary>
    public sealed class FavoritesManager
    {
        private static readonly ILogger logger = AppLogger.For<FavoritesManager>();

        /// <summary>
        /// Loads favorites from JSON file.
        /// </summary>
        /// <param name="configPath">Path to favorites JSON file.</param>
        /// <returns>List of favorite file paths, or empty list if file doesn't exist or is invalid.</returns>
        public List<string> LoadFavorites(string configPath)
        {
            using var ctx = logger.SetContext(new Props().Add("configPath", configPath));

            if (string.IsNullOrWhiteSpace(configPath))
            {
                logger.LogWarning("Favorites config path is null or empty");
                return new List<string>();
            }

            if (!File.Exists(configPath))
            {
                logger.LogInformation($"Favorites file not found, returning empty list: {configPath}");
                return new List<string>();
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var favorites = JsonSerializer.Deserialize<FavoritesData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                if (favorites?.Favorites == null)
                {
                    logger.LogWarning("Favorites JSON is null or missing 'favorites' array");
                    return new List<string>();
                }

                logger.LogInformation($"Loaded {favorites.Favorites.Count} favorites from {configPath}");
                return favorites.Favorites.ConvertAll(f => f.Path);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, $"Failed to parse favorites JSON: {ex.Message}");
                return new List<string>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unexpected error loading favorites: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Saves favorites to JSON file.
        /// </summary>
        /// <param name="configPath">Path to favorites JSON file.</param>
        /// <param name="favorites">List of favorite file paths.</param>
        public void SaveFavorites(string configPath, List<string> favorites)
        {
            using var ctx = logger.SetContext(new Props()
                .Add("configPath", configPath)
                .Add("count", favorites?.Count ?? 0));

            configPath.ThrowIfNullOrWhiteSpace(nameof(configPath), logger);
            favorites.ThrowIfNull(nameof(favorites), logger);

            try
            {
                var data = new FavoritesData
                {
                    Favorites = favorites.ConvertAll(path => new FavoriteEntry
                    {
                        Path = path,
                        Label = Path.GetFileNameWithoutExtension(path),
                        Rank = favorites.IndexOf(path) + 1
                    })
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Ensure directory exists
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(configPath, json);
                logger.LogInformation($"Saved {favorites.Count} favorites to {configPath}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to save favorites: {ex.Message}");
                throw;
            }
        }

        // Internal models for JSON serialization
        private sealed class FavoritesData
        {
            public List<FavoriteEntry> Favorites { get; set; } = new();
        }

        private sealed class FavoriteEntry
        {
            public string Path { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public int Rank { get; set; }
        }
    }
}