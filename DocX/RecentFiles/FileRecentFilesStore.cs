// File: DocXHandler/RecentFiles/FileRecentFilesStore.cs
namespace DocXHandler.RecentFiles
{
    // Stores JSON snapshot of recent files list in a single file under OutputPath.
    public sealed class FileRecentFilesStore : IRecentFilesStore
    {
        private readonly string _jsonPath;

        public FileRecentFilesStore(RecentFilesConfig config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.OutputPath))
                throw new ArgumentException("OutputPath must not be empty.", nameof(config));

            Directory.CreateDirectory(config.OutputPath);
            _jsonPath = Path.Combine(config.OutputPath, "recentFiles.json");
        }

        public string? Read()
        {
            try
            {
                if (!File.Exists(_jsonPath))
                    return null;

                return File.ReadAllText(_jsonPath);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to read recent files JSON from '{_jsonPath}'.", ex);
            }
        }

        public void Write(string json)
        {
            if (json is null) throw new ArgumentNullException(nameof(json));

            try
            {
                File.WriteAllText(_jsonPath, json);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to write recent files JSON to '{_jsonPath}'.", ex);
            }
        }
    }
}
