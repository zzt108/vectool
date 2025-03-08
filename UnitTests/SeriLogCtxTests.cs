using FluentAssertions;
using SeriLogShared;

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
            // Setup any required environment configuration for tests
            // Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Debug()
            //    .WriteTo.Console()
            //    .CreateLogger();
        }

        [Test]
        public void ConfigureJson_ShouldReadConfigurationFile()
        {
            // Arrange
            var seriLogCtx = new CtxLogger();

            // Act
            var result = seriLogCtx.ConfigureJson(ConfigPathJson);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void ConfigureXml_ShouldReadConfigurationFile()
        {
            // Arrange
            var seriLogCtx = new CtxLogger();

            // Act
            var result = seriLogCtx.ConfigureXml(ConfigPathXml);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void CanDoStructuredLog_ShouldLogMessagesCorrectly()
        {
            // Arrange
            Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine(msg));
            using var log = new CtxLogger();
            log.ConfigureXml(ConfigPathXml);
            var props = new LogCtxShared.Props("first", log);

            // Act
            log.Ctx.Set(props);
            log.Debug("Debug message");
            log.Fatal(new ArgumentException("Test Fatal Argument Exception", "Param name"), "Fatal message");
            log.Error(new ArgumentException("Test Argument Exception", "Param name"), "Error message");

            // Assert
            // Here you could verify the log output if you had a way to capture it
        }

        [Test]
        public void Dispose_ShouldFlushLogs()
        {
            // Arrange
            using var log = new CtxLogger();
            log.ConfigureJson(ConfigPathJson);

            // Act
            log.Dispose();

            // Assert
            // Check if logs are flushed, could be done by checking log output if possible
        }
    }
}