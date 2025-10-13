#nullable enable
using System;
using System.IO;
using System.Text.Json;
using NLog;

namespace VecTool.Services
{
    /// <summary>
    /// Persists the last selected vector store name to a JSON file.
    /// Thread-safe for single-user desktop scenarios.
    /// </summary>
    public sealed class LastSelectionService
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        private readonly string filePath;
        private readonly object syncLock = new object();

        /// <summary>
        /// Initializes a new instance with the specified directory and filename.
        /// </summary>
        /// <param name="directory">Directory where the settings file will be stored.</param>
        /// <param name="fileName">Name of the JSON settings file.</param>
        public LastSelectionService(string directory, string fileName = "user-settings.json")
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Directory cannot be empty.", nameof(directory));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty.", nameof(fileName));

            filePath = Path.Combine(directory, fileName);

            // Ensure directory exists
            try
            {
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create directory for LastSelectionService: {Directory}", directory);
                throw;
            }
        }

        /// <summary>
        /// Gets the last selected vector store name, or null if none is persisted.
        /// </summary>
        public string? GetLastSelectedVectorStore()
        {
            lock (syncLock)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        Log.Debug("Settings file does not exist: {FilePath}", filePath);
                        return null;
                    }

                    var json = File.ReadAllText(filePath);
                    if (string.IsNullOrWhiteSpace(json))
                        return null;

                    var data = JsonSerializer.Deserialize<SettingsData>(json);
                    Log.Debug("Loaded last selected vector store: {StoreName}", data?.SelectedVectorStore ?? "null");
                    return data?.SelectedVectorStore;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Failed to read last selection from {FilePath}", filePath);
                    return null;
                }
            }
        }

        /// <summary>
        /// Sets the last selected vector store name. Pass null or empty to clear the selection.
        /// </summary>
        public void SetLastSelectedVectorStore(string? storeName)
        {
            lock (syncLock)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(storeName))
                    {
                        // Clear the selection by deleting the file
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            Log.Info("Cleared last selected vector store");
                        }
                        return;
                    }

                    var data = new SettingsData { SelectedVectorStore = storeName };
                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, json);
                    Log.Info("Saved last selected vector store: {StoreName}", storeName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to save last selection to {FilePath}", filePath);
                    throw;
                }
            }
        }

        private sealed class SettingsData
        {
            public string? SelectedVectorStore { get; set; }
        }
    }
}
