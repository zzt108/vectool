#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LogCtxShared;
using NLogShared;

namespace VecTool.Core.Helpers
{
    /// <summary>
    /// Manages loading and saving of favorite prompt file paths to JSON.
    /// </summary>
    public sealed class FavoritesManager
    {
        private static readonly CtxLogger log = new();

        /// <summary>
        /// Loads favorites from JSON file.
        /// </summary>
        /// <param name="configPath">Path to favorites JSON file.</param>
        /// <returns>List of favorite file paths, or empty list if file doesn't exist or is invalid.</returns>
        public List<string> LoadFavorites(string configPath)
        {
            using var ctx = log.Ctx.Set(new Props().Add("configPath", configPath));

            if (string.IsNullOrWhiteSpace(configPath))
            {
                log.Warn("Favorites config path is null or empty");
                return new List<string>();
            }

            if (!File.Exists(configPath))
            {
                log.Info($"Favorites file not found, returning empty list: {configPath}");
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
                    log.Warn("Favorites JSON is null or missing 'favorites' array");
                    return new List<string>();
                }

                log.Info($"Loaded {favorites.Favorites.Count} favorites from {configPath}");
                return favorites.Favorites.ConvertAll(f => f.Path);
            }
            catch (JsonException ex)
            {
                log.Error(ex, $"Failed to parse favorites JSON: {ex.Message}");
                return new List<string>();
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Unexpected error loading favorites: {ex.Message}");
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
            using var ctx = log.Ctx.Set(new Props()
                .Add("configPath", configPath)
                .Add("count", favorites?.Count ?? 0));

            if (string.IsNullOrWhiteSpace(configPath))
            {
                var ex = new ArgumentException("Config path is required.", nameof(configPath));
                log.Error(ex, "Config path is null or empty");
                throw ex;
            }

            if (favorites == null)
            {
                var ex = new ArgumentNullException(nameof(favorites));
                log.Error(ex, "Favorites list is null");
                throw ex;
            }

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
                log.Info($"Saved {favorites.Count} favorites to {configPath}");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to save favorites: {ex.Message}");
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
