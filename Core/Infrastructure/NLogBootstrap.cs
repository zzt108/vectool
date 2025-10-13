// ✅ FULL FILE VERSION
// Core/Logging/NLogBootstrap.cs
// Centralized NLog initialization that never throws - falls back to Console target if NLog.config is missing.

using NLog;
using NLog.Config;
using NLog.Targets;
using System;

namespace VecTool.Core.Infrastructure
{
    /// <summary>
    /// Provides safe, centralized NLog initialization with fallback configuration.
    /// This bootstrap ensures logging is always available even if NLog.config is missing or invalid.
    /// </summary>
    public static class NLogBootstrap
    {
        private static bool initialized;
        private static readonly object initLock = new object();

        /// <summary>
        /// Initializes NLog configuration. Safe to call multiple times - only initializes once.
        /// Never throws exceptions; falls back to console logging if configuration cannot be loaded.
        /// </summary>
        public static void Init()
        {
            if (initialized)
                return;

            lock (initLock)
            {
                if (initialized)
                    return;

                try
                {
                    // Attempt to load NLog.config from the application directory
                    var configPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "NLog.config");

                    if (File.Exists(configPath))
                    {
                        LogManager.LoadConfiguration(configPath);
                    }
                    else
                    {
                        // Fallback: create a minimal in-memory configuration
                        CreateFallbackConfiguration();
                    }
                }
                catch (Exception)
                {
                    // If loading fails for any reason, use fallback configuration
                    CreateFallbackConfiguration();
                }

                initialized = true;
            }
        }

        /// <summary>
        /// Creates a minimal fallback configuration that logs to the console.
        /// This ensures logging functionality is always available.
        /// </summary>
        private static void CreateFallbackConfiguration()
        {
            var config = new LoggingConfiguration();

            // Console target for fallback logging
            var consoleTarget = new ConsoleTarget("console")
            {
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
            };

            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget);

            LogManager.Configuration = config;
        }

        /// <summary>
        /// Resets the initialized state. Use only for testing purposes.
        /// </summary>
        internal static void Reset()
        {
            lock (initLock)
            {
                initialized = false;
            }
        }
    }
}
