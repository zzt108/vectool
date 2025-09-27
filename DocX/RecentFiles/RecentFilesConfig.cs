// Path: DocX/RecentFiles/RecentFilesConfig.cs
using System.Configuration;

namespace DocXHandler.RecentFiles
{
    public interface IAppSettingsReader
    {
        string? Get(string key);
    }

    public sealed class ConfigurationManagerAppSettingsReader : IAppSettingsReader
    {
        public string? Get(string key) => ConfigurationManager.AppSettings[key];
    }

    public sealed class RecentFilesConfig
    {
        public int MaxCount { get; }
        public int RetentionDays { get; }
        public string OutputPath { get; }

        public RecentFilesConfig(int maxCount, int retentionDays, string outputPath)
        {
            MaxCount = maxCount;
            RetentionDays = retentionDays;
            OutputPath = outputPath;
        }

        public static RecentFilesConfig FromAppConfig()
            => FromReader(new ConfigurationManagerAppSettingsReader());

        public static RecentFilesConfig FromReader(IAppSettingsReader reader)
        {
            string? maxCountStr = reader.Get("recentFilesMaxCount");
            string? retentionStr = reader.Get("recentFilesRetentionDays");
            string? outputPath = reader.Get("recentFilesOutputPath");

            int maxCount = ParsePositiveIntOrDefault(maxCountStr, 10, nameof(maxCount));
            int retentionDays = ParsePositiveIntOrDefault(retentionStr, 15, nameof(retentionDays));

            string defaultOutput = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VecTool",
                "Generated");

            string finalOutput = string.IsNullOrWhiteSpace(outputPath) ? defaultOutput : Expand(outputPath);

            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "recentFilesMaxCount must be > 0");
            if (retentionDays <= 0)
                throw new ArgumentOutOfRangeException(nameof(retentionDays), "recentFilesRetentionDays must be > 0");
            if (string.IsNullOrWhiteSpace(finalOutput))
                throw new ArgumentException("recentFilesOutputPath must not be empty after expansion.", nameof(outputPath));

            return new RecentFilesConfig(maxCount, retentionDays, finalOutput);
        }

        private static string Expand(string path)
        {
            var expanded = Environment.ExpandEnvironmentVariables(path ?? string.Empty);
            return expanded.Replace('/', System.IO.Path.DirectorySeparatorChar)
                           .Replace('\\', System.IO.Path.DirectorySeparatorChar);
        }

        private static int ParsePositiveIntOrDefault(string? value, int @default, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                return @default;

            if (!int.TryParse(value, out var parsed) || parsed <= 0)
                throw new ArgumentOutOfRangeException(name, $"{name} must be a positive integer");

            return parsed;
        }
    }
}
