// File: Constants/VersionInfo.cs

using System.Diagnostics;
using System.Reflection;

namespace VecTool.Constants;

public static class VersionInfo
{
    /// <summary>
    /// Gets the raw file version (e.g., "1.26.0106.0007").
    /// </summary>
    public static string DisplayVersion
    {
        get
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            return fvi.FileVersion ?? "0.0.0.0";
        }
    }

    /// <summary>
    /// Gets the informational version (e.g., "1.26.p4.0106-0007").
    /// </summary>
    public static string InformationalVersion
    {
        get
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            return asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                   ?? Const.NA;
        }
    }

    /// <summary>
    /// Gets formatted version summary (existing method).
    /// </summary>
    public static string GetSummary()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var name = asm.GetName();
        var fvi = FileVersionInfo.GetVersionInfo(asm.Location);

        var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? Const.NA;
        return $"{name.Name} v{fvi.FileVersion} ({informational})";
    }
}