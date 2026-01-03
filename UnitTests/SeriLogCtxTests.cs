using Shouldly;
using NUnit.Framework;
using System;
using SeriLogShared;
using LogCtxShared;

namespace SeriLogAdapter.Tests
{
    [TestFixture]
    public class SeriLogCtxTests
    {
        private const string ConfigPathJson = "Config/SeriLogConfig.json";
        private const string ConfigPathXml = "Config/SeriLogConfig.xml";

        [SetUp]
        public void Setup()
        {
            // Setup any required environment configuration for tests, e.g., setting up a logger
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.LogDebug()
            //    .WriteTo.Console()
            //    .CreateLogger();
        }

        [Test]
        public void Configure_ShouldReadConfigurationFile()
        {
            // Arrange
            var seriLogCtx = new ILogger();

            // Act
            var result = seriLogCtx.ConfigureJson(ConfigPathJson);

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void CanDoStructuredLog()
        {
            Serilog.Debugging.SelfLog.Enable(msg => Console.LogError.WriteLine(msg));
            // Arrange
            using var log = new ILogger();
            var result = logger.ConfigureXml(ConfigPathXml);

            // Act
            logger.SetContext(new Props("first", result, log));
            logger.LogDebug("LogDebug");
            logger.Fatal(new ArgumentException("Test Fatal Argument Exception", "Param name"), "Fatal");
            logger.LogError(new ArgumentException("Test Argument Exception", "Param name"), "LogError");

            // Assert
            // Log.CloseAndFlush();
        }

        // Additional tests can be written to cover more functionality as needed.
    }
}