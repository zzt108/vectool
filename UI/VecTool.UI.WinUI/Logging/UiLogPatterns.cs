// Path: src/UI/VecTool.UI.WinUI/Logging/UiLogPatterns.cs

// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

namespace VecTool.UI.WinUI.Logging
{
    internal static class UiLogPatterns
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public static void ReportStartup(string appVersion)
        {
            Log.Info("WinUI shell starting with version {Version}", appVersion);
        }

        public static void ReportWarning(string fileName, string reason)
        {
            var evt = new LogEventInfo(LogLevel.Warning, Log.Name, "Excluded file encountered");
            evt.Properties["FileName"] = fileName;
            evt.Properties["Reason"] = reason;
            Log.Log(evt);
        }
    }
}
