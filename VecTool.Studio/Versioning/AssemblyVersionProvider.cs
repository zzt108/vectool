using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using VecTool.Configuration.Helpers;

namespace VecTool.Studio.Versioning
{
    public sealed class AssemblyVersionProvider : IVersionProvider
    {
        private readonly Assembly _assembly;

        public AssemblyVersionProvider() : this(typeof(AssemblyVersionProvider).Assembly)
        {
        }

        public AssemblyVersionProvider(Assembly assembly)
        {
            _assembly = assembly.ThrowIfNull(nameof(assembly));
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
}