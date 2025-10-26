// Path: Configuration/RecentFilesConfig.cs
using System;
using System.Configuration;
using System.IO;

namespace VecTool.Configuration
{
    /// <summary>
    /// Configuration for the Recent Files feature with validation and defaults.
    /// </summary>
    public sealed class RecentFilesConfig
    {
        private const string CONFIG_KEY_MAX_COUNT = "recentFilesMaxCount";
        private const string CONFIG_KEY_RETENTION_DAYS = "recentFilesRetentionDays";
        private const string CONFIG_KEY_OUTPUT_PATH = "recentFilesOutputPath";

        public const int DefaultMaxCount = 200;
        public const int DefaultRetentionDays = 30;
        public const string DefaultOutputPath = "Generated";

        public int MaxCount { get; }
        public int RetentionDays { get; }
        public string OutputPath { get; set; }
        public string StorageFilePath { get; }

        /// <summary>
        /// Construct with explicit values; validates ranges and path.
        /// </summary>
        public RecentFilesConfig(int maxCount, int retentionDays, string outputPath)
        {
            if (maxCount <= 0 || maxCount > 10000)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "MaxCount must be between 1 and 10,000.");

            if (retentionDays < 0 || retentionDays > 3650) // 10 years
                throw new ArgumentOutOfRangeException(nameof(retentionDays), "RetentionDays must be between 0 and 3650.");

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("OutputPath is required and cannot be empty.", nameof(outputPath));

            if (outputPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new ArgumentException("OutputPath contains invalid path characters.", nameof(outputPath));

            MaxCount = maxCount;
            RetentionDays = retentionDays;
            OutputPath = outputPath;
            StorageFilePath = Path.Combine(OutputPath, "recentFiles.json");
        }

        /// <summary>
        /// Factory method to load configuration from appSettings with defaults and validation.
        /// </summary>
        public static RecentFilesConfig FromAppConfig(IAppSettingsReader? reader = null)
        {
            reader ??= new ConfigurationManagerAppSettingsReader();

            int maxCount = ParseIntOrDefault(reader.Get(CONFIG_KEY_MAX_COUNT), DefaultMaxCount);
            int retention = ParseIntOrDefault(reader.Get(CONFIG_KEY_RETENTION_DAYS), DefaultRetentionDays);
            string output = reader.Get(CONFIG_KEY_OUTPUT_PATH) ?? DefaultOutputPath;
            if (string.IsNullOrWhiteSpace(output))
            {
                output = DefaultOutputPath;
            }

            return new RecentFilesConfig(maxCount, retention, output);
        }

        private static int ParseIntOrDefault(string? value, int defaultValue)
        {
            return int.TryParse(value, out int parsed) ? parsed : defaultValue;
        }
    }
}
