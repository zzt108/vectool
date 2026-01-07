using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace VecTool.Studio.Versioning
{
    public static class VersionInfoParser
    {
        // Pattern matches semver-ish-YYYYMMDDHHmm-commit
        private static readonly Regex InfoPattern = new Regex(
            @"(?<head>.*?)-(?<dt>\d{12})-(?<sha>[A-Za-z0-9]+)?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static DateTime? TryParseBuildTimestampUtc(string? informationalVersion)
        {
            if (string.IsNullOrWhiteSpace(informationalVersion)) return null;
            var m = InfoPattern.Match(informationalVersion);
            if (!m.Success) return null;

            var dt = m.Groups["dt"].Value; // yyyyMMddHHmm
            if (DateTime.TryParseExact(dt, "yyyyMMddHHmm", null, DateTimeStyles.AssumeUniversal, out var when))
            {
                return DateTime.SpecifyKind(when, DateTimeKind.Utc);
            }
            return null;
        }

        public static string? TryParseCommitShort(string? informationalVersion)
        {
            if (string.IsNullOrWhiteSpace(informationalVersion)) return null;
            var m = InfoPattern.Match(informationalVersion);
            if (!m.Success) return null;

            var sha = m.Groups["sha"].Value;
            return string.IsNullOrWhiteSpace(sha) ? null : sha;
        }
    }
}