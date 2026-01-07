using System;

namespace VecTool.Studio.Versioning
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