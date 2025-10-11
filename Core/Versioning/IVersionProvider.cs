using System;
using System.Diagnostics;
using System.Reflection;

namespace VecTool.Core.Versioning
{
    public interface IVersionProvider
    {
        string ApplicationName { get; }
        string AssemblyVersion { get; }
        string FileVersion { get; }
        string InformationalVersion { get; }
        string? CommitShort { get; }
        DateTime? BuildTimestampUtc { get; }
    }
}
