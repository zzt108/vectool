// Path: RecentFiles/FileRecentFilesStore.cs
using VecTool.Configuration.Helpers;
using VecTool.Core.Configuration;

namespace VecTool.RecentFiles
{
    /// <summary>
    /// File-based implementation of recent files storage.
    /// </summary>
    public sealed class FileRecentFilesStore : IRecentFilesStore
    {
        private readonly string _jsonPath;

        public FileRecentFilesStore(RecentFilesConfig config)
        {
            _jsonPath = config?.StorageFilePath.ThrowIfNull(nameof(config))!;
        }

        public string? Read()
        {
            try
            {
                return File.Exists(_jsonPath) ? File.ReadAllText(_jsonPath) : null;
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to read recent files JSON from {_jsonPath}.", ex);
            }
        }

        public void Write(string json)
        {
            try
            {
                var directory = Path.GetDirectoryName(_jsonPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(_jsonPath, json);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to write recent files JSON to {_jsonPath}.", ex);
            }
        }
    }
}