// UnitTests/TestInfrastructure/TestLogger.cs
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace UnitTests
{
    /// <summary>
    /// Static logger using MS.Extensions.Logging → NLog backend.
    /// Configured once, reused everywhere. 🔥
    /// </summary>
    public static class TestLogger
    {
        private static readonly ILoggerFactory _factory;

        static TestLogger()
        {
            _factory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddNLog(); // ✅ Routes to your existing nlog.config
            });
        }

        public static ILogger<T> For<T>() => _factory.CreateLogger<T>();

        public static ILogger Create(string categoryName = "not-specified")
            => _factory.CreateLogger(categoryName);
    }
}