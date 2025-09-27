// OaiUI/Services/LastSelectionService.cs
using System;
using System.IO;
using System.Text.Json;

namespace oaiUI.Services
{
    public interface ILastSelectionService
    {
        string? GetLastSelectedVectorStore();
        void SetLastSelectedVectorStore(string? name);
    }

    public sealed class LastSelectionService : ILastSelectionService
    {
        private readonly string _settingsPath;
        private readonly object _sync = new();

        private sealed class UserSettings
        {
            public string? LastSelectedVectorStore { get; set; }
        }

        public LastSelectionService(string? baseFolder = null, string fileName = "user-settings.json")
        {
            var baseDir = baseFolder ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VecTool");
            Directory.CreateDirectory(baseDir);
            _settingsPath = Path.Combine(baseDir, fileName);
        }

        public string? GetLastSelectedVectorStore()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                    return null;

                lock (_sync)
                {
                    var json = File.ReadAllText(_settingsPath);
                    var data = JsonSerializer.Deserialize<UserSettings>(json);
                    return data?.LastSelectedVectorStore;
                }
            }
            catch
            {
                // Be defensive: if anything goes wrong, don't block startup
                return null;
            }
        }

        public void SetLastSelectedVectorStore(string? name)
        {
            try
            {
                var data = new UserSettings { LastSelectedVectorStore = string.IsNullOrWhiteSpace(name) ? null : name };
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                lock (_sync)
                {
                    File.WriteAllText(_settingsPath, json);
                }
            }
            catch
            {
                // Swallow: do not crash UI because of persistence failure
            }
        }
    }
}
