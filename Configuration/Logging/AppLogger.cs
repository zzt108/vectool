#nullable enable

using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace VecTool.Configuration.Logging
{
    /// <summary>
    /// Centralized logger factory for VecTool application.
    /// Provides consistent NLog-backed loggers across the codebase.
    /// </summary>
    public static class AppLogger
    {
        private static readonly Lazy<ILoggerFactory> _factory = new(() =>
            LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddNLog();
            })
        );

        /// <summary>
        /// Creates a logger for the specified type.
        /// Thread-safe singleton factory.
        /// </summary>
        public static ILogger<T> For<T>()
        {
            return _factory.Value.CreateLogger<T>();
        }

        /// <summary>
        /// Creates a logger with the specified category name.
        /// </summary>
        public static ILogger Create(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException("Category name cannot be null or empty", nameof(categoryName));

            return _factory.Value.CreateLogger(categoryName);
        }

        /// <summary>
        /// Expose factory for DI containers and tests.
        /// </summary>
        public static ILoggerFactory Factory => _factory.Value;

        /// <summary>
        /// Disposes the internal factory. Use only during app shutdown.
        /// </summary>
        internal static void Shutdown()
        {
            if (_factory.IsValueCreated)
            {
                _factory.Value.Dispose();
            }
        }
    }
}