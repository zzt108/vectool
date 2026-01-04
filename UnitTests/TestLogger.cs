#nullable enable

using Microsoft.Extensions.Logging;
using VecTool.Configuration.Logging;

namespace UnitTests
{
    /// <summary>
    /// Test logger facade - delegates to production AppLogger.
    /// Ensures tests use the same logging infrastructure as production code.
    /// </summary>
    public static class TestLogger
    {
        /// <summary>
        /// Creates a logger for the specified type.
        /// Delegates to <see cref="AppLogger.For{T}"/>.
        /// </summary>
        public static ILogger<T> For<T>() => AppLogger.For<T>();

        /// <summary>
        /// Creates a logger with the specified category name.
        /// Delegates to <see cref="AppLogger.Create"/>.
        /// </summary>
        public static ILogger Create(string categoryName = "not-specified")
            => AppLogger.Create(categoryName);

        /// <summary>
        /// Expose factory for DI tests (e.g., MainForm constructor).
        /// Delegates to <see cref="AppLogger.Factory"/>.
        /// </summary>
        public static ILoggerFactory Factory => AppLogger.Factory;
    }
}