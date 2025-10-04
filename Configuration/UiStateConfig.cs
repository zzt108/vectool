// File: Configuration/UiStateConfig.cs

using System;
using System.Configuration;
using System.IO;
using System.Text.Json;

namespace VecTool.Configuration
{
    /// <summary>
    /// Persists simple UI state such as the last selected vector store name.
    /// Stored as JSON next to vectorStoreFolders.json or in the Generated folder.
    /// </summary>
    public sealed class UiStateConfig
    {
        public string? LastSelectedVectorStore { get; init; }

        public static UiStateConfig Load(string? storageDirectory = null)
        {
            var path = ResolveUiStatePath(storageDirectory);
            if (!File.Exists(path))
            {
                return new UiStateConfig();
            }

            try
            {
                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<UiStateConfig>(json);
                return state ?? new UiStateConfig();
            }
            catch
            {
                // Defensive: ignore corrupt files to avoid blocking UI startup
                return new UiStateConfig();
            }
        }

        public static void Save(UiStateConfig state, string? storageDirectory = null)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));
            var path = ResolveUiStatePath(storageDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private static string ResolveUiStatePath(string? storageDirectory)
        {
            string? baseDir = storageDirectory;

            // Prefer the folder of vectorStoreFolders.json if configured
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                var vectorStoreFoldersPath = ConfigurationManager.AppSettings["vectorStoreFoldersPath"];
                if (!string.IsNullOrWhiteSpace(vectorStoreFoldersPath))
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(vectorStoreFoldersPath);
                        if (!string.IsNullOrWhiteSpace(dir))
                        {
                            baseDir = dir;
                        }
                    }
                    catch
                    {
                        // ignore path errors and fall back
                    }
                }
            }

            // Fallback to "Generated" under the app base directory (pattern used by RecentFilesConfig)
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");
            }

            return Path.Combine(baseDir, "uiState.json");
        }
    }
}
