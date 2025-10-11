using NLog; // NLog is mandatory for structured logging
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Seq;
using NLog.Targets.Wrappers;
using NUnit.Framework;
using Shouldly;
using System;

namespace VecTool.UI.WinUI.Infrastructure
{
    internal static class NLogBootstrap
    {
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;

            try
            {
                LogManager.Setup().LoadConfigurationFromFile("NLog.config");
            }
            catch
            {
                var cfg = new LoggingConfiguration();

                var console = new ConsoleTarget("console");
                cfg.AddRuleForAllLevels(console);

                var seq = new SeqTarget() { ServerUrl = "http://localhost:5341" };
                var asyncSeq = new AsyncTargetWrapper(seq);
                var buffer = new BufferingTargetWrapper(asyncSeq, 1000, 2000);

                cfg.AddRule(LogLevel.Info, LogLevel.Fatal, buffer);
                LogManager.Configuration = cfg;
            }

            _initialized = true;
        }
    }
}