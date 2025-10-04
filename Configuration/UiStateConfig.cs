// File: Configuration/UiStateConfig.cs

using System;
using System.Configuration;
using System.Text.Json;

namespace VecTool.Configuration
{
    /// <summary>
    /// Persists simple UI state such as the last selected vector store name and Recent Files layout.
    /// Stored as JSON next to vectorStoreFolders.json if configured, otherwise under the app "Generated" folder. 
    /// </summary>
    public sealed class UiStateConfig
    {
        /// <summary>
        /// The last selected vector store name.
        /// </summary>
        public string? LastSelectedVectorStore { get; set; }

        /// <summary>
        /// Recent Files ListView column widths keyed by column header text.
        /// </summary>
        public Dictionary<string, int>? RecentFilesColumnWidths { get; set; }

        /// <summary>
        /// Applied row-height scale for Recent Files ListView; if not set, consumer should use default (e.g., 1.10).
        /// </summary>
        public double? RecentFilesRowHeightScale { get; set; }

        /// <summary>
        /// Load UI state from uiState.json located in a resolved storage directory.
        /// Returns a new default instance if the file does not exist or cannot be parsed.
        /// </summary>
        /// <param name="storageDirectory">Optional explicit directory to load from; if null, a default location is resolved.</param>
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
                var state = JsonSerializer.Deserialize<UiStateConfig>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return state ?? new UiStateConfig();
            }
            catch
            {
                // Defensive: ignore corrupt files to avoid blocking UI startup.
                return new UiStateConfig();
            }
        }

        /// <summary>
        /// Save UI state into uiState.json located in a resolved storage directory.
        /// Creates the directory if needed and overwrites the file atomically.
        /// </summary>
        /// <param name="state">The state to persist.</param>
        /// <param name="storageDirectory">Optional explicit directory to save into; if null, a default location is resolved.</param>
        public static void Save(UiStateConfig state, string? storageDirectory = null)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));

            var path = ResolveUiStatePath(storageDirectory);
            var dir = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(dir))
            {
                throw new InvalidOperationException("Could not resolve directory for uiState.json.");
            }

            Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            // Write atomically to reduce chance of corrupting the file.
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }

        /// <summary>
        /// Resolves the full path to uiState.json based on explicit directory, configured vectorStoreFoldersPath, or the app's Generated folder.
        /// </summary>
        private static string ResolveUiStatePath(string? storageDirectory)
        {
            string? baseDir = storageDirectory;

            // Prefer the folder of vectorStoreFolders.json if configured
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                try
                {
                    var vectorStoreFoldersPath = ConfigurationManager.AppSettings["vectorStoreFoldersPath"];
                    if (!string.IsNullOrWhiteSpace(vectorStoreFoldersPath))
                    {
                        var dir = Path.GetDirectoryName(vectorStoreFoldersPath);
                        if (!string.IsNullOrWhiteSpace(dir))
                        {
                            baseDir = dir;
                        }
                    }
                }
                catch
                {
                    // ignore config access errors and fall back
                }
            }

            // Fallback to "Generated" under the app base directory
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");
            }

            return Path.Combine(baseDir, "uiState.json");
        }
    }
}
