using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Vectool.UI.Versioning
{
    public sealed class AssemblyVersionProvider : IVersionProvider
    {
        private readonly Assembly _assembly;

        public AssemblyVersionProvider() : this(typeof(AssemblyVersionProvider).Assembly) { }

        public AssemblyVersionProvider(Assembly assembly)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        public string ApplicationName =>
            _assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? _assembly.GetName().Name
            ?? "VecTool";

        public string AssemblyVersion => _assembly.GetName().Version?.ToString() ?? "0.0.0.0";

        public string FileVersion
        {
            get
            {
                try
                {
                    var fvi = FileVersionInfo.GetVersionInfo(_assembly.Location);
                    return string.IsNullOrWhiteSpace(fvi.FileVersion) ? "0.0.0.0" : fvi.FileVersion!;
                }
                catch
                {
                    return "0.0.0.0";
                }
            }
        }

        public string InformationalVersion =>
            _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? AssemblyVersion;

        public string? CommitShort => VersionInfoParser.TryParseCommitShort(InformationalVersion);

        public DateTime? BuildTimestampUtc => VersionInfoParser.TryParseBuildTimestampUtc(InformationalVersion);
    }

    public static class VersionInfoParser
    {
        // Pattern matches: <semver-ish>-YYYYMMDDHHmm[-commit]
        private static readonly Regex InfoPattern =
            new(@"^(?<head>.+?)-(?<dt>\d{12})(?:-(?<sha>[A-Za-z0-9]+))?$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static DateTime? TryParseBuildTimestampUtc(string? informationalVersion)
        {
            if (string.IsNullOrWhiteSpace(informationalVersion)) return null;
            var m = InfoPattern.Match(informationalVersion);
            if (!m.Success) return null;
            var dt = m.Groups["dt"].Value; // yyyyMMddHHmm
            if (DateTime.TryParseExact(dt, "yyyyMMddHHmm", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var when))
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
