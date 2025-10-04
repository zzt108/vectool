using System;
using System.Diagnostics;
using System.Reflection;

namespace VecTool.Constants
{
    public static class VersionInfo
    {
        public static string GetSummary()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var name = asm.GetName();
            var fvi = FileVersionInfo.GetVersionInfo(asm.Location);

            var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "n/a";
            return $"{name.Name} v{fvi.FileVersion} ({informational})";
        }
    }
}
