// ✅ FULL FILE VERSION
// File: Tests/Common/Bootstrap.cs
// Purpose: Deterministic NLog bootstrap for all tests (NUnit)

using NUnit.Framework;
using NLog;
using NLog.Config;
using NLog.Targets.Wrappers; // ✅ NEW: Wrappers namespace for BufferingTargetWrapper
using System;

namespace VecTool.Tests.Common;

[SetUpFixture]
public sealed class Bootstrap
{
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        NLogTestBootstrap.Init();
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        // Ensure buffered logs are flushed at the end of the test run
        LogManager.Shutdown();
    }
}

internal static class NLogTestBootstrap
{
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized) return;

        try
        {
            var config = TryLoadConfig() ?? CreateDefaultConfig();
            LogManager.Configuration = config;

            var log = LogManager.GetCurrentClassLogger();
            log.Info("NLog initialized for tests");

            _initialized = true;
        }
        catch (Exception ex)
        {
            var evt = new LogEventInfo(LogLevel.Error, nameof(NLogTestBootstrap), "Failed to initialize NLog for tests");
            evt.Exception = ex;
            LogManager.GetLogger(nameof(NLogTestBootstrap)).Log(evt);
        }
    }

    private static LoggingConfiguration? TryLoadConfig()
    {
        // Prefer local NLog.config in test output
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            System.IO.Path.Combine(baseDir, "NLog.config"),
            // Fallback to WinUI app config during local runs if copied/known
            System.IO.Path.Combine(baseDir, "..", "..", "..", "UI", "VecTool.UI.WinUI", "NLog.config"),
        };

        foreach (var path in candidates)
        {
            if (System.IO.File.Exists(path))
            {
                // NLog v5: use LogManager.LogFactory instead of removed bool-overload
                return new XmlLoggingConfiguration(path, LogManager.LogFactory);
            }
        }

        return null;
    }

    private static LoggingConfiguration CreateDefaultConfig()
    {
        var config = new LoggingConfiguration();

        var console = new NLog.Targets.ConsoleTarget("console");

        // Wrap console with buffering for faster CI output
        var buffer = new BufferingTargetWrapper(console)
        {
            BufferSize = 200,
            FlushTimeout = 1000
        };

        // Register targets
        config.AddTarget(console);
        config.AddTarget(buffer);

        // Capture Info+ by default to keep CI logs useful and fast
        config.AddRule(minLevel: LogLevel.Info, maxLevel: LogLevel.Fatal, target: buffer);

        return config;
    }
}
