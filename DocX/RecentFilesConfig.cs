// File: DocXHandler/RecentFilesConfig.cs
// Purpose: Holds and loads configuration for the “recent files” feature (Step 1).
// Language: All code, comments, and identifiers are in English.

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace DocXHandler
{
    public class RecentFilesConfig
    {
        // Defaults per plan: Max=10, Retention=15 days, Output=%AppData%/VecTool/Generated
        public int MaxCount { get; private set; } = 10;
        public int RetentionDays { get; private set; } = 15;
        public string OutputPath { get; private set; }

        private RecentFilesConfig()
        {
            OutputPath = GetDefaultOutputPath();
        }

        public static RecentFilesConfig FromAppSettings(NameValueCollection? appSettings = null)
        {
            var cfg = new RecentFilesConfig();

            var settings = appSettings ?? ConfigurationManager.AppSettings;

            // Parse integers with safe defaults
            cfg.MaxCount = ParsePositiveInt(settings["recentFilesMaxCount"], defaultValue: 10);
            cfg.RetentionDays = ParsePositiveInt(settings["recentFilesRetentionDays"], defaultValue: 15);

            // Resolve output path, expand environment variables, and normalize
            var rawOut = settings["recentFilesOutputPath"];
            cfg.OutputPath = ResolveOutputPathOrDefault(rawOut);

            return cfg;
        }

        private static int ParsePositiveInt(string? value, int defaultValue)
        {
            if (int.TryParse(value, out var parsed) && parsed > 0)
                return parsed;

            return defaultValue;
        }

        private static string ResolveOutputPathOrDefault(string? value)
        {
            var path = value;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = GetDefaultOutputPath();
            }
            else
            {
                path = Environment.ExpandEnvironmentVariables(path);
                path = path.Replace('\\', Path.DirectorySeparatorChar)
                           .Replace('/', Path.DirectorySeparatorChar);
            }

            return path;
        }

        private static string GetDefaultOutputPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "VecTool", "Generated");
        }
    }
}
